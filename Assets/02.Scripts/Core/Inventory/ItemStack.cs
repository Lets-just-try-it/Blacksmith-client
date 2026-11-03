using System;

namespace Blacksmith.Core.Inventory
{
    // 인벤토리 칸 하나에 든 물건이다. 같은 아이템이라도 등급이 다르면 다른 물건으로 본다.
    public readonly struct ItemStack : IEquatable<ItemStack>
    {
        public const int NoItemId = 0;

        public static readonly ItemStack Empty = default;

        public int ItemId { get; }
        public int Grade { get; }
        public int Count { get; }

        public bool IsEmpty => ItemId == NoItemId || Count <= 0;

        public ItemStack(int itemId, int grade, int count)
        {
            ItemId = itemId;
            Grade = grade;
            Count = count;
        }

        public bool IsSameItem(ItemStack other)
        {
            return !IsEmpty && !other.IsEmpty && ItemId == other.ItemId && Grade == other.Grade;
        }

        public ItemStack WithCount(int count)
        {
            return count <= 0 ? Empty : new ItemStack(ItemId, Grade, count);
        }

        public bool Equals(ItemStack other)
        {
            if (IsEmpty || other.IsEmpty)
            {
                return IsEmpty && other.IsEmpty;
            }

            return ItemId == other.ItemId && Grade == other.Grade && Count == other.Count;
        }

        public override bool Equals(object obj)
        {
            return obj is ItemStack other && Equals(other);
        }

        public override int GetHashCode()
        {
            return IsEmpty ? 0 : HashCode.Combine(ItemId, Grade, Count);
        }

        public override string ToString()
        {
            return IsEmpty ? "Empty" : $"{ItemId} G{Grade} x{Count}";
        }
    }
}
