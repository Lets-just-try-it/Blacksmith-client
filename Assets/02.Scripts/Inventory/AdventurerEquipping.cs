using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Inventory;
using Blacksmith.Plaza;

namespace Blacksmith.Inventory
{
    // 모험가에게 장비를 입히고 벗기는 일을 인벤토리와 관리 목록에 함께 반영한다. (docs/GAME_CONCEPT.md 5.4)
    // 입힌 장비는 쓴 내구도와 함께 모험가에게 저장된다. 다른 장비로 바꾸거나 벗기면 원래 장비는 공용 창고로 돌아간다.
    // 기본무기만 예외다. 바꾸면 사라지고, 벗길 수 없다. 다른 무기를 벗기면 직업의 기본무기로 돌아간다.
    public static class AdventurerEquipping
    {
        // 장비 창의 표시와 호스트의 확인이 같이 쓴다.
        public static bool CanEquip(ItemCatalog catalog, AdventurerGenerationData jobs, AdventurerData adventurer, ItemStack item, EquipmentSlot slot)
        {
            ItemData data = item.IsEmpty ? null : catalog.Find(item.ItemId);
            if (data == null || data.Kind != ItemKind.Equipment || adventurer == null)
            {
                return false;
            }

            AdventurerJobData job = jobs.FindJob(adventurer.JobId);
            WeaponType jobWeapon = job != null ? job.WeaponType : WeaponType.None;
            return EquipmentRules.CanEquip(slot, data.EquipmentSlot, data.WeaponType, jobWeapon);
        }

        // 장비 창의 표시와 호스트의 확인이 같이 쓴다. 기본무기를 찾지 못하면 무기를 벗기지 않는다.
        public static bool CanUnequip(AdventurerGenerationData jobs, AdventurerData adventurer, EquipmentSlot slot)
        {
            if (adventurer == null)
            {
                return false;
            }

            ItemStack defaultWeapon = jobs.GetDefaultWeapon(adventurer.JobId);
            if (slot == EquipmentSlot.Weapon && defaultWeapon.IsEmpty)
            {
                return false;
            }

            return EquipmentRules.CanUnequip(slot, EquipmentRules.GetEquipped(adventurer, slot).ItemId, defaultWeapon.ItemId);
        }

        // 호스트 전용. 바꾼 장비를 공용 창고에 넣을 자리가 없으면 아무것도 바꾸지 않는다.
        public static bool TryEquip(NetworkInventory from, int fromSlot, AdventurerRosterState roster, int adventurerId,
            EquipmentSlot slot, NetworkInventory storage, AdventurerGenerationData jobs)
        {
            AdventurerData adventurer = roster.FindById(adventurerId);
            if (!roster.HasStateAuthority || !CanEquip(from.Catalog, jobs, adventurer, from.Get(fromSlot), slot))
            {
                return false;
            }

            ItemStack previous = EquipmentRules.GetEquipped(adventurer, slot);
            if (slot == EquipmentSlot.Weapon && previous.ItemId == jobs.GetDefaultWeapon(adventurer.JobId).ItemId)
            {
                previous = ItemStack.Empty;
            }

            if (!previous.IsEmpty && !storage.CanAdd(previous))
            {
                return false;
            }

            if (!from.TryRemove(fromSlot, 1, out ItemStack equipped))
            {
                return false;
            }

            if (!previous.IsEmpty)
            {
                storage.TryAdd(previous);
            }

            return roster.TrySetEquipment(adventurerId, slot, equipped);
        }

        // 호스트 전용. 벗길 수 없거나 공용 창고에 넣을 자리가 없으면 벗기지 않는다.
        public static bool TryUnequip(AdventurerRosterState roster, int adventurerId, EquipmentSlot slot, NetworkInventory storage,
            AdventurerGenerationData jobs)
        {
            AdventurerData adventurer = roster.FindById(adventurerId);
            if (!roster.HasStateAuthority || !CanUnequip(jobs, adventurer, slot))
            {
                return false;
            }

            if (!storage.TryAdd(EquipmentRules.GetEquipped(adventurer, slot)))
            {
                return false;
            }

            ItemStack replacement = slot == EquipmentSlot.Weapon ? jobs.GetDefaultWeapon(adventurer.JobId) : ItemStack.Empty;
            return roster.TrySetEquipment(adventurerId, slot, replacement);
        }
    }
}
