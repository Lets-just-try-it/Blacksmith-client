using System;
using System.Collections.Generic;
using Blacksmith.Core;
using Blacksmith.Core.Battle;
using Blacksmith.Core.Inventory;
using Blacksmith.Inventory;
using Blacksmith.Network;
using Fusion;
using UnityEngine;

namespace Blacksmith.Battle
{
    public enum GatheringNodeStatus : byte
    {
        Available,
        // 한 플레이어가 캐는 중이다. 다른 플레이어는 캘 수 없다.
        InProgress,
        // 다 캐서 모든 플레이어에게서 사라졌다.
        Taken,
    }

    public enum GatheringRejectReason
    {
        // 다른 플레이어가 캐고 있거나 이미 다 캤다.
        Busy,
        // 낮 시간이 끝났다(영업 마감 중·밤).
        Closed,
        // 개인 인벤토리에 다 들어갈 자리가 없다.
        InventoryFull,
        // 채집할 맵이 바뀌었거나 요청이 맞지 않는다.
        Unavailable,
    }

    public struct NetworkGatheringNode : INetworkStruct
    {
        public GatheringNodeStatus Status;
        public PlayerRef User;
        public TickTimer Timer;
    }

    // 채집 맵에서 채집할 것마다의 상태다 (docs/GAME_CONCEPT.md 7.4). 낮 씬 오브젝트이며 호스트가 소유한다.
    // 모든 플레이어가 함께 쓴다. 먼저 요청한 플레이어가 캐고, 정한 시간이 지나면 재료가 그 플레이어의 개인 인벤토리로 들어간다.
    // 캐는 동안은 진행 중인 작업이라, 낮 시간이 끝나도 다 캘 때까지 "영업 마감 중"으로 기다린다.
    // 채집할 맵이 바뀌면(시드) 모두 처음 상태로 돌린다.
    public class GatheringNodesState : NetworkBehaviour, IPlayerLeft
    {
        [SerializeField] private GatheringSiteState _site;
        [SerializeField] private GatheringSiteLayout _layout;
        [SerializeField] private GameState _gameState;

        private ChangeDetector _changes;

        // 지금 상태가 어느 채집할 맵의 것인지. 채집할 맵이 없으면 0이다.
        [Networked] public int ActiveSeed { get; private set; }
        [Networked, Capacity(GatheringSiteLayout.MaxNodes)] private NetworkArray<NetworkGatheringNode> Nodes => default;

        // 스폰 직후와 상태가 바뀔 때 모든 참가자에게 불린다.
        public event Action Changed;

        public bool IsReady => Object != null && Object.IsValid;

        private void OnEnable()
        {
            _gameState.AddWorkInProgressSource(IsAnyInProgress);
        }

        private void OnDisable()
        {
            _gameState.RemoveWorkInProgressSource(IsAnyInProgress);
        }

        public override void Spawned()
        {
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
            Changed?.Invoke();
        }

        public override void Render()
        {
            bool hasChanged = false;
            foreach (string _ in _changes.DetectChanges(this))
            {
                hasChanged = true;
            }

            if (hasChanged)
            {
                Changed?.Invoke();
            }
        }

        // seed는 보고 있는 채집할 맵의 시드다. 상태가 다른 맵의 것이면 아직 아무도 손대지 않은 것으로 본다.
        public NetworkGatheringNode Get(int seed, int index)
        {
            if (!IsReady || seed != ActiveSeed || index < 0 || index >= GatheringSiteLayout.MaxNodes)
            {
                return default;
            }

            return Nodes[index];
        }

        // 표시 전용. 캐는 중이면 남은 시간(초), 아니면 0이다.
        public float GetRemainingSeconds(NetworkGatheringNode node)
        {
            return node.Status == GatheringNodeStatus.InProgress ? node.Timer.RemainingTime(Runner) ?? 0f : 0f;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
            {
                return;
            }

            int seed = _site.IsReady && _site.HasSite ? _site.Seed : 0;
            if (seed != ActiveSeed)
            {
                ResetAll(seed);
                return;
            }

            for (int i = 0; i < GatheringSiteLayout.MaxNodes; i++)
            {
                NetworkGatheringNode node = Nodes[i];
                if (node.Status == GatheringNodeStatus.InProgress && node.Timer.Expired(Runner))
                {
                    Finish(i, node.User);
                }
            }
        }

        // 호스트 전용. 거절하면 false와 이유를 돌려준다.
        public bool TryStart(PlayerRef player, PlayerInventoryState inventory, int seed, int index, out GatheringRejectReason reason)
        {
            reason = GatheringRejectReason.Unavailable;
            if (!HasStateAuthority || seed == 0 || seed != ActiveSeed || !_layout.TryRefresh()
                || index < 0 || index >= _layout.Nodes.Count)
            {
                return false;
            }

            if (!_gameState.IsReady || !DayCycleRules.CanStartWork(_gameState.Phase))
            {
                reason = GatheringRejectReason.Closed;
                return false;
            }

            if (Nodes[index].Status != GatheringNodeStatus.Available || IsGathering(player))
            {
                reason = GatheringRejectReason.Busy;
                return false;
            }

            if (!inventory.CanAddAll(ToStacks(_layout.GetYield(index))))
            {
                reason = GatheringRejectReason.InventoryFull;
                return false;
            }

            float seconds = _layout.Rules.GetSeconds(_layout.Nodes[index].Kind);
            Nodes.Set(index, new NetworkGatheringNode
            {
                Status = GatheringNodeStatus.InProgress,
                User = player,
                Timer = TickTimer.CreateFromSeconds(Runner, seconds),
            });
            return true;
        }

        // 캐던 플레이어가 나가면 다른 플레이어가 캘 수 있게 돌려놓는다.
        public void PlayerLeft(PlayerRef player)
        {
            if (!HasStateAuthority)
            {
                return;
            }

            for (int i = 0; i < GatheringSiteLayout.MaxNodes; i++)
            {
                if (Nodes[i].Status == GatheringNodeStatus.InProgress && Nodes[i].User == player)
                {
                    Nodes.Set(i, default);
                }
            }
        }

        // 다 캐면 재료를 넣는다. 그사이 인벤토리가 차서 다 들어가지 않으면 캐지 않은 것으로 돌려 다시 캘 수 있게 한다.
        private void Finish(int index, PlayerRef user)
        {
            PlayerInventoryState inventory = FindInventory(user);
            if (inventory == null || !_layout.TryRefresh() || !inventory.TryAddAll(ToStacks(_layout.GetYield(index))))
            {
                Nodes.Set(index, default);
                return;
            }

            Nodes.Set(index, new NetworkGatheringNode { Status = GatheringNodeStatus.Taken, User = user });
        }

        private PlayerInventoryState FindInventory(PlayerRef player)
        {
            return Runner.TryGetPlayerObject(player, out NetworkObject playerObject)
                && playerObject.TryGetComponent(out PlayerInventoryState inventory)
                ? inventory
                : null;
        }

        private bool IsGathering(PlayerRef player)
        {
            for (int i = 0; i < GatheringSiteLayout.MaxNodes; i++)
            {
                if (Nodes[i].Status == GatheringNodeStatus.InProgress && Nodes[i].User == player)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsAnyInProgress()
        {
            if (!IsReady)
            {
                return false;
            }

            for (int i = 0; i < GatheringSiteLayout.MaxNodes; i++)
            {
                if (Nodes[i].Status == GatheringNodeStatus.InProgress)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResetAll(int seed)
        {
            ActiveSeed = seed;
            for (int i = 0; i < GatheringSiteLayout.MaxNodes; i++)
            {
                Nodes.Set(i, default);
            }
        }

        // 재료는 등급이 없어 0등급으로 넣는다.
        private static List<ItemStack> ToStacks(List<MaterialReward> materials)
        {
            var stacks = new List<ItemStack>(materials.Count);
            foreach (MaterialReward material in materials)
            {
                stacks.Add(new ItemStack(material.MaterialId, 0, material.Count));
            }

            return stacks;
        }
    }
}
