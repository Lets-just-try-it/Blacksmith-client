using Blacksmith.Core.Forge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Forge
{
    // 장신구 제작 화면의 글자와 버튼을 맡는다. 작업판 그림은 NecklaceBoardView가 그린다.
    // 위쪽: 단계 칩 네 개와 방법 창. 아래쪽: 지금 할 일, 단계별 수치, 남은 시간. 판 위: 뒤집기·숫돌·돌리기 버튼과 이름표.
    // 버튼은 누른 사실을 한 번만 알려 주고(Consume), 작업대가 그 값을 입력에 합쳐 규칙에 넘긴다.
    public class NecklaceView : MonoBehaviour
    {
        private const float PopupTime = 1.2f;

        private struct PopupSlot
        {
            public TMP_Text Text;
            public float Start;
            public bool Active;
        }

        [SerializeField] private GameObject _promptRoot;
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Image[] _stepBoxes;
        [SerializeField] private TMP_Text[] _stepTexts;
        [SerializeField] private TMP_Text _objectiveText;
        [SerializeField] private TMP_Text _guideTitle;

        [Header("판 위")]
        [SerializeField] private TMP_Text _shapeLabel;
        [SerializeField] private TMP_Text _crateLabel;
        [SerializeField] private Button _flipButton;
        [SerializeField] private Button[] _toolButtons;
        [SerializeField] private Image[] _toolBoxes;
        [SerializeField] private Button _rotateLeftButton;
        [SerializeField] private Button _rotateRightButton;
        [SerializeField] private GameObject _gaugeLabels;
        [SerializeField] private TMP_Text _gaugeTolerance;
        [SerializeField] private TMP_Text _gaugeThick;
        [SerializeField] private TMP_Text _gaugeOver;

        [Header("아래 패널")]
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private GameObject _timeRoot;
        [SerializeField] private RectTransform _timeFill;
        [SerializeField] private TMP_Text _timeText;

        [Header("결과·효과")]
        [SerializeField] private TMP_Text _resultText;
        [SerializeField] private GameObject _resultBack;
        [SerializeField] private Image _flash;
        [SerializeField] private TMP_Text[] _popupTexts;

        private PopupSlot[] _popups;
        private int _nextPopup;
        private float _flashAlpha;
        private string _shownStatus;
        private string _shownScore;
        private int _shownTime = int.MinValue;
        private int _shownStep = -2;
        private int _shownTool = -1;
        private bool _flipRequested;
        private int _arrowRequested;
        private int _toolRequested;

        private void Awake()
        {
            _popups = new PopupSlot[_popupTexts.Length];
            for (int i = 0; i < _popups.Length; i++)
            {
                _popups[i].Text = _popupTexts[i];
                _popupTexts[i].gameObject.SetActive(false);
            }

            _flipButton.onClick.AddListener(() => _flipRequested = true);
            _rotateLeftButton.onClick.AddListener(() => _arrowRequested = -1);
            _rotateRightButton.onClick.AddListener(() => _arrowRequested = 1);
            for (int i = 0; i < _toolButtons.Length; i++)
            {
                int number = i + 1;
                _toolButtons[i].onClick.AddListener(() => _toolRequested = number);
            }

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

            for (int i = 0; i < _popups.Length; i++)
            {
                if (!_popups[i].Active)
                {
                    continue;
                }

                float age = (Time.time - _popups[i].Start) / PopupTime;
                if (age >= 1f)
                {
                    _popups[i].Active = false;
                    _popups[i].Text.gameObject.SetActive(false);
                    continue;
                }

                Color color = _popups[i].Text.color;
                color.a = 1f - age * age;
                _popups[i].Text.color = color;
                ((RectTransform)_popups[i].Text.transform).anchoredPosition += Vector2.up * (30f * Time.deltaTime);
            }
        }

        public void ShowPrompt(bool visible)
        {
            _promptRoot.SetActive(visible);
        }

        public void ShowStart()
        {
            _panelRoot.SetActive(true);
            _resultText.text = string.Empty;
            _resultBack.SetActive(false);
            _shownStatus = null;
            _shownScore = null;
            _shownTime = int.MinValue;
            _shownStep = -2;
            _shownTool = -1;
            _flipRequested = false;
            _arrowRequested = 0;
            _toolRequested = 0;
            foreach (TMP_Text text in _popupTexts)
            {
                text.gameObject.SetActive(false);
            }
        }

        public void HidePanel()
        {
            _flashAlpha = 0f;
            _flash.color = Color.clear;
            _panelRoot.SetActive(false);
        }

        public void Flash(float alpha, Color color)
        {
            if (!MinigameFeedbackSwitch.Enabled)
            {
                return;
            }

            _flashAlpha = Mathf.Max(_flashAlpha, alpha);
            color.a = _flashAlpha;
            _flash.color = color;
        }

        // 판 중심을 기준으로 한 위치(위쪽이 +)에 글자를 띄운다.
        public void Popup(string text, Color color, Vector2 boardLocalPosition)
        {
            if (_popups.Length == 0)
            {
                return;
            }

            ref PopupSlot popup = ref _popups[_nextPopup];
            _nextPopup = (_nextPopup + 1) % _popups.Length;
            popup.Active = true;
            popup.Start = Time.time;
            popup.Text.text = text;
            popup.Text.color = color;
            popup.Text.gameObject.SetActive(true);
            ((RectTransform)popup.Text.transform).anchoredPosition = boardLocalPosition;
        }

        public void ShowResult(NecklaceGame game)
        {
            _resultBack.SetActive(true);
            _resultText.text =
                $"장신구 {Mathf.RoundToInt(game.Accuracy * 100f)}점\n" +
                $"<size=60%>체인 {Mathf.RoundToInt(game.ChainScore * 100f)}%   깎기 {Mathf.RoundToInt(game.CutScore * 100f)}%   " +
                $"장착 {Mathf.RoundToInt(game.SetScore * 100f)}%   광택 {Mathf.RoundToInt(game.PolishScore * 100f)}%</size>";
        }

        // 버튼으로 요청한 입력을 한 번만 돌려준다.
        public bool ConsumeFlip()
        {
            bool requested = _flipRequested;
            _flipRequested = false;
            return requested;
        }

        public int ConsumeArrow()
        {
            int requested = _arrowRequested;
            _arrowRequested = 0;
            return requested;
        }

        public int ConsumeTool()
        {
            int requested = _toolRequested;
            _toolRequested = 0;
            return requested;
        }

        // 값이 바뀔 때만 문자열을 만든다.
        public void Refresh(NecklaceGame game)
        {
            int step = DisplayStep(game);
            RefreshStep(game, step);
            RefreshStatus(game, step);
            RefreshScore(game, step);
            RefreshTime(game, step);
            RefreshButtons(game, step);
        }

        private static int DisplayStep(NecklaceGame game)
        {
            switch (game.Phase)
            {
                case NecklaceGame.PhaseKind.Intro:
                    return -1;
                case NecklaceGame.PhaseKind.Chain:
                    return 0;
                case NecklaceGame.PhaseKind.Cut:
                    return 1;
                case NecklaceGame.PhaseKind.Set:
                    return 2;
                case NecklaceGame.PhaseKind.Polish:
                case NecklaceGame.PhaseKind.Done:
                    return 3;
                default:
                    return game.Step;
            }
        }

        private void RefreshStep(NecklaceGame game, int step)
        {
            if (step == _shownStep)
            {
                return;
            }

            _shownStep = step;
            for (int i = 0; i < _stepBoxes.Length; i++)
            {
                bool current = i == step;
                bool done = i < step;
                _stepBoxes[i].color = current ? new Color(0.3f, 0.22f, 0.08f, 0.95f) : (done ? new Color(0.1f, 0.2f, 0.13f, 0.95f) : new Color(0.1f, 0.1f, 0.13f, 0.9f));
                _stepTexts[i].text = (i + 1) + "  " + NecklaceGame.StepNames[i];
                _stepTexts[i].color = current ? new Color(1f, 0.9f, 0.5f) : Color.white;
            }

            _shapeLabel.gameObject.SetActive(step == 0);
            _crateLabel.gameObject.SetActive(step == 0);
            _shapeLabel.text = game.ShapeName;
            _gaugeLabels.SetActive(step == 1);
            _objectiveText.text = ObjectiveFor(step);
        }

        private static string ObjectiveFor(int step)
        {
            switch (step)
            {
                case 0:
                    return "상자에서 조각을 꺼내 빈 칸에 놓는다.\n• 조각이 있는 칸엔 못 놓지만 다시 집을 수 있다\n• 양 끝의 모양·색이 이웃과 같아야 맞물린다\n• 우클릭·E·스페이스바나 뒤집기 버튼으로 뒤집는다\n• 모든 이음새가 초록이면 완성";
                case 1:
                    return "보석을 목표 모양(점선)으로 깎자.\n• ←/→ 로 점선 밖으로 튀어나온 면을 아래 숫돌에 맞춘다\n• E·스페이스바를 누르는 동안 갈린다\n• 1 거친 숫돌: 빠름 / 2 고운 숫돌: 정밀\n• 두께 계가 초록이면 놓는다. 너무 깎으면 못 되돌린다\n• F: 마무리";
                case 2:
                    return "깎은 보석을 틀의 구멍에 끼우자.\n• 보석을 끌어 구멍 위에 놓는다\n• ←/→ 로 돌려 점(●)과 모양을 맞춘다\n• 위치·각도가 초록이면 E·스페이스바를 꾹 눌러 박는다\n• 맞지 않을 때 누르면 보석이 상한다";
                case 3:
                    return "천으로 보석의 면을 문질러 윤을 내자.\n• 마우스를 누른 채 면 위를 왔다 갔다 문지른다\n• 가만히 있으면 안 닦인다\n• 거칠게 깎은 면은 오래 걸린다\n• F: 마무리";
                default:
                    return "장신구 제작을 시작한다";
            }
        }

        private void RefreshStatus(NecklaceGame game, int step)
        {
            string message;
            Color color = Color.white;
            if (game.Phase == NecklaceGame.PhaseKind.Intro)
            {
                message = game.ShapeName + "를 만든다";
                color = new Color(1f, 0.9f, 0.6f);
            }
            else if (game.Phase == NecklaceGame.PhaseKind.StepDone)
            {
                message = NecklaceGame.StepNames[Mathf.Clamp(step, 0, 3)] + " 끝! 곧 다음 단계로 넘어간다";
                color = new Color(0.6f, 1f, 0.7f);
            }
            else if (game.Phase == NecklaceGame.PhaseKind.Done)
            {
                message = "마감 끝!";
                color = new Color(0.6f, 1f, 0.7f);
            }
            else
            {
                message = StatusFor(game, step, out color);
            }

            _statusText.color = color;
            if (message != _shownStatus)
            {
                _shownStatus = message;
                _statusText.text = message;
            }
        }

        private static string StatusFor(NecklaceGame game, int step, out Color color)
        {
            color = new Color(1f, 0.9f, 0.5f);
            switch (step)
            {
                case 0:
                    if (game.Held >= 0)
                    {
                        return "빈 칸을 클릭해 놓자. 안 맞으면 뒤집고, 상자 빈 곳을 클릭하면 도로 넣는다";
                    }

                    if (game.Joined == game.JointTotal)
                    {
                        color = new Color(0.6f, 1f, 0.7f);
                        return "모두 맞물렸다!";
                    }

                    return "둥근 단자와 같은 모양·색의 끝을 가진 조각을 옆에 놓자";
                case 1:
                    return CutStatus(game, out color);
                case 2:
                    return SetStatus(game, out color);
                default:
                    if (game.Scrubbing && game.Hover >= 0)
                    {
                        return "닦는 중... 면 위를 왔다 갔다 문지르자";
                    }

                    if (game.Hover >= 0)
                    {
                        color = new Color(0.6f, 1f, 0.7f);
                        return "면 위다. 마우스를 누른 채 문지르자";
                    }

                    return "천으로 보석의 면마다 문질러 윤을 내자";
            }
        }

        private static string CutStatus(NecklaceGame game, out Color color)
        {
            color = new Color(1f, 0.9f, 0.5f);
            float remaining = game.Remaining;
            if (game.Grinding)
            {
                if (Mathf.Abs(remaining) <= game.Tol)
                {
                    color = new Color(0.6f, 1f, 0.7f);
                    return "지금 놓자! 목표에 닿았다";
                }

                if (remaining < -game.Tol)
                {
                    color = new Color(1f, 0.5f, 0.4f);
                    return "너무 깎았다! 놓자 (되돌릴 수 없다)";
                }

                return remaining < 0.05f && game.Tool == 0 ? "목표가 가깝다. 2번 고운 숫돌로 바꾸면 정밀하다" : "갈리는 중... 목표(점선)까지";
            }

            if (game.Match >= 0.95f)
            {
                color = new Color(0.6f, 1f, 0.7f);
                return "거의 다 깎았다! F로 마무리하자";
            }

            if (remaining > game.Tol)
            {
                return "이 면은 아직 두껍다. 누르고 있어 갈자 (←/→ 로 다른 면으로)";
            }

            color = new Color(0.7f, 1f, 0.75f);
            return "이 면은 맞았다. ←/→ 로 점선 밖으로 튀어나온 다른 면을 숫돌에 맞추자";
        }

        private static string SetStatus(NecklaceGame game, out Color color)
        {
            color = new Color(1f, 0.9f, 0.5f);
            if (game.Seating && game.Aligned)
            {
                color = new Color(0.6f, 1f, 0.7f);
                return "좋다! 그대로 꾹 누르고 있자";
            }

            if (game.Seating && !game.Near)
            {
                color = new Color(1f, 0.55f, 0.45f);
                return "맞지 않았는데 눌렀다! 보석이 상한다. 먼저 모양을 맞추자";
            }

            if (game.Aligned)
            {
                color = new Color(0.6f, 1f, 0.7f);
                return "딱 맞았다! 꾹 눌러 박자";
            }

            return game.PosErr > game.PosTol * 2f ? "보석을 마우스로 끌어 틀의 구멍 위에 놓자" : "거의 다 왔다. ←/→ 로 돌려 점(●)과 모양을 맞추자";
        }

        private void RefreshScore(NecklaceGame game, int step)
        {
            string text;
            switch (step)
            {
                case 0:
                    text = $"맞물린 이음새  {game.Joined} / {game.JointTotal}   실수 {game.Mistakes}회";
                    break;
                case 1:
                    text = $"목표와 일치  {Mathf.RoundToInt(game.Match * 100f)}%   너무 깎은 횟수 {game.Overcuts}회";
                    break;
                case 2:
                    text = $"위치 {(game.PosErr <= game.PosTol ? "맞음" : game.PosErr.ToString("0.0") + "칸")}   각도 {(game.IdxDiff == 0 ? "맞음" : Mathf.Abs(game.IdxDiff) + "칸")}   " +
                        $"박는 진행 {Mathf.RoundToInt(game.SeatProgress * 100f)}%   손상 {Mathf.RoundToInt(game.Damage * 100f)}%";
                    break;
                case 3:
                    text = $"광택  {Mathf.RoundToInt(game.AvgShine * 100f)}%   윤이 난 면  {game.ShinedCount} / {game.TargetFacets.Count}";
                    break;
                default:
                    text = string.Empty;
                    break;
            }

            if (text != _shownScore)
            {
                _shownScore = text;
                _scoreText.text = text;
            }
        }

        private void RefreshTime(NecklaceGame game, int step)
        {
            bool timed = game.Phase == NecklaceGame.PhaseKind.Chain || game.Phase == NecklaceGame.PhaseKind.Cut
                || game.Phase == NecklaceGame.PhaseKind.Set || game.Phase == NecklaceGame.PhaseKind.Polish;
            _timeRoot.SetActive(timed);
            if (!timed)
            {
                return;
            }

            float left;
            float limit;
            switch (step)
            {
                case 0:
                    left = game.ChainLeft;
                    limit = game.ChainLimit;
                    break;
                case 1:
                    left = game.CutLeft;
                    limit = game.CutLimit;
                    break;
                case 2:
                    left = game.SetLeft;
                    limit = game.SetLimit;
                    break;
                default:
                    left = game.PolishLeft;
                    limit = game.PolishLimit;
                    break;
            }

            _timeFill.anchorMax = new Vector2(Mathf.Clamp01(left / Mathf.Max(0.01f, limit)), 1f);
            int tenths = Mathf.CeilToInt(left * 10f);
            if (tenths != _shownTime)
            {
                _shownTime = tenths;
                _timeText.text = "남은 시간 " + (tenths / 10f).ToString("0.0");
                _timeText.color = left < 8f ? new Color(1f, 0.5f, 0.4f) : new Color(1f, 1f, 1f, 0.7f);
            }
        }

        // 단계에 맞는 버튼만 보이게 하고, 깎기 단계의 숫돌 선택과 두께 계 글자를 갱신한다.
        private void RefreshButtons(NecklaceGame game, int step)
        {
            bool active = game.Phase == NecklaceGame.PhaseKind.Chain || game.Phase == NecklaceGame.PhaseKind.Cut || game.Phase == NecklaceGame.PhaseKind.Set;
            _flipButton.gameObject.SetActive(step == 0 && active);
            _flipButton.interactable = game.Held >= 0;
            bool cutting = step == 1 && game.Phase == NecklaceGame.PhaseKind.Cut;
            foreach (Button tool in _toolButtons)
            {
                tool.gameObject.SetActive(cutting);
            }

            _rotateLeftButton.gameObject.SetActive((cutting || (step == 2 && game.Phase == NecklaceGame.PhaseKind.Set)));
            _rotateRightButton.gameObject.SetActive(_rotateLeftButton.gameObject.activeSelf);
            if (cutting && game.Tool != _shownTool)
            {
                _shownTool = game.Tool;
                for (int i = 0; i < _toolBoxes.Length; i++)
                {
                    _toolBoxes[i].color = i == game.Tool ? new Color(0.35f, 0.25f, 0.08f, 0.98f) : new Color(0.1f, 0.1f, 0.13f, 0.9f);
                }
            }

            if (cutting)
            {
                float remaining = game.Remaining;
                _gaugeTolerance.text = "← 멈춤 (±" + game.TolCm.ToString("0.00") + "cm)";
                _gaugeThick.text = remaining > 0f ? "아직 두껍다 " + game.Cm(remaining) : "아직 두껍다";
                _gaugeOver.text = remaining < 0f ? "너무 깎음 " + game.Cm(-remaining) : "너무 깎음";
            }
        }
    }
}
