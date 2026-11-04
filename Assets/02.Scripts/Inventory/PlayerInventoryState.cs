using Fusion;
using UnityEngine;

namespace Blacksmith.Inventory
{
    // 플레이어마다 가지는 개인 슬롯 8칸이다. Player 프리팹에 붙고, 다른 인벤토리와 같이 호스트가 값을 바꾼다.
    // 재접속하면 PlayerRef와 플레이어 오브젝트가 바뀌므로, 슬롯을 이어 주는 일은 플레이어 식별 방식이 정해진 뒤 추가한다.
    public class PlayerInventoryState : NetworkInventory
    {
        // [Networked] 배열 크기는 컴파일할 때 정해야 해서 상수로 둔다. 기획상 개인 슬롯 수와 같다.
        public const int MaxSlotCount = 8;

        [SerializeField] private InventoryChannel _channel;

        [Networked, Capacity(MaxSlotCount)] private NetworkArray<NetworkItemStack> Slots => default;
        [Networked] private int SlotRows { get; set; }
        // 손에 든 슬롯 번호 + 1. 0이면 아무것도 들지 않았다. 기본값 0이 '들지 않음'이 되도록 한 칸 밀어 둔다.
        [Networked] private int HeldSlotNumber { get; set; }

        // 손에 든 슬롯 번호. 아무것도 들지 않았으면 -1이다. 다른 참가자 화면에 손에 든 물건을 보일 때 쓴다.
        // 내 화면은 지연 없이 보이도록 InventoryChannel.HeldSlot을 쓴다.
        public int HeldSlot => HeldSlotNumber - 1;

        protected override int MaxCapacity => MaxSlotCount;

        protected override int StateRows
        {
            get => SlotRows;
            set => SlotRows = value;
        }

        public override void Spawned()
        {
            base.Spawned();
            if (HasInputAuthority)
            {
                _channel.Register(this);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _channel.Unregister(this);
        }

        // 호스트 전용.
        public void SetHeldSlot(int slot)
        {
            if (HasStateAuthority && slot >= -1 && slot < MaxSlotCount)
            {
                HeldSlotNumber = slot + 1;
            }
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
