using System;
using System.Collections.Generic;
using Blacksmith.Core.Inventory;

namespace Blacksmith.Core.Forge
{
    // 제작 한 단계에 필요한 재료 하나(아이템 번호와 개수).
    public readonly struct CraftingIngredient
    {
        public int ItemId { get; }
        public int Count { get; }

        public CraftingIngredient(int itemId, int count)
        {
            ItemId = itemId;
            Count = count < 1 ? 1 : count;
        }
    }

    // 제작대 슬롯에서 뺄 칸과 개수.
    public readonly struct SlotRemoval
    {
        public int Slot { get; }
        public int Count { get; }

        public SlotRemoval(int slot, int count)
        {
            Slot = slot;
            Count = count;
        }
    }

    // 제작대 슬롯에 든 물건이 한 단계의 재료를 채우는지 따지고, 채운다면 어느 칸에서 몇 개를 뺄지 정한다.
    // 네트워크와 Unity에 의존하지 않는다. 같은 재료가 여러 칸에 나뉘어 있어도 앞 칸부터 채운다.
    public static class CraftingRequirements
    {
        // 슬롯에 든 이 아이템의 전체 개수.
        public static int CountOf(int itemId, IReadOnlyList<ItemStack> slots)
        {
            int total = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty && slots[i].ItemId == itemId)
                {
                    total += slots[i].Count;
                }
            }

            return total;
        }

        // 작업대 슬롯에 이 물건을 몇 개까지 더 놓을 수 있는가. 고른 항목이 쓰지 않는 물건이면 0이고,
        // 쓰는 물건이면 필요한 개수에서 이미 놓인 개수를 뺀 만큼(들어오는 개수를 넘지 않게)이다.
        public static int AcceptableCount(IReadOnlyList<CraftingIngredient> needs, IReadOnlyList<ItemStack> slots, int itemId, int incomingCount)
        {
            int needed = 0;
            for (int i = 0; i < needs.Count; i++)
            {
                if (needs[i].ItemId == itemId)
                {
                    needed += needs[i].Count;
                }
            }

            int room = needed - CountOf(itemId, slots);
            return room < 0 ? 0 : (room > incomingCount ? incomingCount : room);
        }

        public static bool IsSatisfied(IReadOnlyList<CraftingIngredient> needs, IReadOnlyList<ItemStack> slots)
        {
            for (int i = 0; i < needs.Count; i++)
            {
                if (CountOf(needs[i].ItemId, slots) < Needed(needs, i))
                {
                    return false;
                }
            }

            return true;
        }

        // 재료를 뺄 계획을 removals에 채운다. 모자라면 false이고 removals는 비운다.
        // carriesAccuracy가 true인 재료(중간 형태)는 실제로 뺄 개수만큼의 정확도 평균을 averagePercent로 돌려준다.
        // 그런 재료를 하나도 쓰지 않으면 0이다.
        public static bool TryPlan(
            IReadOnlyList<CraftingIngredient> needs,
            IReadOnlyList<ItemStack> slots,
            Func<int, bool> carriesAccuracy,
            List<SlotRemoval> removals,
            out int averagePercent)
        {
            removals.Clear();
            averagePercent = 0;
            long accuracySum = 0;
            int accuracyCount = 0;

            for (int i = 0; i < needs.Count; i++)
            {
                int itemId = needs[i].ItemId;
                int remaining = Needed(needs, i);
                bool carries = carriesAccuracy != null && carriesAccuracy(itemId);

                for (int slot = 0; slot < slots.Count && remaining > 0; slot++)
                {
                    ItemStack stack = slots[slot];
                    if (stack.IsEmpty || stack.ItemId != itemId)
                    {
                        continue;
                    }

                    int take = Math.Min(remaining, stack.Count);
                    removals.Add(new SlotRemoval(slot, take));
                    remaining -= take;
                    if (carries)
                    {
                        accuracySum += (long)stack.Accuracy * take;
                        accuracyCount += take;
                    }
                }

                if (remaining > 0)
                {
                    removals.Clear();
                    averagePercent = 0;
                    return false;
                }
            }

            averagePercent = accuracyCount == 0 ? 0 : (int)((accuracySum + accuracyCount / 2) / accuracyCount);
            return true;
        }

        // 같은 아이템이 재료 목록에 여러 번 있으면 그 개수를 합쳐 한 번에 센다. 첫 항목에서만 합계를 돌려주고 나머지는 0이다.
        private static int Needed(IReadOnlyList<CraftingIngredient> needs, int index)
        {
            int itemId = needs[index].ItemId;
            for (int i = 0; i < index; i++)
            {
                if (needs[i].ItemId == itemId)
                {
                    return 0;
                }
            }

            int total = 0;
            for (int i = index; i < needs.Count; i++)
            {
                if (needs[i].ItemId == itemId)
                {
                    total += needs[i].Count;
                }
            }

            return total;
        }
    }
}
