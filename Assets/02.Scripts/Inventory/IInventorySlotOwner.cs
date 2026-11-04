using UnityEngine.EventSystems;

namespace Blacksmith.Inventory
{
    // 칸 화면(InventorySlotView)이 끌기·놓기·클릭을 넘기는 창이다. 인벤토리 창과 장비 착용 창이 구현한다.
    public interface IInventorySlotOwner
    {
        void BeginDrag(InventorySlotView slot, PointerEventData eventData);
        void MoveDrag(PointerEventData eventData);
        void EndDrag();
        void DropOn(InventorySlotView slot);
        void Click(InventorySlotView slot, int clickCount);
    }
}
