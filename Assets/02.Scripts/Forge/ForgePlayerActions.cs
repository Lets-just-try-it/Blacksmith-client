using System.Collections.Generic;
using Blacksmith.Core.Forge;
using Blacksmith.Core.Inventory;
using Blacksmith.Inventory;
using Fusion;
using UnityEngine;

namespace Blacksmith.Forge
{
    // Player 프리팹에 붙는 대장간 행동 컴포넌트다. 작업대 열기·닫기·제작 요청과 미니게임 결과를 호스트에 보내고,
    // 호스트는 사용권과 개인 슬롯의 재료를 확인한 뒤 미니게임이 끝날 때 재료를 빼고 결과물을 만든다.
    // 제작 요청에는 쓸 재료의 예약(개인 슬롯 칸마다 개수)이 실려 있다. 재료는 그때까지 개인 슬롯에 그대로 있다.
    // 규칙은 docs/NETWORK_DESIGN.md의 "작업대 사용권 관리"와 "미니게임 진행과 결과 처리"를 따른다.
    public class ForgePlayerActions : NetworkBehaviour
    {
        [SerializeField] private ForgeChannel _channel;
        [SerializeField] private PlayerInventoryState _inventory;
        [SerializeField] private GradeThresholdData _grades;
        [SerializeField] private SmeltingRulesData _smeltingRules;

        // 호스트에서 재료를 확인할 때 쓰는 작업 목록. 매번 새로 만들지 않는다.
        private readonly int[] _reservedCounts = new int[CraftingReservation.MaxSlots];
        private readonly List<ItemStack> _slotBuffer = new List<ItemStack>();
        private readonly List<ItemStack> _reservedBuffer = new List<ItemStack>();
        private readonly List<CraftingIngredient> _ingredientBuffer = new List<CraftingIngredient>();
        private readonly List<SlotRemoval> _removalBuffer = new List<SlotRemoval>();

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

        public void RequestOpen(ForgeStationState station)
        {
            if (station.IsReady)
            {
                RpcRequestOpen(station.Object.Id);
            }
        }

        public void RequestClose(ForgeStationState station)
        {
            if (station.IsReady)
            {
                RpcRequestClose(station.Object.Id);
            }
        }

        public void RequestCraft(ForgeStationState station, int entryIndex, long reserved)
        {
            if (station.IsReady)
            {
                RpcRequestCraft(station.Object.Id, entryIndex, reserved);
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
        private void RpcRequestOpen(NetworkId stationId)
        {
            if (!TryFindStation(stationId, out ForgeStationState station))
            {
                RpcRejected(stationId, (int)ForgeRejectReason.Unavailable);
                return;
            }

            if (!_channel.IsOpen)
            {
                RpcRejected(stationId, (int)ForgeRejectReason.Closed);
                return;
            }

            if (station.TryOpen(Object.InputAuthority))
            {
                RpcOpened(stationId);
            }
            else
            {
                RpcRejected(stationId, (int)ForgeRejectReason.Busy);
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcRequestClose(NetworkId stationId)
        {
            if (TryFindStation(stationId, out ForgeStationState station))
            {
                station.TryClose(Object.InputAuthority);
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcRequestCraft(NetworkId stationId, int entryIndex, long reserved)
        {
            if (!TryFindStation(stationId, out ForgeStationState station)
                || !station.IsUsedBy(Object.InputAuthority)
                || !station.TryGetEntry(entryIndex, out CraftingEntry entry))
            {
                RpcRejected(stationId, (int)ForgeRejectReason.Unavailable);
                return;
            }

            if (!_channel.IsOpen)
            {
                RpcRejected(stationId, (int)ForgeRejectReason.Closed);
                return;
            }

            if (!TryPlan(entry, reserved, out _))
            {
                RpcRejected(stationId, (int)ForgeRejectReason.MissingMaterials);
                return;
            }

            if (station.TryBeginCraft(Object.InputAuthority, entryIndex, reserved, out int sessionId))
            {
                RpcCraftApproved(stationId, sessionId, entryIndex);
            }
            else
            {
                RpcRejected(stationId, (int)ForgeRejectReason.Busy);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
        private void RpcOpened(NetworkId stationId)
        {
            if (TryFindStation(stationId, out ForgeStationState station))
            {
                _channel.RaiseOpened(station);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
        private void RpcCraftApproved(NetworkId stationId, int sessionId, int entryIndex)
        {
            if (TryFindStation(stationId, out ForgeStationState station))
            {
                _channel.RaiseCraftApproved(station, sessionId, entryIndex);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
        private void RpcRejected(NetworkId stationId, int reason)
        {
            if (TryFindStation(stationId, out ForgeStationState station))
            {
                _channel.RaiseRejected(station, (ForgeRejectReason)reason);
            }
        }

        // 결과를 보낸 사람, 세션, 중복 여부를 확인한 뒤 예약한 재료를 개인 슬롯에서 빼고 결과물을 만든다. 중복으로 온 결과는 무시한다.
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcSubmitResult(NetworkId stationId, int sessionId, float accuracy)
        {
            if (!TryFindStation(stationId, out ForgeStationState station)
                || !station.TryFinish(Object.InputAuthority, sessionId, out int entryIndex, out long reserved)
                || !station.TryGetEntry(entryIndex, out CraftingEntry entry))
            {
                return;
            }

            ApplyResult(entry, reserved, accuracy);
        }

        private void ApplyResult(CraftingEntry entry, long reserved, float accuracy)
        {
            if (!TryPlan(entry, reserved, out int previousPercent))
            {
                // 미니게임 중에 재료가 사라졌다. 바꿀 것이 없으므로 사용권만 남긴 채 끝낸다.
                return;
            }

            foreach (SlotRemoval removal in _removalBuffer)
            {
                _inventory.TryRemove(removal.Slot, removal.Count, out _);
            }

            ItemStack output = entry.IsSmelting
                ? CraftingRules.MakeIngots(entry.Output.ItemId, _smeltingRules.ToRules().IngotCount(accuracy))
                : CraftingRules.MakeOutput(entry.Output.ItemId, entry.ScoredStepsDone, entry.IsFinal, previousPercent, accuracy, _grades.LowerBounds);
            if (!_inventory.TryAdd(output))
            {
                Debug.LogWarning($"결과물 {entry.Output.DisplayName}을(를) 둘 자리가 없어 사라졌다.");
            }
        }

        // 예약한 재료가 개인 슬롯에 실제로 있고 이 제작 항목의 재료를 채우는지 따지고, 뺄 계획을 _removalBuffer에 채운다.
        private bool TryPlan(CraftingEntry entry, long reserved, out int previousPercent)
        {
            previousPercent = 0;
            CraftingReservation.Unpack(reserved, _reservedCounts);
            _slotBuffer.Clear();
            for (int i = 0; i < _inventory.SlotCount; i++)
            {
                _slotBuffer.Add(_inventory.Get(i));
            }

            if (!CraftingReservation.TryBuildStacks(_reservedCounts, _slotBuffer, _reservedBuffer))
            {
                return false;
            }

            entry.GetIngredients(_ingredientBuffer);
            return CraftingRequirements.TryPlan(_ingredientBuffer, _reservedBuffer, IsIntermediate, _removalBuffer, out previousPercent);
        }

        private bool IsIntermediate(int itemId)
        {
            ItemData item = _inventory.Catalog.Find(itemId);
            return item != null && item.Kind == ItemKind.Intermediate;
        }

        private bool TryFindStation(NetworkId id, out ForgeStationState station)
        {
            station = null;
            return Runner.TryFindObject(id, out NetworkObject found) && found.TryGetComponent(out station);
        }
    }
}
