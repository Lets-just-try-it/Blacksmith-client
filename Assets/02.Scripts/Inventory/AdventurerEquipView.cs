using System;
using System.Collections.Generic;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Inventory;
using Blacksmith.Forge;
using Blacksmith.Network;
using Blacksmith.Plaza;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Blacksmith.Inventory
{
    // 모험가 한 명에게 장비를 입히는 창이다. 공용 장비 목록(공용 창고와 모든 플레이어 개인 슬롯에 있는 완성 장비)과
    // 고른 모험가의 모습·능력치·투구·갑옷·무기·장신구 칸을 보여 준다. 목록은 부위별로 걸러 볼 수 있다.
    // 목록의 장비를 끄는 동안에는 그 장비를 입혔을 때의 능력치 변화를 보여 준다.
    // 목록에서 장비 칸으로 끌어 놓거나 목록의 장비를 더블 클릭하면 입히고,
    // 장비 칸을 목록으로 끌어 놓거나 더블 클릭하면 벗겨 공용 창고로 돌려보낸다.
    // 칸에 맞지 않는 장비는 놓아도 아무것도 바뀌지 않아 원래 자리에 남고, 맞지 않는 이유를 토스트로 잠깐 띄운다.
    // 파견 창(Battle/DispatchView)이 고른 모험가로 이 창을 연다.
    // 장비 선택은 호스트만 한다(GAME_CONCEPT 9절). 다른 참가자는 보기만 한다. 창이 열린 동안 하단 개인 슬롯은 숨긴다.
    public class AdventurerEquipView : MonoBehaviour, IInventorySlotOwner
    {
        private struct Entry
        {
            public NetworkInventory Inventory;
            public int Slot;
        }

        // 공용 장비 목록을 거르는 탭. Slot이 None이면 모든 장비를 보여 준다.
        [Serializable]
        private struct FilterTab
        {
            public Button Button;
            public EquipmentSlot Slot;
        }

        [SerializeField] private InventoryChannel _channel;
        [SerializeField] private AdventurerRosterState _roster;
        [Tooltip("공용 장비 목록에 넣고, 벗긴 장비를 돌려보내는 공용 창고")]
        [SerializeField] private SharedInventoryState _storage;
        [Tooltip("직업 이름과 직업별 무기 종류를 찾는 데 쓴다")]
        [SerializeField] private AdventurerGenerationData _jobs;
        [Tooltip("등급 이름을 찾는 데 쓴다")]
        [SerializeField] private GradeThresholdData _grades;
        [SerializeField] private PlayerNetworkInputLock _inputLock;
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _adventurerText;
        [SerializeField] private TMP_Text _hintText;
        [Tooltip("맞지 않는 장비를 놓았을 때 이유를 잠깐 띄운다")]
        [SerializeField] private InventoryToast _toast;
        [SerializeField] private Button _previousButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private InventorySlotView _helmetSlot;
        [SerializeField] private InventorySlotView _armorSlot;
        [SerializeField] private InventorySlotView _weaponSlot;
        [SerializeField] private InventorySlotView _accessorySlot;
        [SerializeField] private TMP_Text _statsText;
        [Tooltip("장비 보정 계산. 전투와 같은 에셋을 쓴다")]
        [SerializeField] private EquipmentStatData _equipmentStats;
        [SerializeField] private AdventurerPreview _preview;
        [SerializeField] private FilterTab[] _filterTabs = new FilterTab[0];
        [SerializeField] private Color _tabColor = new Color(0.18f, 0.16f, 0.14f, 1f);
        [SerializeField] private Color _selectedTabColor = new Color(0.55f, 0.42f, 0.18f, 1f);
        [Tooltip("GridLayoutGroup이 붙은 공용 장비 목록 칸 부모")]
        [SerializeField] private Transform _listRoot;
        [SerializeField] private InventorySlotView _slotPrefab;
        [Tooltip("장비가 적어도 이만큼은 칸을 보여 준다. 빈 칸에 장비 칸을 끌어 놓으면 벗긴다")]
        [SerializeField, Min(1)] private int _minListSlots = 15;
        [Tooltip("끄는 동안 포인터를 따라다니는 그림. 레이캐스트를 받지 않아야 한다")]
        [SerializeField] private Image _dragIcon;
        [SerializeField] private int _dragSortingOrder = 100;

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly List<InventorySlotView> _listSlots = new List<InventorySlotView>();
        private readonly List<NetworkInventory> _watched = new List<NetworkInventory>();
        private InventoryDragIcon _drag;
        private int _adventurerIndex;
        private EquipmentSlot _draggingEquipment;
        private EquipmentSlot _filter;
        // 목록에서 끄는 중인 장비. 입혔을 때의 능력치 변화를 보여 줄 때 쓴다.
        private ItemStack _previewItem;
        private bool _isOpen;

        private bool CanEdit => _roster.IsReady && _roster.HasStateAuthority;

        private void Awake()
        {
            _drag = new InventoryDragIcon(_dragIcon, _dragSortingOrder);
            // 장비 칸은 목록 칸과 구분하도록 음수 번호(-장비 부위)로 묶는다.
            _helmetSlot.Bind(this, -(int)EquipmentSlot.Helmet);
            _armorSlot.Bind(this, -(int)EquipmentSlot.Armor);
            _weaponSlot.Bind(this, -(int)EquipmentSlot.Weapon);
            _accessorySlot.Bind(this, -(int)EquipmentSlot.Accessory);
            _panel.SetActive(false);
            _preview.Hide();
        }

        private void OnEnable()
        {
            _previousButton.onClick.AddListener(ShowPrevious);
            _nextButton.onClick.AddListener(ShowNext);
            _closeButton.onClick.AddListener(Hide);
            foreach (FilterTab tab in _filterTabs)
            {
                EquipmentSlot slot = tab.Slot;
                tab.Button.onClick.AddListener(() => SetFilter(slot));
            }
        }

        private void OnDisable()
        {
            _previousButton.onClick.RemoveListener(ShowPrevious);
            _nextButton.onClick.RemoveListener(ShowNext);
            _closeButton.onClick.RemoveListener(Hide);
            foreach (FilterTab tab in _filterTabs)
            {
                tab.Button.onClick.RemoveAllListeners();
            }

            Hide();
        }

        public void Show()
        {
            if (_isOpen || !_roster.IsReady || !_storage.IsReady)
            {
                return;
            }

            _isOpen = true;
            _inputLock.Lock(this);
            _channel.HideHud(this);
            _roster.Changed += Refresh;
            _channel.PlayerInventoriesChanged += WatchInventories;
            WatchInventories();
            _panel.SetActive(true);
        }

        // 파견 창이 고른 모험가로 연다. 이미 열려 있으면 그 모험가로 바꾼다.
        public void Show(int adventurerId)
        {
            if (!_roster.IsReady)
            {
                return;
            }

            for (int i = 0; i < _roster.Count; i++)
            {
                if (_roster.Get(i).AdventurerId == adventurerId)
                {
                    _adventurerIndex = i;
                    break;
                }
            }

            Show();
            Refresh();
        }

        public void Hide()
        {
            if (!_isOpen)
            {
                return;
            }

            _isOpen = false;
            EndDrag();
            _roster.Changed -= Refresh;
            _channel.PlayerInventoriesChanged -= WatchInventories;
            Unwatch();
            _inputLock.Release(this);
            _channel.ShowHud(this);
            // 씬을 내릴 때는 창과 무대가 먼저 파괴되었을 수 있다.
            if (_panel != null)
            {
                _panel.SetActive(false);
            }

            if (_preview != null)
            {
                _preview.Hide();
            }
        }

        private void ShowPrevious()
        {
            _adventurerIndex--;
            Refresh();
        }

        private void ShowNext()
        {
            _adventurerIndex++;
            Refresh();
        }

        private void SetFilter(EquipmentSlot slot)
        {
            _filter = slot;
            Refresh();
        }

        // 공용 창고와 모든 플레이어 개인 슬롯이 바뀌면 목록을 다시 그린다.
        private void WatchInventories()
        {
            Unwatch();
            _watched.Add(_storage);
            foreach (PlayerInventoryState inventory in _channel.PlayerInventories)
            {
                _watched.Add(inventory);
            }

            foreach (NetworkInventory inventory in _watched)
            {
                inventory.Changed += Refresh;
            }

            Refresh();
        }

        private void Unwatch()
        {
            foreach (NetworkInventory inventory in _watched)
            {
                if (inventory != null)
                {
                    inventory.Changed -= Refresh;
                }
            }

            _watched.Clear();
        }

        private void Refresh()
        {
            // 세션을 끝낼 때는 관리 목록이 먼저 디스폰된 뒤 다른 인벤토리의 디스폰 알림이 올 수 있다.
            if (!_isOpen || !_roster.IsReady)
            {
                return;
            }

            AdventurerData adventurer = GetAdventurer();
            _adventurerText.text = adventurer != null ? GetAdventurerLabel(adventurer) : "관리 중인 모험가가 없다";
            _hintText.text = CanEdit
                ? "목록에서 장비 칸으로 끌어 놓아 입히기 · 무기 외 장비 칸을 목록에 놓거나 더블 클릭해 벗기기"
                : "장비는 호스트만 바꿀 수 있다";
            ShowEquipped(_helmetSlot, adventurer, EquipmentSlot.Helmet);
            ShowEquipped(_armorSlot, adventurer, EquipmentSlot.Armor);
            ShowEquipped(_weaponSlot, adventurer, EquipmentSlot.Weapon);
            ShowEquipped(_accessorySlot, adventurer, EquipmentSlot.Accessory);
            ShowStats(adventurer);
            _preview.Show(adventurer);
            ShowTabs();
            CollectEquipment();
            ShowList();
        }

        private AdventurerData GetAdventurer()
        {
            if (_roster.Count == 0)
            {
                return null;
            }

            _adventurerIndex = (_adventurerIndex % _roster.Count + _roster.Count) % _roster.Count;
            return _roster.Get(_adventurerIndex);
        }

        private string GetAdventurerLabel(AdventurerData adventurer)
        {
            AdventurerJobData job = _jobs.FindJob(adventurer.JobId);
            string jobName = job != null ? job.DisplayName : $"직업 {adventurer.JobId}";
            return $"{adventurer.Name} · {jobName} Lv{adventurer.Level}\n({_adventurerIndex + 1}/{_roster.Count})";
        }

        private void ShowEquipped(InventorySlotView view, AdventurerData adventurer, EquipmentSlot slot)
        {
            ItemStack stack = adventurer != null ? EquipmentRules.GetEquipped(adventurer, slot) : ItemStack.Empty;
            ItemData item = stack.IsEmpty ? null : _storage.Catalog.Find(stack.ItemId);
            view.Show(stack, item, ItemLabels.Get(stack, item, _grades));
        }

        // 능력치 = 레벨 능력치 + 장비 보정. 목록의 장비를 끄는 중이면 그 장비를 입혔을 때의 값과 변화량을 함께 보여 준다.
        private void ShowStats(AdventurerData adventurer)
        {
            if (adventurer == null)
            {
                _statsText.text = "";
                return;
            }

            AdventurerStats level = AdventurerStatRules.GetCurrentStats(adventurer);
            AdventurerStats bonus = _equipmentStats.GetEquipmentBonus(adventurer);
            AdventurerStats current = AdventurerStatRules.Add(level, bonus);
            AdventurerStats? preview = null;
            ItemData previewItem = _previewItem.IsEmpty ? null : _storage.Catalog.Find(_previewItem.ItemId);
            if (previewItem != null && AdventurerEquipping.CanEquip(_storage.Catalog, _jobs, adventurer, _previewItem, previewItem.EquipmentSlot))
            {
                preview = AdventurerStatRules.Add(level, _equipmentStats.GetEquipmentBonus(adventurer, previewItem.EquipmentSlot, _previewItem));
            }

            _statsText.text =
                FormatStat("체력", current.MaxHp, bonus.MaxHp, preview?.MaxHp) + "\n" +
                FormatStat("공격력", current.Attack, bonus.Attack, preview?.Attack) + "\n" +
                FormatStat("방어력", current.Defense, bonus.Defense, preview?.Defense);
        }

        private static string FormatStat(string label, int value, int bonus, int? preview)
        {
            string text = $"{label} {value} <color=#B8A98A>(장비 +{bonus})</color>";
            if (preview.HasValue && preview.Value != value)
            {
                int delta = preview.Value - value;
                string color = delta > 0 ? "#7CE07C" : "#E07C7C";
                text += $"\n  → {preview.Value} <color={color}>({delta:+0;-0})</color>";
            }

            return text;
        }

        private void ShowTabs()
        {
            foreach (FilterTab tab in _filterTabs)
            {
                tab.Button.image.color = tab.Slot == _filter ? _selectedTabColor : _tabColor;
            }
        }

        // 재료와 제작 중간 형태는 빼고 완성 장비만 모은다. 고른 탭의 부위만 남긴다. 공용 창고를 먼저, 그다음 플레이어 순서로 둔다.
        private void CollectEquipment()
        {
            _entries.Clear();
            foreach (NetworkInventory inventory in _watched)
            {
                if (inventory == null || !inventory.IsReady)
                {
                    continue;
                }

                for (int i = 0; i < inventory.SlotCount; i++)
                {
                    ItemStack stack = inventory.Get(i);
                    ItemData item = stack.IsEmpty ? null : inventory.Catalog.Find(stack.ItemId);
                    if (item != null && item.Kind == ItemKind.Equipment && (_filter == EquipmentSlot.None || item.EquipmentSlot == _filter))
                    {
                        _entries.Add(new Entry { Inventory = inventory, Slot = i });
                    }
                }
            }
        }

        private void ShowList()
        {
            int count = Mathf.Max(_minListSlots, _entries.Count);
            while (_listSlots.Count < count)
            {
                InventorySlotView slot = Instantiate(_slotPrefab, _listRoot);
                slot.Bind(this, _listSlots.Count);
                _listSlots.Add(slot);
            }

            for (int i = 0; i < _listSlots.Count; i++)
            {
                _listSlots[i].gameObject.SetActive(i < count);
                ItemStack stack = i < _entries.Count ? _entries[i].Inventory.Get(_entries[i].Slot) : ItemStack.Empty;
                ItemData item = stack.IsEmpty ? null : _storage.Catalog.Find(stack.ItemId);
                _listSlots[i].Show(stack, item, ItemLabels.Get(stack, item, _grades));
                _listSlots[i].SetHeld(false);
            }
        }

        public void BeginDrag(InventorySlotView slot, PointerEventData eventData)
        {
            if (!CanEdit)
            {
                return;
            }

            if (IsEquipmentSlot(slot))
            {
                _draggingEquipment = ToEquipmentSlot(slot);
                _drag.Show(_storage.Catalog.Find(EquipmentRules.GetEquipped(GetAdventurer(), _draggingEquipment).ItemId), eventData);
            }
            else if (slot.Slot < _entries.Count)
            {
                Entry entry = _entries[slot.Slot];
                _previewItem = entry.Inventory.Get(entry.Slot);
                _drag.Show(_storage.Catalog.Find(_previewItem.ItemId), eventData);
                _channel.BeginDrag(entry.Inventory, entry.Slot);
                ShowStats(GetAdventurer());
            }
        }

        public void MoveDrag(PointerEventData eventData)
        {
            _drag.Move(eventData);
        }

        // 놓기(OnDrop)가 끌기 끝(OnEndDrag)보다 먼저 불리므로, 여기서는 그림만 치우고 남은 드래그를 비운다.
        public void EndDrag()
        {
            if (_drag != null)
            {
                _drag.Hide();
            }

            _draggingEquipment = EquipmentSlot.None;
            _channel.EndDrag();
            if (!_previewItem.IsEmpty)
            {
                _previewItem = ItemStack.Empty;
                if (_isOpen)
                {
                    ShowStats(GetAdventurer());
                }
            }
        }

        public void DropOn(InventorySlotView slot)
        {
            AdventurerData adventurer = GetAdventurer();
            if (!CanEdit || adventurer == null)
            {
                return;
            }

            if (IsEquipmentSlot(slot))
            {
                TryEquipDragged(adventurer, ToEquipmentSlot(slot));
            }
            else if (_draggingEquipment != EquipmentSlot.None)
            {
                TryUnequip(adventurer, _draggingEquipment);
            }
        }

        // 칸에 맞지 않으면 요청하지 않고 이유를 토스트로 띄운다. 끌던 장비는 옮겨지지 않았으므로 원래 자리에 그대로 보인다.
        private void TryEquipDragged(AdventurerData adventurer, EquipmentSlot target)
        {
            if (_channel.DragSource == null)
            {
                return;
            }

            ItemStack dragged = _channel.DragSource.Get(_channel.DragSlot);
            if (AdventurerEquipping.CanEquip(_storage.Catalog, _jobs, adventurer, dragged, target))
            {
                _channel.EquipDragged(_roster, adventurer.AdventurerId, target, _storage);
                return;
            }

            ItemData item = dragged.IsEmpty ? null : _storage.Catalog.Find(dragged.ItemId);
            bool wrongWeapon = item != null && target == EquipmentSlot.Weapon && item.EquipmentSlot == EquipmentSlot.Weapon;
            _toast.Show(wrongWeapon ? "맞지 않는 무기입니다" : "이 칸에 맞지 않는 장비입니다");
        }

        // 무기는 벗길 수 없어 요청하지 않고 이유를 토스트로 띄운다. 호스트도 같은 규칙으로 막는다.
        private void TryUnequip(AdventurerData adventurer, EquipmentSlot slot)
        {
            if (EquipmentRules.CanUnequip(slot))
            {
                _channel.Unequip(_roster, adventurer.AdventurerId, slot, _storage);
            }
            else if (slot == EquipmentSlot.Weapon)
            {
                _toast.Show("무기는 벗길 수 없습니다. 다른 무기로 바꿔 주세요");
            }
        }

        // 장비 칸을 더블 클릭하면 벗기고(무기 제외), 목록의 장비를 더블 클릭하면 맞는 칸에 입힌다.
        public void Click(InventorySlotView slot, int clickCount)
        {
            AdventurerData adventurer = GetAdventurer();
            if (clickCount != 2 || !CanEdit || adventurer == null)
            {
                return;
            }

            if (IsEquipmentSlot(slot))
            {
                TryUnequip(adventurer, ToEquipmentSlot(slot));
            }
            else if (slot.Slot < _entries.Count)
            {
                Entry entry = _entries[slot.Slot];
                ItemData item = _storage.Catalog.Find(entry.Inventory.Get(entry.Slot).ItemId);
                _channel.BeginDrag(entry.Inventory, entry.Slot);
                TryEquipDragged(adventurer, item != null ? item.EquipmentSlot : EquipmentSlot.None);
                _channel.EndDrag();
            }
        }

        private static bool IsEquipmentSlot(InventorySlotView slot)
        {
            return slot.Slot < 0;
        }

        private static EquipmentSlot ToEquipmentSlot(InventorySlotView slot)
        {
            return (EquipmentSlot)(-slot.Slot);
        }
    }
}
