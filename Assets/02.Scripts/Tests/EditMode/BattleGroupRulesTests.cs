using System.Collections.Generic;
using Blacksmith.Core.Battle;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class BattleGroupRulesTests
    {
        // 몹 풀 사거리: 0번 근접, 1번 원거리, 2번 근접
        private static readonly int[] PoolRanges = { 100, 500, 120 };

        [Test]
        public void PickMobs_UsesEachGroupCount()
        {
            IReadOnlyList<int[]> groups = BattleGroupRules.PickMobs(5, new[] { 1, 2, 4 }, PoolRanges);

            Assert.AreEqual(3, groups.Count);
            Assert.AreEqual(1, groups[0].Length);
            Assert.AreEqual(2, groups[1].Length);
            Assert.AreEqual(4, groups[2].Length);
        }

        [Test]
        public void PickMobs_SameSeedGivesSameGroups()
        {
            int[] counts = { 3, 3, 4 };
            IReadOnlyList<int[]> first = BattleGroupRules.PickMobs(42, counts, PoolRanges);
            IReadOnlyList<int[]> second = BattleGroupRules.PickMobs(42, counts, PoolRanges);

            for (int i = 0; i < counts.Length; i++)
            {
                CollectionAssert.AreEqual(first[i], second[i]);
            }
        }

        [Test]
        public void PickMobs_PutsShorterRangeInFront()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                foreach (int[] group in BattleGroupRules.PickMobs(seed, new[] { 5 }, PoolRanges))
                {
                    for (int i = 1; i < group.Length; i++)
                    {
                        Assert.LessOrEqual(PoolRanges[group[i - 1]], PoolRanges[group[i]]);
                    }
                }
            }
        }

        [Test]
        public void PickMobs_EmptyPoolGivesEmptyGroups()
        {
            IReadOnlyList<int[]> groups = BattleGroupRules.PickMobs(1, new[] { 2, 3 }, new int[0]);

            Assert.AreEqual(2, groups.Count);
            Assert.AreEqual(0, groups[0].Length);
        }
    }
}
