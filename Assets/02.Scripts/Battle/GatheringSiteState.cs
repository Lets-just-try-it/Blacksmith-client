using System;
using System.Collections.Generic;
using Blacksmith.Core.Battle;
using Fusion;
using UnityEngine;

namespace Blacksmith.Battle
{
    // BattleCorpse를 [Networked]에 담기 위한 형식이다. Core는 Fusion을 모르므로 필드를 풀어서 담는다.
    public struct NetworkCorpse : INetworkStruct
    {
        public int MonsterId;
        public int GroupIndex;
        public int GroupSlot;
        public int X;
        public int Z;

        public static NetworkCorpse FromCorpse(BattleCorpse corpse)
        {
            return new NetworkCorpse
            {
                MonsterId = corpse.MonsterId,
                GroupIndex = corpse.GroupIndex,
                GroupSlot = corpse.GroupSlot,
                X = corpse.X,
                Z = corpse.Z,
            };
        }

        public BattleCorpse ToCorpse()
        {
            return new BattleCorpse(MonsterId, GroupIndex, GroupSlot, X, Z);
        }
    }

    // 다음 날 낮에 채집하러 갈 수 있는 맵이다. 전날 밤 모험가가 클리어한 스테이지의 맵(시드)과 시체 자리를 담는다 (docs/GAME_CONCEPT.md 7.4).
    // 낮 씬 오브젝트이며 호스트가 소유한다. 맵은 DungeonData.GenerateMap(Seed)로 전투와 같은 맵을 다시 만든다.
    // 이긴 전투의 결과를 반영하면 그 맵으로 바꾸고, 지거나 파견하지 않고 다음 날로 넘기면 비운다.
    // 몬스터는 시간이 지나면 다시 나타나므로, 채집할 수 있는 날은 토벌한 다음 날 하루뿐이다.
    public class GatheringSiteState : NetworkBehaviour
    {
        // [Networked] 배열 크기는 컴파일할 때 정해야 해서 상수로 둔다. 한 스테이지의 몬스터 수(현재 최대 13)보다 넉넉하다.
        public const int MaxCorpses = 32;

        [SerializeField] private BattleChannel _channel;
        [SerializeField] private DispatchState _dispatch;

        private ChangeDetector _changes;

        [Networked] public NetworkBool HasSite { get; private set; }
        [Networked] public int DungeonId { get; private set; }
        [Networked] public int Stage { get; private set; }
        [Networked] public int Seed { get; private set; }
        [Networked] private int CorpseCount { get; set; }
        [Networked, Capacity(MaxCorpses)] private NetworkArray<NetworkCorpse> Corpses => default;

        // 스폰 직후와 채집할 맵이 바뀔 때 모든 참가자에게 불린다.
        public event Action Changed;

        public bool IsReady => Object != null && Object.IsValid;

        private void OnEnable()
        {
            _channel.ResultConfirmed += ApplyResult;
            _dispatch.Skipped += Clear;
        }

        private void OnDisable()
        {
            _channel.ResultConfirmed -= ApplyResult;
            _dispatch.Skipped -= Clear;
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

        // 전투에서 몬스터가 쓰러진 자리. 채집할 맵이 없으면 비어 있다.
        public List<BattleCorpse> GetCorpses()
        {
            var corpses = new List<BattleCorpse>();
            if (IsReady && HasSite)
            {
                for (int i = 0; i < CorpseCount; i++)
                {
                    corpses.Add(Corpses[i].ToCorpse());
                }
            }

            return corpses;
        }

        // 호스트에서만 불린다. 클리어하지 못한 스테이지에서는 채집할 수 없다.
        private void ApplyResult(BattleOutcome outcome)
        {
            if (!HasStateAuthority)
            {
                return;
            }

            if (!outcome.Result.IsVictory)
            {
                Clear();
                return;
            }

            IReadOnlyList<BattleCorpse> corpses = outcome.Result.Corpses;
            if (corpses.Count > MaxCorpses)
            {
                Debug.LogWarning($"시체가 {corpses.Count}개라 앞의 {MaxCorpses}개만 채집 맵에 남긴다.", this);
            }

            HasSite = true;
            DungeonId = outcome.DungeonId;
            Stage = outcome.Stage;
            Seed = outcome.Seed;
            CorpseCount = Math.Min(corpses.Count, MaxCorpses);
            for (int i = 0; i < MaxCorpses; i++)
            {
                Corpses.Set(i, i < CorpseCount ? NetworkCorpse.FromCorpse(corpses[i]) : default);
            }
        }

        // 호스트에서만 불린다.
        private void Clear()
        {
            if (!HasStateAuthority)
            {
                return;
            }

            HasSite = false;
            DungeonId = 0;
            Stage = 0;
            Seed = 0;
            CorpseCount = 0;
            for (int i = 0; i < MaxCorpses; i++)
            {
                Corpses.Set(i, default);
            }
        }
    }
}
