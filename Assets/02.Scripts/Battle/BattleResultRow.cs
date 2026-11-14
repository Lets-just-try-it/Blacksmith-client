using System.Collections.Generic;
using Blacksmith.Core.Battle;
using TMPro;
using UnityEngine;

namespace Blacksmith.Battle
{
    public class BattleResultRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _damageText;

        public void Show(string adventurerName, AdventurerBattleResult result, AdventurerReward reward)
        {
            _nameText.text = adventurerName;
            string status = result.IsDead ? "사망" : FormatInjury(result);
            string broken = result.IsDead ? "" : FormatBroken(result);
            _statusText.text = broken.Length > 0 ? $"{status}({broken})" : status;
            string bonus = reward.ExperienceBonus > 0 ? $"(사파이어 +{reward.ExperienceBonus})" : "";
            _damageText.text = $"피해량 {result.DamageDealt} · 경험치 +{reward.ExperienceGained}{bonus}{FormatLevelUp(reward)}{FormatAffinityEvents(result)}";
        }

        // 친밀도 전투 이벤트를 짧게 덧붙인다. 예: " · 맹세: 사망 면제" (docs/GAME_CONCEPT.md 6.6)
        private static string FormatAffinityEvents(AdventurerBattleResult result)
        {
            var parts = new List<string>();
            if (result.IsSkillBoosted)
            {
                parts.Add("스킬 강화");
            }

            if (result.IsFastRecovered)
            {
                parts.Add("빠른 회복");
            }

            if (result.IsDeathExempted)
            {
                parts.Add("사망 면제");
            }

            return parts.Count > 0 ? $" · 친밀도: {string.Join("·", parts)}" : "";
        }

        // 예: "부상 91(+51)". 이번 전투에서 다치지 않았으면 늘어난 값을 적지 않는다.
        private static string FormatInjury(AdventurerBattleResult result)
        {
            if (result.Injury == 0)
            {
                return "건강";
            }

            return result.InjuryGained > 0 ? $"부상 {result.Injury}(+{result.InjuryGained})" : $"부상 {result.Injury}";
        }

        // 예: "무기·갑옷 파손". 사망하면 장비가 모두 사라지므로 부르지 않는다.
        private static string FormatBroken(AdventurerBattleResult result)
        {
            var parts = new List<string>();
            if (result.Weapon.IsBroken)
            {
                parts.Add("무기");
            }

            if (result.Helmet.IsBroken)
            {
                parts.Add("투구");
            }

            if (result.Armor.IsBroken)
            {
                parts.Add("갑옷");
            }

            if (result.IsAccessoryBroken)
            {
                parts.Add("목걸이");
            }

            return parts.Count > 0 ? $"{string.Join("·", parts)} 파손" : "";
        }

        private static string FormatLevelUp(AdventurerReward reward)
        {
            return reward.IsLevelUp ? $" · Lv{reward.PreviousLevel} → {reward.Progress.Level}" : "";
        }
    }
}
