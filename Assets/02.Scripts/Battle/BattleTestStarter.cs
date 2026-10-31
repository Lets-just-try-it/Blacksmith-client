using System;
using System.Collections.Generic;
using Blacksmith.Core.Battle;
using UnityEngine;

namespace Blacksmith.Battle
{
    // Fusion 연동 전 전투 씬을 혼자 확인하기 위한 진입점이다.
    // 연동 후에는 호스트가 동기화한 시드와 파티 정보로 BattlePlayer.Play를 부르는 쪽으로 바꾼다.
    public class BattleTestStarter : MonoBehaviour
    {
        [Serializable]
        private class TestAdventurer
        {
            public int AdventurerId;
            public string DisplayName;
            public UnitStats Stats;
            public SkillData Skill;
            public BattleUnitView ViewPrefab;
            public Sprite Portrait;
        }

        [SerializeField] private BattlePlayer _player;
        [SerializeField] private BattleHud _hud;
        [SerializeField] private BattleResultView _resultView;
        [SerializeField] private BattleRulesData _rules;
        [SerializeField] private DungeonData _dungeon;
        [SerializeField] private List<TestAdventurer> _adventurers = new List<TestAdventurer>();
        [SerializeField] private int _seed;
        [Tooltip("이 단계에서 이미 클리어한 스테이지 수. 진행 상황은 원래 호스트 게임 상태에 둔다.")]
        [SerializeField, Min(0)] private int _clearedStages;
        [Tooltip("어느 스테이지까지 싸울지 (1부터 센 번호)")]
        [SerializeField, Min(1)] private int _selectedStageNumber = 3;

        private BattleSetup _setup;
        private int _firstStage;
        private int _lastStage;

        private void OnEnable()
        {
            _player.Finished += HandleFinished;
            _resultView.ContinueClicked += StartBattle;
        }

        private void OnDisable()
        {
            _player.Finished -= HandleFinished;
            _resultView.ContinueClicked -= StartBattle;
        }

        private void Start()
        {
            _hud.SetPortraitProvider(id => FindAdventurer(id)?.Portrait);
            StartBattle();
        }

        private void StartBattle()
        {
            if (DungeonProgressRules.IsTierCleared(_clearedStages, _dungeon.StageCount))
            {
                Debug.Log("테스트 던전을 모두 클리어해 진행 상황을 처음으로 되돌린다.");
                _clearedStages = 0;
            }

            _firstStage = DungeonProgressRules.FirstStageToFight(_clearedStages);
            _lastStage = Mathf.Clamp(_selectedStageNumber - 1, _firstStage, _dungeon.StageCount - 1);
            if (!DungeonProgressRules.CanSelectStage(_clearedStages, _dungeon.StageCount, _lastStage))
            {
                Debug.LogWarning($"선택할 수 없는 스테이지다: {_selectedStageNumber}");
                return;
            }

            _setup = new BattleSetup { Seed = _seed, Rules = _rules.ToRules() };
            foreach (TestAdventurer adventurer in _adventurers)
            {
                _setup.Adventurers.Add(new AdventurerSetup
                {
                    AdventurerId = adventurer.AdventurerId,
                    Stats = adventurer.Stats,
                    Skill = adventurer.Skill,
                });
            }

            _dungeon.AddStagesTo(_setup, _firstStage, _lastStage);
            _hud.Clear();
            _player.Play(_setup, _rules.TicksPerSecond, GetPrefab);
        }

        private BattleUnitView GetPrefab(BattleUnit unit)
        {
            return unit.Team == BattleTeam.Adventurer
                ? FindAdventurer(unit.SourceId).ViewPrefab
                : _dungeon.FindMonster(unit.SourceId).ViewPrefab;
        }

        private void HandleFinished(BattleResult result)
        {
            _clearedStages = DungeonProgressRules.ApplyResult(_clearedStages, result);
            _resultView.Show(result, _dungeon.DisplayName, _firstStage, _lastStage,
                id => FindAdventurer(id)?.DisplayName ?? id.ToString());
        }

        private TestAdventurer FindAdventurer(int adventurerId)
        {
            return _adventurers.Find(adventurer => adventurer.AdventurerId == adventurerId);
        }
    }
}
