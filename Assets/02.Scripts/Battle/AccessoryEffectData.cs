using System;
using System.Collections.Generic;
using Blacksmith.Core;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Battle;
using Blacksmith.Inventory;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 목걸이 효과 수치다. 효과 종류는 목걸이 아이템(ItemData)이, 티어는 아이템 티어가, 등급은 입은 목걸이 등급이 정한다.
    // (docs/GAME_CONCEPT.md 4.4 "장신구 효과")
    [CreateAssetMenu(fileName = "AccessoryEffects", menuName = "Blacksmith/Balance/Accessory Effects")]
    public class AccessoryEffectData : ScriptableObject
    {
        [Serializable]
        private class TierEntry
        {
            [Tooltip("등급별 효과(퍼센트). 첫 칸이 최하 등급이다")]
            public int[] GradePercents;
        }

        [Serializable]
        private class EffectEntry
        {
            public AccessoryEffectKind Kind;
            [Tooltip("첫 항목이 1티어다")]
            public List<TierEntry> Tiers = new List<TierEntry>();
        }

        [SerializeField] private ItemCatalog _catalog;
        [SerializeField] private List<EffectEntry> _effects = new List<EffectEntry>();

        // 장신구가 없거나 효과가 없는 장신구면 Kind가 None이다.
        public AccessoryEffect GetEffect(AdventurerData adventurer)
        {
            ItemData item = adventurer.AccessoryId == AdventurerData.NoEquipment ? null : _catalog.Find(adventurer.AccessoryId);
            if (item == null || item.AccessoryEffect == AccessoryEffectKind.None)
            {
                return default;
            }

            EffectEntry entry = _effects.Find(effect => effect.Kind == item.AccessoryEffect);
            int tierIndex = item.Tier - 1;
            if (entry == null || tierIndex < 0 || tierIndex >= entry.Tiers.Count)
            {
                Debug.LogWarning($"장신구 효과 수치가 없다: {item.name} ({item.AccessoryEffect}, {item.Tier}티어)", this);
                return default;
            }

            int[] percents = entry.Tiers[tierIndex].GradePercents;
            int gradeIndex = Mathf.Clamp(adventurer.AccessoryGrade - GradeRule.MinGrade, 0, percents.Length - 1);
            return new AccessoryEffect(item.AccessoryEffect, percents[gradeIndex]);
        }
    }
}
