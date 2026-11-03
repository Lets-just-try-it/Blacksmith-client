using Fusion;
using UnityEngine;

namespace Blacksmith.Inventory
{
    // Player 프리팹에 붙는 인벤토리 행동 컴포넌트다. 화면에서 칸을 끌어다 놓으면 호스트에 이동을 요청한다.
    // 같은 인벤토리 안의 이동과 서로 다른 인벤토리 사이의 이동(공용 창고 ↔ 플레이어 슬롯)을 같은 RPC로 처리한다.
    public class InventoryPlayerActions : NetworkBehaviour
    {
        [SerializeField] private InventoryChannel _channel;

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

        // expected: 요청할 때 화면에 보이던 출발 칸의 물건.
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcMoveItem(NetworkId fromId, int fromSlot, NetworkId toId, int toSlot, NetworkItemStack expected)
        {
            if (!TryFindInventory(fromId, out NetworkInventory from) || !TryFindInventory(toId, out NetworkInventory to))
            {
                return;
            }

            // 요청이 도착하기 전에 다른 참가자가 같은 칸을 먼저 옮겼으면 칸의 물건이 달라졌으므로 무시한다.
            if (!from.Get(fromSlot).Equals(expected.ToStack()))
            {
                return;
            }

            NetworkInventory.TryMove(from, fromSlot, to, toSlot);
        }

        private bool TryFindInventory(NetworkId id, out NetworkInventory inventory)
        {
            inventory = null;
            return Runner.TryFindObject(id, out NetworkObject found) && found.TryGetComponent(out inventory);
        }
    }
}
