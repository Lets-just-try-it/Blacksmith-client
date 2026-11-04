using System.Collections.Generic;
using Blacksmith.Core;
using Blacksmith.Core.Inventory;
using Blacksmith.Forge;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Blacksmith.Inventory
{
    // 인벤토리 하나를 칸 격자로 보여 주는 창이다. 칸을 끌어다 놓으면 InventoryChannel로 호스트에 이동을 요청하고,
    // 화면은 호스트가 바꾼 값이 복제되어 Changed가 오면 다시 그린다.
    // Ctrl을 누른 채 놓으면 개수를 물어 나누고, Shift+클릭이나 더블 클릭은 내 슬롯과 열린 공용 창고 사이로 빠르게 옮긴다.
    // 내 슬롯을 보여 줄 때는 손에 든 칸을 강조한다.
    public class InventoryView : MonoBehaviour
    {
        [SerializeField] private InventoryChannel _channel;
        [Tooltip("등급 이름을 찾는 데 쓴다")]
        [SerializeField] private GradeThresholdData _grades;
        [SerializeField] private GameObject _panel;
        [Tooltip("비워 두면 크기를 표시하지 않는다")]
        [SerializeField] private TMP_Text _sizeText;
        [Tooltip("GridLayoutGroup이 붙은 칸 부모. 가로 칸 수는 인벤토리에 맞춰 바꾼다")]
        [SerializeField] private GridLayoutGroup _slotRoot;
        [SerializeField] private InventorySlotView _slotPrefab;
        [Tooltip("끄는 동안 포인터를 따라다니는 그림. 레이캐스트를 받지 않아야 한다")]
        [SerializeField] private Image _dragIcon;
        [Tooltip("끄는 그림의 캔버스 정렬 순서. 다른 인벤토리 창보다 위에 그려지도록 크게 둔다")]
        [SerializeField] private int _dragSortingOrder = 100;

        private readonly List<InventorySlotView> _slots = new List<InventorySlotView>();
        private NetworkInventory _inventory;
        private RectTransform _dragRect;
        private Canvas _dragCanvas;
        private InputAction _quickMoveKey;
        private InputAction _splitKey;

        private void Awake()
        {
            _dragRect = (RectTransform)_dragIcon.transform;
            // 끄는 그림은 이 창 안에 있어서, 나중에 그려지는 다른 창(예: 하단 슬롯과 공용 창고) 뒤로 숨을 수 있다.
            // 자기 캔버스를 두고 정렬 순서를 덮어써 항상 맨 위에 그린다.
            if (!_dragIcon.TryGetComponent(out _dragCanvas))
            {
                _dragCanvas = _dragIcon.gameObject.AddComponent<Canvas>();
            }

            _dragIcon.gameObject.SetActive(false);
            _panel.SetActive(false);

            // 입력 에셋 대신 코드로 정의한다. 클릭·놓기 순간에 눌려 있는지만 본다.
            _quickMoveKey = new InputAction("InventoryQuickMove", InputActionType.Button, "<Keyboard>/leftShift");
            _splitKey = new InputAction("InventorySplit", InputActionType.Button, "<Keyboard>/ctrl");
        }

        private void OnEnable()
        {
            _quickMoveKey.Enable();
            _splitKey.Enable();
        }

        private void OnDisable()
        {
            Hide();
            _quickMoveKey.Disable();
            _splitKey.Disable();
        }

        private void OnDestroy()
        {
            _quickMoveKey.Dispose();
            _splitKey.Dispose();
        }

        public void Show(NetworkInventory inventory)
        {
            Hide();
            _inventory = inventory;
            _inventory.Changed += Refresh;
            _channel.HeldSlotChanged += Refresh;
            _panel.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            if (_inventory != null)
            {
                _inventory.Changed -= Refresh;
                _channel.HeldSlotChanged -= Refresh;
                _channel.CloseSplit(_inventory);
                _inventory = null;
            }

            _channel.EndDrag();
            // 씬을 내릴 때는 이 창의 자식이 먼저 파괴된 뒤 다른 오브젝트(예: 상자)의 OnDisable에서 불릴 수 있다.
            if (_dragIcon != null)
            {
                _dragIcon.gameObject.SetActive(false);
            }

            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        private void Refresh()
        {
            if (_inventory == null || !_inventory.IsReady)
            {
                return;
            }

            _slotRoot.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _slotRoot.constraintCount = _inventory.Columns;
            if (_sizeText != null)
            {
                _sizeText.text = $"{_inventory.Columns} x {_inventory.Rows}";
            }
            EnsureSlotCount(_inventory.SlotCount);
            int heldSlot = _inventory == _channel.LocalPlayerInventory ? _channel.HeldSlot : -1;
            for (int i = 0; i < _slots.Count; i++)
            {
                ItemStack stack = _inventory.Get(i);
                ItemData item = stack.IsEmpty ? null : _inventory.Catalog.Find(stack.ItemId);
                _slots[i].Show(stack, item, GetLabel(stack, item));
                _slots[i].SetHeld(i == heldSlot);
            }
        }

        private void EnsureSlotCount(int count)
        {
            while (_slots.Count < count)
            {
                InventorySlotView slot = Instantiate(_slotPrefab, _slotRoot.transform);
                slot.Bind(this, _slots.Count);
                _slots.Add(slot);
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].gameObject.SetActive(i < count);
            }
        }

        private string GetLabel(ItemStack stack, ItemData item)
        {
            if (stack.IsEmpty)
            {
                return "";
            }

            string name = item != null ? item.DisplayName : $"#{stack.ItemId}";
            // 등급이 없는 물건(예: 재료)은 등급을 0으로 둔다.
            bool hasGrade = _grades != null && stack.Grade >= GradeRule.MinGrade && stack.Grade < _grades.GradeCount + GradeRule.MinGrade;
            return hasGrade ? $"{name}\n{_grades.GetGradeName(stack.Grade)}" : name;
        }

        public void BeginDrag(InventorySlotView slot, PointerEventData eventData)
        {
            if (_inventory == null)
            {
                return;
            }

            ItemStack stack = _inventory.Get(slot.Slot);
            InventorySlotView.ApplyIcon(_dragIcon, _inventory.Catalog.Find(stack.ItemId));
            _dragIcon.gameObject.SetActive(true);
            // 캔버스가 켜진 뒤에 덮어써야 적용된다.
            _dragCanvas.overrideSorting = true;
            _dragCanvas.sortingOrder = _dragSortingOrder;
            _dragIcon.transform.SetAsLastSibling();
            MoveDrag(eventData);
            _channel.BeginDrag(_inventory, slot.Slot);
        }

        public void MoveDrag(PointerEventData eventData)
        {
            RectTransform parent = (RectTransform)_dragRect.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position,
                    eventData.pressEventCamera, out Vector2 localPoint))
            {
                _dragRect.localPosition = localPoint;
            }
        }

        // 놓기(OnDrop)가 끌기 끝(OnEndDrag)보다 먼저 불리므로, 여기서는 그림만 치우고 남은 드래그를 비운다.
        public void EndDrag()
        {
            _dragIcon.gameObject.SetActive(false);
            _channel.EndDrag();
        }

        public void DropOn(InventorySlotView slot)
        {
            if (_inventory != null)
            {
                _channel.DropOn(_inventory, slot.Slot, _splitKey.IsPressed());
            }
        }

        // Shift+클릭이나 더블 클릭이면 반대쪽 인벤토리로 보낸다. Shift를 누른 채 두 번 누르면 첫 클릭에서만 보낸다.
        public void Click(InventorySlotView slot, int clickCount)
        {
            bool quickMove = _quickMoveKey.IsPressed() ? clickCount == 1 : clickCount == 2;
            if (_inventory != null && quickMove)
            {
                _channel.QuickMove(_inventory, slot.Slot);
            }
        }
    }
}
