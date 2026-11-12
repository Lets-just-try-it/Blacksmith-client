using System;
using Blacksmith.Core.Battle;
using Fusion;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 던전 단계별로 클리어한 스테이지 수다. 낮 씬 오브젝트이며 호스트가 소유한다.
    // 전투 씬은 결과를 반영한 뒤 내려가므로, 하루가 지나도 남아야 하는 진행 상황은 여기에 둔다.
    // 전투 결과는 BattleChannel.ResultConfirmed로 받아 반영한다.
    // [미정] 게임을 껐다 켤 때의 저장 구조가 정해지면 그 저장에 넣는다 (docs/NETWORK_DESIGN.md 8절).
    public class DungeonProgressState : NetworkBehaviour
    {
        // [Networked] 사전 크기는 컴파일할 때 정해야 해서 상수로 둔다. 기획상 전체 던전 단계 수(5)보다 넉넉하다.
        private const int MaxDungeons = 16;

        [SerializeField] private BattleChannel _channel;
        [Tooltip("던전 단계 순서. 앞 단계의 보스를 클리어해야 다음 단계가 열린다")]
        [SerializeField] private DungeonData[] _dungeons;

        private ChangeDetector _changes;

        [Networked, Capacity(MaxDungeons)] private NetworkDictionary<int, int> ClearedStages => default;

        // 스폰 직후와 진행 상황이 바뀔 때 모든 참가자에게 불린다.
        public event Action Changed;

        public bool IsReady => Object != null && Object.IsValid;
        public int TierCount => _dungeons.Length;

        private void OnEnable()
        {
            _channel.ResultConfirmed += ApplyResult;
        }

        private void OnDisable()
        {
            _channel.ResultConfirmed -= ApplyResult;
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

        public DungeonData GetDungeon(int tierIndex)
        {
            return _dungeons[tierIndex];
        }

        // 없는 ID면 null이다.
        public DungeonData FindDungeon(int dungeonId)
        {
            return Array.Find(_dungeons, dungeon => dungeon.DungeonId == dungeonId);
        }

        public int GetClearedStages(int dungeonId)
        {
            return IsReady && ClearedStages.TryGet(dungeonId, out int cleared) ? cleared : 0;
        }

        public bool IsTierSelectable(int tierIndex)
        {
            var cleared = new int[_dungeons.Length];
            var stageCounts = new int[_dungeons.Length];
            for (int i = 0; i < _dungeons.Length; i++)
            {
                cleared[i] = GetClearedStages(_dungeons[i].DungeonId);
                stageCounts[i] = _dungeons[i].StageCount;
            }

            return DispatchRules.IsTierSelectable(tierIndex, cleared, stageCounts);
        }

        // 호스트 전용. 테스트 씬에서 단계를 모두 클리어했을 때 처음부터 다시 싸우기 위해 쓴다.
        public void ResetProgress(int dungeonId)
        {
            if (HasStateAuthority)
            {
                ClearedStages.Remove(dungeonId);
            }
        }

        // 호스트에서만 불린다. 클리어한 스테이지를 다시 싸웠으면 진행 상황은 그대로다.
        private void ApplyResult(BattleOutcome outcome)
        {
            if (HasStateAuthority)
            {
                ClearedStages.Set(outcome.DungeonId,
                    DungeonProgressRules.ApplyResult(GetClearedStages(outcome.DungeonId), outcome.Stage, outcome.Result));
            }
        }
    }
}
