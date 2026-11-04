using Blacksmith.Core;
using Blacksmith.Network;
using TMPro;
using UnityEngine;

namespace Blacksmith.UI
{
    // 낮이 끝나 밤으로 넘어가는 연출(DayPhase.Dusk) 동안 화면 가운데에 "밤이 되었습니다"를 띄우고 캐릭터 조작을 잠근다.
    // 모든 참가자에게 보인다. 연출이 끝나기 직전에 문구가 흐려지며 사라지고, 밤이 되면 밤 파견 창이 뜬다.
    public class NightTransitionView : MonoBehaviour
    {
        [SerializeField] private GameState _gameState;
        [SerializeField] private PlayerNetworkInputLock _inputLock;
        [SerializeField] private CanvasGroup _toast;
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private string _message = "밤이 되었습니다";
        [SerializeField, Min(0.01f)] private float _fadeInSeconds = 0.4f;
        [SerializeField, Min(0.01f)] private float _fadeOutSeconds = 0.5f;

        private bool _isShowing;

        private void Awake()
        {
            _messageText.text = _message;
            _toast.alpha = 0f;
            _toast.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _gameState.PhaseChanged += ApplyPhase;
            ApplyPhase();
        }

        private void OnDisable()
        {
            _gameState.PhaseChanged -= ApplyPhase;
            _inputLock.Release(this);
        }

        // 표시 전용이라 Update에서 흐려지고 짙어지게 한다. 파견 창과 겹치지 않게 연출이 끝나기 전에 사라지기 시작한다.
        private void Update()
        {
            if (!_toast.gameObject.activeSelf)
            {
                return;
            }

            if (_isShowing && _gameState.RemainingDuskSeconds <= _fadeOutSeconds)
            {
                _isShowing = false;
            }

            float step = Time.deltaTime / (_isShowing ? _fadeInSeconds : _fadeOutSeconds);
            _toast.alpha = Mathf.MoveTowards(_toast.alpha, _isShowing ? 1f : 0f, step);
            if (!_isShowing && _toast.alpha <= 0f)
            {
                _toast.gameObject.SetActive(false);
            }
        }

        private void ApplyPhase()
        {
            bool isDusk = _gameState.IsReady && _gameState.Phase == DayPhase.Dusk;
            if (isDusk && !_isShowing && !_toast.gameObject.activeSelf)
            {
                _isShowing = true;
                _toast.gameObject.SetActive(true);
                _inputLock.Lock(this);
            }
            else if (!isDusk)
            {
                // 조작 잠금은 문구가 먼저 사라져도 연출이 끝날 때까지 둔다. 밤이 되면 파견 창이 이어서 잠근다.
                _isShowing = false;
                _inputLock.Release(this);
            }
        }
    }
}
