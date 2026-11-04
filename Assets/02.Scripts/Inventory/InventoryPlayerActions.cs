using Fusion;
using UnityEngine;

namespace Blacksmith.Inventory
{
    // Player 프리팹에 붙는 인벤토리 행동 컴포넌트다. 화면에서 칸을 끌어다 놓거나 빠른 이동·스택 나누기를 하면 호스트에 요청한다.
    // 같은 인벤토리 안의 이동과 서로 다른 인벤토리 사이의 이동(공용 창고 ↔ 플레이어 슬롯)을 같은 RPC로 처리한다.
    // 손에 든 슬롯 번호도 다른 참가자가 손에 든 물건을 볼 수 있도록 호스트에 알린다.
    public class InventoryPlayerActions : NetworkBehaviour
    {
        [SerializeField] private InventoryChannel _channel;
        [SerializeField] private PlayerInventoryState _playerInventory;

        public override void Spawned()
        {
            if (HasInputAuthority)
            {
                _channel.Register(this);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _channel.Unregister(this);
        }

        public void RequestMove(NetworkInventory from, int fromSlot, NetworkInventory to, int toSlot)
        {
            if (!from.IsReady || !to.IsReady)
            {
                return;
            }

            RpcMoveItem(from.Object.Id, fromSlot, to.Object.Id, toSlot, NetworkItemStack.FromStack(from.Get(fromSlot)));
        }

        public void RequestMoveCount(NetworkInventory from, int fromSlot, NetworkInventory to, int toSlot, int count)
        {
            if (!from.IsReady || !to.IsReady)
            {
                return;
            }

            RpcMoveCount(from.Object.Id, fromSlot, to.Object.Id, toSlot, count, NetworkItemStack.FromStack(from.Get(fromSlot)));
        }

        public void RequestTransfer(NetworkInventory from, int fromSlot, NetworkInventory to)
        {
            if (!from.IsReady || !to.IsReady)
            {
                return;
            }

            RpcTransfer(from.Object.Id, fromSlot, to.Object.Id, NetworkItemStack.FromStack(from.Get(fromSlot)));
        }

        public void RequestHeldSlot(int slot)
        {
            if (Object != null && Object.IsValid)
            {
                RpcSetHeldSlot(slot);
            }
        }

        // expected: 요청할 때 화면에 보이던 출발 칸의 물건.
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcMoveItem(NetworkId fromId, int fromSlot, NetworkId toId, int toSlot, NetworkItemStack expected)
        {
            if (TryFindSource(fromId, fromSlot, expected, out NetworkInventory from) && TryFindInventory(toId, out NetworkInventory to))
            {
                NetworkInventory.TryMove(from, fromSlot, to, toSlot);
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcMoveCount(NetworkId fromId, int fromSlot, NetworkId toId, int toSlot, int count, NetworkItemStack expected)
        {
            if (TryFindSource(fromId, fromSlot, expected, out NetworkInventory from) && TryFindInventory(toId, out NetworkInventory to))
            {
                NetworkInventory.TryMoveCount(from, fromSlot, to, toSlot, count);
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcTransfer(NetworkId fromId, int fromSlot, NetworkId toId, NetworkItemStack expected)
        {
            if (TryFindSource(fromId, fromSlot, expected, out NetworkInventory from) && TryFindInventory(toId, out NetworkInventory to))
            {
                NetworkInventory.TryTransfer(from, fromSlot, to);
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcSetHeldSlot(int slot)
        {
            _playerInventory.SetHeldSlot(slot);
        }

        // 요청이 도착하기 전에 다른 참가자가 같은 칸을 먼저 옮겼으면 칸의 물건이 달라졌으므로 무시한다.
        private bool TryFindSource(NetworkId id, int slot, NetworkItemStack expected, out NetworkInventory inventory)
        {
            return TryFindInventory(id, out inventory) && inventory.Get(slot).Equals(expected.ToStack());
        }

        private bool TryFindInventory(NetworkId id, out NetworkInventory inventory)
        {
            inventory = null;
            return Runner.TryFindObject(id, out NetworkObject found) && found.TryGetComponent(out inventory);
        }
    }
}
