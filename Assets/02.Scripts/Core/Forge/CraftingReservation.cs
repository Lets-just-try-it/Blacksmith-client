using System;
using System.Collections.Generic;
using Blacksmith.Core.Inventory;

namespace Blacksmith.Core.Forge
{
    // 제작 목록에서 "작업대 슬롯에 놓은" 재료의 예약이다. 아이템은 개인 슬롯에 그대로 있고, 칸마다 몇 개를 예약했는지만 적는다.
    // 화면은 예약한 만큼을 개인 슬롯에서 빼고 작업대 슬롯에 놓은 것처럼 보여 주고, 제작이 끝날 때 호스트가 그 칸에서 재료를 뺀다.
    // 네트워크와 Unity에 의존하지 않는다. 예약은 칸마다 1바이트로 64비트 하나에 담아 호스트에 보낸다(칸 8개, 칸당 최대 255개).
    public sealed class CraftingReservation
    {
        public const int MaxSlots = 8;
        private const int BitsPerSlot = 8;
        private const int MaxPerSlot = 255;

        private readonly int[] _counts = new int[MaxSlots];
        private readonly int[] _itemIds = new int[MaxSlots];

        public bool IsEmpty
        {
            get
            {
                for (int i = 0; i < MaxSlots; i++)
                {
                    if (_counts[i] > 0)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public int GetReserved(int slot)
        {
            return slot >= 0 && slot < MaxSlots ? _counts[slot] : 0;
        }

        public void Clear()
        {
            Array.Clear(_counts, 0, MaxSlots);
            Array.Clear(_itemIds, 0, MaxSlots);
        }

        // 개인 슬롯 slot의 물건을 wantedCount개까지 예약한다. 칸에 남은 개수와 고른 항목이 더 받을 수 있는 개수를 넘지 않게 줄이고,
        // 실제로 예약한 개수를 돌려준다. 고른 항목이 쓰지 않는 물건이면 0이다.
        public int TryReserve(int slot, IReadOnlyList<ItemStack> actual, int wantedCount, IReadOnlyList<CraftingIngredient> needs)
        {
            if (slot < 0 || slot >= MaxSlots || slot >= actual.Count || actual[slot].IsEmpty || wantedCount <= 0)
            {
                return 0;
            }

            ItemStack stack = actual[slot];
            if (_counts[slot] > 0 && _itemIds[slot] != stack.ItemId)
            {
                return 0;
            }

            int available = Math.Min(MaxPerSlot, stack.Count) - _counts[slot];
            int wanted = Math.Min(wantedCount, available);
            if (wanted <= 0)
            {
                return 0;
            }

            var reservedStacks = new List<ItemStack>();
            BuildStacks(actual, reservedStacks);
            int allowed = CraftingRequirements.AcceptableCount(needs, reservedStacks, stack.ItemId, wanted);
            if (allowed <= 0)
            {
                return 0;
            }

            _counts[slot] += allowed;
            _itemIds[slot] = stack.ItemId;
            return allowed;
        }

        // 칸의 예약을 풀고, 푼 개수를 돌려준다.
        public int Release(int slot)
        {
            if (slot < 0 || slot >= MaxSlots)
            {
                return 0;
            }

            int released = _counts[slot];
            _counts[slot] = 0;
            _itemIds[slot] = 0;
            return released;
        }

        // 인벤토리가 바뀌어 예약한 칸의 물건이 달라졌거나 줄었으면 예약을 맞춘다. 바뀐 것이 있으면 true다.
        public bool Reconcile(IReadOnlyList<ItemStack> actual)
        {
            bool changed = false;
            for (int i = 0; i < MaxSlots; i++)
            {
                if (_counts[i] <= 0)
                {
                    continue;
                }

                ItemStack stack = i < actual.Count ? actual[i] : ItemStack.Empty;
                if (stack.IsEmpty || stack.ItemId != _itemIds[i])
                {
                    _counts[i] = 0;
                    _itemIds[i] = 0;
                    changed = true;
                }
                else if (stack.Count < _counts[i])
                {
                    _counts[i] = stack.Count;
                    changed = true;
                }
            }

            return changed;
        }

        // 예약한 만큼의 물건을 칸 번호 순서대로 results에 채운다. 예약하지 않은 칸은 빈 칸이다(목록 길이는 MaxSlots).
        public void BuildStacks(IReadOnlyList<ItemStack> actual, List<ItemStack> results)
        {
            results.Clear();
            for (int i = 0; i < MaxSlots; i++)
            {
                bool reserved = _counts[i] > 0 && i < actual.Count && !actual[i].IsEmpty;
                results.Add(reserved ? actual[i].WithCount(Math.Min(_counts[i], actual[i].Count)) : ItemStack.Empty);
            }
        }

        public long Pack()
        {
            long packed = 0;
            for (int i = 0; i < MaxSlots; i++)
            {
                packed |= (long)(_counts[i] & MaxPerSlot) << (BitsPerSlot * i);
            }

            return packed;
        }

        public static void Unpack(long packed, int[] counts)
        {
            for (int i = 0; i < MaxSlots; i++)
            {
                counts[i] = (int)((packed >> (BitsPerSlot * i)) & MaxPerSlot);
            }
        }

        // 호스트가 받은 예약(counts)이 지금 개인 슬롯(actual)에 실제로 있는 만큼인지 확인하고, 예약한 만큼의 물건 목록을 만든다.
        // 어느 칸이든 예약이 실제 개수보다 많으면 false다.
        public static bool TryBuildStacks(int[] counts, IReadOnlyList<ItemStack> actual, List<ItemStack> results)
        {
            results.Clear();
            for (int i = 0; i < MaxSlots; i++)
            {
                int count = counts[i];
                ItemStack stack = i < actual.Count ? actual[i] : ItemStack.Empty;
                if (count > 0 && (stack.IsEmpty || stack.Count < count))
                {
                    results.Clear();
                    return false;
                }

                results.Add(count > 0 ? stack.WithCount(count) : ItemStack.Empty);
            }

            return true;
        }
    }
}
