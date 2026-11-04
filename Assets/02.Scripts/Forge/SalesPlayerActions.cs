using Blacksmith.Core.Inventory;
using Blacksmith.Inventory;
using Fusion;
using UnityEngine;

namespace Blacksmith.Forge
{
    // Player 프리팹에 붙는 판매 행동 컴포넌트다. 진열대에 장비를 올리거나 내리는 요청을 호스트에 보낸다.
    // 호스트는 요청한 사람의 슬롯에 그 장비가 있는지, 진열대가 비었는지만 확인한 뒤 옮긴다.
    public class SalesPlayerActions : NetworkBehaviour
    {
        [SerializeField] private PlayerInventoryState _inventory;

        public void RequestPlace(DisplayShelfState shelf, int slot)
        {
            if (shelf.IsReady)
            {
                RpcPlace(shelf.Object.Id, slot);
            }
        }

        public void RequestTake(DisplayShelfState shelf)
        {
            if (shelf.IsReady)
            {
                RpcTake(shelf.Object.Id);
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcPlace(NetworkId shelfId, int slot)
        {
            if (!TryFindShelf(shelfId, out DisplayShelfState shelf) || !shelf.IsEmpty)
            {
                return;
            }

            ItemStack stack = _inventory.Get(slot);
            if (stack.IsEmpty || !_inventory.TryRemove(slot, 1, out ItemStack removed))
            {
                return;
            }

            // 장비가 아니어서 올리지 못했으면 원래대로 돌려 놓는다.
            if (!shelf.TryPlace(removed))
            {
                _inventory.TryAdd(removed);
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcTake(NetworkId shelfId)
        {
            if (!TryFindShelf(shelfId, out DisplayShelfState shelf) || shelf.IsEmpty)
            {
                return;
            }

            // 슬롯에 자리가 있을 때만 내린다.
            if (_inventory.TryAdd(shelf.Item))
            {
                shelf.Take();
            }
        }

        private bool TryFindShelf(NetworkId id, out DisplayShelfState shelf)
        {
            shelf = null;
            return Runner.TryFindObject(id, out NetworkObject found) && found.TryGetComponent(out shelf);
        }
    }
}
