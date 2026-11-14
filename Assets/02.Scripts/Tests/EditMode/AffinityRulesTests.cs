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
                FastRecoveryChancePercent = 10,
                FastRecoveryInjuryPercent = 50,
                SkillBoostMinChancePercent = 5,
                SkillBoostMaxChancePercent = 10,
                SkillBoostPowerPercent = 50,
                DeathExemptionChancePercent = 5,
            };
        }

        // 빠른 회복·스킬 강화는 신뢰부터, 사망 면제는 맹세부터. 스킬 강화는 50에서 5%, 100에서 10%로 비례한다.
        [TestCase(49, 0, 0, 0)]
        [TestCase(50, 10, 5, 0)]
        [TestCase(79, 10, 7, 0)]
        [TestCase(80, 10, 8, 5)]
        [TestCase(100, 10, 10, 5)]
        public void BattleChances_FollowStages(int affinity, int fastRecovery, int skillBoost, int deathExemption)
        {
            AffinityBattleChances chances = AffinityRules.GetBattleChances(affinity, CreateBalance());

            Assert.AreEqual(fastRecovery, chances.FastRecoveryPercent);
            Assert.AreEqual(skillBoost, chances.SkillBoostPercent);
            Assert.AreEqual(deathExemption, chances.DeathExemptionPercent);
        }

        [TestCase(90, 45)]
        [TestCase(17, 8)]
        [TestCase(0, 0)]
        public void ReduceInjury_KeepsPercentRoundedDown(int gained, int expected)
        {
            Assert.AreEqual(expected, AffinityRules.ReduceInjury(gained, 50));
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

        // 상급(4) 이상이면 더 오른다.
        [TestCase(1, 3)]
        [TestCase(3, 3)]
        [TestCase(4, 5)]
        [TestCase(5, 5)]
        public void EquipGiftGain_HigherFromHighGrade(int grade, int expected)
        {
            AffinityBalance balance = CreateBalance();
            balance.EquipGain = 3;
            balance.HighGradeEquipGain = 5;
            balance.HighGradeEquipMinGrade = 4;

            Assert.AreEqual(expected, AffinityRules.GetEquipGiftGain(grade, balance));
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
