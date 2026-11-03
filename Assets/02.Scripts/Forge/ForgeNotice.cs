using TMPro;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 화면 가운데 아래에 잠깐 띄우는 안내 한 줄이다. 작업대를 쓸 수 없는 이유 등을 알려 준다.
    public class ForgeNotice : MonoBehaviour
    {
        private const float ShowTime = 2.2f;

        [SerializeField] private TMP_Text _text;

        private float _hideAt;

        private void Awake()
        {
            _text.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_text.gameObject.activeSelf && Time.unscaledTime >= _hideAt)
            {
                _text.gameObject.SetActive(false);
            }
        }

        public void Show(string message)
        {
            _text.text = message;
            _text.gameObject.SetActive(true);
            _hideAt = Time.unscaledTime + ShowTime;
        }
    }
}
