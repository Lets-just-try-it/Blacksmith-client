using System.Collections.Generic;
using System.Text;
using Blacksmith.Core.Forge;
using Blacksmith.Core.Inventory;
using Blacksmith.Inventory;
using Blacksmith.Network;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Blacksmith.Forge
{
    // 모든 작업대가 함께 쓰는 제작 목록 창이다. 작업대를 열면 그 작업대가 하는 제작 항목을 목록으로 보여 주고,
    // 항목을 고르면 필요한 재료와 지금 작업대 슬롯에 놓인 개수를 보여 준다.
    // 재료는 창 안의 작업대 슬롯(CraftingStagingView)에 개인 슬롯의 재료를 끌어다 놓는다. 이때 아이템은 옮기지 않고 예약만 하며,
    // 고른 항목이 쓰는 재료만 필요한 개수까지 예약된다. 재료가 다 놓이면 제작 버튼이 켜지고, 누르면 예약을 실어
    // 미니게임을 시작하는 승인 요청을 호스트에 보낸다. 아이템은 제작이 끝날 때 호스트가 개인 슬롯에서 뺀다.
    // 미니게임이 끝나거나 영업이 끝나면 창이 닫힌다. 규칙은 건드리지 않고 작업대와 개인 슬롯의 값만 읽는다.
    public class CraftingMenuView : MonoBehaviour
    {
        private static readonly string[] DifficultyNames = { "쉬움", "보통", "어려움" };

        [SerializeField] private InventoryChannel _inventoryChannel;
        [SerializeField] private PlayerNetworkInputLock _inputLock;
        [SerializeField] private InputActionReference _cancel;
        [SerializeField] private TierRulesData _tiers;

        [Header("창")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private RectTransform _listRoot;
        [SerializeField] private CraftingListRow _rowPrefab;
        [Tooltip("창 안의 작업대 슬롯 칸")]
        [SerializeField] private CraftingStagingView _staging;

        [Header("고른 항목")]
        [SerializeField] private TMP_Text _outputText;
        [SerializeField] private TMP_Text _infoText;
        [SerializeField] private TMP_Text _ingredientsText;
        [SerializeField] private TMP_Text _resultText;
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private Button _craftButton;
        [SerializeField] private TMP_Text _craftButtonText;
        [SerializeField] private Button _closeButton;

        private readonly List<CraftingListRow> _rows = new List<CraftingListRow>();
        private readonly List<CraftingIngredient> _ingredients = new List<CraftingIngredient>();
        private readonly List<ItemStack> _actual = new List<ItemStack>();
        private readonly List<ItemStack> _reservedStacks = new List<ItemStack>();
        private readonly List<SlotRemoval> _plan = new List<SlotRemoval>();
        private readonly StringBuilder _builder = new StringBuilder();
        private readonly CraftingReservation _reservation = new CraftingReservation();
        private ForgeStationLink _link;
        private ForgeStationState _station;
        private PlayerInventoryState _playerInventory;
        private InputAction _upKey;
        private InputAction _downKey;
        private InputAction _confirmKey;
        private int _selected = -1;
        private bool _isOpen;
        private bool _isShown;
        private bool _cursorWasVisible;
        private CursorLockMode _cursorLockState;

        // 열려 있는 동안 true다. 미니게임을 하느라 창을 잠시 숨긴 동안도 true다.
        public bool IsOpen => _isOpen;

        private void Awake()
        {
            _panelRoot.SetActive(false);
            _craftButton.onClick.AddListener(Craft);
            _closeButton.onClick.AddListener(Close);
            _staging.Bind(_reservation, GetSelectedIngredients, OnReservationChanged);

            // 입력 에셋 대신 코드로 정의한다. 창이 보이는 동안에만 읽는다.
            _upKey = new InputAction("CraftingMenuUp", InputActionType.Button);
            _upKey.AddBinding("<Keyboard>/upArrow");
            _upKey.AddBinding("<Keyboard>/w");
            _downKey = new InputAction("CraftingMenuDown", InputActionType.Button);
            _downKey.AddBinding("<Keyboard>/downArrow");
            _downKey.AddBinding("<Keyboard>/s");
            _confirmKey = new InputAction("CraftingMenuConfirm", InputActionType.Button, "<Keyboard>/enter");
        }

        private void OnEnable()
        {
            _cancel.action.Enable();
            _upKey.Enable();
            _downKey.Enable();
            _confirmKey.Enable();
        }

        private void OnDisable()
        {
            if (_isOpen)
            {
                Close();
            }

            _upKey.Disable();
            _downKey.Disable();
            _confirmKey.Disable();
        }

        private void OnDestroy()
        {
            _upKey.Dispose();
            _downKey.Dispose();
            _confirmKey.Dispose();
        }

        private void Update()
        {
            if (!_isShown)
            {
                return;
            }

            if (_cancel.action.WasPressedThisFrame())
            {
                Close();
                return;
            }

            if (_upKey.WasPressedThisFrame())
            {
                Select(_selected - 1);
            }
            else if (_downKey.WasPressedThisFrame())
            {
                Select(_selected + 1);
            }
            else if (_confirmKey.WasPressedThisFrame())
            {
                Craft();
            }

            RefreshCraftButton();
        }

        // 작업대 연결이 사용권을 받으면 불러서 창을 연다.
        public void Open(ForgeStationLink link)
        {
            if (_isOpen)
            {
                return;
            }

            _link = link;
            _station = link.Station;
            _isOpen = true;
            _link.Approved += HideForGame;
            _link.CraftFinished += CloseAfterGame;
            _link.Rejected += OnRejected;
            _link.Channel.OpenChanged += OnWorkOpenChanged;

            _inputLock.Lock(this);
            _cursorWasVisible = Cursor.visible;
            _cursorLockState = Cursor.lockState;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            _titleText.text = _station.DisplayName;
            BuildRows();
            _reservation.Clear();
            Show();
            Select(_station.Entries.Count > 0 ? 0 : -1);
        }

        public void Close()
        {
            if (!_isOpen)
            {
                return;
            }

            _isOpen = false;
            _link.Approved -= HideForGame;
            _link.CraftFinished -= CloseAfterGame;
            _link.Rejected -= OnRejected;
            _link.Channel.OpenChanged -= OnWorkOpenChanged;
            Hide();
            _reservation.Clear();
            Cursor.visible = _cursorWasVisible;
            Cursor.lockState = _cursorLockState;
            _inputLock.Release(this);

            ForgeStationLink link = _link;
            _link = null;
            _station = null;
            link.RequestClose();
        }

        private void Show()
        {
            _isShown = true;
            _panelRoot.SetActive(true);
            _playerInventory = _inventoryChannel.LocalPlayerInventory;
            if (_playerInventory != null)
            {
                _playerInventory.Changed += OnInventoryChanged;
            }

            // 예약한 재료는 개인 슬롯 화면에서 빠진 것처럼 보이고, 빠른 이동은 작업대 슬롯 예약으로 바뀐다.
            _inventoryChannel.SetReservation(_reservation);
            _inventoryChannel.QuickMoveOverride = QuickReserve;
            _messageText.text = string.Empty;
            RefreshDetail();
        }

        private void Hide()
        {
            if (!_isShown)
            {
                return;
            }

            _isShown = false;
            if (_playerInventory != null)
            {
                _playerInventory.Changed -= OnInventoryChanged;
                _playerInventory = null;
            }

            _inventoryChannel.QuickMoveOverride = null;
            _inventoryChannel.SetReservation(null);
            _panelRoot.SetActive(false);
        }

        // 제작을 승인받으면 미니게임을 하는 동안 창만 숨긴다. 열린 상태는 그대로다.
        private void HideForGame()
        {
            Hide();
        }

        // 미니게임이 끝나면 목록으로 돌아가지 않고 작업대를 닫는다.
        private void CloseAfterGame()
        {
            if (_isOpen)
            {
                Close();
            }
        }

        // 영업이 끝나면(마감) 열려 있던 목록을 닫는다. 미니게임 중이면 미니게임이 끝날 때 닫힌다.
        private void OnWorkOpenChanged(bool isOpen)
        {
            if (!isOpen && _isShown)
            {
                Close();
            }
        }

        private void OnRejected(ForgeRejectReason reason)
        {
            if (!_isOpen)
            {
                return;
            }

            if (reason == ForgeRejectReason.Busy || reason == ForgeRejectReason.Closed)
            {
                // 다른 사람이 쓰고 있거나 영업이 끝났다. 알림은 작업대 연결이 띄우고 창은 닫는다.
                Close();
                return;
            }

            _messageText.text = reason == ForgeRejectReason.MissingMaterials ? "재료가 모자라다" : "지금은 만들 수 없다";
        }

        private void BuildRows()
        {
            IReadOnlyList<CraftingEntry> entries = _station.Entries;
            while (_rows.Count < entries.Count)
            {
                CraftingListRow row = Instantiate(_rowPrefab, _listRoot);
                _rows.Add(row);
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                bool used = i < entries.Count;
                _rows[i].gameObject.SetActive(used);
                if (used)
                {
                    _rows[i].Bind(i, entries[i].Output.DisplayName, entries[i].Tier + "티어", Select);
                }
            }
        }

        // 항목을 바꾸면 이미 예약한 재료는 모두 풀어 개인 슬롯으로 돌려보낸 것처럼 보인다.
        private void Select(int index)
        {
            int count = _station.Entries.Count;
            int next = count == 0 ? -1 : Mathf.Clamp(index, 0, count - 1);
            if (next != _selected)
            {
                _selected = next;
                if (!_reservation.IsEmpty)
                {
                    _reservation.Clear();
                    _inventoryChannel.NotifyReservationChanged();
                }
            }

            for (int i = 0; i < count; i++)
            {
                _rows[i].SetSelected(i == _selected);
            }

            RefreshDetail();
        }

        // 작업대 슬롯이 받을 재료: 지금 고른 항목의 재료
        private IReadOnlyList<CraftingIngredient> GetSelectedIngredients()
        {
            if (_station != null && _station.TryGetEntry(_selected, out CraftingEntry entry))
            {
                entry.GetIngredients(_ingredients);
            }
            else
            {
                _ingredients.Clear();
            }

            return _ingredients;
        }

        // 개인 슬롯 화면의 빠른 이동(Shift+클릭, 더블 클릭)을 작업대 슬롯 예약으로 바꾼다.
        private bool QuickReserve(NetworkInventory source, int slot)
        {
            if (!_isShown || source != _playerInventory)
            {
                return false;
            }

            _staging.Reserve(_playerInventory, slot, int.MaxValue);
            return true;
        }

        private void OnReservationChanged()
        {
            _inventoryChannel.NotifyReservationChanged();
            RefreshDetail();
        }

        // 개인 슬롯이 바뀌어 예약한 칸의 물건이 달라졌으면 예약을 맞춘다.
        private void OnInventoryChanged()
        {
            ReadActual();
            if (_reservation.Reconcile(_actual))
            {
                _inventoryChannel.NotifyReservationChanged();
            }

            RefreshDetail();
        }

        // 고른 항목의 정보와 재료 개수, 제작 버튼을 다시 그린다. 예약이나 개인 슬롯이 바뀔 때마다 불린다.
        private void RefreshDetail()
        {
            if (_station == null)
            {
                return;
            }

            ReadActual();
            _reservation.BuildStacks(_actual, _reservedStacks);
            _staging.Refresh(_playerInventory);

            IReadOnlyList<CraftingEntry> entries = _station.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                _rows[i].SetReady(IsSatisfied(entries[i]));
            }

            if (!_station.TryGetEntry(_selected, out CraftingEntry entry))
            {
                _outputText.text = "만들 수 있는 것이 없다";
                _infoText.text = string.Empty;
                _ingredientsText.text = string.Empty;
                _resultText.text = string.Empty;
                RefreshCraftButton();
                return;
            }

            ItemData output = entry.Output;
            int difficulty = _tiers.DifficultyIndex(entry.Tier);
            _outputText.text = output.DisplayName;
            _infoText.text = $"{entry.Tier}티어 · 난이도 {DifficultyNames[Mathf.Clamp(difficulty, 0, DifficultyNames.Length - 1)]} · {StationName(_station.Kind)} 미니게임";
            _ingredientsText.text = BuildIngredientsText(entry);
            _resultText.text = entry.IsFinal
                ? $"완성품: {entry.Recipe.FinalItem.DisplayName}\n<size=80%>등급은 미니게임 점수의 평균으로 정해진다</size>"
                : $"결과: {output.DisplayName}\n<size=80%>다음 단계: {NextStationName(entry)}</size>";
            RefreshCraftButton();
        }

        private string BuildIngredientsText(CraftingEntry entry)
        {
            entry.GetIngredients(_ingredients);
            _builder.Clear();
            foreach (CraftingIngredient ingredient in _ingredients)
            {
                int have = CraftingRequirements.CountOf(ingredient.ItemId, _reservedStacks);
                bool enough = have >= ingredient.Count;
                ItemData item = _playerInventory != null ? _playerInventory.Catalog.Find(ingredient.ItemId) : null;
                string itemName = item != null ? item.DisplayName : "?";
                _builder.Append("<color=").Append(enough ? "#8DEB99" : "#FF7A6B").Append('>')
                    .Append(itemName).Append("   ").Append(Mathf.Min(have, 999)).Append(" / ").Append(ingredient.Count)
                    .Append("</color>\n");
            }

            return _builder.ToString();
        }

        private static string NextStationName(CraftingEntry entry)
        {
            if (entry.IsFinal)
            {
                return string.Empty;
            }

            return StationName(entry.Recipe.GetStep(entry.StepIndex + 1).Station);
        }

        public static string StationName(CraftingStationKind kind)
        {
            switch (kind)
            {
                case CraftingStationKind.Smelting:
                    return "정제";
                case CraftingStationKind.Forging:
                    return "단조";
                case CraftingStationKind.Quench:
                    return "담금질";
                case CraftingStationKind.Polish:
                    return "연마";
                case CraftingStationKind.PlateForming:
                    return "판 성형";
                case CraftingStationKind.Assembly:
                    return "조립";
                default:
                    return kind.ToString();
            }
        }

        private void ReadActual()
        {
            _actual.Clear();
            if (_playerInventory == null)
            {
                return;
            }

            for (int i = 0; i < _playerInventory.SlotCount; i++)
            {
                _actual.Add(_playerInventory.Get(i));
            }
        }

        private bool IsSatisfied(CraftingEntry entry)
        {
            entry.GetIngredients(_ingredients);
            return CraftingRequirements.IsSatisfied(_ingredients, _reservedStacks);
        }

        private bool IsIntermediate(int itemId)
        {
            ItemData item = _playerInventory != null ? _playerInventory.Catalog.Find(itemId) : null;
            return item != null && item.Kind == ItemKind.Intermediate;
        }

        private void RefreshCraftButton()
        {
            bool canCraft = _station != null && _station.TryGetEntry(_selected, out CraftingEntry entry) && IsSatisfied(entry) && !_link.Requesting;
            _craftButton.interactable = canCraft;
            _craftButtonText.text = canCraft ? "제작" : "재료가 부족하다";
        }

        private void Craft()
        {
            if (!_isShown || !_station.TryGetEntry(_selected, out CraftingEntry entry))
            {
                return;
            }

            ReadActual();
            _reservation.BuildStacks(_actual, _reservedStacks);
            if (!IsSatisfied(entry) || _link.Requesting)
            {
                return;
            }

            // 예약한 중간 형태 재료의 정확도 평균. 미니게임을 시작하는 상태에 쓴다. 중간 형태를 쓰지 않으면 1이다.
            entry.GetIngredients(_ingredients);
            float prior = 1f;
            if (HasIntermediate(_ingredients) && CraftingRequirements.TryPlan(_ingredients, _reservedStacks, IsIntermediate, _plan, out int percent))
            {
                prior = Mathf.Clamp01(percent / (float)CraftingRules.MaxPercent);
            }

            _messageText.text = string.Empty;
            _link.RequestCraft(entry, _reservation.Pack(), prior);
        }

        private bool HasIntermediate(List<CraftingIngredient> ingredients)
        {
            foreach (CraftingIngredient ingredient in ingredients)
            {
                if (IsIntermediate(ingredient.ItemId))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
