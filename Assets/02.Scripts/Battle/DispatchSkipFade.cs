using System;
using Blacksmith.UI;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 파견을 건너뛸 때 모든 참가자의 화면을 어둡게 했다가, 대장간으로 옮긴 뒤 밝힌다. 낮 씬에 둔다. 표시 전용이다.
    // 호스트는 DispatchState.IsSkipping이 켜진 뒤 정한 시간이 지나야 플레이어를 옮기므로, 그동안 화면이 어두워진다.
    public class DispatchSkipFade : MonoBehaviour
    {
        [SerializeField] private DispatchState _dispatch;
        [SerializeField] private ScreenFadeChannel _screenFade;
        [Tooltip("건너뛰기가 끝난 뒤 화면을 밝히기 전에 기다리는 시간(초). 호스트가 옮긴 위치가 도착할 때까지 가린다")]
        [SerializeField, Min(0f)] private float _holdSeconds = 0.25f;

        private bool _wasSkipping;

        private void OnEnable()
        {
            _dispatch.Changed += HandleChanged;
        }

        private void OnDisable()
        {
            _dispatch.Changed -= HandleChanged;
        }

        private void HandleChanged()
        {
            bool isSkipping = _dispatch.IsReady && _dispatch.IsSkipping;
            if (isSkipping == _wasSkipping)
            {
                return;
            }

            _wasSkipping = isSkipping;
            if (isSkipping)
            {
                _ = _screenFade.FadeOut();
            }
            else
            {
                _ = FadeInAfterHold();
            }
        }

        private async Awaitable FadeInAfterHold()
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(_holdSeconds, destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            // 기다리는 동안 다시 건너뛰기가 시작됐으면 어두운 채로 둔다.
            if (!_wasSkipping)
            {
                await _screenFade.FadeIn();
            }
        }
    }
}
