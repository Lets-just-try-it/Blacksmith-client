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

        public void Show(string adventurerName, AdventurerBattleResult result)
        {
            _nameText.text = adventurerName;
            _statusText.text = result.IsDead ? "사망" : result.IsInjured ? "부상" : "생존";
            _damageText.text = $"피해량 {result.DamageDealt}";
        }
    }
}
