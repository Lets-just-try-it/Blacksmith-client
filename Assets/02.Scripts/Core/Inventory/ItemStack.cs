using System;

namespace Blacksmith.Core.Inventory
{
    // 인벤토리 칸 하나에 든 물건이다. 같은 아이템이라도 등급이나 정확도가 다르면 다른 물건으로 본다.
    // Grade는 완성된 장비의 등급(1~5)이고, Accuracy는 제작 중간 형태가 지금까지 쌓은 정확도 평균(0~100 퍼센트)이다.
    // 재료와 완성 장비의 Accuracy는 0이다.
    // Wear는 무기·방어구가 지금까지 쓴 내구도다. 새 장비는 0이고, 최대 내구도는 아이템 데이터에 있다(DurabilityRules).
    public readonly struct ItemStack : IEquatable<ItemStack>
    {
        public const int NoItemId = 0;

        public static readonly ItemStack Empty = default;

        public int ItemId { get; }
        public int Grade { get; }
        public int Count { get; }
        public int Accuracy { get; }
        public int Wear { get; }

        public bool IsEmpty => ItemId == NoItemId || Count <= 0;

        public ItemStack(int itemId, int grade, int count, int accuracy = 0, int wear = 0)
        {
            ItemId = itemId;
            Grade = grade;
            Count = count;
            Accuracy = accuracy;
            Wear = wear;
        }

        public bool IsSameItem(ItemStack other)
        {
            return !IsEmpty && !other.IsEmpty && ItemId == other.ItemId && Grade == other.Grade && Accuracy == other.Accuracy && Wear == other.Wear;
        }

        public ItemStack WithCount(int count)
        {
            return count <= 0 ? Empty : new ItemStack(ItemId, Grade, count, Accuracy, Wear);
        }

        public bool Equals(ItemStack other)
        {
            if (IsEmpty || other.IsEmpty)
            {
                return IsEmpty && other.IsEmpty;
            }

            return ItemId == other.ItemId && Grade == other.Grade && Accuracy == other.Accuracy && Wear == other.Wear && Count == other.Count;
        }

        public override bool Equals(object obj)
        {
            return obj is ItemStack other && Equals(other);
        }

        public override int GetHashCode()
        {
            return IsEmpty ? 0 : HashCode.Combine(ItemId, Grade, Accuracy, Wear, Count);
        }

        public override string ToString()
        {
            if (IsEmpty)
            {
                return "Empty";
            }

            string accuracy = Accuracy > 0 ? $" A{Accuracy}" : "";
            string wear = Wear > 0 ? $" W{Wear}" : "";
            return $"{ItemId} G{Grade}{accuracy}{wear} x{Count}";
        }
    }
}
