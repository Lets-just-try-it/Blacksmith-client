using System;
using System.Collections.Generic;
using Blacksmith.Core.Battle;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class GatheringLayoutRulesTests
    {
        private const int MaxNodes = 64;

        // 40×40칸이 모두 바닥이고 가운데에서 시작하는 맵
        private static StageMap OpenMap()
        {
            var floor = new bool[40, 40];
            for (int x = 0; x < 40; x++)
            {
                for (int z = 0; z < 40; z++)
                {
                    floor[x, z] = true;
                }
            }

            return new StageMap(floor, new MapPoint(20.5f, 20.5f));
        }

        private static GatheringLayoutSettings Settings()
        {
            return new GatheringLayoutSettings
            {
                OreVeins = 3,
                Trees = 2,
                LooseOre = 2,
                LooseWood = 2,
                MinSpacing = 3f,
                StartClearance = 5f,
                WallMargin = 1,
            };
        }

        private static GatheringYieldSettings Yields()
        {
            return new GatheringYieldSettings
            {
                OreItemId = 13,
                WoodItemId = 54,
                OreVeinMin = 3,
                OreVeinMax = 5,
                TreeMin = 2,
                TreeMax = 4,
                LooseOreMin = 1,
                LooseOreMax = 1,
                LooseWoodMin = 1,
                LooseWoodMax = 2,
            };
        }

        private static List<BattleCorpse> Corpses()
        {
            return new List<BattleCorpse>
            {
                new BattleCorpse(1, 1, 0, 1000, 1000),
                new BattleCorpse(1, 0, 0, 3000, 500),
                new BattleCorpse(1, 1, 1, 1200, 1400),
            };
        }

        [Test]
        public void CorpsePiles_OnePerGroupInGroupOrderAtCenter()
        {
            List<GatheringNode> nodes = GatheringLayoutRules.Generate(OpenMap(), Corpses(), 1, Settings(), MaxNodes);

            Assert.AreEqual(GatheringNodeKind.CorpsePile, nodes[0].Kind);
            Assert.AreEqual(0, nodes[0].GroupIndex);
            Assert.AreEqual(GatheringNodeKind.CorpsePile, nodes[1].Kind);
            Assert.AreEqual(1, nodes[1].GroupIndex);
            Assert.AreEqual(1100, nodes[1].X);
            Assert.AreEqual(1200, nodes[1].Z);
            Assert.AreNotEqual(GatheringNodeKind.CorpsePile, nodes[2].Kind);
        }

        [Test]
        public void SameSeed_GivesSameLayout()
        {
            List<GatheringNode> first = GatheringLayoutRules.Generate(OpenMap(), Corpses(), 42, Settings(), MaxNodes);
            List<GatheringNode> second = GatheringLayoutRules.Generate(OpenMap(), Corpses(), 42, Settings(), MaxNodes);

            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void ManySeeds_PlaceAllCountsAwayFromStartWallsAndEachOther()
        {
            GatheringLayoutSettings settings = Settings();
            StageMap map = OpenMap();
            for (int seed = 0; seed < 50; seed++)
            {
                List<GatheringNode> nodes = GatheringLayoutRules.Generate(map, Corpses(), seed, settings, MaxNodes);
                Assert.AreEqual(2 + 3 + 2 + 2 + 2, nodes.Count, $"시드 {seed}");
                for (int i = 2; i < nodes.Count; i++)
                {
                    float x = nodes[i].X / (float)BattleSpace.UnitsPerCell;
                    float z = nodes[i].Z / (float)BattleSpace.UnitsPerCell;
                    Assert.GreaterOrEqual(Distance(x, z, map.Start.X, map.Start.Z), settings.StartClearance, $"시드 {seed} {i}");
                    Assert.IsTrue(x > 1f && z > 1f && x < 39f && z < 39f, $"시드 {seed} {i}: 벽에 붙음");
                    for (int j = 0; j < i; j++)
                    {
                        float distance = Distance(nodes[i].X, nodes[i].Z, nodes[j].X, nodes[j].Z) / BattleSpace.UnitsPerCell;
                        Assert.GreaterOrEqual(distance, settings.MinSpacing - 0.01f, $"시드 {seed} {i}-{j}");
                    }
                }
            }
        }

        [Test]
        public void TooSmallMap_PlacesFewerWithoutFailing()
        {
            var floor = new bool[5, 5];
            for (int x = 0; x < 5; x++)
            {
                for (int z = 0; z < 5; z++)
                {
                    floor[x, z] = true;
                }
            }

            var map = new StageMap(floor, new MapPoint(2.5f, 2.5f));
            List<GatheringNode> nodes = GatheringLayoutRules.Generate(map, new List<BattleCorpse>(), 3, Settings(), MaxNodes);

            Assert.Less(nodes.Count, 9);
        }

        [Test]
        public void OreVein_GivesOreInRangeAndSameForSameNode()
        {
            var node = new GatheringNode(GatheringNodeKind.OreVein, 0, 0);
            for (int index = 0; index < 20; index++)
            {
                List<MaterialReward> first = GatheringYieldRules.Roll(node, index, 9, Yields(), Array.Empty<GroupLoot>());
                List<MaterialReward> second = GatheringYieldRules.Roll(node, index, 9, Yields(), Array.Empty<GroupLoot>());

                Assert.AreEqual(1, first.Count);
                Assert.AreEqual(13, first[0].MaterialId);
                Assert.That(first[0].Count, Is.InRange(3, 5));
                CollectionAssert.AreEqual(first, second);
            }
        }

        [Test]
        public void CorpsePile_SumsTheGroupsLoot()
        {
            var monster = new MonsterReward
            {
                ChanceDrops = { new ChanceDrop { Rolls = 2, ChancePercent = 100, MaterialIds = new[] { 15 } } },
                GemDrops = { new ChanceDrop { Rolls = 1, ChancePercent = 100, MaterialIds = new[] { 18 } } },
            };
            var groups = new List<GroupReward> { new GroupReward(new[] { monster }), new GroupReward(new[] { monster, monster }) };
            IReadOnlyList<GroupLoot> loots = BattleDropRules.Roll(groups, 4);

            List<MaterialReward> pile = GatheringYieldRules.Roll(new GatheringNode(GatheringNodeKind.CorpsePile, 0, 0, 1), 1, 4,
                Yields(), loots);

            Assert.AreEqual(4, pile.Find(material => material.MaterialId == 15).Count);
            Assert.AreEqual(2, pile.Find(material => material.MaterialId == 18).Count);
        }

        private static float Distance(float ax, float az, float bx, float bz)
        {
            float dx = ax - bx;
            float dz = az - bz;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }
    }
}
