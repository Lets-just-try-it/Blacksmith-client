using System;
using System.Collections.Generic;
using System.Text;
using Blacksmith.Core.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Battle
{
    public class BattleResultView : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _stageText;
        [SerializeField] private Transform _rowRoot;
        [SerializeField] private BattleResultRow _rowPrefab;
        [SerializeField] private Button _continueButton;

        private readonly List<BattleResultRow> _rows = new List<BattleResultRow>();

        public event Action ContinueClicked;

        private void Awake()
        {
            _continueButton.onClick.AddListener(HandleContinueClicked);
            _panel.SetActive(false);
        }

        private void OnDestroy()
        {
            _continueButton.onClick.RemoveListener(HandleContinueClicked);
        }

        // stageName은 싸운 스테이지 이름, waveCount는 그 스테이지의 웨이브 수다.
        // rewards.Adventurers는 result.Adventurers와 같은 순서다.
        // 다음으로 넘어가는 것은 호스트만 할 수 있어 canContinue가 false면 버튼을 숨긴다.
        public void Show(BattleResult result, BattleRewards rewards, string dungeonName, string stageName, int waveCount,
            Func<int, string> getAdventurerName, Func<int, string> getItemName, bool canContinue)
        {
            _continueButton.gameObject.SetActive(canContinue);
            ClearRows();
            _titleText.text = result.IsVictory ? "승리" : "패배";
            _stageText.text = $"{dungeonName} {stageName} · 웨이브 {result.WavesCleared}/{waveCount} 클리어\n{FormatRewards(rewards, getItemName)}";

            for (int i = 0; i < result.Adventurers.Count; i++)
            {
                AdventurerBattleResult adventurer = result.Adventurers[i];
                BattleResultRow row = Instantiate(_rowPrefab, _rowRoot);
                row.Show(getAdventurerName(adventurer.AdventurerId), adventurer, rewards.Adventurers[i]);
                _rows.Add(row);
            }

            _panel.SetActive(true);
        }

        public void Hide()
        {
            _panel.SetActive(false);
        }

        // 장신구 효과로 늘어난 값은 괄호로 따로 보여 준다.
        private static string FormatRewards(BattleRewards rewards, Func<int, string> getItemName)
        {
            var text = new StringBuilder($"보상: 골드 {rewards.Gold}");
            if (rewards.GoldBonus > 0)
            {
                text.Append($"(토파즈 +{rewards.GoldBonus})");
            }

            foreach (MaterialReward material in rewards.Materials)
            {
                text.Append($", {getItemName(material.MaterialId)} ×{material.Count}");
                int bonus = FindCount(rewards.BonusMaterials, material.MaterialId);
                if (bonus > 0)
                {
                    text.Append($"(에메랄드 +{bonus})");
                }
            }

            return text.ToString();
        }

        private static int FindCount(IReadOnlyList<MaterialReward> materials, int materialId)
        {
            foreach (MaterialReward material in materials)
            {
                if (material.MaterialId == materialId)
                {
                    return material.Count;
                }
            }

            return 0;
        }

        private void HandleContinueClicked()
        {
            Hide();
            ContinueClicked?.Invoke();
        }

        private void ClearRows()
        {
            foreach (BattleResultRow row in _rows)
            {
                Destroy(row.gameObject);
            }

            _rows.Clear();
        }
    }
}
