using Blacksmith.Core;
using Blacksmith.Network;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 낮·밤 단계와 파견을 잇는다. 낮 씬에 둔다. (docs/GAME_CONCEPT.md 2.3)
    // 밤이 되면 파견 창을 열고, 전투 결과를 반영했거나 파견을 건너뛰면 다음 날 낮을 시작한다. 호스트에서만 동작한다.
    public class NightDispatchLink : MonoBehaviour
    {
        [SerializeField] private GameState _gameState;
        [SerializeField] private DispatchState _dispatch;
        [SerializeField] private BattleChannel _channel;
        [SerializeField] private BattleSceneLoader _loader;

        private void OnEnable()
        {
            _gameState.PhaseChanged += OpenDispatchAtNight;
            _dispatch.Changed += OpenDispatchAtNight;
            _dispatch.Skipped += StartNextDay;
            _channel.ResultConfirmed += StartNextDay;
        }

        private void OnDisable()
        {
            _gameState.PhaseChanged -= OpenDispatchAtNight;
            _dispatch.Changed -= OpenDispatchAtNight;
            _dispatch.Skipped -= StartNextDay;
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

        private void StartNextDay()
        {
            _gameState.StartNextDay();
        }

        private void StartNextDay(BattleOutcome outcome)
        {
            StartNextDay();
        }
    }
}
