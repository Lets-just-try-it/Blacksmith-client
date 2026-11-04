using System.Collections;
using TMPro;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 보스 스테이지가 시작되면 "BOSS STAGE" 문구와 보스 이름을 띄우고, 그동안 전투 재생을 멈춘다.
    public class BattleBossIntro : MonoBehaviour
    {
        [SerializeField] private BattlePlayer _player;
        [SerializeField] private GameObject _root;
        [SerializeField] private CanvasGroup _canvasGroup;
        [Tooltip("등장할 때 크게 시작해 제자리로 줄어드는 대상")]
        [SerializeField] private RectTransform _banner;
        [SerializeField] private TMP_Text _bossNameText;
        [Tooltip("[추정] 문구가 떠 있는 동안 전투 재생을 멈추는 시간(초). 나타나고 사라지는 시간을 포함한다.")]
        [SerializeField, Min(0.5f)] private float _duration = 2.2f;
        [SerializeField, Min(0.01f)] private float _fadeInTime = 0.25f;
        [SerializeField, Min(0.01f)] private float _fadeOutTime = 0.4f;
        [SerializeField, Min(1f)] private float _startScale = 1.8f;

        private Coroutine _routine;

        private void Awake()
        {
            Hide();
        }

        public void Play(string bossName)
        {
            _bossNameText.text = bossName;
            _player.Hold(_duration);
            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            _routine = StartCoroutine(Animate());
        }

        public void Hide()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            _root.SetActive(false);
        }

        private IEnumerator Animate()
        {
            _root.SetActive(true);
            for (float t = 0f; t < _fadeInTime; t += Time.deltaTime)
            {
                float ratio = t / _fadeInTime;
                _canvasGroup.alpha = ratio;
                _banner.localScale = Vector3.one * Mathf.Lerp(_startScale, 1f, ratio * (2f - ratio));
                yield return null;
            }

            _canvasGroup.alpha = 1f;
            _banner.localScale = Vector3.one;
            yield return new WaitForSeconds(Mathf.Max(0f, _duration - _fadeInTime - _fadeOutTime));

            for (float t = 0f; t < _fadeOutTime; t += Time.deltaTime)
            {
                _canvasGroup.alpha = 1f - t / _fadeOutTime;
                yield return null;
            }

            _routine = null;
            _root.SetActive(false);
        }
    }
}
