using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Inventory;
using Blacksmith.Plaza;

namespace Blacksmith.Inventory
{
    // 모험가에게 장비를 입히고 벗기는 일을 인벤토리와 관리 목록에 함께 반영한다.
    // 입힌 장비는 인벤토리에서 빠져 모험가에게 저장된다. 다른 장비로 바꾸면 원래 장비는 사라진다(기본무기도 같다).
    // returnReplaced를 켜면 바꾼 장비를 공용 창고로 돌려보낸다. 기획이 바뀔 때를 위해 남겨 둔 방식이다.
    // 벗긴 장비는 공용 창고로 돌아간다. 기획에서는 벗기기가 빠졌고(교체만 가능), 기능 제거는 따로 한다.
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

        // 호스트 전용. returnReplaced일 때 바꾼 장비를 공용 창고에 넣을 자리가 없으면 아무것도 바꾸지 않는다.
        public static bool TryEquip(NetworkInventory from, int fromSlot, AdventurerRosterState roster, int adventurerId,
            EquipmentSlot slot, NetworkInventory storage, AdventurerGenerationData jobs, bool returnReplaced)
        {
            AdventurerData adventurer = roster.FindById(adventurerId);
            if (!roster.HasStateAuthority || !CanEquip(from.Catalog, jobs, adventurer, from.Get(fromSlot), slot))
            {
                return false;
            }

            ItemStack previous = returnReplaced ? EquipmentRules.GetEquipped(adventurer, slot) : ItemStack.Empty;
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

            return roster.TrySetEquipment(adventurerId, slot, equipped.ItemId, equipped.Grade);
        }

        // 호스트 전용. 공용 창고에 넣을 자리가 없으면 벗기지 않는다.
        public static bool TryUnequip(AdventurerRosterState roster, int adventurerId, EquipmentSlot slot, NetworkInventory storage)
        {
            AdventurerData adventurer = roster.FindById(adventurerId);
            ItemStack previous = adventurer != null ? EquipmentRules.GetEquipped(adventurer, slot) : ItemStack.Empty;
            if (!roster.HasStateAuthority || previous.IsEmpty || !storage.TryAdd(previous))
            {
                return false;
            }

            return roster.TrySetEquipment(adventurerId, slot, AdventurerData.NoEquipment, 0);
        }
    }
}
