using Blacksmith.Core.Forge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Forge
{
    // 단조 화면 문구만 맡는다. 진행은 ForgingStation이 정하고, 박자 레인 그리기는 ForgingLaneView가 한다.
    public class ForgingView : MonoBehaviour
    {
        private const float FeedbackTime = 0.8f;

        [SerializeField] private GameObject _promptRoot;
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private ForgingLaneView _lane;
        [SerializeField] private TMP_Text _matchText;
        [SerializeField] private TMP_Text _comboText;
        [SerializeField] private TMP_Text _beatsLeftText;
        [SerializeField] private TMP_Text _feedbackText;
        [SerializeField] private TMP_Text _hintText;
        [SerializeField] private TMP_Text _resultText;
        [SerializeField] private Image _flash;

        private float _feedbackTimer;
        private float _flashAlpha;
        private int _shownMatch = -1;
        private int _shownCombo = -1;
        private int _shownBeatsLeft = -1;

        private void Awake()
        {
            ShowPrompt(false);
            HidePanel();
        }

        private void Update()
        {
            if (_flashAlpha > 0f)
            {
                _flashAlpha = Mathf.MoveTowards(_flashAlpha, 0f, Time.deltaTime * 3f);
                Color color = _flash.color;
                color.a = _flashAlpha;
                _flash.color = color;
            }

            if (_feedbackTimer > 0f)
            {
                _feedbackTimer -= Time.deltaTime;
                if (_feedbackTimer <= 0f)
                {
                    _feedbackText.text = string.Empty;
                }
            }
        }

        // 강하게 맞는 순간 화면 전체가 번쩍인다.
        public void Flash(float alpha, Color color)
        {
            _flashAlpha = Mathf.Max(_flashAlpha, alpha);
            color.a = _flashAlpha;
            _flash.color = color;
        }

        public void ShowPrompt(bool visible)
        {
            _promptRoot.SetActive(visible);
        }

        public void ShowStart(ForgingGame game, ForgingRules rules)
        {
            _panelRoot.SetActive(true);
            _lane.Bind(game, rules);
            _feedbackText.text = string.Empty;
            _resultText.text = string.Empty;
            _hintText.text = "박자에 맞춰 망치질하자!\n• 작은 박자: E 또는 스페이스바를 톡\n• 금색 긴 박자: 시작에서 꾹 눌러 힘을 모으고, 끝에서 떼기";
            _shownMatch = _shownCombo = _shownBeatsLeft = -1;
            Refresh(game);
        }

        // 값이 바뀔 때만 문자열을 만든다.
        public void Refresh(ForgingGame game)
        {
            int match = Mathf.RoundToInt(game.Match * 100f);
            if (match != _shownMatch)
            {
                _shownMatch = match;
                _matchText.text = $"{match}%";
            }
            if (game.Combo != _shownCombo)
            {
                _shownCombo = game.Combo;
                _comboText.text = game.Combo.ToString();
            }
            if (game.BeatsLeft != _shownBeatsLeft)
            {
                _shownBeatsLeft = game.BeatsLeft;
                _beatsLeftText.text = game.BeatsLeft.ToString();
            }

            _lane.Refresh();
        }

        // 판정 결과를 잠깐 보여 준다. 허공을 친 경우는 Value가 음수다.
        public void ShowJudgement(MiniEvent miniEvent, ForgingGame game)
        {
            bool heavy = miniEvent.Value >= 2f;
            string text;
            switch (miniEvent.Kind)
            {
                case MiniEventKind.Perfect:
                    text = game.Combo >= 3 ? $"{(heavy ? "강타!" : "완벽!")}  x{game.Combo}  ({Milliseconds(miniEvent.Extra)})" : $"{(heavy ? "강타!" : "완벽!")}  ({Milliseconds(miniEvent.Extra)})";
                    break;
                case MiniEventKind.Good:
                    text = $"좋음 ({Direction(miniEvent.Extra)} {Milliseconds(miniEvent.Extra)})";
                    break;
                case MiniEventKind.Hit:
                    text = $"약함 ({Direction(miniEvent.Extra)} {Milliseconds(miniEvent.Extra)})";
                    break;
                case MiniEventKind.Miss:
                    text = miniEvent.Value < 0f ? "헛스윙" : (miniEvent.Extra < -game.WeakWindow ? "너무 일찍 뗐다" : "놓침");
                    break;
                default:
                    return;
            }

            _feedbackText.text = text;
            _feedbackTimer = FeedbackTime;
        }

        // meanOffset은 박자를 친 순간들의 평균 시간 차이(초)다. 판정 보정값을 맞출 때 참고한다.
        public void ShowResult(float accuracy, float meanOffset)
        {
            _hintText.text = $"평균 타이밍 {Milliseconds(meanOffset)} ({Direction(meanOffset)})";
            _feedbackText.text = string.Empty;
            _resultText.text = $"도안 일치율 {Mathf.RoundToInt(accuracy * 100f)}%";
        }

        public void HidePanel()
        {
            _flashAlpha = 0f;
            _flash.color = Color.clear;
            _panelRoot.SetActive(false);
        }

        private static string Milliseconds(float offset)
        {
            int milliseconds = Mathf.RoundToInt(offset * 1000f);
            return milliseconds >= 0 ? $"+{milliseconds}ms" : $"{milliseconds}ms";
        }

        private static string Direction(float offset)
        {
            return offset < 0f ? "빠름" : "느림";
        }
    }
}
