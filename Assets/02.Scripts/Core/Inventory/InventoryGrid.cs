using System;
using System.Collections.Generic;

namespace Blacksmith.Core.Inventory
{
    // 인벤토리 칸 규칙이다. 공용 창고와 플레이어 슬롯이 같은 규칙을 쓴다.
    // 네트워크 상태는 이 격자로 값을 옮겨 규칙을 적용한 뒤 다시 써 넣는다.
    public class InventoryGrid
    {
        private readonly InventoryRules _rules;
        private readonly Func<int, int> _getMaxStack;
        private readonly ItemStack[] _slots;

        public int Columns => _rules.Columns;
        public int Rows { get; private set; }
        public int SlotCount => Rows * _rules.Columns;
        public int MaxSlotCount => _slots.Length;

        // getMaxStack: 아이템 번호를 받아 한 칸에 쌓을 수 있는 최대 개수를 돌려준다. 쌓지 않는 아이템은 1이다.
        public InventoryGrid(InventoryRules rules, Func<int, int> getMaxStack)
        {
            if (rules.Columns <= 0 || rules.InitialRows <= 0 || rules.MaxRows < rules.InitialRows || rules.GrowRows < 0)
            {
                throw new ArgumentException("인벤토리 크기 규칙이 올바르지 않다.", nameof(rules));
            }

            _rules = rules;
            _getMaxStack = getMaxStack;
            _slots = new ItemStack[rules.MaxSlotCount];
            Rows = rules.InitialRows;
        }

        public ItemStack Get(int slot)
        {
            return IsValidSlot(slot) ? _slots[slot] : ItemStack.Empty;
        }

        public bool IsValidSlot(int slot)
        {
            return slot >= 0 && slot < SlotCount;
        }

        // 네트워크 상태를 격자로 옮길 때 쓴다. 줄 수는 규칙 범위 안으로 맞춘다.
        public void Load(int rows, Func<int, ItemStack> getSlot)
        {
            Rows = Math.Max(_rules.InitialRows, Math.Min(rows, _rules.MaxRows));
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = i < SlotCount ? Normalize(getSlot(i)) : ItemStack.Empty;
            }
        }

        // 같은 물건이 쌓인 칸을 먼저 채우고, 남으면 앞쪽 빈 칸부터 넣는다. 빈 칸이 없으면 줄을 늘린다.
        // 최대 줄 수까지 늘려도 다 들어가지 않으면 아무것도 넣지 않고 false를 돌려준다.
        public bool TryAdd(ItemStack item)
        {
            if (item.IsEmpty || GetFreeRoom(item) < item.Count)
            {
                return false;
            }

            int maxStack = GetMaxStack(item.ItemId);
            int remaining = item.Count;
            for (int i = 0; i < SlotCount && remaining > 0; i++)
            {
                if (_slots[i].IsSameItem(item) && _slots[i].Count < maxStack)
                {
                    int moved = Math.Min(maxStack - _slots[i].Count, remaining);
                    _slots[i] = _slots[i].WithCount(_slots[i].Count + moved);
                    remaining -= moved;
                }
            }

            while (remaining > 0)
            {
                int slot = FindEmptySlot();
                if (slot < 0)
                {
                    Rows = Math.Min(Rows + _rules.GrowRows, _rules.MaxRows);
                    continue;
                }

                int placed = Math.Min(maxStack, remaining);
                _slots[slot] = item.WithCount(placed);
                remaining -= placed;
            }

            return true;
        }

        // 지금 줄 수에서 최대 줄 수까지 늘렸을 때 이 물건을 더 넣을 수 있는 개수.
        public int GetFreeRoom(ItemStack item)
        {
            if (item.IsEmpty)
            {
                return 0;
            }

            int maxStack = GetMaxStack(item.ItemId);
            int room = 0;
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    room += maxStack;
                }
                else if (_slots[i].IsSameItem(item))
                {
                    room += Math.Max(0, maxStack - _slots[i].Count);
                }
            }

            int growableSlots = _rules.GrowRows > 0 ? MaxSlotCount - SlotCount : 0;
            return room + growableSlots * maxStack;
        }

        public bool TryRemove(int slot, int count, out ItemStack removed)
        {
            removed = ItemStack.Empty;
            if (!IsValidSlot(slot) || _slots[slot].IsEmpty || count <= 0 || count > _slots[slot].Count)
            {
                return false;
            }

            removed = _slots[slot].WithCount(count);
            _slots[slot] = _slots[slot].WithCount(_slots[slot].Count - count);
            return true;
        }

        // 칸의 물건 하나를 결과물로 바꾼다. 그 칸에 하나뿐이면 같은 칸에 결과물을 두고, 여럿이면 하나만 줄이고
        // 결과물을 다른 칸에 넣는다. 결과물을 둘 곳이 없으면 아무것도 바꾸지 않는다.
        public bool TryReplaceOne(int slot, ItemStack result)
        {
            if (!IsValidSlot(slot) || _slots[slot].IsEmpty || result.IsEmpty)
            {
                return false;
            }

            ItemStack source = _slots[slot];
            if (source.Count == 1 && result.Count <= GetMaxStack(result.ItemId))
            {
                _slots[slot] = Normalize(result);
                return true;
            }

            _slots[slot] = source.WithCount(source.Count - 1);
            if (TryAdd(result))
            {
                return true;
            }

            _slots[slot] = source;
            return false;
        }

        // 조건에 맞는 물건이 든 칸 번호를 앞에서부터 results에 담는다.
        public void FindSlots(Predicate<ItemStack> match, List<int> results)
        {
            results.Clear();
            for (int i = 0; i < SlotCount; i++)
            {
                if (!_slots[i].IsEmpty && match(_slots[i]))
                {
                    results.Add(i);
                }
            }
        }

        // 칸을 끌어다 놓는 규칙이다. from과 to는 같은 격자일 수도, 다른 격자일 수도 있다.
        // 빈 칸이면 옮기고, 같은 물건이면 쌓을 수 있는 만큼 합치며(남은 개수는 원래 칸에 남음), 다른 물건이면 맞바꾼다.
        public static bool TryMove(InventoryGrid from, int fromSlot, InventoryGrid to, int toSlot)
        {
            if (!from.IsValidSlot(fromSlot) || !to.IsValidSlot(toSlot) || (from == to && fromSlot == toSlot))
            {
                return false;
            }

            ItemStack moving = from._slots[fromSlot];
            ItemStack target = to._slots[toSlot];
            if (moving.IsEmpty)
            {
                return false;
            }

            if (target.IsEmpty)
            {
                // 옮겨 갈 격자의 최대 쌓기 수가 더 작을 수 있으므로 넘치는 만큼은 원래 칸에 남긴다.
                int placed = Math.Min(moving.Count, to.GetMaxStack(moving.ItemId));
                to._slots[toSlot] = moving.WithCount(placed);
                from._slots[fromSlot] = moving.WithCount(moving.Count - placed);
                return true;
            }

            if (target.IsSameItem(moving))
            {
                int moved = Math.Min(to.GetMaxStack(target.ItemId) - target.Count, moving.Count);
                if (moved <= 0)
                {
                    return false;
                }

                to._slots[toSlot] = target.WithCount(target.Count + moved);
                from._slots[fromSlot] = moving.WithCount(moving.Count - moved);
                return true;
            }

            if (moving.Count > to.GetMaxStack(moving.ItemId) || target.Count > from.GetMaxStack(target.ItemId))
            {
                return false;
            }

            to._slots[toSlot] = moving;
            from._slots[fromSlot] = target;
            return true;
        }

        private int GetMaxStack(int itemId)
        {
            return Math.Max(1, _getMaxStack(itemId));
        }

        private int FindEmptySlot()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    return i;
                }
            }

            return -1;
        }

        private static ItemStack Normalize(ItemStack item)
        {
            return item.IsEmpty ? ItemStack.Empty : item;
        }
    }
}
