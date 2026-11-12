using System;
using System.Collections.Generic;

namespace Blacksmith.Core.Battle
{
    // 스테이지 맵에 몬스터 무리와 보스의 스폰 지점을 전투 시드로 정한다. 같은 맵, 시드, 수치면 같은 배치가 나온다.
    // 1. 보스는 벽에서 떨어진 칸 중 시작 지점에서 걸어서 가장 먼 칸에 둔다.
    // 2. 잡몹 무리는 벽에서 떨어지고 시작 지점에서 멀리 떨어진 칸을 시드로 섞어, 서로(보스 포함) 간격을 지키는 칸부터 고른다.
    // 3. 맵이 좁아 다 놓지 못하면 거리 조건을 반씩 줄여 다시 고른다. 그래서 바닥 칸이 충분하면 무리 수만큼 항상 놓인다.
    public static class StageLayoutGenerator
    {
        private const int SeedSalt = 0x6C1B_9F27;
        private static readonly float[] RelaxSteps = { 1f, 0.5f, 0.25f, 0f };

        public static StageLayout Generate(StageMap map, int seed, int mobGroupCount, bool hasBoss, StageLayoutSettings settings)
        {
            int[,] clearance = GetWallClearance(map);
            int[,] pathDistances = GetPathDistances(map, map.Start);
            var placed = new List<MapPoint>();
            MapPoint boss = default;
            if (hasBoss)
            {
                boss = FindDeepest(map, clearance, pathDistances, settings.WallMargin);
                placed.Add(boss);
            }

            var mobGroups = new List<MapPoint>();
            var random = new Random(seed ^ SeedSalt);
            foreach (float relax in RelaxSteps)
            {
                if (mobGroups.Count >= mobGroupCount)
                {
                    break;
                }

                List<MapPoint> candidates = FindCandidates(map, clearance, map.Start, settings.WallMargin * relax, settings.MinStartDistance * relax);
                Shuffle(candidates, random);
                float spacing = settings.MinGroupSpacing * relax;
                foreach (MapPoint candidate in candidates)
                {
                    if (mobGroups.Count >= mobGroupCount)
                    {
                        break;
                    }

                    if (IsSpaced(candidate, placed, spacing))
                    {
                        placed.Add(candidate);
                        mobGroups.Add(candidate);
                    }
                }
            }

            return new StageLayout(mobGroups, hasBoss, boss);
        }

        // from이 있는 칸에서 상하좌우로 걸어 각 바닥 칸까지 가는 칸 수. 닿지 않는 칸과 바닥이 아닌 칸은 -1이다.
        public static int[,] GetPathDistances(StageMap map, MapPoint from)
        {
            var distances = Filled(map.Width, map.Height, -1);
            int startX = (int)Math.Floor(from.X);
            int startZ = (int)Math.Floor(from.Z);
            if (!map.IsFloor(startX, startZ))
            {
                return distances;
            }

            var queue = new Queue<int>();
            distances[startX, startZ] = 0;
            queue.Enqueue(startX * map.Height + startZ);
            Spread(map, distances, queue);
            return distances;
        }

        // 바닥 칸마다 가장 가까운 바닥 밖 칸(맵 테두리 밖 포함)까지 상하좌우로 걷는 칸 수. 벽에 붙은 칸이 1이다.
        private static int[,] GetWallClearance(StageMap map)
        {
            var clearance = Filled(map.Width, map.Height, -1);
            var queue = new Queue<int>();
            for (int x = 0; x < map.Width; x++)
            {
                for (int z = 0; z < map.Height; z++)
                {
                    if (!map.IsFloor(x, z))
                    {
                        clearance[x, z] = 0;
                        queue.Enqueue(x * map.Height + z);
                    }
                    else if (x == 0 || z == 0 || x == map.Width - 1 || z == map.Height - 1)
                    {
                        clearance[x, z] = 1;
                        queue.Enqueue(x * map.Height + z);
                    }
                }
            }

            Spread(map, clearance, queue);
            return clearance;
        }

        // 큐에 든 칸에서 바닥 칸으로 한 걸음씩 퍼지며 아직 값이 없는(-1) 칸에 거리를 적는다.
        private static void Spread(StageMap map, int[,] distances, Queue<int> queue)
        {
            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int x = index / map.Height;
                int z = index % map.Height;
                int next = distances[x, z] + 1;
                Visit(x + 1, z);
                Visit(x - 1, z);
                Visit(x, z + 1);
                Visit(x, z - 1);

                void Visit(int vx, int vz)
                {
                    if (map.IsFloor(vx, vz) && distances[vx, vz] < 0)
                    {
                        distances[vx, vz] = next;
                        queue.Enqueue(vx * map.Height + vz);
                    }
                }
            }
        }

        // 벽 여유를 지키는 칸 중 가장 먼 칸. 그런 칸이 없으면 벽 여유가 가장 큰 칸들 중에서 고른다.
        private static MapPoint FindDeepest(StageMap map, int[,] clearance, int[,] pathDistances, int wallMargin)
        {
            int maxClearance = 0;
            foreach (int value in clearance)
            {
                maxClearance = Math.Max(maxClearance, value);
            }

            int margin = Math.Min(wallMargin, maxClearance);
            int best = -1;
            var result = map.Start;
            for (int x = 0; x < map.Width; x++)
            {
                for (int z = 0; z < map.Height; z++)
                {
                    if (clearance[x, z] >= margin && pathDistances[x, z] > best)
                    {
                        best = pathDistances[x, z];
                        result = CellCenter(x, z);
                    }
                }
            }

            return result;
        }

        private static List<MapPoint> FindCandidates(StageMap map, int[,] clearance, MapPoint start, float wallMargin, float minStartDistance)
        {
            var candidates = new List<MapPoint>();
            float minSquared = minStartDistance * minStartDistance;
            for (int x = 0; x < map.Width; x++)
            {
                for (int z = 0; z < map.Height; z++)
                {
                    MapPoint cell = CellCenter(x, z);
                    if (map.IsFloor(x, z) && clearance[x, z] >= wallMargin && SquaredDistance(cell, start) >= minSquared)
                    {
                        candidates.Add(cell);
                    }
                }
            }

            return candidates;
        }

        private static bool IsSpaced(MapPoint candidate, List<MapPoint> placed, float spacing)
        {
            foreach (MapPoint point in placed)
            {
                bool isSameCell = point.X == candidate.X && point.Z == candidate.Z;
                if (isSameCell || SquaredDistance(point, candidate) < spacing * spacing)
                {
                    return false;
                }
            }

            return true;
        }

        private static void Shuffle(List<MapPoint> points, Random random)
        {
            for (int i = points.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (points[i], points[j]) = (points[j], points[i]);
            }
        }

        private static MapPoint CellCenter(int x, int z)
        {
            return new MapPoint(x + 0.5f, z + 0.5f);
        }

        private static float SquaredDistance(MapPoint a, MapPoint b)
        {
            float dx = a.X - b.X;
            float dz = a.Z - b.Z;
            return dx * dx + dz * dz;
        }

        private static int[,] Filled(int width, int height, int value)
        {
            var grid = new int[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    grid[x, z] = value;
                }
            }

            return grid;
        }
    }
}
