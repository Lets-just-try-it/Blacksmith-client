using Blacksmith.Core.Inventory;
using Blacksmith.Inventory;
using Fusion;
using UnityEngine;

namespace Blacksmith.Plaza
{
    // Player 프리팹에 붙는 여관 행동 컴포넌트다. 여관 대화로 연 장비 착용 창에서 모험가에게 장비를 건네고,
    // 모험가 목록 창에서 파견 파티에서 빼는 요청을 호스트에 보낸다. 호스트는 InnResidents가 처리한다.
    public class InnPlayerActions : NetworkBehaviour
    {
        [SerializeField] private InnChannel _channel;

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

        // storage: 바꾼 장비가 돌아갈 공용 창고. 칸에 맞는지는 장비 창이 먼저 확인한다.
        public void RequestGift(InnResidents inn, NetworkInventory from, int fromSlot, int adventurerId, EquipmentSlot slot,
            NetworkInventory storage)
        {
            if (IsReady() && inn.Object != null && from.IsReady && storage.IsReady)
            {
                RpcGift(inn.Object.Id, from.Object.Id, fromSlot, NetworkItemStack.FromStack(from.Get(fromSlot)), adventurerId,
                    (int)slot, storage.Object.Id);
            }
        }

        public void RequestLeaveParty(InnResidents inn, int adventurerId)
        {
            if (IsReady() && inn.Object != null)
            {
                RpcLeaveParty(inn.Object.Id, adventurerId);
            }
        }

        // expected: 요청할 때 화면에 보이던 출발 칸의 물건. 그사이 다른 참가자가 옮겼으면 무시한다.
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcGift(NetworkId innId, NetworkId fromId, int fromSlot, NetworkItemStack expected, int adventurerId, int slot,
            NetworkId storageId)
        {
            if (TryFind(innId, out InnResidents inn)
                && TryFind(fromId, out NetworkInventory from) && from.Get(fromSlot).Equals(expected.ToStack())
                && TryFind(storageId, out NetworkInventory storage))
            {
                inn.Gift(from, fromSlot, adventurerId, (EquipmentSlot)slot, storage);
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcLeaveParty(NetworkId innId, int adventurerId)
        {
            if (TryFind(innId, out InnResidents inn))
            {
                inn.RemoveFromParty(adventurerId);
            }
        }

        private bool IsReady()
        {
            return Object != null && Object.IsValid;
        }

        private bool TryFind<T>(NetworkId id, out T component) where T : Component
        {
            component = null;
            return Runner.TryFindObject(id, out NetworkObject found) && found.TryGetComponent(out component);
        }
    }
}
