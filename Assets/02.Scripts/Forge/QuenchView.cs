using Blacksmith.Core.Forge;
using TMPro;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 담금질 화면 문구와 결과 시간선만 맡는다. 진행은 QuenchStation이 정하고 이 클래스는 받은 값을 보여 준다.
    public class QuenchView : MonoBehaviour
    {
        // 시간선 끝은 기준 시간과 꺼낸 시간 중 큰 값에 이 배율을 곱한 시간이다.
        private const float TimelineMargin = 1.2f;
        private const float MarkerDelay = 0.3f;
        private const float MarkerSpeed = 2f;

        [SerializeField] private GameObject _promptRoot;
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TMP_Text _targetText;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _hintText;
        [SerializeField] private TMP_Text _resultText;
        [SerializeField] private TMP_Text _noteText;
        [SerializeField] private GameObject _timelineRoot;
        [SerializeField] private RectTransform _timelineBar;
        [SerializeField] private RectTransform _perfectBand;
        [SerializeField] private RectTransform _targetMarker;
        [SerializeField] private RectTransform _pulledMarker;

        private float _resultAge;
        private float _pulledX;
        private bool _showingResult;

        private void Awake()
        {
            ShowPrompt(false);
            HidePanel();
        }

        private void Update()
        {
            if (!_showingResult)
            {
                return;
            }

            _resultAge += Time.deltaTime;
            MovePulledMarker();
        }

        public void ShowPrompt(bool visible)
        {
            _promptRoot.SetActive(visible);
        }

        public void ShowReady(float target)
        {
            _panelRoot.SetActive(true);
            _showingResult = false;
            _targetText.text = $"기준 시간 {target:0.0}초";
            _timeText.text = "00:00";
            _hintText.text = "기준 시간에 맞춰 꺼내자!\n• E 또는 스페이스바로 칼날을 물에 담그기";
            _resultText.text = string.Empty;
            _noteText.text = string.Empty;
            _timelineRoot.SetActive(false);
        }

        public void ShowDunk()
        {
            _hintText.text = "기준 시간에 맞춰 꺼내자!\n• 기준 시간이 되었다 싶을 때 E 또는 스페이스바로 꺼내기\n• 초는 처음 2초만 보임";
        }

        // 담근 뒤 처음 몇 초까지만 시간을 보여 주고 그 뒤는 가린다.
        public void ShowTime(float elapsed, bool visible)
        {
            if (!visible)
            {
                _timeText.text = "?";
                return;
            }

            int seconds = (int)elapsed;
            int centiseconds = (int)((elapsed - seconds) * 100f);
            _timeText.text = $"{seconds:00}:{centiseconds:00}";
        }

        public void ShowResult(QuenchGame game)
        {
            _hintText.text = string.Empty;
            _resultText.text = $"정확도 {Mathf.RoundToInt(game.Accuracy * 100f)}%";
            _noteText.text = DescribeTiming(game);

            _showingResult = true;
            _resultAge = 0f;
            _timelineRoot.SetActive(true);
            LayoutTimeline(game);
        }

        public void MarkPerfect()
        {
            _resultText.text += "  완벽!";
        }

        public void HidePanel()
        {
            _showingResult = false;
            _panelRoot.SetActive(false);
        }

        private static string DescribeTiming(QuenchGame game)
        {
            if (game.TimedOut)
            {
                return "시간이 다 되어 자동으로 꺼냈어요";
            }

            if (Mathf.Abs(game.Diff) <= game.PerfectBand)
            {
                return $"{game.Pulled:0.00}초에 꺼냈어요. 기준 시간에 딱 맞췄어요";
            }

            string direction = game.Diff < 0f ? "일찍" : "늦게";
            return $"{game.Pulled:0.00}초에 꺼냈어요. 기준보다 {Mathf.Abs(game.Diff):0.00}초 {direction} 꺼냈어요";
        }

        // 시간선에 기준 시간과 완벽 구간, 내가 꺼낸 시간을 놓는다.
        private void LayoutTimeline(QuenchGame game)
        {
            float width = _timelineBar.rect.width;
            float maxTime = Mathf.Max(game.Target, game.Pulled) * TimelineMargin;
            float TimeToX(float seconds) => width * Mathf.Clamp01(seconds / maxTime);

            float bandStart = TimeToX(game.Target - game.PerfectBand);
            float bandEnd = TimeToX(game.Target + game.PerfectBand);
            _perfectBand.anchoredPosition = new Vector2(bandStart, 0f);
            _perfectBand.sizeDelta = new Vector2(bandEnd - bandStart, _perfectBand.sizeDelta.y);
            _targetMarker.anchoredPosition = new Vector2(TimeToX(game.Target), 0f);
            _pulledX = TimeToX(game.Pulled);
            MovePulledMarker();
        }

        private void MovePulledMarker()
        {
            float progress = Mathf.Clamp01((_resultAge - MarkerDelay) * MarkerSpeed);
            _pulledMarker.anchoredPosition = new Vector2(Mathf.Lerp(0f, _pulledX, progress), 0f);
        }
    }
}
