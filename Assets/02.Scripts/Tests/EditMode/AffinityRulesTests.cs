using Blacksmith.Core.Adventurers;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class AffinityRulesTests
    {
        // 테스트 전용 수치다. 실제 밸런스는 ScriptableObject 데이터를 따른다.
        private static AffinityBalance CreateBalance()
        {
            return new AffinityBalance
            {
                InitialAffinity = 10,
                MaxAffinity = 100,
                FriendlyAffinity = 20,
                TrustedAffinity = 50,
                SwornAffinity = 80,
                SurvivedBattleGain = 2,
                SevereInjuryLoss = 5,
                DailyGainLimit = 15,
            };
        }

        [TestCase(5, 0, 5)]
        [TestCase(5, 12, 3)]
        [TestCase(5, 15, 0)]
        [TestCase(5, 20, 0)]
        public void AllowedGain_StopsAtDailyLimit(int amount, int gainedToday, int expected)
        {
            Assert.AreEqual(expected, AffinityRules.GetAllowedGain(amount, gainedToday, CreateBalance()));
        }

        [Test]
        public void TalkGain_OncePerDay()
        {
            Assert.IsTrue(AffinityRules.CanGainFromTalk(0, 1));
            Assert.IsFalse(AffinityRules.CanGainFromTalk(1, 1));
            Assert.IsTrue(AffinityRules.CanGainFromTalk(1, 2));
        }

        [TestCase(10, 5, 15)]
        [TestCase(98, 5, 100)]
        [TestCase(3, -5, 0)]
        public void Add_ClampsBetweenZeroAndMax(int affinity, int amount, int expected)
        {
            Assert.AreEqual(expected, AffinityRules.Add(affinity, amount, CreateBalance()));
        }

        [TestCase(0, AffinityStage.Stranger)]
        [TestCase(19, AffinityStage.Stranger)]
        [TestCase(20, AffinityStage.Friendly)]
        [TestCase(49, AffinityStage.Friendly)]
        [TestCase(50, AffinityStage.Trusted)]
        [TestCase(79, AffinityStage.Trusted)]
        [TestCase(80, AffinityStage.Sworn)]
        [TestCase(100, AffinityStage.Sworn)]
        public void GetStage_UsesThresholds(int affinity, AffinityStage expected)
        {
            Assert.AreEqual(expected, AffinityRules.GetStage(affinity, CreateBalance()));
        }

        [TestCase(0, 2)]
        [TestCase(49, 2)]
        [TestCase(50, -5)]
        [TestCase(95, -5)]
        public void BattleChange_LosesOnSevereInjury(int injury, int expected)
        {
            Assert.AreEqual(expected, AffinityRules.GetBattleChange(injury, InjuryRulesTests.CreateBalance(), CreateBalance()));
        }
    }
}
