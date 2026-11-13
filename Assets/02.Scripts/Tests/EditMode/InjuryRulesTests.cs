using Blacksmith.Core.Adventurers;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class InjuryRulesTests
    {
        // 테스트 전용 수치다. 실제 밸런스는 ScriptableObject 데이터를 따른다.
        public static InjuryBalance CreateBalance()
        {
            return new InjuryBalance
            {
                MaxInjury = 100,
                DispatchLimit = 50,
                DeathCheckInjury = 90,
                BaseDeathChancePercent = 10,
                DeathChancePerInjury = 1,
                UninjuredHpPercent = 70,
                MaxSurvivedInjury = 60,
                FallenInjury = 90,
                DailyRecovery = 15,
            };
        }

        [TestCase(100, 0)]
        [TestCase(70, 0)]
        [TestCase(50, 17)]
        [TestCase(30, 34)]
        [TestCase(10, 51)]
        [TestCase(1, 59)]
        public void BattleInjury_GrowsAsRemainingHpDrops(int remainingHp, int expected)
        {
            Assert.AreEqual(expected, InjuryRules.GetBattleInjury(remainingHp, 100, false, CreateBalance()));
        }

        [Test]
        public void BattleInjury_FallenGetsFallenInjury()
        {
            Assert.AreEqual(90, InjuryRules.GetBattleInjury(0, 100, true, CreateBalance()));
        }

        [Test]
        public void AddInjury_ClampsToMax()
        {
            Assert.AreEqual(91, InjuryRules.AddInjury(40, 51, CreateBalance()));
            Assert.AreEqual(100, InjuryRules.AddInjury(40, 90, CreateBalance()));
        }

        [Test]
        public void Recover_StopsAtZero()
        {
            Assert.AreEqual(36, InjuryRules.Recover(51, 15));
            Assert.AreEqual(0, InjuryRules.Recover(10, 30));
        }

        [Test]
        public void Dispatch_BlockedFromLimit()
        {
            Assert.IsTrue(InjuryRules.CanDispatch(49, CreateBalance()));
            Assert.IsFalse(InjuryRules.CanDispatch(50, CreateBalance()));
        }

        [TestCase(89, 0)]
        [TestCase(90, 10)]
        [TestCase(95, 15)]
        [TestCase(100, 20)]
        public void DeathChance_StartsAtDeathCheckInjury(int injury, int expected)
        {
            Assert.AreEqual(expected, InjuryRules.GetDeathChancePercent(injury, CreateBalance()));
        }

        [Test]
        public void Condition_FollowsInjury()
        {
            Assert.AreEqual(AdventurerCondition.Healthy, new AdventurerData { Injury = 0 }.Condition);
            Assert.AreEqual(AdventurerCondition.Injured, new AdventurerData { Injury = 1 }.Condition);
        }
    }
}
