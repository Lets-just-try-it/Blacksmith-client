using System;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Inventory;
using UnityEngine;

namespace Blacksmith.Inventory
{
    // 장비가 모험가 능력치에 주는 보정의 등급 배수와 등급별 내구도 소모량이다. 아이템별 기본 보정과 최대 내구도는 ItemData에 있다.
    // 장비 착용 창과 전투(AdventurerBattleData)가 같은 에셋을 써서 화면 표시와 실제 전투 수치가 같다.
    [CreateAssetMenu(fileName = "EquipmentStats", menuName = "Blacksmith/Balance/Equipment Stats")]
    public class EquipmentStatData : ScriptableObject
    {
        [Serializable]
        private struct WeaponWear
        {
            public WeaponType Weapon;
            [Min(0)] public int Percent;
        }

        [SerializeField] private ItemCatalog _catalog;
        [Tooltip("등급별 배수(퍼센트). 첫 칸이 최하 등급이다")]
        [SerializeField] private int[] _gradePercents = { 60, 80, 100, 120, 150 };
        [Tooltip("무기는 공격 한 번, 투구·갑옷은 공격받기 한 번에 줄어드는 내구도. 소수 둘째 자리까지 쓴다. 첫 칸이 최하 등급이다")]
        [SerializeField] private float[] _wearPerUseByGrade = { 3f, 2f, 1.33f, 1f, 0.8f };
        [Tooltip("무기 종류별 내구도 소모 배율(퍼센트). 공격이 잦은 무기는 한 번에 덜 닳는다. 표에 없는 무기와 방어구는 100이다 (docs/GAME_CONCEPT.md 4.6)")]
        [SerializeField] private WeaponWear[] _weaponWearPercents =
        {
            new WeaponWear { Weapon = WeaponType.GreatSword, Percent = 100 },
            new WeaponWear { Weapon = WeaponType.Bow, Percent = 65 },
            new WeaponWear { Weapon = WeaponType.Staff, Percent = 75 },
        };

        // _wearPerUseByGrade를 규칙이 쓰는 0.01 단위 정수로 바꾼 값.
        [NonSerialized] private int[] _wearUnitsByGrade;

        private void OnValidate()
        {
            _wearUnitsByGrade = null;
        }

        public AdventurerStats GetItemBonus(ItemStack item)
        {
            ItemData data = item.IsEmpty ? null : _catalog.Find(item.ItemId);
            return data != null ? EquipmentStatRules.GetItemBonus(data.StatBonus, item.Grade, _gradePercents) : default;
        }

        public AdventurerStats GetEquipmentBonus(AdventurerData adventurer)
        {
            return EquipmentStatRules.GetEquipmentBonus(adventurer, GetItemBonus);
        }

        // 장비 하나의 내구도 상태. 닳지 않는 장비와 빈 칸은 MaxDurability가 0이다.
        public EquipmentDurability GetDurability(ItemStack item)
        {
            ItemData data = item.IsEmpty ? null : _catalog.Find(item.ItemId);
            if (data == null || data.MaxDurability <= 0)
            {
                return default;
            }

            int wearPerUse = DurabilityRules.GetWearPerUse(item.Grade, GetWearUnitsByGrade(), GetWeaponWearPercent(data.WeaponType));
            return new EquipmentDurability(item.Wear, data.MaxDurability, wearPerUse);
        }

        private int[] GetWearUnitsByGrade()
        {
            if (_wearUnitsByGrade == null)
            {
                _wearUnitsByGrade = new int[_wearPerUseByGrade.Length];
                for (int i = 0; i < _wearPerUseByGrade.Length; i++)
                {
                    _wearUnitsByGrade[i] = Mathf.RoundToInt(_wearPerUseByGrade[i] * DurabilityRules.WearUnitsPerDurability);
                }
            }

            return _wearUnitsByGrade;
        }

        private int GetWeaponWearPercent(WeaponType weapon)
        {
            foreach (WeaponWear entry in _weaponWearPercents)
            {
                if (entry.Weapon == weapon && weapon != WeaponType.None)
                {
                    return entry.Percent;
                }
            }

            return 100;
        }

        // slot 칸만 item으로 바꿨을 때의 장비 보정 합.
        public AdventurerStats GetEquipmentBonus(AdventurerData adventurer, EquipmentSlot slot, ItemStack item)
        {
            return EquipmentStatRules.GetEquipmentBonus(adventurer, GetItemBonus, slot, item);
        }
    }
}
