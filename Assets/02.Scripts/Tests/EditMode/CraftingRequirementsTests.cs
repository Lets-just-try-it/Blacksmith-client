using System.Collections.Generic;
using Blacksmith.Core.Forge;
using Blacksmith.Core.Inventory;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class CraftingRequirementsTests
    {
        private const int Ingot = 5;
        private const int Hide = 9;

        private static ItemStack[] Slots(params ItemStack[] stacks)
        {
            var slots = new ItemStack[8];
            for (int i = 0; i < stacks.Length; i++)
            {
                slots[i] = stacks[i];
            }

            return slots;
        }

        private static bool CarriesIngot(int itemId)
        {
            return itemId == Ingot;
        }

        [Test]
        public void CountOf_SumsAcrossSlots()
        {
            ItemStack[] slots = Slots(new ItemStack(Hide, 0, 3), new ItemStack(Ingot, 0, 1, 80), new ItemStack(Hide, 0, 4));
            Assert.AreEqual(7, CraftingRequirements.CountOf(Hide, slots));
            Assert.AreEqual(0, CraftingRequirements.CountOf(99, slots));
        }

        [Test]
        public void IsSatisfied_NeedsEveryIngredient()
        {
            var needs = new List<CraftingIngredient> { new CraftingIngredient(Ingot, 2), new CraftingIngredient(Hide, 3) };
            Assert.IsFalse(CraftingRequirements.IsSatisfied(needs, Slots(new ItemStack(Ingot, 0, 1, 80), new ItemStack(Ingot, 0, 1, 80))));
            Assert.IsTrue(CraftingRequirements.IsSatisfied(needs, Slots(new ItemStack(Ingot, 0, 1, 80), new ItemStack(Ingot, 0, 1, 70), new ItemStack(Hide, 0, 3))));
        }

        [Test]
        public void IsSatisfied_AddsSameItemListedTwice()
        {
            var needs = new List<CraftingIngredient> { new CraftingIngredient(Hide, 2), new CraftingIngredient(Hide, 3) };
            Assert.IsFalse(CraftingRequirements.IsSatisfied(needs, Slots(new ItemStack(Hide, 0, 4))));
            Assert.IsTrue(CraftingRequirements.IsSatisfied(needs, Slots(new ItemStack(Hide, 0, 5))));
        }

        [Test]
        public void TryPlan_TakesFromEarlierSlotsFirst()
        {
            var needs = new List<CraftingIngredient> { new CraftingIngredient(Hide, 5) };
            ItemStack[] slots = Slots(new ItemStack(Hide, 0, 3), new ItemStack(Ingot, 0, 1, 50), new ItemStack(Hide, 0, 4));
            var removals = new List<SlotRemoval>();

            Assert.IsTrue(CraftingRequirements.TryPlan(needs, slots, CarriesIngot, removals, out int average));

            Assert.AreEqual(2, removals.Count);
            Assert.AreEqual(0, removals[0].Slot);
            Assert.AreEqual(3, removals[0].Count);
            Assert.AreEqual(2, removals[1].Slot);
            Assert.AreEqual(2, removals[1].Count);
            Assert.AreEqual(0, average);
        }

        [Test]
        public void TryPlan_AveragesAccuracyOfCarryingIngredients()
        {
            var needs = new List<CraftingIngredient> { new CraftingIngredient(Ingot, 2) };
            ItemStack[] slots = Slots(new ItemStack(Ingot, 0, 1, 80), new ItemStack(Ingot, 0, 1, 61), new ItemStack(Ingot, 0, 1, 0));
            var removals = new List<SlotRemoval>();

            Assert.IsTrue(CraftingRequirements.TryPlan(needs, slots, CarriesIngot, removals, out int average));

            // 앞 두 칸의 (80 + 61) / 2 = 70.5가 반올림된다. 셋째 칸은 쓰지 않는다.
            Assert.AreEqual(71, average);
            Assert.AreEqual(2, removals.Count);
        }

        [Test]
        public void TryPlan_Fails_AndLeavesNoPlan_WhenShort()
        {
            var needs = new List<CraftingIngredient> { new CraftingIngredient(Ingot, 2), new CraftingIngredient(Hide, 3) };
            ItemStack[] slots = Slots(new ItemStack(Ingot, 0, 1, 80), new ItemStack(Ingot, 0, 1, 80), new ItemStack(Hide, 0, 2));
            var removals = new List<SlotRemoval>();

            Assert.IsFalse(CraftingRequirements.TryPlan(needs, slots, CarriesIngot, removals, out int average));

            Assert.AreEqual(0, removals.Count);
            Assert.AreEqual(0, average);
        }

        [Test]
        public void AcceptableCount_StopsAtTheRequiredCount()
        {
            var needs = new List<CraftingIngredient> { new CraftingIngredient(Ingot, 2) };

            Assert.AreEqual(2, CraftingRequirements.AcceptableCount(needs, Slots(), Ingot, 5));
            Assert.AreEqual(1, CraftingRequirements.AcceptableCount(needs, Slots(new ItemStack(Ingot, 0, 1, 80)), Ingot, 5));
            Assert.AreEqual(0, CraftingRequirements.AcceptableCount(needs, Slots(new ItemStack(Ingot, 0, 1, 80), new ItemStack(Ingot, 0, 1, 70)), Ingot, 5));
        }

        [Test]
        public void AcceptableCount_NeverMoreThanIncoming()
        {
            var needs = new List<CraftingIngredient> { new CraftingIngredient(Hide, 5) };

            Assert.AreEqual(2, CraftingRequirements.AcceptableCount(needs, Slots(), Hide, 2));
        }

        [Test]
        public void AcceptableCount_RejectsItemsTheEntryDoesNotUse()
        {
            var needs = new List<CraftingIngredient> { new CraftingIngredient(Ingot, 2) };

            Assert.AreEqual(0, CraftingRequirements.AcceptableCount(needs, Slots(), Hide, 3));
        }

        [Test]
        public void Ingredient_CountIsAtLeastOne()
        {
            Assert.AreEqual(1, new CraftingIngredient(Ingot, 0).Count);
            Assert.AreEqual(1, new CraftingIngredient(Ingot, -3).Count);
        }
    }
}
