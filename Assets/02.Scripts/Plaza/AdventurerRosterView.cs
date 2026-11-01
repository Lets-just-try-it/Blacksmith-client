using System.Collections.Generic;
using Blacksmith.Core.Adventurers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Plaza
{
    // 관리 중인 모험가 목록 창. 화면 버튼으로 열고 닫는다.
    public class AdventurerRosterView : MonoBehaviour
    {
        [SerializeField] private AdventurerRosterState _roster;
        [Tooltip("직업 이름을 찾는 데 쓴다")]
        [SerializeField] private AdventurerGenerationData _generation;
        [SerializeField] private AdventurerPortraitProvider _portraits;
        [SerializeField] private Button _toggleButton;
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _countText;
        [SerializeField] private Transform _rowRoot;
        [SerializeField] private AdventurerRosterRow _rowPrefab;

        private readonly List<AdventurerRosterRow> _rows = new List<AdventurerRosterRow>();

        private void Awake()
        {
            _toggleButton.onClick.AddListener(TogglePanel);
            _panel.SetActive(false);
        }

        private void OnDestroy()
        {
            _toggleButton.onClick.RemoveListener(TogglePanel);
        }

        private void OnEnable()
        {
            _roster.Changed += Refresh;
        }

        private void OnDisable()
        {
            _roster.Changed -= Refresh;
        }

        private void TogglePanel()
        {
            _panel.SetActive(!_panel.activeSelf);
            Refresh();
        }

        private void Refresh()
        {
            if (!_panel.activeSelf || !_roster.IsReady)
            {
                return;
            }

            _countText.text = $"관리 중인 모험가 {_roster.Count}/{_roster.SlotCount}";
            EnsureRowCount(_roster.SlotCount);
            for (int i = 0; i < _rows.Count; i++)
            {
                if (i < _roster.Count)
                {
                    AdventurerData adventurer = _roster.Get(i);
                    _rows[i].Show(adventurer, AdventurerLabels.GetJobName(_generation, adventurer.JobId), _portraits.GetPortrait(adventurer));
                }
                else
                {
                    _rows[i].ShowEmpty();
                }
            }
        }

        private void EnsureRowCount(int count)
        {
            while (_rows.Count < count)
            {
                _rows.Add(Instantiate(_rowPrefab, _rowRoot));
            }

            while (_rows.Count > count)
            {
                int last = _rows.Count - 1;
                Destroy(_rows[last].gameObject);
                _rows.RemoveAt(last);
            }
        }
    }
}
