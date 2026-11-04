using System;
using System.Collections.Generic;
using Blacksmith.Core.Adventurers;

namespace Blacksmith.Core.Inventory
{
    // 장비가 모험가 능력치에 더하는 보정이다. 장비 착용 창의 표시와 전투 유닛 만들기가 같은 함수를 쓴다.
    public static class EquipmentStatRules
    {
        public static readonly EquipmentSlot[] Slots =
        {
            EquipmentSlot.Helmet,
            EquipmentSlot.Armor,
            EquipmentSlot.Weapon,
            EquipmentSlot.Accessory,
        };

        // 장비 하나의 보정 = 아이템 기본 보정 × 등급 배수(퍼센트, 소수점 아래 버림).
        // gradePercents[0]이 최하 등급(GradeRule.MinGrade)의 배수다. 등급이 표 밖이면 100%로 본다.
        public static AdventurerStats GetItemBonus(AdventurerStats itemBonus, int grade, IReadOnlyList<int> gradePercents)
        {
            int index = grade - GradeRule.MinGrade;
            int percent = index >= 0 && index < gradePercents.Count ? gradePercents[index] : 100;
            return new AdventurerStats
            {
                MaxHp = itemBonus.MaxHp * percent / 100,
                Attack = itemBonus.Attack * percent / 100,
                Defense = itemBonus.Defense * percent / 100,
            };
        }

        // 모험가가 입은 장비 보정의 합. bonusOf는 장비 하나의 보정을 돌려준다.
        public static AdventurerStats GetEquipmentBonus(AdventurerData adventurer, Func<ItemStack, AdventurerStats> bonusOf)
        {
            return GetEquipmentBonus(adventurer, bonusOf, EquipmentSlot.None, ItemStack.Empty);
        }

        // replaced 칸만 item으로 바꿨을 때의 장비 보정 합. 장비 창이 입히기 전에 변화량을 보여 줄 때 쓴다.
        public static AdventurerStats GetEquipmentBonus(AdventurerData adventurer, Func<ItemStack, AdventurerStats> bonusOf,
            EquipmentSlot replaced, ItemStack item)
        {
            AdventurerStats total = default;
            foreach (EquipmentSlot slot in Slots)
            {
                ItemStack equipped = slot == replaced ? item : EquipmentRules.GetEquipped(adventurer, slot);
                if (!equipped.IsEmpty)
                {
                    total = AdventurerStatRules.Add(total, bonusOf(equipped));
                }
            }

            return total;
        }
    }
}
