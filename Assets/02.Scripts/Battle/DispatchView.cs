using System.Collections.Generic;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Battle;
using Blacksmith.Core.Inventory;
using Blacksmith.Inventory;
using Blacksmith.Network;
using Blacksmith.Plaza;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Battle
{
    // 밤 파견 창. 관리 중인 모험가에서 파티(최대 5명)를 고르고, 장비 착용 창으로 장비를 입히고,
    // 던전 단계와 어느 스테이지까지 싸울지 골라 파견한다 (docs/GAME_CONCEPT.md 5.3, 6.3, 7.3).
    // 모험가 능력치는 전투 유닛을 만드는 계산(AdventurerBattleData.ToSetup)으로 보여 주므로 장비 보정이 전투와 같다.
    // 창은 DispatchState.IsOpen을 따라 모든 참가자에게 열리고, 고르기는 호스트만 한다.
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
        [SerializeField] private GameObject _panel;
        [SerializeField] private Transform _rowRoot;
        [SerializeField] private DispatchAdventurerRow _rowPrefab;
        [SerializeField] private TMP_Text _partyText;
        [SerializeField] private TMP_Text _dungeonText;
        [SerializeField] private TMP_Text _stageText;
        [SerializeField] private TMP_Text _hintText;
        [SerializeField] private Button _previousTierButton;
        [SerializeField] private Button _nextTierButton;
        [SerializeField] private Button _previousStageButton;
        [SerializeField] private Button _nextStageButton;
        [SerializeField] private Button _dispatchButton;
        [SerializeField] private Button _closeButton;
        [Tooltip("테스트 씬에서 창을 여는 버튼. 비워 두면 낮·밤 사이클이 DispatchState.Open을 부른다")]
        [SerializeField] private Button _openButton;
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
            _previousTierButton.onClick.AddListener(ShowPreviousTier);
            _nextTierButton.onClick.AddListener(ShowNextTier);
            _previousStageButton.onClick.AddListener(ShowPreviousStage);
            _nextStageButton.onClick.AddListener(ShowNextStage);
            _dispatchButton.onClick.AddListener(Dispatch);
            _closeButton.onClick.AddListener(Close);
            if (_openButton != null)
            {
                _openButton.onClick.AddListener(Open);
            }
        }

        private void OnDisable()
        {
            _state.Changed -= Refresh;
            _roster.Changed -= Refresh;
            _progress.Changed -= Refresh;
            _previousTierButton.onClick.RemoveListener(ShowPreviousTier);
            _nextTierButton.onClick.RemoveListener(ShowNextTier);
            _previousStageButton.onClick.RemoveListener(ShowPreviousStage);
            _nextStageButton.onClick.RemoveListener(ShowNextStage);
            _dispatchButton.onClick.RemoveListener(Dispatch);
            _closeButton.onClick.RemoveListener(Close);
            if (_openButton != null)
            {
                _openButton.onClick.RemoveListener(Open);
            }

            SetShown(false);
        }

        private void Open()
        {
            _state.Open();
        }

        private void Close()
        {
            _state.Close();
        }

        private void ShowPreviousTier()
        {
            _state.ChangeTier(-1);
        }

        private void ShowNextTier()
        {
            _state.ChangeTier(1);
        }

        private void ShowPreviousStage()
        {
            _state.ChangeStage(-1);
        }

        private void ShowNextStage()
        {
            _state.ChangeStage(1);
        }

        private void Dispatch()
        {
            _equipView.Hide();
            _state.Dispatch();
        }

        private void Refresh()
        {
            bool isOpen = _state.IsReady && _roster.IsReady && _progress.IsReady && _state.IsOpen;
            SetShown(isOpen);
            if (_openButton != null)
            {
                _openButton.gameObject.SetActive(_state.CanEdit && !isOpen);
            }

            if (!isOpen)
            {
                return;
            }

            bool canEdit = _state.CanEdit;
            ShowRows(canEdit);
            ShowSelection(canEdit);
        }

        // 창이 열린 동안 캐릭터 조작을 잠근다. 창이 닫히면 장비 착용 창도 닫는다.
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
                return;
            }

            _inputLock.Release(this);
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
                bool canDispatch = DispatchRules.CanDispatch(adventurer.Condition);
                bool canSelect = canEdit && canDispatch
                    && (isInParty || DispatchRules.CanAddToParty(_state.PartyCount, BattleState.MaxPartySize));
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
                : $" · {AdventurerLabels.GetConditionName(adventurer.Condition)}";
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
            int clearedStages = _state.GetClearedStages();
            bool hasStage = _state.LastStage >= 0;
            int firstStage = DungeonProgressRules.FirstStageToFight(clearedStages);

            _partyText.text = $"파티 {_state.PartyCount}/{BattleState.MaxPartySize}";
            _dungeonText.text = $"{_state.TierIndex + 1}단계 {dungeon.DisplayName}";
            _stageText.text = !hasStage
                ? "모두 클리어"
                : firstStage == _state.LastStage
                    ? $"스테이지 {_state.LastStage + 1}{GetBossMark(dungeon, _state.LastStage)}"
                    : $"스테이지 {firstStage + 1} ~ {_state.LastStage + 1}{GetBossMark(dungeon, _state.LastStage)}";
            _hintText.text = canEdit
                ? "모험가를 눌러 파티에 넣고 빼기 · 장비 버튼으로 장비 입히기"
                : "파견은 호스트가 고른다";

            _previousTierButton.interactable = canEdit;
            _nextTierButton.interactable = canEdit;
            _previousStageButton.interactable = canEdit && hasStage && _state.LastStage > firstStage;
            _nextStageButton.interactable = canEdit && hasStage && _state.LastStage < dungeon.StageCount - 1;
            _dispatchButton.interactable = _state.CanDispatch();
            _closeButton.gameObject.SetActive(canEdit);
        }

        private static string GetBossMark(DungeonData dungeon, int stage)
        {
            return stage == dungeon.StageCount - 1 ? " (보스)" : "";
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
            _state.ToggleMember(row.AdventurerId);
        }

        private void HandleEquipClicked(DispatchAdventurerRow row)
        {
            _equipView.Show(row.AdventurerId);
        }
    }
}
