using Blacksmith.Core;
using Blacksmith.Network;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 낮·밤 단계와 대장간 작업대를 잇는다. 낮 씬에 둔다.
    // 낮 시간이 끝나면 새 미니게임 시작을 막고, 진행 중인 작업대가 모두 끝나야 밤으로 넘어가게 한다 (docs/GAME_CONCEPT.md 4절).
    public class ForgeDayCycleLink : MonoBehaviour
    {
        [SerializeField] private GameState _gameState;
        [SerializeField] private ForgeChannel _channel;
        [Tooltip("진행 중인지 확인할 씬의 작업대")]
        [SerializeField] private ForgeStationState[] _stations = new ForgeStationState[0];

        private void OnEnable()
        {
            _gameState.AddWorkInProgressSource(IsAnyStationInUse);
            _gameState.PhaseChanged += ApplyPhase;
            ApplyPhase();
        }

        // 다른 씬에 낮·밤 연결이 없을 때 작업대가 닫힌 채로 남지 않게 연다.
        private void OnDisable()
        {
            _gameState.RemoveWorkInProgressSource(IsAnyStationInUse);
            _gameState.PhaseChanged -= ApplyPhase;
            _channel.SetOpen(true);
        }

        // 모든 참가자에서 불린다. 호스트는 이 값으로 시작 요청을 거절하고, 참가자는 같은 값을 갖는다.
        private void ApplyPhase()
        {
            _channel.SetOpen(!_gameState.IsReady || DayCycleRules.CanStartWork(_gameState.Phase));
        }

        private bool IsAnyStationInUse()
        {
            foreach (ForgeStationState station in _stations)
            {
                if (station != null && station.InUse)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
