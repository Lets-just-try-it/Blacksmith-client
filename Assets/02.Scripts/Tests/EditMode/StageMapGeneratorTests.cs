using System.Collections.Generic;
using Blacksmith.Core.Battle;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class StageMapGeneratorTests
    {
        private static StageMapSettings Settings()
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

        [Test]
        public void ManySeeds_AreOneRegionWithoutHolesOrNarrowNecks()
        {
            StageMapSettings settings = Settings();
            for (int seed = 0; seed < 200; seed++)
            {
                StageMapReport report = StageMapValidator.Check(StageMapGenerator.Generate(seed, settings), settings.MinWidth);
                Assert.IsTrue(report.IsValid,
                    $"시드 {seed}: 바닥 {report.FloorCells}, 이어진 칸 {report.ReachableCells}, 구멍 {report.HoleCells}, 대각선 {report.DiagonalContacts}, 좁은 칸 {report.NarrowCells}");
            }
        }

        [Test]
        public void SameSeed_MakesSameMap()
        {
            Assert.AreEqual(Describe(StageMapGenerator.Generate(1234, Settings())), Describe(StageMapGenerator.Generate(1234, Settings())));
        }

        [Test]
        public void DifferentSeeds_MakeDifferentMaps()
        {
            var shapes = new HashSet<string>();
            for (int seed = 0; seed < 20; seed++)
            {
                shapes.Add(Describe(StageMapGenerator.Generate(seed, Settings())));
            }

            Assert.AreEqual(20, shapes.Count);
        }

        [Test]
        public void Map_SpansAtLeastMostOfMinSize()
        {
            StageMapSettings settings = Settings();
            for (int seed = 0; seed < 50; seed++)
            {
                StageMap map = StageMapGenerator.Generate(seed, settings);
                GetFloorSpan(map, out int spanX, out int spanZ);
                Assert.GreaterOrEqual(System.Math.Max(spanX, spanZ), settings.MinSize * 0.8f, $"시드 {seed}");
            }
        }

        [Test]
        public void Start_IsOnFloor()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                StageMap map = StageMapGenerator.Generate(seed, Settings());
                Assert.IsTrue(map.IsFloor((int)map.Start.X, (int)map.Start.Z), $"시드 {seed}");
            }
        }

        [Test]
        public void Validator_FindsHoleSplitAndNarrowCells()
        {
            var floor = new bool[9, 9];
            for (int x = 1; x <= 7; x++)
            {
                for (int z = 1; z <= 7; z++)
                {
                    floor[x, z] = true;
                }
            }

            floor[4, 4] = false;
            StageMapReport withHole = StageMapValidator.Check(new StageMap(floor, new MapPoint(1.5f, 1.5f)), 3);
            Assert.AreEqual(1, withHole.HoleCells);
            Assert.IsFalse(withHole.IsValid);

            var split = new bool[9, 9];
            split[1, 1] = true;
            split[6, 6] = true;
            StageMapReport splitReport = StageMapValidator.Check(new StageMap(split, new MapPoint(1.5f, 1.5f)), 1);
            Assert.AreEqual(1, splitReport.ReachableCells);
            Assert.IsFalse(splitReport.IsValid);

            // 3×3 덩어리 옆에 한 칸짜리 돌기
            var narrow = new bool[9, 9];
            for (int x = 1; x <= 3; x++)
            {
                for (int z = 1; z <= 3; z++)
                {
                    narrow[x, z] = true;
                }
            }

            narrow[4, 2] = true;
            StageMapReport narrowReport = StageMapValidator.Check(new StageMap(narrow, new MapPoint(1.5f, 1.5f)), 3);
            Assert.AreEqual(1, narrowReport.NarrowCells);
            Assert.IsFalse(narrowReport.IsValid);
        }

        [Test]
        public void Validator_FindsDiagonalOnlyContact()
        {
            var floor = new bool[4, 4];
            floor[1, 1] = true;
            floor[2, 2] = true;
            StageMapReport report = StageMapValidator.Check(new StageMap(floor, new MapPoint(1.5f, 1.5f)), 1);
            Assert.AreEqual(1, report.DiagonalContacts);
            Assert.IsFalse(report.IsValid);
        }

        private static void GetFloorSpan(StageMap map, out int spanX, out int spanZ)
        {
            int minX = int.MaxValue, minZ = int.MaxValue, maxX = -1, maxZ = -1;
            for (int x = 0; x < map.Width; x++)
            {
                for (int z = 0; z < map.Height; z++)
                {
                    if (map.IsFloor(x, z))
                    {
                        minX = System.Math.Min(minX, x);
                        minZ = System.Math.Min(minZ, z);
                        maxX = System.Math.Max(maxX, x);
                        maxZ = System.Math.Max(maxZ, z);
                    }
                }
            }

            spanX = maxX - minX + 1;
            spanZ = maxZ - minZ + 1;
        }

        private static string Describe(StageMap map)
        {
            var chars = new char[map.Width * map.Height];
            for (int x = 0; x < map.Width; x++)
            {
                for (int z = 0; z < map.Height; z++)
                {
                    chars[x * map.Height + z] = map.IsFloor(x, z) ? '#' : '.';
                }
            }

            return $"{map.Width}x{map.Height}:{new string(chars)}";
        }
    }
}
