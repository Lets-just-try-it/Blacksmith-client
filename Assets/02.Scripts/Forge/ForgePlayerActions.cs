using Blacksmith.Core.Forge;
using Blacksmith.Core.Inventory;
using Blacksmith.Inventory;
using Fusion;
using UnityEngine;

namespace Blacksmith.Forge
{
    // Player 프리팹에 붙는 대장간 행동 컴포넌트다. 작업대 사용 요청과 미니게임 결과를 호스트에 보내고,
    // 호스트는 사용권을 확인한 뒤 손에 든 재료를 다음 단계의 형태로 바꾼다.
    // 규칙은 docs/NETWORK_DESIGN.md의 "작업대 사용권 관리"와 "미니게임 진행과 결과 처리"를 따른다.
    public class ForgePlayerActions : NetworkBehaviour
    {
        [SerializeField] private ForgeChannel _channel;
        [SerializeField] private PlayerInventoryState _inventory;
        [SerializeField] private GradeThresholdData _grades;

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

        public void RequestStart(ForgeStationState station, int slot)
        {
            if (station.IsReady)
            {
                RpcRequestStart(station.Object.Id, slot);
            }
        }

        public void SubmitResult(ForgeStationState station, int sessionId, float accuracy)
        {
            if (station.IsReady)
            {
                RpcSubmitResult(station.Object.Id, sessionId, accuracy);
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcRequestStart(NetworkId stationId, int slot)
        {
            if (!TryFindStation(stationId, out ForgeStationState station))
            {
                RpcStartRejected(stationId, (int)ForgeRejectReason.Unavailable);
                return;
            }

            // 손에 든 물건이 이 작업대가 받는 재료인지 확인한다. 상태 정합성만 보고 정확도는 따지지 않는다.
            ItemStack held = _inventory.Get(slot);
            if (held.IsEmpty || station.Recipe == null || !station.Recipe.TryFindStep(station.Kind, held.ItemId, out _))
            {
                RpcStartRejected(stationId, (int)ForgeRejectReason.WrongItem);
                return;
            }

            if (station.TryAcquire(Object.InputAuthority, slot, out int sessionId))
            {
                RpcStartApproved(stationId, sessionId);
            }
            else
            {
                RpcStartRejected(stationId, (int)ForgeRejectReason.Busy);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
        private void RpcStartApproved(NetworkId stationId, int sessionId)
        {
            if (TryFindStation(stationId, out ForgeStationState station))
            {
                _channel.RaiseApproved(station, sessionId);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
        private void RpcStartRejected(NetworkId stationId, int reason)
        {
            if (TryFindStation(stationId, out ForgeStationState station))
            {
                _channel.RaiseRejected(station, (ForgeRejectReason)reason);
            }
        }

        // 결과를 보낸 사람, 세션, 중복 여부를 확인한 뒤 재료를 바꾸고 사용권을 푼다. 중복으로 온 결과는 무시한다.
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcSubmitResult(NetworkId stationId, int sessionId, float accuracy)
        {
            if (!TryFindStation(stationId, out ForgeStationState station)
                || !station.TryFinish(Object.InputAuthority, sessionId, out int slot))
            {
                return;
            }

            ApplyResult(station, slot, accuracy);
        }

        private void ApplyResult(ForgeStationState station, int slot, float accuracy)
        {
            ItemStack held = _inventory.Get(slot);
            if (held.IsEmpty || station.Recipe == null || !station.Recipe.TryFindStep(station.Kind, held.ItemId, out int stepIndex))
            {
                // 미니게임 중에 재료가 사라졌거나 바뀌었다. 바꿀 것이 없으므로 사용권만 풀린 채 끝낸다.
                return;
            }

            CraftingRecipeData.Step step = station.Recipe.GetStep(stepIndex);
            ItemStack output = CraftingRules.MakeOutput(step.Output.ItemId, stepIndex, station.Recipe.IsFinalStep(stepIndex), held.Grade, accuracy, _grades.LowerBounds);
            _inventory.TryReplaceOne(slot, output);
        }

        private bool TryFindStation(NetworkId id, out ForgeStationState station)
        {
            station = null;
            return Runner.TryFindObject(id, out NetworkObject found) && found.TryGetComponent(out station);
        }
    }
}
