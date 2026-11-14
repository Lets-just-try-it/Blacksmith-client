using System;
using System.Collections.Generic;
using Blacksmith.Battle;
using Blacksmith.Core.Adventurers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Plaza
{
    // 관리 중인 모험가 목록 창. 화면 버튼으로 열고 닫는다.
    // 파견 파티에 든 모험가를 표시하고, "빼기"로 파티에서 뺄 수 있다(파티가 가득 찼을 때 다른 모험가를 넣으려고).
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
        [Tooltip("파견 파티 표시와 빼기에 쓴다. 파견이 없는 테스트 씬에서는 비워 둔다")]
        [SerializeField] private DispatchState _dispatch;
        [SerializeField] private InnResidents _inn;
        [SerializeField] private InnChannel _innChannel;

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
            if (_dispatch != null)
            {
                _dispatch.Changed += Refresh;
            }
        }

        private void OnDisable()
        {
            _roster.Changed -= Refresh;
            if (_dispatch != null)
            {
                _dispatch.Changed -= Refresh;
            }
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
                    _rows[i].Show(adventurer, AdventurerLabels.GetJobName(_generation, adventurer.JobId), _portraits.GetPortrait(adventurer),
                        AdventurerLabels.FormatAffinity(adventurer.Affinity, _roster.AffinityBalance), GetLeaveParty(adventurer.AdventurerId));
                }
                else
                {
                    _rows[i].ShowEmpty();
                }
            }
        }

        // 파티에 없거나 뺄 수 없는 씬이면 null이다.
        private Action GetLeaveParty(int adventurerId)
        {
            if (_dispatch == null || !_dispatch.IsReady || !_dispatch.IsInParty(adventurerId) || _inn == null)
            {
                return null;
            }

            return () =>
            {
                if (_innChannel.LocalActions != null)
                {
                    _innChannel.LocalActions.RequestLeaveParty(_inn, adventurerId);
                }
            };
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
