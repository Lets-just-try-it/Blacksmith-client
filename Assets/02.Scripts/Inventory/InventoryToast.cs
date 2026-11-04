using TMPro;
using UnityEngine;

namespace Blacksmith.Inventory
{
    // 잠깐 띄웠다가 흐려지며 사라지는 안내 한 줄이다. 장비 착용 창이 맞지 않는 장비를 놓았을 때 알려 준다.
    // 꺼 둔 채로 두면 Show가 켠다.
    [RequireComponent(typeof(CanvasGroup))]
    public class InventoryToast : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [Tooltip("다 보이는 시간(초)")]
        [SerializeField, Min(0f)] private float _showTime = 1.2f;
        [Tooltip("흐려지는 시간(초)")]
        [SerializeField, Min(0.01f)] private float _fadeTime = 0.4f;

        private CanvasGroup _group;
        private float _shownAt;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
        }

        // 표시 전용이라 Time을 쓴다. 입력 잠금이나 일시 정지와 관계없이 흐르도록 unscaledTime을 쓴다.
        private void Update()
        {
            float fade = (Time.unscaledTime - _shownAt - _showTime) / _fadeTime;
            _group.alpha = 1f - Mathf.Clamp01(fade);
            if (fade >= 1f)
            {
                gameObject.SetActive(false);
            }
        }

        public void Show(string message)
        {
            _text.text = message;
            _shownAt = Time.unscaledTime;
            gameObject.SetActive(true);
            _group.alpha = 1f;
        }
    }
}
