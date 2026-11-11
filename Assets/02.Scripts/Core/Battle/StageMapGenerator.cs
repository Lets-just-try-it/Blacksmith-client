using System;
using System.Collections.Generic;

namespace Blacksmith.Core.Battle
{
    // 스테이지 맵을 전투 시드로 만든다 (docs/GAME_CONCEPT.md 7.2 "맵 생성"). 같은 시드와 수치면 같은 맵이 나온다.
    // 1. 타원 덩어리를 하나 놓고, 이미 놓인 덩어리 옆에 크게 겹치게 새 덩어리를 붙여 넓은 한 덩어리 틀을 만든다.
    //    덩어리끼리 많이 겹쳐 이음목이 넓고, 둘레에는 들쭉날쭉한 만과 곁공간이 생긴다.
    // 2. 틀을 노이즈로 크게 휘고 가장자리를 울퉁불퉁하게 깎아 바닥 칸을 정한다.
    // 3. 가장 큰 덩어리만 남기고, MinWidth보다 좁은 목과 돌기를 지운 뒤 다시 가장 큰 덩어리만 남긴다. 그래서 맵은 항상 한 덩어리다.
    // 4. 안쪽 구멍과 대각선으로만 닿은 곳을 메운다. 검증을 통과하지 못하면 다른 시드로 다시 만든다.
    public static class StageMapGenerator
    {
        private const int SeedSalt = 0x51A6_E0D3;
        private const int MaxAttempts = 8;
        private const int MaxFillPasses = 16;
        private const int MaxLobeTries = 200;

        private readonly struct Ellipse
        {
            public readonly float CenterX;
            public readonly float CenterZ;
            public readonly float RadiusX;
            public readonly float RadiusZ;

            public Ellipse(float centerX, float centerZ, float radiusX, float radiusZ)
            {
                CenterX = centerX;
                CenterZ = centerZ;
                RadiusX = radiusX;
                RadiusZ = radiusZ;
            }
        }

        public static StageMap Generate(int seed, StageMapSettings settings)
        {
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var random = new Random(unchecked((seed ^ SeedSalt) + attempt * 7919));
                List<Ellipse> lobes = CreateLobes(random, settings);
                StageMap map = Build(lobes, settings, random.Next(), true, out int lobeCells);
                bool isLargeEnough = map.CountFloor() * 100L >= (long)lobeCells * settings.MinKeptPercent;
                if (isLargeEnough && StageMapValidator.Check(map, settings.MinWidth).IsValid)
                {
                    return map;
                }
            }

            // 변형한 맵이 모두 검증을 통과하지 못하면 변형하지 않은 덩어리 틀을 그대로 쓴다.
            return Build(CreateLobes(new Random(seed ^ SeedSalt), settings), settings, 0, false, out _);
        }

        // 덩어리 수가 정한 수에 이르고 맵 크기가 MinSize에 닿을 때까지 붙인다. MaxSize를 넘게 하는 덩어리는 붙이지 않는다.
        private static List<Ellipse> CreateLobes(Random random, StageMapSettings settings)
        {
            var lobes = new List<Ellipse> { CreateLobe(random, settings, 0f, 0f) };
            int targetCount = random.Next(settings.MinLobes, Math.Max(settings.MinLobes, settings.MaxLobes) + 1);
            int maxCount = Math.Max(targetCount, settings.MaxLobes) * 2;
            for (int tries = 0; tries < MaxLobeTries && lobes.Count < maxCount; tries++)
            {
                if (lobes.Count >= targetCount && GetExtent(lobes) >= settings.MinSize)
                {
                    break;
                }

                Ellipse parent = lobes[random.Next(lobes.Count)];
                double angle = random.NextDouble() * Math.PI * 2;
                Ellipse lobe = CreateLobe(random, settings, 0f, 0f);
                float parentRadius = (parent.RadiusX + parent.RadiusZ) * 0.5f;
                float lobeRadius = (lobe.RadiusX + lobe.RadiusZ) * 0.5f;
                float distance = (parentRadius + lobeRadius) * settings.LobeSpacingPercent / 100f;
                var placed = new Ellipse(parent.CenterX + (float)Math.Cos(angle) * distance, parent.CenterZ + (float)Math.Sin(angle) * distance,
                    lobe.RadiusX, lobe.RadiusZ);
                lobes.Add(placed);
                if (GetExtent(lobes) > settings.MaxSize)
                {
                    lobes.RemoveAt(lobes.Count - 1);
                }
            }

            return lobes;
        }

        private static Ellipse CreateLobe(Random random, StageMapSettings settings, float centerX, float centerZ)
        {
            float radius = random.Next(settings.MinLobeRadius, Math.Max(settings.MinLobeRadius, settings.MaxLobeRadius) + 1);
            float other = radius * Range(random, 0.75f, 1f);
            return random.Next(2) == 0 ? new Ellipse(centerX, centerZ, radius, other) : new Ellipse(centerX, centerZ, other, radius);
        }

        private static float Range(Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        private static float GetExtent(List<Ellipse> lobes)
        {
            GetBounds(lobes, out float minX, out float minZ, out float maxX, out float maxZ);
            return Math.Max(maxX - minX, maxZ - minZ);
        }

        private static void GetBounds(List<Ellipse> lobes, out float minX, out float minZ, out float maxX, out float maxZ)
        {
            minX = minZ = float.MaxValue;
            maxX = maxZ = float.MinValue;
            foreach (Ellipse lobe in lobes)
            {
                minX = Math.Min(minX, lobe.CenterX - lobe.RadiusX);
                minZ = Math.Min(minZ, lobe.CenterZ - lobe.RadiusZ);
                maxX = Math.Max(maxX, lobe.CenterX + lobe.RadiusX);
                maxZ = Math.Max(maxZ, lobe.CenterZ + lobe.RadiusZ);
            }
        }

        private static StageMap Build(List<Ellipse> lobes, StageMapSettings settings, int noiseSeed, bool deform, out int lobeCells)
        {
            // 변형이 틀 밖으로 밀어낸 바닥과 정리용 여백이 들어갈 만큼 둘레를 비워 둔다.
            int margin = (int)Math.Ceiling(settings.WarpAmplitude + settings.EdgeAmplitude) + settings.MinWidth + 2;
            GetBounds(lobes, out float minX, out float minZ, out float maxX, out float maxZ);
            float offsetX = margin - minX;
            float offsetZ = margin - minZ;
            var floor = new bool[(int)Math.Ceiling(maxX - minX) + margin * 2, (int)Math.Ceiling(maxZ - minZ) + margin * 2];

            var warpX = new ValueNoise(noiseSeed);
            var warpZ = new ValueNoise(unchecked(noiseSeed * 31 + 7));
            var edge = new ValueNoise(unchecked(noiseSeed * 131 + 3));
            float warpScale = Math.Max(1f, settings.WarpScale);
            float edgeScale = Math.Max(1f, settings.EdgeScale);
            lobeCells = 0;
            for (int x = 0; x < floor.GetLength(0); x++)
            {
                for (int z = 0; z < floor.GetLength(1); z++)
                {
                    float px = x + 0.5f;
                    float pz = z + 0.5f;
                    float plain = GetDistance(lobes, px - offsetX, pz - offsetZ);
                    lobeCells += plain <= 0f ? 1 : 0;
                    if (!deform)
                    {
                        floor[x, z] = plain <= 0f;
                        continue;
                    }

                    float qx = px + settings.WarpAmplitude * warpX.Sample(px / warpScale, pz / warpScale);
                    float qz = pz + settings.WarpAmplitude * warpZ.Sample(px / warpScale, pz / warpScale);
                    float distance = GetDistance(lobes, qx - offsetX, qz - offsetZ) + settings.EdgeAmplitude * edge.Fractal(px / edgeScale, pz / edgeScale);
                    floor[x, z] = distance <= 0f;
                }
            }

            CleanUp(floor, settings.MinWidth);
            return new StageMap(floor, FindNearestFloor(floor, new MapPoint(lobes[0].CenterX + offsetX, lobes[0].CenterZ + offsetZ)));
        }

        // 덩어리 경계까지의 거리. 안쪽은 음수다.
        private static float GetDistance(List<Ellipse> lobes, float x, float z)
        {
            float distance = float.MaxValue;
            foreach (Ellipse lobe in lobes)
            {
                float nx = (x - lobe.CenterX) / lobe.RadiusX;
                float nz = (z - lobe.CenterZ) / lobe.RadiusZ;
                distance = Math.Min(distance, ((float)Math.Sqrt(nx * nx + nz * nz) - 1f) * Math.Min(lobe.RadiusX, lobe.RadiusZ));
            }

            return distance;
        }

        private static float Square(float value)
        {
            return value * value;
        }

        private static void CleanUp(bool[,] floor, int minWidth)
        {
            KeepLargestRegion(floor);
            bool[,] covered = StageMapValidator.FindCovered(floor, minWidth);
            Array.Copy(covered, floor, floor.Length);
            KeepLargestRegion(floor);
            for (int pass = 0; pass < MaxFillPasses; pass++)
            {
                if (!(FillDiagonalContacts(floor, minWidth) | FillHoles(floor)))
                {
                    return;
                }
            }
        }

        private static void KeepLargestRegion(bool[,] floor)
        {
            int[,] labels = StageMapValidator.LabelRegions(floor, out List<int> sizes);
            int largest = -1;
            for (int i = 0; i < sizes.Count; i++)
            {
                if (largest < 0 || sizes[i] > sizes[largest])
                {
                    largest = i;
                }
            }

            for (int x = 0; x < floor.GetLength(0); x++)
            {
                for (int z = 0; z < floor.GetLength(1); z++)
                {
                    floor[x, z] = floor[x, z] && labels[x, z] == largest;
                }
            }
        }

        // 대각선으로만 닿은 곳에 MinWidth 정사각형을 깔아 넓게 잇는다. 두 칸만 메우면 좁은 목이 생긴다.
        private static bool FillDiagonalContacts(bool[,] floor, int minWidth)
        {
            bool changed = false;
            int width = floor.GetLength(0);
            int height = floor.GetLength(1);
            for (int x = 0; x < width - 1; x++)
            {
                for (int z = 0; z < height - 1; z++)
                {
                    if (!StageMapValidator.IsDiagonalContact(floor, x, z))
                    {
                        continue;
                    }

                    int x0 = Math.Max(0, Math.Min(width - minWidth, x + 1 - minWidth / 2));
                    int z0 = Math.Max(0, Math.Min(height - minWidth, z + 1 - minWidth / 2));
                    for (int fx = x0; fx < Math.Min(width, x0 + minWidth); fx++)
                    {
                        for (int fz = z0; fz < Math.Min(height, z0 + minWidth); fz++)
                        {
                            floor[fx, fz] = true;
                        }
                    }

                    changed = true;
                }
            }

            return changed;
        }

        private static bool FillHoles(bool[,] floor)
        {
            bool[,] outside = StageMapValidator.FindOutside(floor);
            bool changed = false;
            for (int x = 0; x < floor.GetLength(0); x++)
            {
                for (int z = 0; z < floor.GetLength(1); z++)
                {
                    if (!floor[x, z] && !outside[x, z])
                    {
                        floor[x, z] = true;
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private static MapPoint FindNearestFloor(bool[,] floor, MapPoint anchor)
        {
            float best = float.MaxValue;
            var result = new MapPoint(anchor.X, anchor.Z);
            for (int x = 0; x < floor.GetLength(0); x++)
            {
                for (int z = 0; z < floor.GetLength(1); z++)
                {
                    float distance = Square(x + 0.5f - anchor.X) + Square(z + 0.5f - anchor.Z);
                    if (floor[x, z] && distance < best)
                    {
                        best = distance;
                        result = new MapPoint(x + 0.5f, z + 0.5f);
                    }
                }
            }

            return result;
        }
    }
}
