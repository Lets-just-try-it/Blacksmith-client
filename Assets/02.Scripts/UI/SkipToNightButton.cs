using Blacksmith.Core;
using Blacksmith.Network;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.UI
{
    // 테스트용. 누르면 낮 시간을 바로 끝내 밤 전환으로 넘어간다.
    // 낮 시간을 바꾸는 것은 호스트뿐이라 호스트의 낮에만 보인다.
    public class SkipToNightButton : MonoBehaviour
    {
        [SerializeField] private GameState _gameState;
        [SerializeField] private Button _button;

        private void Awake()
        {
            _button.onClick.AddListener(HandleClicked);
            Refresh();
        }

        private void OnEnable()
        {
            _gameState.PhaseChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            _gameState.PhaseChanged -= Refresh;
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(HandleClicked);
        }

        private void HandleClicked()
        {
            _gameState.EndDayNow();
        }

        private void Refresh()
        {
            bool canSkip = _gameState.IsReady && _gameState.HasStateAuthority && _gameState.Phase == DayPhase.Day;
            _button.gameObject.SetActive(canSkip);
        }
    }
}
