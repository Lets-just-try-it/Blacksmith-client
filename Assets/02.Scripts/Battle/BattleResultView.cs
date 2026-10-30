using System;
using System.Collections.Generic;
using Blacksmith.Core.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Battle
{
    // [담당자 결정] 보상과 경험치 표시는 보상 내용이 정해지면 추가한다.
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
        public void Show(BattleResult result, string dungeonName, int firstStage, int lastStage, Func<int, string> getAdventurerName)
        {
            ClearRows();
            _titleText.text = result.IsVictory ? "승리" : "패배";
            string range = firstStage == lastStage ? $"{lastStage + 1}" : $"{firstStage + 1}~{lastStage + 1}";
            _stageText.text = $"{dungeonName} 스테이지 {range} 중 {result.StagesCleared}개 클리어";

            foreach (AdventurerBattleResult adventurer in result.Adventurers)
            {
                BattleResultRow row = Instantiate(_rowPrefab, _rowRoot);
                row.Show(getAdventurerName(adventurer.AdventurerId), adventurer);
                _rows.Add(row);
            }

            _panel.SetActive(true);
        }

        public void Hide()
        {
            _panel.SetActive(false);
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
