using System.Collections.Generic;
using Blacksmith.Core.Forge;
using Blacksmith.Core.Inventory;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class CraftingReservationTests
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

        private static List<CraftingIngredient> Needs(int itemId, int count)
        {
            return new List<CraftingIngredient> { new CraftingIngredient(itemId, count) };
        }

        [Test]
        public void Reserve_StopsAtTheRequiredCount()
        {
            var reservation = new CraftingReservation();
            ItemStack[] slots = Slots(new ItemStack(Hide, 0, 9));

            int reserved = reservation.TryReserve(0, slots, 9, Needs(Hide, 4));

            Assert.AreEqual(4, reserved);
            Assert.AreEqual(4, reservation.GetReserved(0));
            Assert.AreEqual(0, reservation.TryReserve(0, slots, 5, Needs(Hide, 4)));
        }

        [Test]
        public void Reserve_CountsAcrossSlots()
        {
            var reservation = new CraftingReservation();
            ItemStack[] slots = Slots(new ItemStack(Ingot, 0, 1, 80), new ItemStack(Ingot, 0, 1, 70), new ItemStack(Ingot, 0, 1, 60));
            List<CraftingIngredient> needs = Needs(Ingot, 2);

            Assert.AreEqual(1, reservation.TryReserve(0, slots, 1, needs));
            Assert.AreEqual(1, reservation.TryReserve(1, slots, 1, needs));
            Assert.AreEqual(0, reservation.TryReserve(2, slots, 1, needs));
        }

        [Test]
        public void Reserve_RejectsItemsTheEntryDoesNotUse()
        {
            var reservation = new CraftingReservation();
            ItemStack[] slots = Slots(new ItemStack(Hide, 0, 3));

            Assert.AreEqual(0, reservation.TryReserve(0, slots, 3, Needs(Ingot, 2)));
            Assert.IsTrue(reservation.IsEmpty);
        }

        [Test]
        public void Reserve_NeverMoreThanTheSlotHolds()
        {
            var reservation = new CraftingReservation();
            ItemStack[] slots = Slots(new ItemStack(Hide, 0, 2));

            Assert.AreEqual(2, reservation.TryReserve(0, slots, 10, Needs(Hide, 5)));
        }

        [Test]
        public void Release_FreesTheSlot()
        {
            var reservation = new CraftingReservation();
            ItemStack[] slots = Slots(new ItemStack(Hide, 0, 5));
            reservation.TryReserve(0, slots, 3, Needs(Hide, 5));

            Assert.AreEqual(3, reservation.Release(0));
            Assert.IsTrue(reservation.IsEmpty);
            Assert.AreEqual(5, reservation.TryReserve(0, slots, 5, Needs(Hide, 5)));
        }

        [Test]
        public void Reconcile_DropsReservationsWhoseItemChangedOrShrank()
        {
            var reservation = new CraftingReservation();
            ItemStack[] slots = Slots(new ItemStack(Hide, 0, 4), new ItemStack(Ingot, 0, 1, 80));
            reservation.TryReserve(0, slots, 4, Needs(Hide, 4));
            reservation.TryReserve(1, slots, 1, new List<CraftingIngredient> { new CraftingIngredient(Hide, 4), new CraftingIngredient(Ingot, 1) });

            // 0번 칸은 개수가 줄고 1번 칸은 다른 물건이 들어왔다.
            ItemStack[] changed = Slots(new ItemStack(Hide, 0, 2), new ItemStack(Hide, 0, 1));
            bool didChange = reservation.Reconcile(changed);

            Assert.IsTrue(didChange);
            Assert.AreEqual(2, reservation.GetReserved(0));
            Assert.AreEqual(0, reservation.GetReserved(1));
        }

        [Test]
        public void Reconcile_NothingChanged_ReturnsFalse()
        {
            var reservation = new CraftingReservation();
            ItemStack[] slots = Slots(new ItemStack(Hide, 0, 4));
            reservation.TryReserve(0, slots, 2, Needs(Hide, 4));

            Assert.IsFalse(reservation.Reconcile(slots));
            Assert.AreEqual(2, reservation.GetReserved(0));
        }

        [Test]
        public void PackAndUnpack_RoundTrip()
        {
            var reservation = new CraftingReservation();
            ItemStack[] slots = Slots(new ItemStack(Hide, 0, 9), new ItemStack(Ingot, 0, 1, 80), ItemStack.Empty, ItemStack.Empty, ItemStack.Empty, ItemStack.Empty, ItemStack.Empty, new ItemStack(Hide, 0, 7));
            var needs = new List<CraftingIngredient> { new CraftingIngredient(Hide, 12), new CraftingIngredient(Ingot, 1) };
            reservation.TryReserve(0, slots, 9, needs);
            reservation.TryReserve(1, slots, 1, needs);
            reservation.TryReserve(7, slots, 3, needs);

            var counts = new int[CraftingReservation.MaxSlots];
            CraftingReservation.Unpack(reservation.Pack(), counts);

            Assert.AreEqual(9, counts[0]);
            Assert.AreEqual(1, counts[1]);
            Assert.AreEqual(3, counts[7]);
            Assert.AreEqual(0, counts[3]);
        }

        [Test]
        public void TryBuildStacks_RejectsReservationsLargerThanTheInventory()
        {
            var counts = new int[CraftingReservation.MaxSlots];
            counts[0] = 3;
            var results = new List<ItemStack>();

            Assert.IsFalse(CraftingReservation.TryBuildStacks(counts, Slots(new ItemStack(Hide, 0, 2)), results));
            Assert.IsFalse(CraftingReservation.TryBuildStacks(counts, Slots(), results));
            Assert.IsTrue(CraftingReservation.TryBuildStacks(counts, Slots(new ItemStack(Hide, 0, 3)), results));
            Assert.AreEqual(3, results[0].Count);
        }

        [Test]
        public void BuiltStacks_KeepAccuracyForTheAverage()
        {
            var reservation = new CraftingReservation();
            ItemStack[] slots = Slots(new ItemStack(Ingot, 0, 1, 80), new ItemStack(Ingot, 0, 1, 60));
            List<CraftingIngredient> needs = Needs(Ingot, 2);
            reservation.TryReserve(0, slots, 1, needs);
            reservation.TryReserve(1, slots, 1, needs);
            var stacks = new List<ItemStack>();
            reservation.BuildStacks(slots, stacks);
            var removals = new List<SlotRemoval>();

            Assert.IsTrue(CraftingRequirements.TryPlan(needs, stacks, id => id == Ingot, removals, out int average));
            Assert.AreEqual(70, average);
            Assert.AreEqual(2, removals.Count);
        }
    }
}
