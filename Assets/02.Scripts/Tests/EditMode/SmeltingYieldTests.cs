using Blacksmith.Core.Forge;
using Blacksmith.Core.Inventory;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class SmeltingYieldTests
    {
        // 테스트 전용 수치다. 실제 값은 SmeltingRules 데이터를 따른다.
        private static SmeltingRules CreateRules()
        {
            return new SmeltingRules
            {
                YieldTiers = new[]
                {
                    new SmeltingYieldTier { MinAccuracy = 0f, IngotCount = 1 },
                    new SmeltingYieldTier { MinAccuracy = 0.4f, IngotCount = 2 },
                    new SmeltingYieldTier { MinAccuracy = 0.85f, IngotCount = 3 },
                    new SmeltingYieldTier { MinAccuracy = 0.95f, IngotCount = 4 },
                },
            };
        }

        [Test]
        public void IngotCount_FollowsAccuracyTiers()
        {
            SmeltingRules rules = CreateRules();

            Assert.AreEqual(1, rules.IngotCount(0f));
            Assert.AreEqual(1, rules.IngotCount(0.39f));
            Assert.AreEqual(2, rules.IngotCount(0.4f));
            Assert.AreEqual(2, rules.IngotCount(0.84f));
            Assert.AreEqual(3, rules.IngotCount(0.85f));
            Assert.AreEqual(3, rules.IngotCount(0.94f));
            Assert.AreEqual(4, rules.IngotCount(0.95f));
            Assert.AreEqual(4, rules.IngotCount(1f));
        }

        [Test]
        public void IngotCount_NeverDropsBelowOne()
        {
            Assert.AreEqual(1, CreateRules().IngotCount(-1f));
        }

        [Test]
        public void MinAndMaxIngots_CoverTheWholeRange()
        {
            SmeltingRules rules = CreateRules();

            Assert.AreEqual(1, rules.MinIngots);
            Assert.AreEqual(4, rules.MaxIngots);
        }

        [Test]
        public void MakeIngots_HasNoGradeOrAccuracy_SoTheyStackTogether()
        {
            ItemStack first = CraftingRules.MakeIngots(5, 1);
            ItemStack second = CraftingRules.MakeIngots(5, 3);

            Assert.AreEqual(0, first.Grade);
            Assert.AreEqual(0, first.Accuracy);
            Assert.IsTrue(first.IsSameItem(second));
            Assert.AreEqual(3, second.Count);
        }
    }
}
