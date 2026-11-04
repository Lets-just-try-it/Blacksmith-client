using System.Collections.Generic;
using Blacksmith.Core.Inventory;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class InventoryGridTests
    {
        private const int Ore = 1;
        private const int Sword = 2;
        private const int OreMaxStack = 10;

        private static InventoryGrid CreateShared()
        {
            var rules = new InventoryRules { Columns = 5, InitialRows = 2, GrowRows = 1, MaxRows = 4 };
            return new InventoryGrid(rules, GetMaxStack);
        }

        private static InventoryGrid CreatePlayer()
        {
            var rules = new InventoryRules { Columns = 8, InitialRows = 1, GrowRows = 0, MaxRows = 1 };
            return new InventoryGrid(rules, GetMaxStack);
        }

        private static int GetMaxStack(int itemId)
        {
            return itemId == Ore ? OreMaxStack : 1;
        }

        private static void Fill(InventoryGrid grid, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Assert.IsTrue(grid.TryAdd(new ItemStack(Sword, 1, 1)));
            }
        }

        [Test]
        public void SameItemWithDifferentAccuracy_IsNotSameItem()
        {
            var a = new ItemStack(Ore, 0, 1, 70);
            var b = new ItemStack(Ore, 0, 1, 80);

            Assert.IsFalse(a.IsSameItem(b));
            Assert.IsFalse(a.Equals(b));
            Assert.IsTrue(a.IsSameItem(new ItemStack(Ore, 0, 3, 70)));
        }

        [Test]
        public void TryAdd_DoesNotMergeStacksWithDifferentAccuracy()
        {
            InventoryGrid grid = CreateShared();
            grid.TryAdd(new ItemStack(Ore, 0, 2, 70));

            Assert.IsTrue(grid.TryAdd(new ItemStack(Ore, 0, 2, 80)));

            Assert.AreEqual(new ItemStack(Ore, 0, 2, 70), grid.Get(0));
            Assert.AreEqual(new ItemStack(Ore, 0, 2, 80), grid.Get(1));
        }

        [Test]
        public void WithCount_KeepsAccuracy()
        {
            Assert.AreEqual(70, new ItemStack(Ore, 0, 5, 70).WithCount(2).Accuracy);
        }

        [Test]
        public void TryAdd_PutsItemInFirstEmptySlot()
        {
            InventoryGrid grid = CreateShared();
            grid.TryAdd(new ItemStack(Sword, 1, 1));
            InventoryGrid.TryMove(grid, 0, grid, 3);

            Assert.IsTrue(grid.TryAdd(new ItemStack(Sword, 2, 1)));

            Assert.AreEqual(new ItemStack(Sword, 2, 1), grid.Get(0));
            Assert.AreEqual(new ItemStack(Sword, 1, 1), grid.Get(3));
        }

        [Test]
        public void TryAdd_StacksSameItemAndGradeUpToMax()
        {
            InventoryGrid grid = CreateShared();
            grid.TryAdd(new ItemStack(Ore, 1, 7));

            Assert.IsTrue(grid.TryAdd(new ItemStack(Ore, 1, 5)));

            Assert.AreEqual(new ItemStack(Ore, 1, 10), grid.Get(0));
            Assert.AreEqual(new ItemStack(Ore, 1, 2), grid.Get(1));
        }

        [Test]
        public void TryAdd_DoesNotStackDifferentGrade()
        {
            InventoryGrid grid = CreateShared();
            grid.TryAdd(new ItemStack(Ore, 1, 3));

            grid.TryAdd(new ItemStack(Ore, 2, 3));

            Assert.AreEqual(new ItemStack(Ore, 1, 3), grid.Get(0));
            Assert.AreEqual(new ItemStack(Ore, 2, 3), grid.Get(1));
        }

        [Test]
        public void TryAdd_NonStackableItemTakesOneSlotEach()
        {
            InventoryGrid grid = CreateShared();

            Assert.IsTrue(grid.TryAdd(new ItemStack(Sword, 1, 3)));

            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(new ItemStack(Sword, 1, 1), grid.Get(i));
            }
        }

        [Test]
        public void TryAdd_WhenFull_GrowsRows()
        {
            InventoryGrid grid = CreateShared();
            Fill(grid, 10);
            Assert.AreEqual(2, grid.Rows);

            Assert.IsTrue(grid.TryAdd(new ItemStack(Sword, 1, 1)));

            Assert.AreEqual(3, grid.Rows);
            Assert.AreEqual(15, grid.SlotCount);
            Assert.AreEqual(new ItemStack(Sword, 1, 1), grid.Get(10));
        }

        [Test]
        public void TryAdd_DoesNotGrowWhileEmptySlotRemains()
        {
            InventoryGrid grid = CreateShared();
            Fill(grid, 10);
            grid.TryRemove(4, 1, out _);

            grid.TryAdd(new ItemStack(Sword, 1, 1));

            Assert.AreEqual(2, grid.Rows);
        }

        [Test]
        public void TryAdd_BeyondMaxRows_AddsNothing()
        {
            InventoryGrid grid = CreateShared();
            Fill(grid, 19);

            Assert.IsFalse(grid.TryAdd(new ItemStack(Sword, 1, 2)));

            Assert.AreEqual(4, grid.Rows);
            Assert.IsTrue(grid.Get(19).IsEmpty);
            Assert.IsTrue(grid.TryAdd(new ItemStack(Sword, 1, 1)));
        }

        [Test]
        public void TryAdd_FixedGridWhenFull_Fails()
        {
            InventoryGrid grid = CreatePlayer();
            Fill(grid, 8);

            Assert.IsFalse(grid.TryAdd(new ItemStack(Sword, 1, 1)));
            Assert.AreEqual(1, grid.Rows);
        }

        [Test]
        public void TryMove_ToEmptySlot_MovesItem()
        {
            InventoryGrid grid = CreateShared();
            grid.TryAdd(new ItemStack(Sword, 1, 1));

            Assert.IsTrue(InventoryGrid.TryMove(grid, 0, grid, 7));

            Assert.IsTrue(grid.Get(0).IsEmpty);
            Assert.AreEqual(new ItemStack(Sword, 1, 1), grid.Get(7));
        }

        [Test]
        public void TryMove_ToDifferentItem_Swaps()
        {
            InventoryGrid grid = CreateShared();
            grid.TryAdd(new ItemStack(Sword, 1, 1));
            grid.TryAdd(new ItemStack(Ore, 1, 4));

            Assert.IsTrue(InventoryGrid.TryMove(grid, 0, grid, 1));

            Assert.AreEqual(new ItemStack(Ore, 1, 4), grid.Get(0));
            Assert.AreEqual(new ItemStack(Sword, 1, 1), grid.Get(1));
        }

        [Test]
        public void TryMove_ToSameItem_MergesAndLeavesRemainder()
        {
            InventoryGrid grid = CreateShared();
            grid.TryAdd(new ItemStack(Ore, 1, 14));
            grid.TryRemove(0, 3, out _);

            Assert.IsTrue(InventoryGrid.TryMove(grid, 1, grid, 0));

            Assert.AreEqual(new ItemStack(Ore, 1, 10), grid.Get(0));
            Assert.AreEqual(new ItemStack(Ore, 1, 1), grid.Get(1));
        }

        [Test]
        public void TryMove_ToFullStackOfSameItem_DoesNothing()
        {
            InventoryGrid grid = CreateShared();
            grid.TryAdd(new ItemStack(Ore, 1, 14));

            Assert.IsFalse(InventoryGrid.TryMove(grid, 1, grid, 0));

            Assert.AreEqual(new ItemStack(Ore, 1, 10), grid.Get(0));
            Assert.AreEqual(new ItemStack(Ore, 1, 4), grid.Get(1));
        }

        [Test]
        public void TryMove_SameSlotOrEmptySource_DoesNothing()
        {
            InventoryGrid grid = CreateShared();
            grid.TryAdd(new ItemStack(Sword, 1, 1));

            Assert.IsFalse(InventoryGrid.TryMove(grid, 0, grid, 0));
            Assert.IsFalse(InventoryGrid.TryMove(grid, 3, grid, 0));
            Assert.AreEqual(new ItemStack(Sword, 1, 1), grid.Get(0));
        }

        [Test]
        public void TryMove_OutsideCurrentRows_Fails()
        {
            InventoryGrid grid = CreateShared();
            grid.TryAdd(new ItemStack(Sword, 1, 1));

            Assert.IsFalse(InventoryGrid.TryMove(grid, 0, grid, 10));
        }

        [Test]
        public void TryMove_BetweenGrids_MovesItem()
        {
            InventoryGrid shared = CreateShared();
            InventoryGrid player = CreatePlayer();
            shared.TryAdd(new ItemStack(Ore, 1, 5));

            Assert.IsTrue(InventoryGrid.TryMove(shared, 0, player, 7));

            Assert.IsTrue(shared.Get(0).IsEmpty);
            Assert.AreEqual(new ItemStack(Ore, 1, 5), player.Get(7));
        }

        [Test]
        public void TryRemove_TakesPartOfStack()
        {
            InventoryGrid grid = CreateShared();
            grid.TryAdd(new ItemStack(Ore, 1, 5));

            Assert.IsTrue(grid.TryRemove(0, 2, out ItemStack removed));

            Assert.AreEqual(new ItemStack(Ore, 1, 2), removed);
            Assert.AreEqual(new ItemStack(Ore, 1, 3), grid.Get(0));
            Assert.IsFalse(grid.TryRemove(0, 4, out _));
        }

        [Test]
        public void FindSlots_ReturnsMatchingSlotsInOrder()
        {
            InventoryGrid grid = CreateShared();
            grid.TryAdd(new ItemStack(Sword, 1, 1));
            grid.TryAdd(new ItemStack(Ore, 1, 2));
            grid.TryAdd(new ItemStack(Sword, 3, 1));
            var results = new List<int>();

            grid.FindSlots(item => item.ItemId == Sword, results);

            CollectionAssert.AreEqual(new[] { 0, 2 }, results);
        }

        [Test]
        public void Load_KeepsRowsWithinRules()
        {
            InventoryGrid grid = CreateShared();

            grid.Load(9, slot => slot == 0 ? new ItemStack(Sword, 1, 1) : ItemStack.Empty);

            Assert.AreEqual(4, grid.Rows);
            Assert.AreEqual(new ItemStack(Sword, 1, 1), grid.Get(0));
        }

        [Test]
        public void TryReplaceOne_SingleItem_ReplacesInSameSlot()
        {
            InventoryGrid grid = CreatePlayer();
            grid.TryAdd(new ItemStack(Sword, 1, 1));
            grid.TryAdd(new ItemStack(Ore, 1, 1));

            Assert.IsTrue(grid.TryReplaceOne(1, new ItemStack(Sword, 70, 1)));
            Assert.AreEqual(new ItemStack(Sword, 70, 1), grid.Get(1));
            Assert.AreEqual(new ItemStack(Sword, 1, 1), grid.Get(0));
        }

        [Test]
        public void TryReplaceOne_Stack_ConsumesOneAndPlacesResultInEmptySlot()
        {
            InventoryGrid grid = CreatePlayer();
            grid.TryAdd(new ItemStack(Ore, 1, 5));

            Assert.IsTrue(grid.TryReplaceOne(0, new ItemStack(Sword, 70, 1)));
            Assert.AreEqual(new ItemStack(Ore, 1, 4), grid.Get(0));
            Assert.AreEqual(new ItemStack(Sword, 70, 1), grid.Get(1));
        }

        [Test]
        public void TryReplaceOne_StackWithNoRoom_ChangesNothing()
        {
            InventoryGrid grid = CreatePlayer();
            grid.TryAdd(new ItemStack(Ore, 1, 5));
            Fill(grid, 7);

            Assert.IsFalse(grid.TryReplaceOne(0, new ItemStack(Sword, 70, 1)));
            Assert.AreEqual(new ItemStack(Ore, 1, 5), grid.Get(0));
        }

        [Test]
        public void TryReplaceOne_EmptyOrInvalidSlot_Fails()
        {
            InventoryGrid grid = CreatePlayer();
            Assert.IsFalse(grid.TryReplaceOne(0, new ItemStack(Sword, 1, 1)));
            Assert.IsFalse(grid.TryReplaceOne(99, new ItemStack(Sword, 1, 1)));
        }

        [Test]
        public void TryMoveCount_ToEmptySlot_SplitsStack()
        {
            InventoryGrid grid = CreateShared();
            grid.TryAdd(new ItemStack(Ore, 1, 7));

            Assert.IsTrue(InventoryGrid.TryMoveCount(grid, 0, grid, 4, 3));

            Assert.AreEqual(new ItemStack(Ore, 1, 4), grid.Get(0));
            Assert.AreEqual(new ItemStack(Ore, 1, 3), grid.Get(4));
        }

        [Test]
        public void TryMoveCount_ToSameItem_MovesOnlyWhatFits()
        {
            InventoryGrid grid = CreateShared();
            grid.TryAdd(new ItemStack(Ore, 1, 18));

            Assert.IsTrue(InventoryGrid.TryMoveCount(grid, 0, grid, 1, 5));

            Assert.AreEqual(new ItemStack(Ore, 1, 8), grid.Get(0));
            Assert.AreEqual(new ItemStack(Ore, 1, 10), grid.Get(1));
        }

        [Test]
        public void TryMoveCount_ToDifferentItemOrTooMany_Fails()
        {
            InventoryGrid grid = CreateShared();
            grid.TryAdd(new ItemStack(Ore, 1, 5));
            grid.TryAdd(new ItemStack(Sword, 1, 1));

            Assert.IsFalse(InventoryGrid.TryMoveCount(grid, 0, grid, 1, 2));
            Assert.IsFalse(InventoryGrid.TryMoveCount(grid, 0, grid, 2, 6));
            Assert.IsFalse(InventoryGrid.TryMoveCount(grid, 0, grid, 2, 0));
            Assert.AreEqual(new ItemStack(Ore, 1, 5), grid.Get(0));
        }

        [Test]
        public void TryTransfer_MergesIntoSameItemThenEmptySlot()
        {
            InventoryGrid shared = CreateShared();
            InventoryGrid player = CreatePlayer();
            player.TryAdd(new ItemStack(Ore, 1, 8));
            shared.TryAdd(new ItemStack(Sword, 1, 1));
            shared.TryAdd(new ItemStack(Ore, 1, 5));

            Assert.IsTrue(InventoryGrid.TryTransfer(shared, 1, player));

            Assert.IsTrue(shared.Get(1).IsEmpty);
            Assert.AreEqual(new ItemStack(Ore, 1, 10), player.Get(0));
            Assert.AreEqual(new ItemStack(Ore, 1, 3), player.Get(1));
        }

        [Test]
        public void TryTransfer_NotEnoughRoom_MovesWhatFitsAndKeepsRest()
        {
            InventoryGrid shared = CreateShared();
            InventoryGrid player = CreatePlayer();
            player.TryAdd(new ItemStack(Ore, 1, 6));
            Fill(player, 7);
            shared.TryAdd(new ItemStack(Ore, 1, 9));

            Assert.IsTrue(InventoryGrid.TryTransfer(shared, 0, player));

            Assert.AreEqual(new ItemStack(Ore, 1, 10), player.Get(0));
            Assert.AreEqual(new ItemStack(Ore, 1, 5), shared.Get(0));
        }

        [Test]
        public void TryTransfer_NoRoomOrSameGrid_Fails()
        {
            InventoryGrid shared = CreateShared();
            InventoryGrid player = CreatePlayer();
            Fill(player, 8);
            shared.TryAdd(new ItemStack(Sword, 1, 1));

            Assert.IsFalse(InventoryGrid.TryTransfer(shared, 0, player));
            Assert.IsFalse(InventoryGrid.TryTransfer(shared, 0, shared));
            Assert.AreEqual(new ItemStack(Sword, 1, 1), shared.Get(0));
        }
    }
}
