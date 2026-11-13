using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Inventory;
using Blacksmith.Plaza;

namespace Blacksmith.Inventory
{
    // 약초·포션을 모험가에게 건네 부상수치를 줄인다. 아이템 하나가 인벤토리에서 빠진다. (docs/GAME_CONCEPT.md 5.1, 6.5)
    // 낮에 여관에서 건네는 화면이 부른다. 파견 창에서는 쓰지 않는다.
    public static class AdventurerTreatment
    {
        // 건넬 화면의 표시와 호스트의 확인이 같이 쓴다.
        public static bool CanTreat(ItemCatalog catalog, AdventurerData adventurer, ItemStack item)
        {
            ItemData data = item.IsEmpty ? null : catalog.Find(item.ItemId);
            return data != null && adventurer != null && InjuryRules.CanTreat(adventurer.Injury, data.InjuryRecovery);
        }

        // 호스트 전용.
        public static bool TryTreat(NetworkInventory from, int fromSlot, AdventurerRosterState roster, int adventurerId)
        {
            AdventurerData adventurer = roster.FindById(adventurerId);
            ItemStack item = from.Get(fromSlot);
            if (!roster.HasStateAuthority || !CanTreat(from.Catalog, adventurer, item))
            {
                return false;
            }

            int recovery = from.Catalog.Find(item.ItemId).InjuryRecovery;
            return from.TryRemove(fromSlot, 1, out _)
                && roster.TrySetInjury(adventurerId, InjuryRules.Recover(adventurer.Injury, recovery));
        }
    }
}
