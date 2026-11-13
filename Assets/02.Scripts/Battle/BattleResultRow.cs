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
            _statusText.text = result.IsAccessoryBroken ? $"{status}(목걸이 파손)" : status;
            string bonus = reward.ExperienceBonus > 0 ? $"(사파이어 +{reward.ExperienceBonus})" : "";
            _damageText.text = $"피해량 {result.DamageDealt} · 경험치 +{reward.ExperienceGained}{bonus}{FormatLevelUp(reward)}";
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

        private static string FormatLevelUp(AdventurerReward reward)
        {
            return reward.IsLevelUp ? $" · Lv{reward.PreviousLevel} → {reward.Progress.Level}" : "";
        }
    }
}
