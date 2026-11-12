using Blacksmith.Core;
using Blacksmith.Network;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 낮·밤 단계와 파견을 잇는다. 낮 씬에 둔다. (docs/GAME_CONCEPT.md 2.3)
    // 밤이 되면 파견 창을 열고, 전투 결과를 반영했거나 파견을 건너뛰면 다음 날 낮을 시작한다. 호스트에서만 동작한다.
    // 다음 날은 대장간에서 시작하므로, 전투 결과를 반영했거나 파견을 건너뛰면 플레이어를 대장간으로 모은다(채집 맵에 있던 플레이어 포함).
    public class NightDispatchLink : MonoBehaviour
    {
        [SerializeField] private GameState _gameState;
        [SerializeField] private DispatchState _dispatch;
        [SerializeField] private BattleChannel _channel;
        [SerializeField] private BattleSceneLoader _loader;
        [Tooltip("다음 날이 시작될 때 모든 플레이어를 대장간 스폰 위치로 돌려보낸다")]
        [SerializeField] private PlayerSpawner _spawner;

        private void OnEnable()
        {
            _gameState.PhaseChanged += OpenDispatchAtNight;
            _dispatch.Changed += OpenDispatchAtNight;
            _dispatch.Skipped += ReturnAndStartNextDay;
            _channel.ResultConfirmed += StartNextDay;
        }

        private void OnDisable()
        {
            _gameState.PhaseChanged -= OpenDispatchAtNight;
            _dispatch.Changed -= OpenDispatchAtNight;
            _dispatch.Skipped -= ReturnAndStartNextDay;
            _channel.ResultConfirmed -= StartNextDay;
        }

        // 두 오브젝트의 스폰 순서가 정해져 있지 않아 어느 쪽이 바뀌어도 다시 확인한다.
        // 파견해 전투 씬을 불러오는 중이거나 떠 있는 동안에는 결과를 기다린다.
        private void OpenDispatchAtNight()
        {
            if (_gameState.IsReady && _gameState.HasStateAuthority && _dispatch.CanEdit
                && _gameState.Phase == DayPhase.Night && !_dispatch.IsOpen && _loader.CanDispatch)
            {
                _dispatch.OpenForNight();
            }
        }

        // 파견 창이 화면을 덮고 있는 동안 옮긴다. 전투 때와 달리 화면을 어둡게 하지 않아, 창이 닫힐 때 대장간으로 바로 바뀐다.
        private void ReturnAndStartNextDay()
        {
            _spawner.ReturnAllToSpawnPoints();
            _gameState.StartNextDay();
        }

        // 전투 씬이 낮 화면을 가리고 있는 동안 옮겨서, 페이드 인 하면 대장간이 보인다.
        private void StartNextDay(BattleOutcome outcome)
        {
            _spawner.ReturnAllToSpawnPoints();
            _gameState.StartNextDay();
        }
    }
}
