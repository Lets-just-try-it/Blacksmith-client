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

        // firstStage, lastStage는 0부터 센 번호다. 화면에는 1부터 보여 준다.
        // rewards.Adventurers는 result.Adventurers와 같은 순서다.
        public void Show(BattleResult result, BattleRewards rewards, string dungeonName, int firstStage, int lastStage,
            Func<int, string> getAdventurerName)
        {
            ClearRows();
            _titleText.text = result.IsVictory ? "승리" : "패배";
            string range = firstStage == lastStage ? $"{lastStage + 1}" : $"{firstStage + 1}~{lastStage + 1}";
            _stageText.text = $"{dungeonName} 스테이지 {range} 중 {result.StagesCleared}개 클리어\n{FormatRewards(rewards)}";

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

        // 재료 이름은 대장간의 재료 데이터가 생기면 번호 대신 쓴다.
        private static string FormatRewards(BattleRewards rewards)
        {
            var text = new StringBuilder($"보상: 골드 {rewards.Gold}");
            foreach (MaterialReward material in rewards.Materials)
            {
                text.Append($", 재료 #{material.MaterialId} ×{material.Count}");
            }

            return text.ToString();
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
