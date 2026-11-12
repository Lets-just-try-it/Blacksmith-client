using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Forge
{
    // 활 휘기, 마석 박기처럼 같은 틀을 쓰는 미니게임 화면의 글자와 게이지를 맡는다. 그림은 각 보드 화면이 그린다.
    // 위쪽: 칩 세 개와 방법 창. 오른쪽: 목록. 아래쪽: 상태, 점수, 남은 시간. 내용은 작업대가 문자열로 넘긴다.
    public class MiniPanelView : MonoBehaviour
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

        private PopupSlot[] _popups;
        private int _nextPopup;
        private float _flashAlpha;

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

        public void ShowStart(string objective)
        {
            _panelRoot.SetActive(true);
            _resultText.text = string.Empty;
            _resultBack.SetActive(false);
            _objectiveText.text = objective;
            _tabBoxes[0].color = new Color(0.12f, 0.2f, 0.14f, 0.95f);
            _tabBoxes[1].color = new Color(0.1f, 0.1f, 0.13f, 0.9f);
            _tabBoxes[2].color = new Color(0.1f, 0.16f, 0.24f, 0.95f);
            foreach (TMP_Text text in _tabTexts)
            {
                text.color = Color.white;
            }

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

        public void SetChip(int index, string text, Color? boxColor = null)
        {
            _tabTexts[index].text = text;
            if (boxColor.HasValue)
            {
                _tabBoxes[index].color = boxColor.Value;
            }
        }

        public void SetList(string text)
        {
            _partsText.text = text;
        }

        public void SetStatus(string text, Color color)
        {
            _statusText.color = color;
            _statusText.text = text;
        }

        public void SetScore(string text, Color color)
        {
            _scoreText.text = text;
            _scoreText.color = color;
        }

        public void SetTime(bool visible, float fraction, string text, Color color)
        {
            _timeRoot.SetActive(visible);
            if (!visible)
            {
                return;
            }

            _timeFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            _timeText.text = text;
            _timeText.color = color;
        }

        public void ShowResult(string text)
        {
            _resultBack.SetActive(true);
            _resultText.text = text;
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
    }
}
