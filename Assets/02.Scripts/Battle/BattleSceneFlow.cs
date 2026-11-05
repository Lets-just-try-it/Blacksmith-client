using Blacksmith.Characters;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Battle;
using Blacksmith.Inventory;
using Blacksmith.Plaza;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Blacksmith.Battle
{
    // 낮 씬 위에 Additive로 불러온 전투 씬의 흐름이다: 파견 정보로 전투 시작 → 재생 → 결과창 → 결과 반영.
    // 모험가 정보와 파견 정보는 BattleChannel로 낮 씬에서 받고, 결과도 BattleChannel로 넘긴다.
    // 결과를 넘기면 낮 씬의 BattleSceneLoader가 이 씬을 내린다.
    public class BattleSceneFlow : MonoBehaviour
    {
        [SerializeField] private BattleChannel _channel;
        [SerializeField] private BattleState _battleState;
        [SerializeField] private BattlePlayer _player;
        [SerializeField] private BattleHud _hud;
        [SerializeField] private BattleBossHpBar _bossHpBar;
        [SerializeField] private BattleBossIntro _bossIntro;
        [SerializeField] private BattleResultView _resultView;
        [SerializeField] private BattleRulesData _rules;
        [SerializeField] private AdventurerBattleData _adventurerBattle;
        [Tooltip("직업 이름을 찾는 데 쓴다")]
        [SerializeField] private AdventurerGenerationData _adventurerGeneration;
        [Tooltip("입은 장비의 색을 찾는 데 쓴다. 장비 착용 창 미리보기와 같은 표를 쓴다")]
        [SerializeField] private ItemCatalog _itemCatalog;

        private void Awake()
        {
            _battleState.SetAdventurerSource(_channel.FindAdventurer);
        }

        private void OnEnable()
        {
            _battleState.HostReady += StartBattle;
            _battleState.Started += PlayBattle;
            _battleState.ResultConfirmed += PassResult;
            _player.UnitSpawned += HandleUnitSpawned;
            _player.Finished += HandleFinished;
            _resultView.ContinueClicked += HandleContinueClicked;
        }

        private void OnDisable()
        {
            _battleState.HostReady -= StartBattle;
            _battleState.Started -= PlayBattle;
            _battleState.ResultConfirmed -= PassResult;
            _player.UnitSpawned -= HandleUnitSpawned;
            _player.Finished -= HandleFinished;
            _resultView.ContinueClicked -= HandleContinueClicked;
        }

        // 이 씬에서 새로 만드는 오브젝트와 조명 설정이 전투 씬을 따르게 한다. 낮 씬은 이 알림을 받고 화면을 끈다.
        private void Start()
        {
            SceneManager.SetActiveScene(gameObject.scene);
            _channel.NotifyBattleOpened(this);
        }

        private void OnDestroy()
        {
            _channel.NotifyBattleClosed(this);
        }

        // 호스트에서만 불린다.
        private void StartBattle()
        {
            DispatchRequest request = _channel.TakePendingDispatch();
            if (request == null)
            {
                Debug.LogError("파견 정보 없이 전투 씬이 열렸다.", this);
                return;
            }

            if (!_battleState.StartBattle(request.Seed, request.DungeonId, request.ClearedStages, request.LastStage, request.PartyIds))
            {
                Debug.LogError($"전투를 시작할 수 없다: 던전 {request.DungeonId}, 스테이지 {request.LastStage + 1}, 파티 {request.PartyIds.Count}명", this);
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
                ? _adventurerBattle.GetViewPrefab(_channel.FindAdventurer(unit.SourceId).JobId)
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

            AdventurerData adventurer = _channel.FindAdventurer(view.Unit.SourceId);
            string jobName = AdventurerLabels.GetJobName(_adventurerGeneration, adventurer.JobId);
            view.gameObject.AddComponent<AdventurerUnitInfo>().Bind(adventurer, jobName, view.Unit);
            if (view.TryGetComponent(out AdventurerLook look))
            {
                look.Show(adventurer, _itemCatalog);
            }
        }

        private void HandleFinished(BattleResult result)
        {
            _resultView.Show(result, _battleState.CalculateRewards(result), _battleState.CurrentDungeon.DisplayName,
                _battleState.FirstStage, _battleState.LastStage, id => _channel.FindAdventurer(id)?.Name ?? id.ToString(),
                _battleState.HasStateAuthority);
        }

        // 호스트에서만 눌린다.
        private void HandleContinueClicked()
        {
            _battleState.ConfirmResult();
        }

        private void PassResult(BattleResult result, BattleRewards rewards)
        {
            Debug.Log($"전투 결과 반영: {(result.IsVictory ? "승리" : "패배")}, 스테이지 {result.StagesCleared}개, 골드 {rewards.Gold}");
            _channel.ConfirmResult(new BattleOutcome { DungeonId = _battleState.DungeonId, Result = result, Rewards = rewards });
        }
    }
}
