using System;
using System.Collections.Generic;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Battle;
using Fusion;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 전투 씬의 파견 정보를 모든 참가자에게 동기화한다. 씬 오브젝트이며 호스트가 소유한다.
    // 각 참가자는 같은 파견 정보로 같은 BattleSetup을 만들어 재생하므로 결과도 같다. (docs/GAME_CONCEPT.md 7.1)
    // 결과는 호스트가 전투를 시작할 때 계산해 두고, ConfirmResult에서 넘긴다.
    // 던전 진행 상황은 전투 씬을 내려도 남아야 해서 낮 씬의 DungeonProgressState에 둔다.
    public class BattleState : NetworkBehaviour
    {
        // [Networked] 배열 크기는 컴파일할 때 정해야 해서 상수로 둔다. 기획상 한 번에 파견하는 최대 인원과 같다.
        public const int MaxPartySize = 5;

        [SerializeField] private BattleRulesData _rules;
        [SerializeField] private DungeonData[] _dungeons;
        [SerializeField] private AdventurerBattleData _adventurerBattle;
        [SerializeField] private AdventurerLevelData _adventurerLevel;

        // 낮 씬의 관리 목록 조회. 입은 장비도 함께 담겨 있어 전투 유닛에 장비 보정이 들어간다.
        private Func<int, AdventurerData> _findAdventurer;
        private ChangeDetector _changes;

        // 호스트에서만 쓴다.
        private BattleResult _pendingResult;
        private BattleRewards _pendingRewards;

        [Networked] private int BattleId { get; set; }
        [Networked] private int Seed { get; set; }
        [Networked] public int DungeonId { get; private set; }
        [Networked] public int FirstStage { get; private set; }
        [Networked] public int LastStage { get; private set; }
        [Networked] private int PartyCount { get; set; }
        [Networked, Capacity(MaxPartySize)] private NetworkArray<int> PartyIds => default;

        // 호스트에서만 불린다. 스폰된 뒤 첫 전투를 시작할 수 있을 때다.
        public event Action HostReady;
        // 모든 참가자에게 불린다. CurrentSetup으로 재생을 시작한다.
        public event Action Started;
        // 호스트에서만 불린다. 받는 쪽(BattleSceneFlow)이 낮 씬에 결과를 넘기고 전투 씬을 내린다.
        public event Action<BattleResult, BattleRewards> ResultConfirmed;

        public BattleSetup CurrentSetup { get; private set; }
        public DungeonData CurrentDungeon { get; private set; }
        public bool HasPendingResult => _pendingResult != null;

        public void SetAdventurerSource(Func<int, AdventurerData> findAdventurer)
        {
            _findAdventurer = findAdventurer;
        }

        public override void Spawned()
        {
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
            if (BattleId > 0)
            {
                BeginPlayback();
            }

            if (HasStateAuthority)
            {
                HostReady?.Invoke();
            }
        }

        public override void Render()
        {
            foreach (string change in _changes.DetectChanges(this))
            {
                if (change == nameof(BattleId))
                {
                    BeginPlayback();
                }
            }
        }

        public DungeonData FindDungeon(int dungeonId)
        {
            return Array.Find(_dungeons, dungeon => dungeon.DungeonId == dungeonId);
        }

        // 호스트 전용. 이전 전투의 결과를 반영하기 전에는 새 전투를 시작하지 않는다.
        // clearedStages는 파견을 확정할 때의 진행 상황이다. 고른 stage 하나를 싸운다.
        // FirstStage·LastStage는 스테이지 안의 웨이브가 생기기 전까지 같은 값이다.
        public bool StartBattle(int seed, int dungeonId, int clearedStages, int stage, IReadOnlyList<int> partyIds)
        {
            DungeonData dungeon = FindDungeon(dungeonId);
            if (!HasStateAuthority || HasPendingResult || dungeon == null
                || partyIds.Count == 0 || partyIds.Count > MaxPartySize
                || !DungeonProgressRules.CanSelectStage(clearedStages, dungeon.StageCount, stage))
            {
                return false;
            }

            Seed = seed;
            DungeonId = dungeonId;
            FirstStage = stage;
            LastStage = stage;
            PartyCount = partyIds.Count;
            for (int i = 0; i < partyIds.Count; i++)
            {
                PartyIds.Set(i, partyIds[i]);
            }

            BattleId++;

            _pendingResult = new BattleSimulation(BuildSetup(dungeon)).RunToEnd();
            _pendingRewards = CalculateRewards(_pendingResult, dungeon);
            return true;
        }

        // 호스트 전용. 결과창 다음, 전투 씬을 내리기 직전에 부른다.
        public bool ConfirmResult()
        {
            if (!HasStateAuthority || !HasPendingResult)
            {
                return false;
            }

            BattleResult result = _pendingResult;
            BattleRewards rewards = _pendingRewards;
            _pendingResult = null;
            _pendingRewards = null;

            ResultConfirmed?.Invoke(result, rewards);
            return true;
        }

        // 결과창 표시와 호스트의 결과 계산이 같은 함수를 쓴다.
        public BattleRewards CalculateRewards(BattleResult result)
        {
            return CalculateRewards(result, CurrentDungeon);
        }

        private BattleRewards CalculateRewards(BattleResult result, DungeonData dungeon)
        {
            return BattleRewardRules.Calculate(dungeon.GetStageRewards(FirstStage, LastStage), result,
                _findAdventurer, _adventurerLevel.ToRules());
        }

        private void BeginPlayback()
        {
            CurrentDungeon = FindDungeon(DungeonId);
            CurrentSetup = BuildSetup(CurrentDungeon);
            Started?.Invoke();
        }

        private BattleSetup BuildSetup(DungeonData dungeon)
        {
            var setup = new BattleSetup { Seed = Seed, Rules = _rules.ToRules() };
            for (int i = 0; i < PartyCount; i++)
            {
                setup.Adventurers.Add(_adventurerBattle.ToSetup(_findAdventurer(PartyIds[i])));
            }

            dungeon.AddStagesTo(setup, FirstStage, LastStage);
            return setup;
        }
    }
}
