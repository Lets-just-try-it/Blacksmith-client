using System.Text;
using Blacksmith.Core.Forge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Forge
{
    // 조립 화면의 글자와 게이지를 맡는다. 방어구 그림은 AssembleBoardView가 그린다.
    // 위쪽: 이음새·어긋남·방어구 칩과 방법 창. 오른쪽: 이음새 목록. 아래쪽: 상태, 점수, 남은 시간.
    public class AssembleView : MonoBehaviour
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
        private int _seamsKey = int.MinValue;
        private bool _objectiveShown;

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
            _shownScore = _shownTime = _seamsKey = int.MinValue;
            _objectiveShown = false;
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

        public void ShowResult(AssembleGame game)
        {
            _resultBack.SetActive(true);
            _resultText.text =
                $"조립 {Mathf.RoundToInt(game.Accuracy * 100f)}점\n" +
                $"<size=60%>{ArmorKinds.NameOf(game.Kind)} 구멍 {game.DoneHoles}/{game.TotalHoles}개 박음\n" +
                $"판 어긋남 {game.Misaligned}회   판 맞춤 평균 {Mathf.RoundToInt(game.AlignAverage * 100f)}점</size>";
        }

        // 값이 바뀔 때만 문자열을 만든다.
        public void Refresh(AssembleGame game)
        {
            RefreshObjective();
            RefreshChips(game);
            RefreshStatus(game);
            RefreshSeams(game);
            RefreshScore(game);
            RefreshTime(game);
        }

        private void RefreshObjective()
        {
            if (_objectiveShown)
            {
                return;
            }

            _objectiveShown = true;
            _objectiveText.text = "오려 낸 조각을 리벳으로 잇자!\n• 이음새마다 판을 끌어 구멍에 맞추고 놓기\n• 맞으면 핀으로 고정\n• 구멍을 눌러 리벳을 끼우기\n• 바깥 링이 안쪽 원에 닿을 때 클릭 (E도 가능)\n• 이음새마다 제한시간이 있다";
        }

        private void RefreshChips(AssembleGame game)
        {
            int seam = game.Phase == AssembleGame.PhaseKind.Done ? game.Seams.Count : game.Current + 1;
            if (seam != _tabKeys[0])
            {
                _tabKeys[0] = seam;
                _tabTexts[0].text = "이음새  " + seam + " / " + game.Seams.Count;
            }

            if (game.Misaligned != _tabKeys[1])
            {
                _tabKeys[1] = game.Misaligned;
                _tabTexts[1].text = "판 어긋남  " + game.Misaligned + "회";
                _tabBoxes[1].color = game.Misaligned > 0 ? new Color(0.3f, 0.1f, 0.08f, 0.95f) : new Color(0.1f, 0.1f, 0.13f, 0.9f);
            }

            if (game.Kind != _tabKeys[2])
            {
                _tabKeys[2] = game.Kind;
                _tabTexts[2].text = ArmorKinds.NameOf(game.Kind);
            }
        }

        private void RefreshStatus(AssembleGame game)
        {
            string message;
            Color color;
            switch (game.Phase)
            {
                case AssembleGame.PhaseKind.Intro:
                    message = "조립 시작! 곧 첫 이음새가 나온다";
                    color = new Color(1f, 0.85f, 0.5f);
                    break;
                case AssembleGame.PhaseKind.SeamDone:
                    message = "이음새 완료! 다음 이음새를 가져오는 중";
                    color = new Color(0.6f, 1f, 0.7f);
                    break;
                case AssembleGame.PhaseKind.Done:
                    message = "끝!";
                    color = new Color(1f, 0.95f, 0.8f);
                    break;
                default:
                    (message, color) = WorkStatus(game);
                    break;
            }

            _statusText.color = color;
            if (message != _shownStatus)
            {
                _shownStatus = message;
                _statusText.text = message;
            }
        }

        private static (string, Color) WorkStatus(AssembleGame game)
        {
            if (!game.Locked)
            {
                if (game.Dragging && game.Aligned)
                {
                    return ("구멍이 맞았다! 놓으면 핀으로 고정된다", new Color(0.6f, 1f, 0.7f));
                }

                if (game.Dragging)
                {
                    return ("판을 구멍 위로 옮기자", new Color(1f, 0.85f, 0.5f));
                }

                return (game.Misaligned > 0 ? "맞지 않았다! 판을 다시 끌어 맞추자" : "어긋난 판을 끌어 구멍에 맞추자", game.Misaligned > 0 ? new Color(1f, 0.5f, 0.4f) : new Color(1f, 0.85f, 0.5f));
            }

            if (!game.Hammering)
            {
                return ("구멍을 눌러 리벳을 끼우자", new Color(1f, 0.85f, 0.5f));
            }

            if (game.RingTime < 0f)
            {
                return ("리벳을 끼웠다. 링이 줄어든다", new Color(0.8f, 0.85f, 1f));
            }

            return ("링이 안쪽 원에 닿을 때 클릭!", new Color(1f, 0.9f, 0.5f));
        }

        // 이음새마다 한 줄: 끝낸 이음새는 점수, 하는 중인 이음새는 박은 구멍 수
        private void RefreshSeams(AssembleGame game)
        {
            int key = game.Seams.Count * 100000 + game.Current * 10000 + (int)game.Phase * 1000 + game.DoneHoles;
            foreach (AssembleGame.Seam seam in game.Seams)
            {
                key += Mathf.RoundToInt(seam.Score * 100f);
            }

            if (key == _seamsKey)
            {
                return;
            }

            _seamsKey = key;
            _builder.Clear();
            _builder.Append("<size=130%><color=#FFD973>이음새 목록</color></size>\n");
            for (int i = 0; i < game.Seams.Count; i++)
            {
                AssembleGame.Seam seam = game.Seams[i];
                if (IsSeamDone(game, i))
                {
                    _builder.Append("<color=#99FFB3>").Append(seam.Name).Append("  ").Append(Mathf.RoundToInt(seam.Score * 100f)).Append("%</color>\n");
                }
                else if (i == game.Current && game.Phase == AssembleGame.PhaseKind.Work)
                {
                    _builder.Append("<color=#FFD973>> ").Append(seam.Name).Append("  ").Append(seam.Done).Append("/").Append(seam.Holes.Length).Append("</color>\n");
                }
                else
                {
                    _builder.Append("<color=#AAAAAA>").Append(seam.Name).Append("</color>\n");
                }
            }

            _partsText.text = _builder.ToString();
        }

        private static bool IsSeamDone(AssembleGame game, int index)
        {
            return game.Phase == AssembleGame.PhaseKind.Done
                || index < game.Current
                || (index == game.Current && game.Phase == AssembleGame.PhaseKind.SeamDone);
        }

        // 지금까지 끝낸 이음새의 평균 점수
        private void RefreshScore(AssembleGame game)
        {
            float sum = 0f;
            int count = 0;
            for (int i = 0; i < game.Seams.Count; i++)
            {
                if (IsSeamDone(game, i))
                {
                    sum += game.Seams[i].Score;
                    count++;
                }
            }

            int score = count == 0 ? -1 : Mathf.RoundToInt(sum / count * 100f);
            if (score == _shownScore)
            {
                return;
            }

            _shownScore = score;
            _scoreText.text = score < 0 ? "끝낸 이음새 평균  -" : "끝낸 이음새 평균  " + score + "점";
            float value = score / 100f;
            _scoreText.color = score < 0 ? new Color(1f, 1f, 1f, 0.7f) : (value >= game.SuccessScore ? new Color(0.5f, 0.92f, 0.45f) : (value >= 0.5f ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.55f, 0.45f)));
        }

        private void RefreshTime(AssembleGame game)
        {
            bool working = game.Phase == AssembleGame.PhaseKind.Work;
            _timeRoot.SetActive(working);
            if (!working)
            {
                return;
            }

            AssembleGame.Seam seam = game.Active;
            float left = Mathf.Max(0f, seam.Limit - seam.Time);
            _timeFill.anchorMax = new Vector2(Mathf.Clamp01(left / seam.Limit), 1f);
            int tenths = Mathf.CeilToInt(left * 10f);
            if (tenths != _shownTime)
            {
                _shownTime = tenths;
                _timeText.text = "남은 시간 " + (tenths / 10f).ToString("0.0");
                _timeText.color = left < 5f ? new Color(1f, 0.5f, 0.4f) : new Color(1f, 1f, 1f, 0.7f);
            }
        }
    }
}
