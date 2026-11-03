using Fusion;

namespace Blacksmith.Inventory
{
    // 대장간에 하나 있는 공용 창고다. 같이 플레이하는 누구나 열어 보고 물건을 넣고 뺄 수 있다.
    // 창고를 여는 상자(SharedInventoryBox)와 분리해 두어, 제작·파견 창·전투 보상은 이 상태를 직접 참조한다.
    public class SharedInventoryState : NetworkInventory
    {
        // [Networked] 배열 크기는 컴파일할 때 정해야 해서 상수로 둔다. 가로 5칸 × 최대 40줄이다.
        public const int MaxSlotCount = 200;

        [Networked, Capacity(MaxSlotCount)] private NetworkArray<NetworkItemStack> Slots => default;
        [Networked] private int SlotRows { get; set; }

        protected override int MaxCapacity => MaxSlotCount;

        protected override int StateRows
        {
            get => SlotRows;
            set => SlotRows = value;
        }

        protected override NetworkItemStack GetState(int slot)
        {
            return Slots[slot];
        }

        protected override void SetState(int slot, NetworkItemStack value)
        {
            Slots.Set(slot, value);
        }
    }
}
