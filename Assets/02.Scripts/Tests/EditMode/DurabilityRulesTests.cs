using Blacksmith.Core.Inventory;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class DurabilityRulesTests
    {
        // 0.01 단위: 2 / 1.6 / 1.2 / 1 / 0.8
        private static readonly int[] WearByGrade = { 200, 160, 120, 100, 80 };

        [TestCase(1, 200)]
        [TestCase(3, 120)]
        [TestCase(5, 80)]
        [TestCase(0, 200)]
        [TestCase(9, 80)]
        public void WearPerUse_FollowsGrade(int grade, int expected)
        {
            Assert.AreEqual(expected, DurabilityRules.GetWearPerUse(grade, WearByGrade));
        }

        [TestCase(1, 50, 100)]
        [TestCase(5, 50, 40)]
        [TestCase(5, 180, 144)]
        [TestCase(1, 180, 360)]
        [TestCase(5, 1, 1)]
        [TestCase(5, 0, 0)]
        public void WearPerUse_ScalesByWeaponType(int grade, int percent, int expected)
        {
            Assert.AreEqual(expected, DurabilityRules.GetWearPerUse(grade, WearByGrade, percent));
        }

        [Test]
        public void AddWear_MultipliesUses()
        {
            Assert.AreEqual(35, DurabilityRules.AddWear(5, 6, 5));
            Assert.AreEqual(5, DurabilityRules.AddWear(5, 0, 5));
        }

        [Test]
        public void Broken_OnlyWhenWearReachesMax()
        {
            Assert.IsFalse(DurabilityRules.IsBroken(9999, 100));
            Assert.IsTrue(DurabilityRules.IsBroken(10000, 100));
            Assert.IsTrue(DurabilityRules.IsBroken(13000, 100));
        }

        [Test]
        public void NoDurability_NeverWearsOrBreaks()
        {
            EquipmentDurability none = new EquipmentDurability(0, 0, 10).AddUses(1000);

            Assert.AreEqual(0, none.Wear);
            Assert.IsFalse(none.IsBroken);
        }

        [Test]
        public void RemainingPercent_RoundsDown()
        {
            Assert.AreEqual(100, DurabilityRules.GetRemainingPercent(0, 100));
            Assert.AreEqual(74, DurabilityRules.GetRemainingPercent(2501, 100));
            Assert.AreEqual(0, DurabilityRules.GetRemainingPercent(12000, 100));
        }

        [TestCase(0, 4)]
        [TestCase(5000, 2)]
        [TestCase(5001, 1)]
        [TestCase(10000, 0)]
        public void MeltIngots_ScaleWithRemainingDurability(int wear, int expected)
        {
            Assert.AreEqual(expected, DurabilityRules.GetMeltIngots(4, wear, 100));
        }

        [Test]
        public void ItemStack_DifferentWear_IsDifferentItem()
        {
            var fresh = new ItemStack(3, 2, 1);
            var worn = new ItemStack(3, 2, 1, 0, 40);

            Assert.IsFalse(fresh.IsSameItem(worn));
            Assert.AreEqual(40, worn.WithCount(1).Wear);
        }
    }
}
