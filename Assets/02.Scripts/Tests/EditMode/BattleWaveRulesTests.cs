using System.Collections.Generic;
using Blacksmith.Core.Battle;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class BattleWaveRulesTests
    {
        // 몹 풀 사거리: 0번 근접, 1번 원거리, 2번 근접
        private static readonly int[] PoolRanges = { 100, 500, 120 };

        [Test]
        public void PickMobs_UsesEachWaveCount()
        {
            IReadOnlyList<int[]> waves = BattleWaveRules.PickMobs(5, new[] { 1, 2, 4 }, PoolRanges);

            Assert.AreEqual(3, waves.Count);
            Assert.AreEqual(1, waves[0].Length);
            Assert.AreEqual(2, waves[1].Length);
            Assert.AreEqual(4, waves[2].Length);
        }

        [Test]
        public void PickMobs_SameSeedGivesSameWaves()
        {
            int[] counts = { 3, 3, 4 };
            IReadOnlyList<int[]> first = BattleWaveRules.PickMobs(42, counts, PoolRanges);
            IReadOnlyList<int[]> second = BattleWaveRules.PickMobs(42, counts, PoolRanges);

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
                foreach (int[] wave in BattleWaveRules.PickMobs(seed, new[] { 5 }, PoolRanges))
                {
                    for (int i = 1; i < wave.Length; i++)
                    {
                        Assert.LessOrEqual(PoolRanges[wave[i - 1]], PoolRanges[wave[i]]);
                    }
                }
            }
        }

        [Test]
        public void PickMobs_EmptyPoolGivesEmptyWaves()
        {
            IReadOnlyList<int[]> waves = BattleWaveRules.PickMobs(1, new[] { 2, 3 }, new int[0]);

            Assert.AreEqual(2, waves.Count);
            Assert.AreEqual(0, waves[0].Length);
        }
    }
}
