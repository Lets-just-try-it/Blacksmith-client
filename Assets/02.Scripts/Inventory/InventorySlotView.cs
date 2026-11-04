using Blacksmith.Core.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Blacksmith.Inventory
{
    // 인벤토리 칸 하나의 화면이다. 끌기·놓기·클릭은 자기를 만든 창(IInventorySlotOwner)에 넘긴다.
    public class InventorySlotView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _countText;
        [Tooltip("손에 든 칸일 때 켜는 테두리. 레이캐스트를 받지 않아야 한다")]
        [SerializeField] private GameObject _heldFrame;

        private IInventorySlotOwner _owner;

        public int Slot { get; private set; }
        public bool IsEmpty { get; private set; } = true;

        public void Bind(IInventorySlotOwner owner, int slot)
        {
            _owner = owner;
            Slot = slot;
        }

        // label: 아이템 이름과 등급. 비어 있으면 무시한다.
        public void Show(ItemStack stack, ItemData item, string label)
        {
            IsEmpty = stack.IsEmpty;
            _icon.enabled = !IsEmpty;
            _nameText.text = IsEmpty ? "" : label;
            _countText.text = !IsEmpty && stack.Count > 1 ? stack.Count.ToString() : "";
            if (!IsEmpty)
            {
                ApplyIcon(_icon, item);
            }
        }

        public void SetHeld(bool held)
        {
            _heldFrame.SetActive(held);
        }

        public static void ApplyIcon(Image image, ItemData item)
        {
            image.sprite = item != null ? item.Icon : null;
            image.color = item != null && item.Icon == null ? item.Color : Color.white;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsEmpty)
            {
                // 빈 칸은 끌지 않는다. 비워 두면 OnDrag·OnEndDrag도 오지 않는다.
                eventData.pointerDrag = null;
                return;
            }

            _owner.BeginDrag(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _owner.MoveDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _owner.EndDrag();
        }

        public void OnDrop(PointerEventData eventData)
        {
            _owner.DropOn(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && !IsEmpty)
            {
                _owner.Click(this, eventData.clickCount);
            }
        }
    }
}
