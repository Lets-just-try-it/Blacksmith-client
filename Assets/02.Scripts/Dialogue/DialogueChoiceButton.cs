using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Dialogue
{
    // 선택지 버튼 하나. 고를 수 없는 선택지도 눌리게 두고(거절 연출을 보여 주려고), 모양만 흐리게 한다.
    public class DialogueChoiceButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private TMP_Text _reason;
        [SerializeField] private Color _normalColor = new Color(0f, 0f, 0f, 0.45f);
        [SerializeField] private Color _highlightColor = new Color(0.85f, 0.55f, 0.15f, 0.9f);
        [Tooltip("고를 수 없는 선택지가 강조됐을 때. 고를 수 있는 강조와 구분되게 둔다")]
        [SerializeField] private Color _disabledHighlightColor = new Color(0.62f, 0.62f, 0.66f, 0.95f);
        [SerializeField] private Color _enabledTextColor = Color.white;
        [SerializeField] private Color _disabledTextColor = new Color(1f, 1f, 1f, 0.4f);
        [SerializeField, Min(0f)] private float _rejectDuration = 0.35f;
        [SerializeField, Min(0f)] private float _shakeDistance = 10f;

        private RectTransform _rect;
        private Vector2 _restPosition;
        private bool _isEnabled;
        private Coroutine _reject;

        public event Action<DialogueChoiceButton> Clicked;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            // 엔터 연타로 선택지가 눌리지 않도록 EventSystem의 포커스(내비게이션)를 받지 않는다. 강조는 DialogueDirector가 관리한다.
            _button.navigation = new Navigation { mode = Navigation.Mode.None };
            _button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        private void OnDisable()
        {
            StopReject();
        }

        public void Show(int number, string text, bool isEnabled, string disabledReason)
        {
            _isEnabled = isEnabled;
            _label.text = $"{number}. {text}";
            _label.color = isEnabled ? _enabledTextColor : _disabledTextColor;
            _reason.text = isEnabled || string.IsNullOrEmpty(disabledReason) ? "" : $"({disabledReason})";
            _reason.color = _disabledTextColor;
            SetHighlighted(false);
        }

        public void SetHighlighted(bool isHighlighted)
        {
            _background.color = !isHighlighted ? _normalColor : _isEnabled ? _highlightColor : _disabledHighlightColor;
        }

        // 고를 수 없는 선택지를 고르려 할 때 흔들고 이유를 깜빡인다.
        public void PlayReject()
        {
            StopReject();
            _restPosition = _rect.anchoredPosition;
            _reject = StartCoroutine(RejectRoutine());
        }

        private IEnumerator RejectRoutine()
        {
            for (float elapsed = 0f; elapsed < _rejectDuration; elapsed += Time.unscaledDeltaTime)
            {
                float progress = elapsed / _rejectDuration;
                float offset = Mathf.Sin(progress * Mathf.PI * 6f) * _shakeDistance * (1f - progress);
                _rect.anchoredPosition = _restPosition + new Vector2(offset, 0f);
                _reason.color = Color.Lerp(_enabledTextColor, _disabledTextColor, progress);
                yield return null;
            }

            StopReject();
        }

        private void StopReject()
        {
            if (_reject == null)
            {
                return;
            }

            StopCoroutine(_reject);
            _reject = null;
            _rect.anchoredPosition = _restPosition;
            _reason.color = _disabledTextColor;
        }

        private void OnClick()
        {
            Clicked?.Invoke(this);
        }
    }
}
