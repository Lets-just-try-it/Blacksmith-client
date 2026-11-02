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
            _statusText.text = result.IsDead ? "사망" : result.IsInjured ? "부상" : "생존";
            _damageText.text = $"피해량 {result.DamageDealt} · 경험치 +{reward.ExperienceGained}{FormatLevelUp(reward)}";
        }

        private static string FormatLevelUp(AdventurerReward reward)
        {
            return reward.IsLevelUp ? $" · Lv{reward.PreviousLevel} → {reward.Progress.Level}" : "";
        }
    }
}
