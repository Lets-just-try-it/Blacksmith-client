using Blacksmith.Core.Forge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Forge
{
    // 연마 화면의 글자와 게이지를 맡는다. 도는 숫돌 그림은 PolishLensView가 그리고, 이 클래스는 그 위아래의 표시를 맡는다.
    // 위쪽: 날 세움·식힘·탄 횟수 칩과 지금 할 일 한 줄. 아래쪽: 상태, 열 눈금, 각도 안내, 콤보와 점수, 남은 시간.
    public class PolishView : MonoBehaviour
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
        [SerializeField] private Image[] _tabBoxes;
        [SerializeField] private TMP_Text[] _tabTexts;
        [SerializeField] private TMP_Text _objectiveText;

        [Header("아래 패널")]
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private RectTransform _speedSweet;
        [SerializeField] private RectTransform _speedMarker;
        [SerializeField] private Image _speedMarkerImage;
        [SerializeField] private TMP_Text _speedHint;
        [SerializeField] private GameObject _directionRoot;
        [SerializeField] private Image _directionBox;
        [SerializeField] private TMP_Text _directionText;
        [SerializeField] private TMP_Text _directionSub;
        [SerializeField] private TMP_Text _comboText;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private GameObject _timeRoot;
        [SerializeField] private RectTransform _timeFill;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private GameObject _buffRoot;
        [SerializeField] private RectTransform _buffFill;

        [Header("결과·효과")]
        [SerializeField] private TMP_Text _resultText;
        [SerializeField] private GameObject _resultBack;
        [SerializeField] private Image _flash;
        [SerializeField] private TMP_Text[] _popupTexts;

        private readonly int[] _tabKeys = { -1, -1, -1 };
        private PopupSlot[] _popups;
        private int _nextPopup;
        private float _flashAlpha;
        private string _shownStatus;
        private int _shownScore = -1;
        private int _shownCombo = -1;
        private int _shownDone = -1;
        private int _shownTime = -1;
        private PolishGame.PhaseKind _shownPhase = (PolishGame.PhaseKind)(-1);

        private void Awake()
        {
            _popups = new PopupSlot[_popupTexts.Length];
            for (int i = 0; i < _popups.Length; i++)
            {
                _popups[i].Text = _popupTexts[i];
                _popupTexts[i].gameObject.SetActive(false);
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
            _shownScore = _shownCombo = _shownDone = _shownTime = -1;
            _shownPhase = (PolishGame.PhaseKind)(-1);
            for (int i = 0; i < _tabKeys.Length; i++)
            {
                _tabKeys[i] = -1;
                _tabTexts[i].color = Color.white;
            }
            _tabBoxes[0].color = new Color(0.12f, 0.2f, 0.14f, 0.95f);
            _tabBoxes[1].color = new Color(0.1f, 0.16f, 0.24f, 0.95f);
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
            _flashAlpha = Mathf.Max(_flashAlpha, alpha);
            color.a = _flashAlpha;
            _flash.color = color;
        }

        // 렌즈 중심을 기준으로 한 위치(위쪽이 +)에 글자를 띄운다. 렌즈 위에 놓인 글자 풀을 쓴다.
        public void Popup(string text, Color color, Vector2 lensLocalPosition)
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
            ((RectTransform)popup.Text.transform).anchoredPosition = lensLocalPosition;
        }

        public void ShowResult(PolishGame game)
        {
            _resultBack.SetActive(true);
            _resultText.text =
                $"연마 {Mathf.RoundToInt(game.Accuracy * 100f)}점\n" +
                $"<size=60%>날 세움 {Mathf.RoundToInt(game.Clean * 100f)}%   {game.DoneCount}/{PolishGame.SegmentCount}칸\n" +
                $"탄 곳 -{Mathf.RoundToInt(game.BurnLoss * 100f)}점   얇아짐 -{Mathf.RoundToInt(game.ThinLoss * 100f)}점   최고 콤보 {game.BestCombo}</size>";
        }

        // 값이 바뀔 때만 문자열을 만든다.
        public void Refresh(PolishGame game)
        {
            RefreshChips(game);
            RefreshObjective(game);
            RefreshStatus(game);
            RefreshHeat(game);
            RefreshAngle(game);
            RefreshScore(game);
            RefreshTime(game);
        }

        // 위쪽 칩 세 개: 날 세움, 식힌 횟수, 탄 횟수
        private void RefreshChips(PolishGame game)
        {
            if (game.DoneCount != _tabKeys[0])
            {
                _tabKeys[0] = game.DoneCount;
                _tabTexts[0].text = "완성  " + game.DoneCount + " / " + PolishGame.SegmentCount + "칸";
            }

            if (game.Dips != _tabKeys[1])
            {
                _tabKeys[1] = game.Dips;
                _tabTexts[1].text = "식힘  " + game.Dips + "회";
            }

            if (game.Burned != _tabKeys[2])
            {
                _tabKeys[2] = game.Burned;
                _tabTexts[2].text = "탄 곳  " + game.Burned;
                _tabBoxes[2].color = game.Burned > 0 ? new Color(0.3f, 0.1f, 0.08f, 0.95f) : new Color(0.1f, 0.1f, 0.13f, 0.9f);
            }
        }

        private void RefreshObjective(PolishGame game)
        {
            if (game.Phase == _shownPhase)
            {
                return;
            }

            _shownPhase = game.Phase;
            switch (game.Phase)
            {
                case PolishGame.PhaseKind.Intro:
                    _objectiveText.text = "숫돌이 돌기 시작한다";
                    break;
                case PolishGame.PhaseKind.Done:
                    _objectiveText.text = "연마 끝!";
                    break;
                default:
                    _objectiveText.text = "누른 채 초록 띠에 맞춰 밀어 갈자 · 아래 칸이 하얗게 가득 차면 완성 · 뜨거우면 E로 식히기";
                    break;
            }
        }

        private void RefreshStatus(PolishGame game)
        {
            string message;
            Color color;
            if (game.Phase == PolishGame.PhaseKind.Intro)
            {
                message = "준비...";
                color = new Color(1f, 1f, 1f, 0.8f);
            }
            else if (game.Phase == PolishGame.PhaseKind.Done)
            {
                message = "끝!";
                color = new Color(1f, 0.95f, 0.8f);
            }
            else if (game.Locked)
            {
                message = "탔다! E로 식히자";
                color = new Color(1f, 0.4f, 0.35f);
            }
            else if (game.Dipping)
            {
                message = "식히는 중...";
                color = new Color(0.6f, 0.85f, 1f);
            }
            else if (game.Heat >= game.WarnHeat)
            {
                message = "뜨겁다! E로 식히자";
                color = new Color(1f, 0.55f, 0.35f);
            }
            else if (game.Grinding && game.Quality >= 0.99f)
            {
                message = "잘 갈린다!";
                color = new Color(0.6f, 1f, 0.7f);
            }
            else if (game.Grinding)
            {
                message = "각도가 어긋났다! 띠에 맞추자";
                color = new Color(1f, 0.8f, 0.4f);
            }
            else
            {
                message = "버튼을 누른 채 띠에 맞춰 밀어 보자";
                color = new Color(1f, 0.85f, 0.5f);
            }

            _statusText.color = color;
            if (message != _shownStatus)
            {
                _shownStatus = message;
                _statusText.text = message;
            }
        }

        // 열 눈금: 안전한 구간(초록)과 현재 열 표시
        private void RefreshHeat(PolishGame game)
        {
            float limit = Mathf.Max(0.01f, game.BurnHeat);
            _speedSweet.anchorMin = new Vector2(0f, 0.125f);
            _speedSweet.anchorMax = new Vector2(game.WarnHeat / limit, 0.875f);

            float position = Mathf.Clamp01(game.Heat / limit);
            _speedMarker.anchorMin = new Vector2(position, 0f);
            _speedMarker.anchorMax = new Vector2(position, 1f);
            bool hot = game.Heat >= game.WarnHeat;
            _speedMarkerImage.color = game.Locked ? new Color(1f, 0.3f, 0.25f) : (hot ? new Color(1f, 0.6f, 0.3f) : new Color(0.6f, 1f, 0.7f));

            string hint = game.Locked ? "탔다" : (hot ? "뜨겁다" : (game.Dipping ? "식는 중" : string.Empty));
            _speedHint.text = hint;
            _speedHint.color = hot || game.Locked ? new Color(1f, 0.55f, 0.4f) : new Color(0.7f, 0.9f, 1f);
        }

        // 각도 안내: 마우스를 올릴지 내릴지 알려 준다.
        private void RefreshAngle(PolishGame game)
        {
            _directionRoot.SetActive(true);
            float difference = game.PointerY - game.TargetAngle;
            bool known = game.PointerX > -50f && game.Phase == PolishGame.PhaseKind.Grind;
            bool good = known && Mathf.Abs(difference) <= game.AngleTolerance;
            _directionBox.color = good ? new Color(0.15f, 0.3f, 0.18f) : new Color(0.12f, 0.12f, 0.16f);
            _directionText.text = !known ? "각도" : (good ? "각도 좋음" : (difference > 0f ? "▲ 올려" : "▼ 내려"));
            _directionText.color = good ? new Color(0.7f, 1f, 0.8f) : new Color(1f, 1f, 1f, 0.85f);
            _directionSub.text = "E 누르는 동안 식힘";
        }

        private void RefreshScore(PolishGame game)
        {
            int score = Mathf.RoundToInt(game.ProjectedScore * 100f);
            int burn = Mathf.RoundToInt(game.BurnLoss * 100f);
            int thin = Mathf.RoundToInt(game.ThinLoss * 100f);
            int combo = game.Combo;
            int done = game.DoneCount;
            int key = score * 10000 + burn * 100 + thin;
            if (combo != _shownCombo || done != _shownDone)
            {
                _shownCombo = combo;
                _shownDone = done;
                _comboText.text = combo >= 2 ? "콤보 x" + combo : "콤보 -";
                _comboText.color = combo >= 2 ? new Color(1f, 0.9f, 0.4f) : new Color(1f, 1f, 1f, 0.7f);
            }
            if (key != _shownScore)
            {
                _shownScore = key;
                _scoreText.text = "점수 " + score + "점" + (burn > 0 ? "   탄 곳 -" + burn : string.Empty) + (thin > 0 ? "   얇아짐 -" + thin : string.Empty);
                float value = game.ProjectedScore;
                _scoreText.color = value >= 0.75f ? new Color(0.5f, 0.92f, 0.45f) : (value >= 0.5f ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.55f, 0.45f));
            }
        }

        private void RefreshTime(PolishGame game)
        {
            bool grinding = game.Phase == PolishGame.PhaseKind.Grind;
            _timeRoot.SetActive(grinding);
            _buffRoot.SetActive(false);
            if (!grinding)
            {
                return;
            }

            float left = game.TimeLeft;
            _timeFill.anchorMax = new Vector2(Mathf.Clamp01(left / game.TimeLimit), 1f);
            int tenths = Mathf.CeilToInt(left * 10f);
            if (tenths != _shownTime)
            {
                _shownTime = tenths;
                _timeText.text = "남은 시간 " + (tenths / 10f).ToString("0.0");
                _timeText.color = left < 8f ? new Color(1f, 0.5f, 0.4f) : new Color(1f, 1f, 1f, 0.7f);
            }
        }
    }
}
