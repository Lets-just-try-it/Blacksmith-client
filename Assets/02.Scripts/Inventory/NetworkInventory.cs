using System;
using System.Collections.Generic;
using Blacksmith.Core.Inventory;
using Fusion;
using UnityEngine;

namespace Blacksmith.Inventory
{
    // 호스트가 소유하는 인벤토리의 공통 부분이다. 공용 창고와 플레이어 슬롯이 이 클래스를 이어받아
    // 칸 배열만 [Networked]로 선언한다. 값은 호스트만 바꾸고, 클라이언트는 복제된 값을 읽기만 한다.
    // 칸 규칙은 Core의 InventoryGrid에 두고, 바꿀 때마다 네트워크 값을 격자로 옮겨 적용한 뒤 다시 써 넣는다.
    public abstract class NetworkInventory : NetworkBehaviour
    {
        [SerializeField] private InventoryRulesData _rules;
        [SerializeField] private ItemCatalog _catalog;

        private InventoryGrid _grid;
        private ChangeDetector _changes;

        // 스폰 직후와 칸·줄 수가 바뀔 때 모든 참가자에게 불린다.
        public event Action Changed;

        // 스폰 전에는 [Networked] 값을 읽을 수 없다.
        public bool IsReady => Object != null && Object.IsValid;
        public ItemCatalog Catalog => _catalog;
        public int Columns => _grid.Columns;
        public int Rows => StateRows;
        public int SlotCount => Rows * Columns;

        // [Networked] 배열 크기는 컴파일할 때 정해야 해서 이어받는 클래스가 상수로 둔다.
        protected abstract int MaxCapacity { get; }
        protected abstract int StateRows { get; set; }
        protected abstract NetworkItemStack GetState(int slot);
        protected abstract void SetState(int slot, NetworkItemStack value);

        public override void Spawned()
        {
            _grid = new InventoryGrid(_rules.ToRules(MaxCapacity), GetMaxStack);
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
            if (HasStateAuthority)
            {
                StateRows = _grid.Rows;
            }

            Changed?.Invoke();
        }

        public override void Render()
        {
            bool hasChanged = false;
            foreach (string _ in _changes.DetectChanges(this))
            {
                hasChanged = true;
            }

            if (hasChanged)
            {
                Changed?.Invoke();
            }
        }

        public ItemStack Get(int slot)
        {
            return slot >= 0 && slot < SlotCount ? GetState(slot).ToStack() : ItemStack.Empty;
        }

        public void FindSlots(Predicate<ItemStack> match, List<int> results)
        {
            LoadGrid();
            _grid.FindSlots(match, results);
        }

        // 다 들어갈 자리가 있는지. 줄이 늘어날 자리도 센다.
        public bool CanAdd(ItemStack item)
        {
            LoadGrid();
            return !item.IsEmpty && _grid.GetFreeRoom(item) >= item.Count;
        }

        // 호스트 전용. 다 들어가지 않으면 아무것도 넣지 않는다.
        public bool TryAdd(ItemStack item)
        {
            if (!HasStateAuthority)
            {
                return false;
            }

            LoadGrid();
            if (!_grid.TryAdd(item))
            {
                return false;
            }

            SaveGrid();
            return true;
        }

        // 여러 물건이 함께 다 들어갈 자리가 있는지. 격자에 넣어 보고 되돌린다.
        public bool CanAddAll(IReadOnlyList<ItemStack> items)
        {
            LoadGrid();
            bool canAdd = AddAllToGrid(items);
            LoadGrid();
            return canAdd;
        }

        // 호스트 전용. 하나라도 들어가지 않으면 아무것도 넣지 않는다.
        public bool TryAddAll(IReadOnlyList<ItemStack> items)
        {
            if (!HasStateAuthority)
            {
                return false;
            }

            LoadGrid();
            if (!AddAllToGrid(items))
            {
                LoadGrid();
                return false;
            }

            SaveGrid();
            return true;
        }

        private bool AddAllToGrid(IReadOnlyList<ItemStack> items)
        {
            foreach (ItemStack item in items)
            {
                if (!item.IsEmpty && !_grid.TryAdd(item))
                {
                    return false;
                }
            }

            return true;
        }

        // 호스트 전용.
        public bool TryRemove(int slot, int count, out ItemStack removed)
        {
            removed = ItemStack.Empty;
            if (!HasStateAuthority)
            {
                return false;
            }

            LoadGrid();
            if (!_grid.TryRemove(slot, count, out removed))
            {
                return false;
            }

            SaveGrid();
            return true;
        }

        // 호스트 전용. 칸의 물건 하나를 결과물로 바꾼다(제작). 결과물을 둘 곳이 없으면 아무것도 바꾸지 않는다.
        public bool TryReplaceOne(int slot, ItemStack result)
        {
            if (!HasStateAuthority)
            {
                return false;
            }

            LoadGrid();
            if (!_grid.TryReplaceOne(slot, result))
            {
                return false;
            }

            SaveGrid();
            return true;
        }

        // 호스트 전용. from과 to는 같은 인벤토리일 수도, 다른 인벤토리일 수도 있다.
        public static bool TryMove(NetworkInventory from, int fromSlot, NetworkInventory to, int toSlot)
        {
            return Apply(from, to, (fromGrid, toGrid) => InventoryGrid.TryMove(fromGrid, fromSlot, toGrid, toSlot));
        }

        // 호스트 전용. 쌓인 칸에서 count개만 떼어 옮긴다.
        public static bool TryMoveCount(NetworkInventory from, int fromSlot, NetworkInventory to, int toSlot, int count)
        {
            return Apply(from, to, (fromGrid, toGrid) => InventoryGrid.TryMoveCount(fromGrid, fromSlot, toGrid, toSlot, count));
        }

        // 호스트 전용. 칸의 물건을 다른 인벤토리의 알맞은 칸으로 보낸다.
        public static bool TryTransfer(NetworkInventory from, int fromSlot, NetworkInventory to)
        {
            return Apply(from, to, (fromGrid, toGrid) => InventoryGrid.TryTransfer(fromGrid, fromSlot, toGrid));
        }

        private static bool Apply(NetworkInventory from, NetworkInventory to, Func<InventoryGrid, InventoryGrid, bool> rule)
        {
            if (!from.HasStateAuthority || !to.HasStateAuthority)
            {
                return false;
            }

            from.LoadGrid();
            if (to != from)
            {
                to.LoadGrid();
            }

            if (!rule(from._grid, to._grid))
            {
                return false;
            }

            from.SaveGrid();
            if (to != from)
            {
                to.SaveGrid();
            }

            return true;
        }

        public int GetMaxStack(int itemId)
        {
            ItemData item = _catalog.Find(itemId);
            return item != null && item.IsStackable ? _rules.MaterialMaxStack : 1;
        }

        private void LoadGrid()
        {
            _grid.Load(StateRows, slot => GetState(slot).ToStack());
        }

        private void SaveGrid()
        {
            StateRows = _grid.Rows;
            for (int i = 0; i < _grid.MaxSlotCount; i++)
            {
                SetState(i, NetworkItemStack.FromStack(_grid.Get(i)));
            }
        }
    }
}
