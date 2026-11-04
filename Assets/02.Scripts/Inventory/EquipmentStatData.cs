using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Inventory;
using UnityEngine;

namespace Blacksmith.Inventory
{
    // 장비가 모험가 능력치에 주는 보정의 등급 배수다. 아이템별 기본 보정은 ItemData에 있다.
    // 장비 착용 창과 전투(AdventurerBattleData)가 같은 에셋을 써서 화면 표시와 실제 전투 수치가 같다.
    [CreateAssetMenu(fileName = "EquipmentStats", menuName = "Blacksmith/Balance/Equipment Stats")]
    public class EquipmentStatData : ScriptableObject
    {
        [SerializeField] private ItemCatalog _catalog;
        [Tooltip("등급별 배수(퍼센트). 첫 칸이 최하 등급이다")]
        [SerializeField] private int[] _gradePercents = { 60, 80, 100, 120, 150 };

        public AdventurerStats GetItemBonus(ItemStack item)
        {
            ItemData data = item.IsEmpty ? null : _catalog.Find(item.ItemId);
            return data != null ? EquipmentStatRules.GetItemBonus(data.StatBonus, item.Grade, _gradePercents) : default;
        }

        public AdventurerStats GetEquipmentBonus(AdventurerData adventurer)
        {
            return EquipmentStatRules.GetEquipmentBonus(adventurer, GetItemBonus);
        }

        // slot 칸만 item으로 바꿨을 때의 장비 보정 합.
        public AdventurerStats GetEquipmentBonus(AdventurerData adventurer, EquipmentSlot slot, ItemStack item)
        {
            return EquipmentStatRules.GetEquipmentBonus(adventurer, GetItemBonus, slot, item);
        }
    }
}
