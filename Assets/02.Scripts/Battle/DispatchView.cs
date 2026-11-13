using System.Collections.Generic;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Battle;
using Blacksmith.Core.Inventory;
using Blacksmith.Forge;
using Blacksmith.Inventory;
using Blacksmith.Network;
using Blacksmith.Plaza;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Battle
{
    // 밤 파견 창. 던전 지도(DungeonMapView)에서 고른 스테이지로, 관리 중인 모험가에서 파티(최대 5명)를 고르고
    // 장비 착용 창으로 장비를 입혀 파견한다 (docs/GAME_CONCEPT.md 5.3, 6.3, 7.3). 스테이지를 바꾸려면 지도로 돌아간다.
    // 모험가 능력치는 전투 유닛을 만드는 계산(AdventurerBattleData.ToSetup)으로 보여 주므로 장비 보정이 전투와 같다.
    // 창은 DispatchState가 열려 있고 파티 고르기 단계일 때 모든 참가자에게 열리고, 고르기는 호스트만 한다.
    public class DispatchView : MonoBehaviour
    {
        [SerializeField] private DispatchState _state;
        [SerializeField] private AdventurerRosterState _roster;
        [SerializeField] private DungeonProgressState _progress;
        [Tooltip("직업 이름을 찾는 데 쓴다")]
        [SerializeField] private AdventurerGenerationData _jobs;
        [Tooltip("전투에 들어갈 능력치(레벨 + 장비 보정)를 계산한다")]
        [SerializeField] private AdventurerBattleData _adventurerBattle;
        [SerializeField] private AdventurerEquipView _equipView;
        [SerializeField] private PlayerNetworkInputLock _inputLock;
        [Tooltip("창이 열린 동안 하단 개인 슬롯을 숨기는 데 쓴다")]
        [SerializeField] private InventoryChannel _inventoryChannel;
        [Tooltip("파견할 수 없는 모험가를 눌렀을 때 이유를 띄운다. 없으면 띄우지 않는다")]
        [SerializeField] private ForgeNotice _notice;
        [SerializeField] private string _injuredMessage = "부상이 심해 파견할 수 없습니다.";
        [SerializeField] private GameObject _panel;
        [SerializeField] private Transform _rowRoot;
        [SerializeField] private DispatchAdventurerRow _rowPrefab;
        [SerializeField] private TMP_Text _partyText;
        [SerializeField] private TMP_Text _dungeonText;
        [SerializeField] private TMP_Text _stageText;
        [SerializeField] private TMP_Text _hintText;
        [Tooltip("던전 지도로 돌아가 스테이지를 다시 고른다")]
        [SerializeField] private Button _mapButton;
        [SerializeField] private Button _dispatchButton;
        [Tooltip("파견하지 않고 다음 날로 넘긴다")]
        [SerializeField] private Button _skipButton;
        [SerializeField] private Color _rowColor = new Color(0.18f, 0.16f, 0.14f, 1f);
        [SerializeField] private Color _selectedRowColor = new Color(0.55f, 0.42f, 0.18f, 1f);
        [SerializeField] private Color _unavailableRowColor = new Color(0.25f, 0.12f, 0.12f, 1f);

        private readonly List<DispatchAdventurerRow> _rows = new List<DispatchAdventurerRow>();
        private bool _isShown;

        private void Awake()
        {
            _panel.SetActive(false);
        }

        private void OnEnable()
        {
            _state.Changed += Refresh;
            _roster.Changed += Refresh;
            _progress.Changed += Refresh;
            _mapButton.onClick.AddListener(ShowMap);
            _dispatchButton.onClick.AddListener(Dispatch);
            _skipButton.onClick.AddListener(Skip);
        }

        private void OnDisable()
        {
            _state.Changed -= Refresh;
            _roster.Changed -= Refresh;
            _progress.Changed -= Refresh;
            _mapButton.onClick.RemoveListener(ShowMap);
            _dispatchButton.onClick.RemoveListener(Dispatch);
            _skipButton.onClick.RemoveListener(Skip);
            SetShown(false);
        }

        private void Skip()
        {
            _state.Skip();
        }

        private void ShowMap()
        {
            _state.ShowMap();
        }

        private void Dispatch()
        {
            _equipView.Hide();
            _state.Dispatch();
        }

        private void Refresh()
        {
            bool isOpen = _state.IsReady && _roster.IsReady && _progress.IsReady && _state.IsOpen;
            SetShown(isOpen && _state.Screen == DispatchScreen.Party);

            if (!_isShown)
            {
                return;
            }

            bool canEdit = _state.CanEdit;
            ShowRows(canEdit);
            ShowSelection(canEdit);
        }

        // 창이 열린 동안 캐릭터 조작을 잠그고 하단 개인 슬롯을 숨긴다. 창이 닫히면 장비 착용 창도 닫는다.
        private void SetShown(bool isShown)
        {
            if (_isShown == isShown)
            {
                return;
            }

            _isShown = isShown;
            if (_panel != null)
            {
                _panel.SetActive(isShown);
            }

            if (isShown)
            {
                _inputLock.Lock(this);
                _inventoryChannel.HideHud(this);
                return;
            }

            _inputLock.Release(this);
            _inventoryChannel.ShowHud(this);
            if (_equipView != null)
            {
                _equipView.Hide();
            }
        }

        private void ShowRows(bool canEdit)
        {
            EnsureRowCount(_roster.Count);
            for (int i = 0; i < _roster.Count; i++)
            {
                AdventurerData adventurer = _roster.Get(i);
                bool isInParty = _state.IsInParty(adventurer.AdventurerId);
                bool canDispatch = _state.IsDispatchable(adventurer);
                // 파견할 수 없는 모험가도 누를 수 있게 두고, 누르면 이유를 띄운다.
                bool canSelect = canEdit
                    && (!canDispatch || isInParty || DispatchRules.CanAddToParty(_state.PartyCount, BattleState.MaxPartySize));
                Color color = !canDispatch ? _unavailableRowColor : isInParty ? _selectedRowColor : _rowColor;
                _rows[i].Show(adventurer.AdventurerId, GetNameLine(adventurer, isInParty), GetStatsLine(adventurer), color, canSelect);
            }
        }

        private string GetNameLine(AdventurerData adventurer, bool isInParty)
        {
            string jobName = AdventurerLabels.GetJobName(_jobs, adventurer.JobId);
            string mark = isInParty ? "[파견] " : "";
            string condition = adventurer.Condition == AdventurerCondition.Healthy
                ? ""
                : $" · {AdventurerLabels.FormatCondition(adventurer)}";
            return $"{mark}{adventurer.Name} · {jobName} Lv{adventurer.Level}{condition}";
        }

        // 전투 유닛과 같은 계산이라 장비를 바꾸면 이 값도 바로 바뀐다.
        private string GetStatsLine(AdventurerData adventurer)
        {
            UnitStats stats = _adventurerBattle.ToSetup(adventurer).Stats;
            int equippedCount = 0;
            foreach (EquipmentSlot slot in EquipmentStatRules.Slots)
            {
                if (!EquipmentRules.GetEquipped(adventurer, slot).IsEmpty)
                {
                    equippedCount++;
                }
            }

            return $"체력 {stats.MaxHp}  공격력 {stats.Attack}  방어력 {stats.Defense}  · 장비 {equippedCount}/{EquipmentStatRules.Slots.Length}";
        }

        private void ShowSelection(bool canEdit)
        {
            DungeonData dungeon = _state.SelectedDungeon;
            _partyText.text = $"파티 {_state.PartyCount}/{BattleState.MaxPartySize}";
            _dungeonText.text = $"{_state.TierIndex + 1}단계 {dungeon.DisplayName}";
            _stageText.text = _state.Stage < 0
                ? "고를 스테이지 없음"
                : $"{DungeonMapLabels.GetStageName(dungeon, _state.Stage)}{(_state.IsReplay() ? " (다시 도전)" : "")}";
            _hintText.text = canEdit
                ? "모험가를 눌러 파티에 넣고 빼기 · 장비 버튼으로 장비 입히기"
                : "파견은 호스트가 고른다";

            _mapButton.gameObject.SetActive(canEdit && _state.HasMap);
            _dispatchButton.interactable = _state.CanDispatch();
            _skipButton.gameObject.SetActive(canEdit);
        }

        private void EnsureRowCount(int count)
        {
            while (_rows.Count < count)
            {
                DispatchAdventurerRow row = Instantiate(_rowPrefab, _rowRoot);
                row.SelectClicked += HandleSelectClicked;
                row.EquipClicked += HandleEquipClicked;
                _rows.Add(row);
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                _rows[i].gameObject.SetActive(i < count);
            }
        }

        private void HandleSelectClicked(DispatchAdventurerRow row)
        {
            AdventurerData adventurer = _roster.FindById(row.AdventurerId);
            if (adventurer != null && !_state.IsDispatchable(adventurer))
            {
                if (_notice != null)
                {
                    _notice.Show(_injuredMessage);
                }

                return;
            }

            _state.ToggleMember(row.AdventurerId);
        }

        private void HandleEquipClicked(DispatchAdventurerRow row)
        {
            _equipView.Show(row.AdventurerId);
        }
    }
}
