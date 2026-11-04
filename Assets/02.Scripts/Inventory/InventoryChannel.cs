using System;
using Blacksmith.Core.Inventory;
using UnityEngine;

namespace Blacksmith.Inventory
{
    // 씬의 인벤토리 화면과 Player 프리팹의 InventoryPlayerActions·PlayerInventoryState를 잇는 에셋이다.
    // 화면은 내 플레이어 오브젝트를 몰라도 이 에셋으로 이동을 요청하고, 다른 화면에서 시작한 드래그도 받아 놓을 수 있다.
    // 빠른 이동(Shift+클릭, 더블 클릭)의 받는 쪽과 스택 나누기 개수 입력 창도 이 에셋으로 잇는다.
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

        // 지금 내 화면에 열려 있는 공용 창고. 빠른 이동으로 내 슬롯의 물건을 보낼 곳이다. 닫혀 있으면 null이다.
        public NetworkInventory OpenStorage { get; private set; }

        // 손에 든 내 개인 슬롯 번호. 아무것도 들지 않았으면 -1이다. 내 PC에서 정하고, 제작 요청에 실려 호스트에 간다.
        // 다른 참가자가 손에 든 물건을 볼 수 있도록 호스트에도 알린다(PlayerInventoryState.HeldSlot).
        public int HeldSlot { get; private set; } = -1;

        public event Action HeldSlotChanged;

        // 개수를 물어보는 중인 스택 나누기. 없으면 SplitSource가 null이다.
        public NetworkInventory SplitSource { get; private set; }
        public int SplitSlot { get; private set; }
        public NetworkInventory SplitTarget { get; private set; }
        public int SplitTargetSlot { get; private set; }

        public event Action SplitRequested;
        public event Action SplitClosed;

        public void SetHeldSlot(int slot)
        {
            if (HeldSlot != slot)
            {
                HeldSlot = slot;
                if (_localActions != null)
                {
                    _localActions.RequestHeldSlot(slot);
                }

                HeldSlotChanged?.Invoke();
            }
        }

        public void SetOpenStorage(NetworkInventory storage)
        {
            OpenStorage = storage;
        }

        public void ClearOpenStorage(NetworkInventory storage)
        {
            if (OpenStorage == storage)
            {
                OpenStorage = null;
            }
        }

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
                SetHeldSlot(-1);
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

        // 드래그 중인 칸을 target의 targetSlot에 놓는다. split이면 바로 옮기지 않고 개수 입력 창을 띄운다.
        public void DropOn(NetworkInventory target, int targetSlot, bool split)
        {
            if (DragSource != null && _localActions != null)
            {
                if (split)
                {
                    RequestSplit(DragSource, DragSlot, target, targetSlot);
                }
                else
                {
                    _localActions.RequestMove(DragSource, DragSlot, target, targetSlot);
                }
            }

            EndDrag();
        }

        // 내 슬롯과 열려 있는 공용 창고 사이에서 칸의 물건을 반대쪽으로 보낸다. 반대쪽이 없으면 아무것도 하지 않는다.
        public void QuickMove(NetworkInventory source, int slot)
        {
            NetworkInventory target = source == LocalPlayerInventory ? OpenStorage
                : source == OpenStorage ? LocalPlayerInventory
                : null;
            if (target != null && _localActions != null)
            {
                _localActions.RequestTransfer(source, slot, target);
            }
        }

        // 개수 입력 창에서 고른 개수만큼 옮긴다.
        public void ConfirmSplit(int count)
        {
            if (SplitSource != null && _localActions != null)
            {
                _localActions.RequestMoveCount(SplitSource, SplitSlot, SplitTarget, SplitTargetSlot, count);
            }

            CloseSplit();
        }

        public void CloseSplit()
        {
            if (SplitSource != null)
            {
                SplitSource = null;
                SplitTarget = null;
                SplitClosed?.Invoke();
            }
        }

        // 창이 닫혀 inventory가 화면에서 사라지면, 그 인벤토리에 걸린 개수 입력도 닫는다.
        public void CloseSplit(NetworkInventory inventory)
        {
            if (inventory != null && (SplitSource == inventory || SplitTarget == inventory))
            {
                CloseSplit();
            }
        }

        // 두 개 이상 쌓인 칸을 빈 칸이나 같은 물건 칸에 놓을 때만 나눈다.
        private void RequestSplit(NetworkInventory source, int slot, NetworkInventory target, int targetSlot)
        {
            ItemStack moving = source.Get(slot);
            ItemStack destination = target.Get(targetSlot);
            bool sameSlot = source == target && slot == targetSlot;
            if (sameSlot || moving.Count < 2 || (!destination.IsEmpty && !destination.IsSameItem(moving)))
            {
                return;
            }

            CloseSplit();
            SplitSource = source;
            SplitSlot = slot;
            SplitTarget = target;
            SplitTargetSlot = targetSlot;
            SplitRequested?.Invoke();
        }
    }
}
