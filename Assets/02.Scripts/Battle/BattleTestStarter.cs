using System;
using System.Collections.Generic;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Battle;
using Blacksmith.Network;
using Blacksmith.Plaza;
using Fusion;
using UnityEngine;

namespace Blacksmith.Battle
{
    // BattleTest 씬을 확인하기 위한 진입점이다. 로비를 거치지 않고 참가자가 없는 호스트로 시작한다.
    // NetworkBootstrap의 Game 씬 경로를 BattleTest로 지정해 둔다.
    // 실제 흐름에서는 밤의 파견 창이 BattleState.StartBattle을 부르고, 결과창 다음에 전투 씬을 내린다.
    public class BattleTestStarter : MonoBehaviour
    {
        [SerializeField] private NetworkBootstrap _bootstrap;
        [SerializeField] private BattleState _battleState;
        [SerializeField] private BattlePlayer _player;
        [SerializeField] private BattleHud _hud;
        [SerializeField] private BattleBossHpBar _bossHpBar;
        [SerializeField] private BattleBossIntro _bossIntro;
        [SerializeField] private BattleResultView _resultView;
        [SerializeField] private BattleRulesData _rules;
        [SerializeField] private DungeonData _dungeon;
        [SerializeField] private AdventurerBattleData _adventurerBattle;
        [Tooltip("파티 모험가를 광장과 같은 규칙으로 생성한다. 모든 참가자가 같은 값으로 생성하므로 파티가 같다.")]
        [SerializeField] private AdventurerGenerationData _adventurerGeneration;
        [Tooltip("파티 모험가의 생성 시드. 모험가마다 이 값에 순번을 더해 쓴다.")]
        [SerializeField] private int _partySeed;
        [Tooltip("파티 모험가별 레벨. 개수가 파티 인원이다.")]
        [SerializeField] private int[] _partyLevels = { 1, 1, 1, 1 };
        [Tooltip("어느 스테이지까지 싸울지 (1부터 센 번호)")]
        [SerializeField, Min(1)] private int _selectedStageNumber = 3;

        // 임시 연결: 전투 씬을 낮 씬 위에 불러오게 되면 낮 씬 관리 목록의 모험가를 쓴다.
        private readonly List<AdventurerData> _party = new List<AdventurerData>();

        private void Awake()
        {
            CreateParty();
            _battleState.SetAdventurerSource(FindAdventurer);
        }

        private void OnEnable()
        {
            _battleState.HostReady += StartBattle;
            _battleState.Started += PlayBattle;
            _battleState.ResultConfirmed += LogConfirmedResult;
            _player.UnitSpawned += HandleUnitSpawned;
            _player.Finished += HandleFinished;
            _resultView.ContinueClicked += HandleContinueClicked;
        }

        private void OnDisable()
        {
            _battleState.HostReady -= StartBattle;
            _battleState.Started -= PlayBattle;
            _battleState.ResultConfirmed -= LogConfirmedResult;
            _player.UnitSpawned -= HandleUnitSpawned;
            _player.Finished -= HandleFinished;
            _resultView.ContinueClicked -= HandleContinueClicked;
        }

        private async void Start()
        {
            // 호스트가 시작되면 이 씬을 네트워크 씬으로 다시 불러온다. 그때는 이미 러너가 있으므로 다시 시작하지 않는다.
            if (NetworkRunner.Instances.Count > 0)
            {
                return;
            }

            // 같은 App ID를 쓰는 팀원과 세션이 겹치지 않게 이름을 매번 새로 만든다.
            await _bootstrap.StartHost($"BattleTest-{Guid.NewGuid():N}");
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

        // 호스트에서만 불린다.
        private void StartBattle()
        {
            int clearedStages = _battleState.GetClearedStages(_dungeon.DungeonId);
            if (DungeonProgressRules.IsTierCleared(clearedStages, _dungeon.StageCount))
            {
                Debug.Log("테스트 던전을 모두 클리어해 진행 상황을 처음으로 되돌린다.");
                _battleState.ResetProgress(_dungeon.DungeonId);
                clearedStages = 0;
            }

            int firstStage = DungeonProgressRules.FirstStageToFight(clearedStages);
            int lastStage = Mathf.Clamp(_selectedStageNumber - 1, firstStage, _dungeon.StageCount - 1);
            List<int> partyIds = _party.ConvertAll(adventurer => adventurer.AdventurerId);
            if (!_battleState.StartBattle(UnityEngine.Random.Range(0, int.MaxValue), _dungeon.DungeonId, lastStage, partyIds))
            {
                Debug.LogWarning($"전투를 시작할 수 없다: 스테이지 {_selectedStageNumber}, 파티 {partyIds.Count}명");
            }
        }

        private void PlayBattle()
        {
            _resultView.Hide();
            _hud.Clear();
            _bossHpBar.Hide();
            _bossIntro.Hide();
            _player.Play(_battleState.CurrentSetup, _rules.TicksPerSecond, GetPrefab);
        }

        private BattleUnitView GetPrefab(BattleUnit unit)
        {
            return unit.Team == BattleTeam.Adventurer
                ? _adventurerBattle.GetViewPrefab(FindAdventurer(unit.SourceId).JobId)
                : _battleState.CurrentDungeon.FindMonster(unit.SourceId).ViewPrefab;
        }

        private void HandleUnitSpawned(BattleUnitView view)
        {
            if (view.Unit.Team != BattleTeam.Adventurer)
            {
                MonsterData monster = _battleState.CurrentDungeon.FindMonster(view.Unit.SourceId);
                if (monster.IsBoss)
                {
                    _bossHpBar.Show(view.Unit, monster.DisplayName);
                    _bossIntro.Play(monster.DisplayName);
                }

                return;
            }

            AdventurerData adventurer = FindAdventurer(view.Unit.SourceId);
            string jobName = AdventurerLabels.GetJobName(_adventurerGeneration, adventurer.JobId);
            view.gameObject.AddComponent<AdventurerUnitInfo>().Bind(adventurer, jobName, view.Unit);
        }

        private void HandleFinished(BattleResult result)
        {
            _resultView.Show(result, _battleState.CalculateRewards(result), _battleState.CurrentDungeon.DisplayName,
                _battleState.FirstStage, _battleState.LastStage, id => FindAdventurer(id)?.Name ?? id.ToString(),
                _battleState.HasStateAuthority);
        }

        // 호스트에서만 눌린다. 실제 흐름에서는 결과를 반영한 뒤 전투 씬을 내린다.
        private void HandleContinueClicked()
        {
            _battleState.ConfirmResult();
            StartBattle();
        }

        // 결과 반영 이벤트가 오는지 확인하기 위한 로그다. 골드·관리 목록 반영은 각 담당이 이 이벤트에 연결한다.
        private void LogConfirmedResult(BattleResult result, BattleRewards rewards)
        {
            Debug.Log($"전투 결과 반영: {(result.IsVictory ? "승리" : "패배")}, 스테이지 {result.StagesCleared}개, 골드 {rewards.Gold}");
        }

        private AdventurerData FindAdventurer(int adventurerId)
        {
            return _party.Find(adventurer => adventurer.AdventurerId == adventurerId);
        }
    }
}
