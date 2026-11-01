using Blacksmith.Core.Adventurers;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class AdventurerStatRulesTests
    {
        private static AdventurerStats Stats(int maxHp, int attack, int defense)
        {
            return new AdventurerStats { MaxHp = maxHp, Attack = attack, Defense = defense };
        }

        [Test]
        public void GetStatsAtLevel_AtLevelOne_ReturnsBaseStats()
        {
            Assert.AreEqual(Stats(50, 40, 30), AdventurerStatRules.GetStatsAtLevel(Stats(50, 40, 30), Stats(3, 2, 1), 1));
        }

        [Test]
        public void GetStatsAtLevel_AddsGrowthPerLevelGained()
        {
            Assert.AreEqual(Stats(77, 58, 39), AdventurerStatRules.GetStatsAtLevel(Stats(50, 40, 30), Stats(3, 2, 1), 10));
        }

        [Test]
        public void GetStatsAtLevel_BelowMinLevel_ReturnsBaseStats()
        {
            Assert.AreEqual(Stats(50, 40, 30), AdventurerStatRules.GetStatsAtLevel(Stats(50, 40, 30), Stats(3, 2, 1), 0));
        }

        [Test]
        public void GetStatsAtLevel_HighGrowthOvertakesHighBase()
        {
            // 기본값 50·성장 3인 모험가 A와 기본값 30·성장 5인 모험가 B: 11레벨에 같아지고 12레벨부터 B가 앞선다.
            AdventurerStats baseA = Stats(50, 50, 50);
            AdventurerStats growthA = Stats(3, 3, 3);
            AdventurerStats baseB = Stats(30, 30, 30);
            AdventurerStats growthB = Stats(5, 5, 5);

            Assert.Greater(AdventurerStatRules.GetStatsAtLevel(baseA, growthA, 10).MaxHp,
                AdventurerStatRules.GetStatsAtLevel(baseB, growthB, 10).MaxHp);
            Assert.AreEqual(AdventurerStatRules.GetStatsAtLevel(baseA, growthA, 11),
                AdventurerStatRules.GetStatsAtLevel(baseB, growthB, 11));
            Assert.Less(AdventurerStatRules.GetStatsAtLevel(baseA, growthA, 12).MaxHp,
                AdventurerStatRules.GetStatsAtLevel(baseB, growthB, 12).MaxHp);
        }

        [Test]
        public void GetCurrentStats_UsesAdventurerLevel()
        {
            var adventurer = new AdventurerData { BaseStats = Stats(10, 10, 10), GrowthStats = Stats(1, 2, 3), Level = 3 };

            Assert.AreEqual(Stats(12, 14, 16), AdventurerStatRules.GetCurrentStats(adventurer));
        }
    }
}
