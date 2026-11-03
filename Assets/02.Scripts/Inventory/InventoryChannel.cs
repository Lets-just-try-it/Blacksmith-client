using System;
using UnityEngine;

namespace Blacksmith.Inventory
{
    // 씬의 인벤토리 화면과 Player 프리팹의 InventoryPlayerActions·PlayerInventoryState를 잇는 에셋이다.
    // 화면은 내 플레이어 오브젝트를 몰라도 이 에셋으로 이동을 요청하고, 다른 화면에서 시작한 드래그도 받아 놓을 수 있다.
    [CreateAssetMenu(fileName = "InventoryChannel", menuName = "Blacksmith/Runtime/Inventory Channel")]
    public class InventoryChannel : ScriptableObject
    {
        private InventoryPlayerActions _localActions;

        // 드래그 중인 칸. 드래그가 없으면 DragSource가 null이다.
        public NetworkInventory DragSource { get; private set; }
        public int DragSlot { get; private set; }

        // 내 플레이어의 슬롯. 내 플레이어가 스폰되기 전이나 디스폰된 뒤에는 null이다.
        public PlayerInventoryState LocalPlayerInventory { get; private set; }

        public event Action LocalPlayerInventoryChanged;

        public void Register(InventoryPlayerActions actions)
        {
            _localActions = actions;
        }

        public void Unregister(InventoryPlayerActions actions)
        {
            if (_localActions == actions)
            {
                _localActions = null;
            }
        }

        public void Register(PlayerInventoryState inventory)
        {
            LocalPlayerInventory = inventory;
            LocalPlayerInventoryChanged?.Invoke();
        }

        public void Unregister(PlayerInventoryState inventory)
        {
            if (LocalPlayerInventory == inventory)
            {
                LocalPlayerInventory = null;
                LocalPlayerInventoryChanged?.Invoke();
            }
        }

        public void BeginDrag(NetworkInventory source, int slot)
        {
            DragSource = source;
            DragSlot = slot;
        }

        public void EndDrag()
        {
            DragSource = null;
        }

        // 드래그 중인 칸을 target의 targetSlot에 놓는다.
        public void DropOn(NetworkInventory target, int targetSlot)
        {
            if (DragSource != null && _localActions != null)
            {
                _localActions.RequestMove(DragSource, DragSlot, target, targetSlot);
            }

            EndDrag();
        }
    }
}
