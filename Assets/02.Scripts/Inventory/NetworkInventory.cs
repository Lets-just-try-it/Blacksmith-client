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

        // 호스트 전용. from과 to는 같은 인벤토리일 수도, 다른 인벤토리일 수도 있다.
        public static bool TryMove(NetworkInventory from, int fromSlot, NetworkInventory to, int toSlot)
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

            if (!InventoryGrid.TryMove(from._grid, fromSlot, to._grid, toSlot))
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
