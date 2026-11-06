using System.Text;
using Blacksmith.Core.Forge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Forge
{
    // 판 성형 화면의 글자와 게이지를 맡는다. 쇠판 그림은 PlateBoardView가 그린다.
    // 위쪽: 조각·띠 벗어남·방어구 칩과 방법 창. 오른쪽: 조각 목록. 아래쪽: 상태, 점수, 남은 시간.
    public class PlateView : MonoBehaviour
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
        [SerializeField] private TMP_Text _partsText;

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

        private readonly int[] _tabKeys = { int.MinValue, int.MinValue, int.MinValue };
        private readonly StringBuilder _builder = new StringBuilder();
        private PopupSlot[] _popups;
        private int _nextPopup;
        private float _flashAlpha;
        private string _shownStatus;
        private int _shownScore = int.MinValue;
        private int _shownTime = int.MinValue;
        private int _partsKey = int.MinValue;
        private PlateGame.PhaseKind _shownPhase = (PlateGame.PhaseKind)(-1);

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
            // 처음 Refresh에서 모든 글자가 새로 쓰이도록 어떤 값과도 같지 않은 값으로 되돌린다.
            _shownScore = _shownTime = _partsKey = int.MinValue;
            _shownPhase = (PlateGame.PhaseKind)(-1);
            for (int i = 0; i < _tabKeys.Length; i++)
            {
                _tabKeys[i] = int.MinValue;
                _tabTexts[i].color = Color.white;
            }

            _tabBoxes[0].color = new Color(0.12f, 0.2f, 0.14f, 0.95f);
            _tabBoxes[1].color = new Color(0.1f, 0.1f, 0.13f, 0.9f);
            _tabBoxes[2].color = new Color(0.1f, 0.16f, 0.24f, 0.95f);
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

        // 보드 중심을 기준으로 한 위치(위쪽이 +)에 글자를 띄운다. 보드 위에 놓인 글자 풀을 쓴다.
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

        public void ShowResult(PlateGame game)
        {
            _resultBack.SetActive(true);
            _resultText.text =
                $"판 성형 {Mathf.RoundToInt(game.Accuracy * 100f)}점\n" +
                $"<size=60%>{ArmorKinds.NameOf(game.Kind)} 조각 {game.DoneCount}/{game.Parts.Count}개 재단\n" +
                $"띠 벗어남 {game.Slips}회   남은 시간 {Mathf.RoundToInt(game.TimeLeft)}초</size>";
        }

        // 값이 바뀔 때만 문자열을 만든다.
        public void Refresh(PlateGame game)
        {
            RefreshPhase(game);
            RefreshChips(game);
            RefreshStatus(game);
            RefreshParts(game);
            RefreshScore(game);
            RefreshTime(game);
        }

        private void RefreshPhase(PlateGame game)
        {
            if (game.Phase == _shownPhase)
            {
                return;
            }

            _shownPhase = game.Phase;
            switch (game.Phase)
            {
                case PlateGame.PhaseKind.Cut:
                    _objectiveText.text = "쇠판을 조각 모양으로 오리자!\n• 초록 점을 누른 채 시계 방향으로 선을 따라가기\n• 띠 안에서만 잘리고, 벗어나면 멈춤\n• 붉은 구간은 띠가 좁다\n• 놓아도 초록 점을 다시 눌러 이어 감\n• E·스페이스바: 그만 오리기";
                    break;
                default:
                    _objectiveText.text = "재단 끝!";
                    break;
            }
        }

        private void RefreshChips(PlateGame game)
        {
            if (game.DoneCount != _tabKeys[0])
            {
                _tabKeys[0] = game.DoneCount;
                _tabTexts[0].text = "조각  " + game.DoneCount + " / " + (game.Parts.Count > 0 ? game.Parts.Count.ToString() : "-");
            }

            if (game.Slips != _tabKeys[1])
            {
                _tabKeys[1] = game.Slips;
                _tabTexts[1].text = "띠 벗어남  " + game.Slips + "회";
                _tabBoxes[1].color = game.Slips > 0 ? new Color(0.3f, 0.1f, 0.08f, 0.95f) : new Color(0.1f, 0.1f, 0.13f, 0.9f);
            }

            if (game.Kind != _tabKeys[2])
            {
                _tabKeys[2] = game.Kind;
                _tabTexts[2].text = ArmorKinds.NameOf(game.Kind);
            }
        }

        private void RefreshStatus(PlateGame game)
        {
            string message;
            Color color;
            if (game.Phase == PlateGame.PhaseKind.Done)
            {
                message = "끝!";
                color = new Color(1f, 0.95f, 0.8f);
            }
            else if (game.Pausing)
            {
                message = "조각 완성! 다음 조각을 꺼내는 중";
                color = new Color(0.6f, 1f, 0.7f);
            }
            else if (game.Cutting && game.OffBand)
            {
                message = "띠를 벗어났다! 초록 점 근처로 돌아오자";
                color = new Color(1f, 0.4f, 0.35f);
            }
            else if (game.Cutting)
            {
                message = "잘 오려진다!";
                color = new Color(0.6f, 1f, 0.7f);
            }
            else
            {
                message = game.CurrentPart != null && game.CurrentPart.Head > 0 ? "초록 점을 눌러 이어서 오리자" : "초록 점을 누른 채 선을 따라 오리자";
                color = new Color(1f, 0.85f, 0.5f);
            }

            _statusText.color = color;
            if (message != _shownStatus)
            {
                _shownStatus = message;
                _statusText.text = message;
            }
        }

        // 조각마다 한 줄: 완료한 조각은 정확도, 오리는 중인 조각은 진행률
        private void RefreshParts(PlateGame game)
        {
            int key = game.Parts.Count * 100000 + game.Current * 10000 + game.DoneCount * 1000;
            if (game.CurrentPart != null)
            {
                key += Mathf.RoundToInt(game.CurrentPart.Progress * 20f);
            }

            foreach (PlateGame.Part part in game.Parts)
            {
                key += part.Done ? Mathf.RoundToInt(part.Score * 100f) : 0;
            }

            if (key == _partsKey)
            {
                return;
            }

            _partsKey = key;
            _builder.Clear();
            _builder.Append("<size=130%><color=#FFD973>조각 목록</color></size>\n");
            for (int i = 0; i < game.Parts.Count; i++)
            {
                PlateGame.Part part = game.Parts[i];
                bool current = i == game.Current && game.Phase == PlateGame.PhaseKind.Cut;
                if (part.Done)
                {
                    _builder.Append("<color=#99FFB3>").Append(part.Name).Append("  ").Append(Mathf.RoundToInt(part.Score * 100f)).Append("%</color>\n");
                }
                else if (current)
                {
                    _builder.Append("<color=#FFD973>> ").Append(part.Name).Append("  ").Append(Mathf.RoundToInt(part.Progress * 100f)).Append("%</color>\n");
                }
                else
                {
                    _builder.Append("<color=#AAAAAA>").Append(part.Name).Append("</color>\n");
                }
            }

            _partsText.text = _builder.ToString();
        }

        // 지금까지 완성한 조각의 평균 점수
        private void RefreshScore(PlateGame game)
        {
            float sum = 0f;
            int count = 0;
            foreach (PlateGame.Part part in game.Parts)
            {
                if (part.Done)
                {
                    sum += part.Score;
                    count++;
                }
            }

            int score = count == 0 ? -1 : Mathf.RoundToInt(sum / count * 100f);
            if (score == _shownScore)
            {
                return;
            }

            _shownScore = score;
            _scoreText.text = score < 0 ? "완성한 조각 평균  -" : "완성한 조각 평균  " + score + "점";
            float value = score / 100f;
            _scoreText.color = score < 0 ? new Color(1f, 1f, 1f, 0.7f) : (value >= 0.75f ? new Color(0.5f, 0.92f, 0.45f) : (value >= 0.5f ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.55f, 0.45f)));
        }

        private void RefreshTime(PlateGame game)
        {
            bool cutting = game.Phase == PlateGame.PhaseKind.Cut;
            _timeRoot.SetActive(cutting);
            if (!cutting)
            {
                return;
            }

            float left = game.TimeLeft;
            _timeFill.anchorMax = new Vector2(Mathf.Clamp01(left / game.Limit), 1f);
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
