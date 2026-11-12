using System;
using Blacksmith.Inventory;
using Fusion;
using UnityEngine;

namespace Blacksmith.Battle
{
    // Player 프리팹에 붙는 채집 행동 컴포넌트다. 채집 맵에서 채집할 것을 캐겠다고 호스트에 요청한다 (docs/GAME_CONCEPT.md 7.4).
    // 허락되면 호스트가 GatheringNodesState를 바꾸고, 거절되면 요청한 참가자에게만 이유를 알린다.
    public class GatheringPlayerActions : NetworkBehaviour
    {
        [SerializeField] private PlayerInventoryState _inventory;

        // 요청한 참가자에서만 불린다.
        public event Action<GatheringRejectReason> Rejected;

        // seed: 보고 있는 채집할 맵의 시드. 그사이 맵이 바뀌었으면 호스트가 거절한다.
        public void RequestGather(GatheringNodesState nodes, int seed, int index)
        {
            if (Object != null && Object.IsValid && nodes.IsReady)
            {
                RpcRequestGather(nodes.Object.Id, seed, index);
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcRequestGather(NetworkId nodesId, int seed, int index)
        {
            GatheringRejectReason reason = GatheringRejectReason.Unavailable;
            if (Runner.TryFindObject(nodesId, out NetworkObject found) && found.TryGetComponent(out GatheringNodesState nodes)
                && nodes.TryStart(Object.InputAuthority, _inventory, seed, index, out reason))
            {
                return;
            }

            RpcRejected(reason);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
        private void RpcRejected(GatheringRejectReason reason)
        {
            Rejected?.Invoke(reason);
        }
    }
}
