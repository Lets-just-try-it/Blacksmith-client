using System.Collections.Generic;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Battle;
using Blacksmith.Plaza;
using UnityEngine;

namespace Blacksmith.Battle
{
    // Fusion 연동 전 전투 씬을 혼자 확인하기 위한 진입점이다.
    // 연동 후에는 호스트가 동기화한 시드와 파티 정보로 BattlePlayer.Play를 부르는 쪽으로 바꾼다.
    public class BattleTestStarter : MonoBehaviour
    {
        [SerializeField] private BattlePlayer _player;
        [SerializeField] private BattleHud _hud;
        [SerializeField] private BattleResultView _resultView;
        [SerializeField] private BattleRulesData _rules;
        [SerializeField] private DungeonData _dungeon;
        [SerializeField] private AdventurerBattleData _adventurerBattle;
        [Tooltip("파티 모험가를 광장과 같은 규칙으로 생성한다. 관리 목록은 호스트 세션이 있어야 해서 여기서는 쓰지 않는다.")]
        [SerializeField] private AdventurerGenerationData _adventurerGeneration;
        [Tooltip("파티 모험가의 생성 시드. 모험가마다 이 값에 순번을 더해 쓴다.")]
        [SerializeField] private int _partySeed;
        [Tooltip("파티 모험가별 레벨. 개수가 파티 인원이다.")]
        [SerializeField] private int[] _partyLevels = { 1, 1, 1, 1 };
        [SerializeField] private int _seed;
        [Tooltip("이 단계에서 이미 클리어한 스테이지 수. 진행 상황은 원래 호스트 게임 상태에 둔다.")]
        [SerializeField, Min(0)] private int _clearedStages;
        [Tooltip("어느 스테이지까지 싸울지 (1부터 센 번호)")]
        [SerializeField, Min(1)] private int _selectedStageNumber = 3;

        private readonly List<AdventurerData> _party = new List<AdventurerData>();
        private BattleSetup _setup;
        private int _firstStage;
        private int _lastStage;

        private void OnEnable()
        {
            _player.UnitSpawned += HandleUnitSpawned;
            _player.Finished += HandleFinished;
            _resultView.ContinueClicked += StartBattle;
        }

        private void OnDisable()
        {
            _player.UnitSpawned -= HandleUnitSpawned;
            _player.Finished -= HandleFinished;
            _resultView.ContinueClicked -= StartBattle;
        }

        private void Start()
        {
            CreateParty();
            StartBattle();
        }

        private void CreateParty()
        {
            AdventurerGenerationRules rules = _adventurerGeneration.ToRules();
            for (int i = 0; i < _partyLevels.Length; i++)
            {
                AdventurerData adventurer = AdventurerGenerator.Generate(_partySeed + i, rules);
                adventurer.AdventurerId = i + 1;
                adventurer.Level = Mathf.Max(AdventurerStatRules.MinLevel, _partyLevels[i]);
                _party.Add(adventurer);
            }
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
            foreach (AdventurerData adventurer in _party)
            {
                _setup.Adventurers.Add(_adventurerBattle.ToSetup(adventurer));
            }

            _dungeon.AddStagesTo(_setup, _firstStage, _lastStage);
            _hud.Clear();
            _player.Play(_setup, _rules.TicksPerSecond, GetPrefab);
        }

        private BattleUnitView GetPrefab(BattleUnit unit)
        {
            return unit.Team == BattleTeam.Adventurer
                ? _adventurerBattle.GetViewPrefab(FindAdventurer(unit.SourceId).JobId)
                : _dungeon.FindMonster(unit.SourceId).ViewPrefab;
        }

        private void HandleUnitSpawned(BattleUnitView view)
        {
            if (view.Unit.Team != BattleTeam.Adventurer)
            {
                return;
            }

            AdventurerData adventurer = FindAdventurer(view.Unit.SourceId);
            string jobName = AdventurerLabels.GetJobName(_adventurerGeneration, adventurer.JobId);
            view.gameObject.AddComponent<AdventurerUnitInfo>().Bind(adventurer, jobName, view.Unit);
        }

        private void HandleFinished(BattleResult result)
        {
            _clearedStages = DungeonProgressRules.ApplyResult(_clearedStages, result);
            _resultView.Show(result, _dungeon.DisplayName, _firstStage, _lastStage,
                id => FindAdventurer(id)?.Name ?? id.ToString());
        }

        private AdventurerData FindAdventurer(int adventurerId)
        {
            return _party.Find(adventurer => adventurer.AdventurerId == adventurerId);
        }
    }
}
