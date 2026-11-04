using Blacksmith.Core.Adventurers;

namespace Blacksmith.Core.Inventory
{
    // 장비가 들어가는 칸. 아이템 정의와 모험가의 장비 칸이 같이 쓴다.
    public enum EquipmentSlot
    {
        None,
        Weapon,
        Helmet,
        Armor,
        Accessory,
    }

    // 무기 종류. 직업마다 쓸 수 있는 무기 종류가 하나씩 정해져 있다.
    public enum WeaponType
    {
        None,
        GreatSword,
        Bow,
        Staff,
    }

    // 모험가에게 장비를 입힐 수 있는지 정한다. 장비 창의 표시와 호스트의 확인이 같은 함수를 쓴다.
    public static class EquipmentRules
    {
        // 장비 부위가 칸과 같아야 한다. 무기 칸은 그 모험가의 직업이 쓰는 무기 종류만 받는다. 투구·갑옷·장신구는 직업을 가리지 않는다.
        public static bool CanEquip(EquipmentSlot target, EquipmentSlot itemSlot, WeaponType itemWeapon, WeaponType jobWeapon)
        {
            if (target == EquipmentSlot.None || itemSlot != target)
            {
                return false;
            }

            return target != EquipmentSlot.Weapon || (itemWeapon != WeaponType.None && itemWeapon == jobWeapon);
        }

        // 모험가가 그 칸에 입은 장비. 없으면 Empty다.
        public static ItemStack GetEquipped(AdventurerData adventurer, EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon:
                    return ToStack(adventurer.WeaponId, adventurer.WeaponGrade);
                case EquipmentSlot.Helmet:
                    return ToStack(adventurer.HelmetId, adventurer.HelmetGrade);
                case EquipmentSlot.Armor:
                    return ToStack(adventurer.ArmorId, adventurer.ArmorGrade);
                case EquipmentSlot.Accessory:
                    return ToStack(adventurer.AccessoryId, adventurer.AccessoryGrade);
                default:
                    return ItemStack.Empty;
            }
        }

        private static ItemStack ToStack(int itemId, int grade)
        {
            return itemId == AdventurerData.NoEquipment ? ItemStack.Empty : new ItemStack(itemId, grade, 1);
        }
    }
}
