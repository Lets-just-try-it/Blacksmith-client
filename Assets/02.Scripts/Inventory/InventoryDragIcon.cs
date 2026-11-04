using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Blacksmith.Inventory
{
    // 칸을 끄는 동안 포인터를 따라다니는 그림이다. 인벤토리 창과 장비 착용 창이 같이 쓴다.
    // 그림은 창 안에 있어서 나중에 그려지는 다른 창(예: 하단 슬롯과 공용 창고) 뒤로 숨을 수 있으므로,
    // 자기 캔버스를 두고 정렬 순서를 덮어써 항상 맨 위에 그린다.
    public class InventoryDragIcon
    {
        private readonly Image _image;
        private readonly RectTransform _rect;
        private readonly Canvas _canvas;
        private readonly int _sortingOrder;

        public InventoryDragIcon(Image image, int sortingOrder)
        {
            _image = image;
            _rect = (RectTransform)image.transform;
            _sortingOrder = sortingOrder;
            if (!image.TryGetComponent(out _canvas))
            {
                _canvas = image.gameObject.AddComponent<Canvas>();
            }

            image.gameObject.SetActive(false);
        }

        public void Show(ItemData item, PointerEventData eventData)
        {
            InventorySlotView.ApplyIcon(_image, item);
            _image.gameObject.SetActive(true);
            // 캔버스가 켜진 뒤에 덮어써야 적용된다.
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = _sortingOrder;
            _rect.SetAsLastSibling();
            Move(eventData);
        }

        public void Move(PointerEventData eventData)
        {
            RectTransform parent = (RectTransform)_rect.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position,
                    eventData.pressEventCamera, out Vector2 localPoint))
            {
                _rect.localPosition = localPoint;
            }
        }

        public void Hide()
        {
            // 씬을 내릴 때는 그림이 먼저 파괴되었을 수 있다.
            if (_image != null)
            {
                _image.gameObject.SetActive(false);
            }
        }
    }
}
