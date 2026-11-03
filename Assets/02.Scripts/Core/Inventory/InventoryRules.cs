namespace Blacksmith.Core.Inventory
{
    // 인벤토리 격자의 크기 규칙이다. 칸 번호는 왼쪽 위부터 가로로 센다(줄 × Columns + 열).
    // 늘어나지 않는 인벤토리는 GrowRows를 0, MaxRows를 InitialRows와 같게 둔다.
    public class InventoryRules
    {
        public int Columns { get; set; }
        public int InitialRows { get; set; }
        // 넣을 빈 칸이 없을 때 한 번에 늘리는 줄 수.
        public int GrowRows { get; set; }
        public int MaxRows { get; set; }

        public int MaxSlotCount => Columns * MaxRows;
    }
}
