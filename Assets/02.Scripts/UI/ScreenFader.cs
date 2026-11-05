using System;
using UnityEngine;

namespace Blacksmith.UI
{
    // 화면 전체를 검게 덮었다가 걷는다. 씬 안의 구역 이동과 씬 전환에 쓴다.
    // 다른 오브젝트는 ScreenFadeChannel로 요청한다. 씬마다 하나만 두며, Additive로 불러온 전투 씬은 낮 씬의 것을 같이 쓴다.
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] private ScreenFadeChannel _channel;
        [SerializeField, Min(0.01f)] private float _fadeOutSeconds = 0.35f;
        [SerializeField, Min(0.01f)] private float _fadeInSeconds = 0.35f;
        [Tooltip("검은 화면으로 시작한다. 이전 씬에서 어두워진 채로 넘어오는 씬에서 켠다.")]
        [SerializeField] private bool _startOpaque;

        private CanvasGroup _group;
        // 진행 중에 반대 방향 요청이 오면 이전 요청은 그 자리에서 끝낸다.
        private int _requestId;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = _startOpaque ? 1f : 0f;
            _group.blocksRaycasts = _startOpaque;
        }

        private void OnEnable()
        {
            _channel.Register(this);
        }

        private void OnDisable()
        {
            _channel.Unregister(this);
        }

        public Awaitable FadeOut()
        {
            return FadeTo(1f, _fadeOutSeconds);
        }

        public Awaitable FadeIn()
        {
            return FadeTo(0f, _fadeInSeconds);
        }

        // 어두워지는 동안과 검은 동안에는 뒤의 UI가 눌리지 않게 막는다.
        private async Awaitable FadeTo(float alpha, float seconds)
        {
            int requestId = ++_requestId;
            _group.blocksRaycasts = true;
            try
            {
                while (!Mathf.Approximately(_group.alpha, alpha))
                {
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                    if (requestId != _requestId)
                    {
                        return;
                    }

                    _group.alpha = Mathf.MoveTowards(_group.alpha, alpha, Time.unscaledDeltaTime / seconds);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _group.blocksRaycasts = alpha > 0f;
        }
    }
}
