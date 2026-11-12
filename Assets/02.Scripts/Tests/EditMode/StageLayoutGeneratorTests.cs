using System;
using Blacksmith.Core.Battle;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class StageLayoutGeneratorTests
    {
        private const int MobGroups = 4;

        // StageMapGeneratorTests와 같은 절반 크기 맵
        private static StageMapSettings MapSettings()
        {
            return new StageMapSettings
            {
                MinSize = 90,
                MaxSize = 120,
                MinLobes = 4,
                MaxLobes = 7,
                MinLobeRadius = 16,
                MaxLobeRadius = 28,
                LobeSpacingPercent = 60,
                WarpAmplitude = 8f,
                WarpScale = 45f,
                EdgeAmplitude = 3f,
                EdgeScale = 9f,
                MinWidth = 12,
                MinKeptPercent = 70,
            };
        }

        private static StageLayoutSettings LayoutSettings()
        {
            return new StageLayoutSettings { MinStartDistance = 20, MinGroupSpacing = 16, WallMargin = 2 };
        }

        [Test]
        public void ManySeeds_PlaceAllGroupsAwayFromStartWallsAndEachOther()
        {
            StageLayoutSettings settings = LayoutSettings();
            for (int seed = 0; seed < 100; seed++)
            {
                StageMap map = StageMapGenerator.Generate(seed, MapSettings());
                StageLayout layout = StageLayoutGenerator.Generate(map, seed, MobGroups, true, settings);
                Assert.AreEqual(MobGroups, layout.MobGroups.Count, $"시드 {seed}");
                for (int i = 0; i < layout.MobGroups.Count; i++)
                {
                    MapPoint group = layout.MobGroups[i];
                    Assert.IsTrue(HasWallMargin(map, group, settings.WallMargin), $"시드 {seed} 무리 {i}: 벽에 붙음");
                    Assert.GreaterOrEqual(Distance(group, map.Start), settings.MinStartDistance, $"시드 {seed} 무리 {i}: 시작 지점과 가까움");
                    Assert.GreaterOrEqual(Distance(group, layout.Boss), settings.MinGroupSpacing, $"시드 {seed} 무리 {i}: 보스와 가까움");
                    for (int j = i + 1; j < layout.MobGroups.Count; j++)
                    {
                        Assert.GreaterOrEqual(Distance(group, layout.MobGroups[j]), settings.MinGroupSpacing, $"시드 {seed} 무리 {i}·{j}: 서로 가까움");
                    }
                }
            }
        }

        [Test]
        public void Boss_IsFarthestWalkFromStart()
        {
            StageLayoutSettings settings = LayoutSettings();
            for (int seed = 0; seed < 50; seed++)
            {
                StageMap map = StageMapGenerator.Generate(seed, MapSettings());
                StageLayout layout = StageLayoutGenerator.Generate(map, seed, MobGroups, true, settings);
                int[,] distances = StageLayoutGenerator.GetPathDistances(map, map.Start);
                int bossDistance = WalkDistance(distances, layout.Boss);
                Assert.IsTrue(HasWallMargin(map, layout.Boss, settings.WallMargin), $"시드 {seed}: 보스가 벽에 붙음");
                foreach (MapPoint group in layout.MobGroups)
                {
                    Assert.GreaterOrEqual(bossDistance, WalkDistance(distances, group), $"시드 {seed}");
                }
            }
        }

        [Test]
        public void SameSeed_MakesSameLayout_DifferentSeedsDiffer()
        {
            StageMap map = StageMapGenerator.Generate(77, MapSettings());
            string first = Describe(StageLayoutGenerator.Generate(map, 77, MobGroups, true, LayoutSettings()));
            Assert.AreEqual(first, Describe(StageLayoutGenerator.Generate(map, 77, MobGroups, true, LayoutSettings())));
            Assert.AreNotEqual(first, Describe(StageLayoutGenerator.Generate(map, 78, MobGroups, true, LayoutSettings())));
        }

        [Test]
        public void NoBossStage_HasNoBoss()
        {
            StageMap map = StageMapGenerator.Generate(5, MapSettings());
            StageLayout layout = StageLayoutGenerator.Generate(map, 5, 2, false, LayoutSettings());
            Assert.IsFalse(layout.HasBoss);
            Assert.AreEqual(2, layout.MobGroups.Count);
        }

        [Test]
        public void SmallMap_RelaxesDistancesButPlacesEveryGroup()
        {
            var floor = new bool[8, 8];
            for (int x = 1; x <= 6; x++)
            {
                for (int z = 1; z <= 6; z++)
                {
                    floor[x, z] = true;
                }
            }

            var map = new StageMap(floor, new MapPoint(1.5f, 1.5f));
            StageLayout layout = StageLayoutGenerator.Generate(map, 3, 3, true, new StageLayoutSettings { MinStartDistance = 40, MinGroupSpacing = 30, WallMargin = 4 });
            Assert.AreEqual(3, layout.MobGroups.Count);
            // 벽 여유 4칸을 지킬 곳이 없어 가장 큰 여유(3칸)를 지키는 가운데 2×2 중 가장 먼 칸에 보스가 선다.
            Assert.AreEqual(4.5f, layout.Boss.X);
            Assert.AreEqual(4.5f, layout.Boss.Z);
            foreach (MapPoint group in layout.MobGroups)
            {
                Assert.IsTrue(map.IsFloor((int)group.X, (int)group.Z));
            }
        }

        [Test]
        public void PathDistances_GoAroundWalls()
        {
            // U자 바닥: (0,0)에서 (2,0)으로 가려면 위로 돌아가야 한다.
            var floor = new bool[3, 3];
            floor[0, 0] = floor[0, 1] = floor[0, 2] = true;
            floor[1, 2] = true;
            floor[2, 0] = floor[2, 1] = floor[2, 2] = true;
            int[,] distances = StageLayoutGenerator.GetPathDistances(new StageMap(floor, new MapPoint(0.5f, 0.5f)), new MapPoint(0.5f, 0.5f));
            Assert.AreEqual(6, distances[2, 0]);
            Assert.AreEqual(-1, distances[1, 0]);
        }

        // 벽 여유 n은 상하좌우로 n - 1 걸음 안의 칸이 모두 바닥이라는 뜻이다.
        private static bool HasWallMargin(StageMap map, MapPoint point, int margin)
        {
            int cx = (int)point.X;
            int cz = (int)point.Z;
            for (int dx = -(margin - 1); dx <= margin - 1; dx++)
            {
                for (int dz = -(margin - 1); dz <= margin - 1; dz++)
                {
                    if (Math.Abs(dx) + Math.Abs(dz) <= margin - 1 && !map.IsFloor(cx + dx, cz + dz))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static int WalkDistance(int[,] distances, MapPoint point)
        {
            return distances[(int)point.X, (int)point.Z];
        }

        private static float Distance(MapPoint a, MapPoint b)
        {
            float dx = a.X - b.X;
            float dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        private static string Describe(StageLayout layout)
        {
            var text = new System.Text.StringBuilder();
            foreach (MapPoint group in layout.MobGroups)
            {
                text.Append($"{group.X},{group.Z};");
            }

            return text.Append($"boss {layout.Boss.X},{layout.Boss.Z}").ToString();
        }
    }
}
