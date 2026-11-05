using Blacksmith.Core.Forge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Forge
{
    // 정제 화면의 글자와 게이지를 맡는다. 진행은 SmeltingStation이 정하고 이 클래스는 받은 값을 보여 준다.
    // 위쪽에 ① 원석 쪼개기 ▶ ② 풀무질 ▶ ③ 쇳물 붓기 표시, 아래쪽에 단계별 패널(수율·콤보, 풍구 게이지)과 안내 한 줄이 있다.
    public class SmeltingView : MonoBehaviour
    {
        private const float PopupTime = 0.9f;
        private const float ScreenPopupTime = 1.4f;
        private const float WorldPopupRise = 60f;

        private static readonly Color Current = new Color(1f, 0.82f, 0.38f);
        private static readonly Color Done = new Color(0.5f, 0.9f, 0.5f);
        private static readonly Color Pending = new Color(1f, 1f, 1f, 0.3f);

        private struct Popup
        {
            public TMP_Text Text;
            public float Start;
            public Vector3 World;
            public float HeightFraction;
            public bool InWorld;
            public bool Active;
        }

        [SerializeField] private GameObject _promptRoot;
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Image[] _stripBoxes;
        [SerializeField] private TMP_Text[] _stripTexts;
        [SerializeField] private TMP_Text _hintText;

        [Header("① 쪼개기")]
        [SerializeField] private GameObject _crackRoot;
        [SerializeField] private TMP_Text _yieldText;
        [SerializeField] private TMP_Text _comboText;
        [SerializeField] private RectTransform _judgedFill;
        [SerializeField] private TMP_Text _judgedText;

        [Header("② 풀무질")]
        [SerializeField] private GameObject _bellowsRoot;
        [SerializeField] private RectTransform _zone;
        [SerializeField] private Image _zoneImage;
        [SerializeField] private RectTransform _cursor;
        [SerializeField] private Image _cursorImage;
        [SerializeField] private RectTransform _heatFill;
        [SerializeField] private Image _heatImage;
        [SerializeField] private TMP_Text _heatText;
        [SerializeField] private TMP_Text _moltenText;
        [SerializeField] private TMP_Text _limitText;

        [Header("③ 붓기")]
        [SerializeField] private GameObject _pourRoot;
        [SerializeField] private RectTransform _pourBand;
        [SerializeField] private RectTransform _pourFill;
        [SerializeField] private Image _pourFillImage;
        [SerializeField] private TMP_Text _pourFillText;
        [SerializeField] private TMP_Text _pourInfoText;
        [SerializeField] private TMP_Text _pourWarnText;
        [SerializeField] private TMP_Text _pourAccuracyText;
        [SerializeField] private TMP_Text _pourTargetText;

        [Header("결과·효과")]
        [SerializeField] private TMP_Text _resultText;
        [SerializeField] private GameObject _resultBack;
        [SerializeField] private Image _flash;
        [SerializeField] private TMP_Text[] _popupTexts;

        private Popup[] _popups;
        private Camera _camera;
        private SmeltingGame.ActKind _shownAct = (SmeltingGame.ActKind)(-1);
        private float _flashAlpha;
        private int _nextPopup;
        private int _shownYield = -1;
        private int _shownCombo = -1;
        private int _shownJudged = -1;
        private int _shownHeat = -1;
        private int _shownLimit = -1;
        private int _shownPourFill = -1;
        private int _shownPourInfo = -1;
        private int _shownPourAccuracy = -1;

        private void Awake()
        {
            _popups = new Popup[_popupTexts.Length];
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

            UpdatePopups();
        }

        public void ShowPrompt(bool visible)
        {
            _promptRoot.SetActive(visible);
        }

        public void ShowStart(Camera camera)
        {
            _camera = camera;
            _panelRoot.SetActive(true);
            _resultText.text = string.Empty;
            _resultBack.SetActive(false);
            _shownAct = (SmeltingGame.ActKind)(-1);
            _shownYield = _shownCombo = _shownJudged = _shownHeat = _shownLimit = _shownPourFill = _shownPourInfo = _shownPourAccuracy = -1;
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

        // 값이 바뀔 때만 문자열을 만든다.
        public void Refresh(SmeltingGame game)
        {
            if (game.Act != _shownAct)
            {
                _shownAct = game.Act;
                OnActChanged(game.Act);
            }

            switch (game.Act)
            {
                case SmeltingGame.ActKind.Crack:
                    RefreshCrack(game);
                    break;
                case SmeltingGame.ActKind.Bellows:
                    RefreshBellows(game);
                    break;
                case SmeltingGame.ActKind.Pour:
                    RefreshPour(game);
                    break;
            }
        }

        // 강한 변화에 화면 전체가 약하게 번쩍인다.
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

        public void PopupWorld(string text, Color color, Vector3 world)
        {
            AddPopup(text, color, true, world, 0f);
        }

        // heightFraction은 화면 아래쪽에서 잰 높이 비율이다.
        public void PopupScreen(string text, Color color, float heightFraction)
        {
            AddPopup(text, color, false, Vector3.zero, heightFraction);
        }

        public void ShowResult(SmeltingGame game)
        {
            _hintText.text = string.Empty;
            _resultBack.SetActive(true);
            _resultText.text =
                $"정제 {Mathf.RoundToInt(game.Accuracy * 100f)}%\n" +
                $"<size=60%>원석 수율 {Mathf.RoundToInt(game.CrackScore * 100f)}%   풀무질 {Mathf.RoundToInt(game.Fluidity * 100f)}%\n" +
                $"주조 {Mathf.RoundToInt(game.PourQuality * 100f)}%   순도 {Mathf.RoundToInt(game.Purity * 100f)}%</size>";
        }

        private void OnActChanged(SmeltingGame.ActKind act)
        {
            _crackRoot.SetActive(act == SmeltingGame.ActKind.Crack);
            _bellowsRoot.SetActive(act == SmeltingGame.ActKind.Bellows);
            _pourRoot.SetActive(act == SmeltingGame.ActKind.Pour);

            int current = act == SmeltingGame.ActKind.Crack ? 0 : (act == SmeltingGame.ActKind.Bellows ? 1 : 2);
            for (int i = 0; i < _stripBoxes.Length; i++)
            {
                bool done = i < current || act == SmeltingGame.ActKind.Done;
                bool now = i == current && act != SmeltingGame.ActKind.Done;
                _stripBoxes[i].color = now ? new Color(0.25f, 0.17f, 0.08f, 0.92f) : new Color(0.1f, 0.1f, 0.13f, 0.8f);
                _stripTexts[i].color = now || done ? Color.white : new Color(1f, 1f, 1f, 0.5f);
            }

            switch (act)
            {
                case SmeltingGame.ActKind.Crack:
                    _hintText.text = "원석을 쪼개자!\n• 고리가 균열에 맞는 순간 마우스 클릭\n• 보라색 저주 균열은 누르지 않기";
                    break;
                case SmeltingGame.ActKind.Bellows:
                    _hintText.text = "쇳물을 달구자!\n• E 또는 스페이스바를 누르면 지시선이 올라가고, 떼면 내려감\n• 지시선을 불꽃 띠 안에 붙들기";
                    break;
                case SmeltingGame.ActKind.Pour:
                    _hintText.text = "쇳물을 틀에 붓자!\n• 마우스로 도가니를 좌우로 옮기기\n• 클릭한 채 위로 올리면 기울어 쏟음\n• 틀 눈금까지만 채우기\n• 다 부었으면 E 또는 스페이스바";
                    break;
            }
        }

        private void RefreshCrack(SmeltingGame game)
        {
            float crack = game.CrackSoFar;
            int percent = Mathf.RoundToInt(crack * 100f);
            if (percent != _shownYield)
            {
                _shownYield = percent;
                _yieldText.text = percent + "%";
                _yieldText.color = GradeColor(crack);
            }

            if (game.Combo != _shownCombo)
            {
                _shownCombo = game.Combo;
                _comboText.text = game.Combo.ToString();
                _comboText.color = game.Combo >= 6 ? new Color(1f, 0.6f, 0.2f) : (game.Combo >= 3 ? new Color(1f, 0.85f, 0.3f) : Color.white);
            }

            int judged = game.Judged;
            if (judged != _shownJudged)
            {
                _shownJudged = judged;
                _judgedFill.anchorMax = new Vector2((float)judged / game.Targets.Count, 1f);
                _judgedText.text = $"균열 {judged} / {game.Targets.Count}";
            }
        }

        private void RefreshBellows(SmeltingGame game)
        {
            float half = game.FlameHalfWidth;
            _zone.anchorMin = new Vector2(game.Zone - half, 0f);
            _zone.anchorMax = new Vector2(game.Zone + half, 1f);
            _zoneImage.color = game.InZone ? new Color(1f, 0.75f, 0.2f, 0.95f) : new Color(1f, 0.45f, 0.1f, 0.7f);

            float cursorHalf = game.CursorHalf;
            _cursor.anchorMin = new Vector2(game.Cursor - cursorHalf, 0f);
            _cursor.anchorMax = new Vector2(game.Cursor + cursorHalf, 1f);
            _cursorImage.color = game.InZone ? new Color(1f, 1f, 1f, 0.95f) : new Color(0.75f, 0.85f, 1f, 0.9f);

            float heat = Mathf.Clamp01(game.Heat);
            _heatFill.anchorMax = new Vector2(heat, 1f);
            _heatImage.color = ForgeGreybox.HeatColor(Mathf.Clamp01(0.25f + heat * 0.8f));
            int percent = Mathf.RoundToInt(heat * 100f);
            if (percent != _shownHeat)
            {
                _shownHeat = percent;
                _heatText.text = percent + "%";
            }

            _moltenText.gameObject.SetActive(game.Molten);
            if (game.Molten)
            {
                _moltenText.text = $"다 녹았다! 묽기 {Mathf.RoundToInt(game.Fluidity * 100f)}%  (빨리 녹일수록 쇳물이 오래 묽다)";
            }

            int limit = Mathf.CeilToInt(Mathf.Max(0f, game.BellowsLimit - game.ActClock) * 10f);
            if (limit != _shownLimit)
            {
                _shownLimit = limit;
                _limitText.text = $"제한 {limit / 10f:0.0}초";
                _limitText.color = limit < 30 ? new Color(1f, 0.5f, 0.4f) : new Color(1f, 1f, 1f, 0.7f);
            }
        }

        // 틀에 찬 양을 눈금(0 ~ FillMax)에 보여 준다. 초록 띠가 목표 구간이고, 지금 끝냈을 때의 정확도도 같이 보여 준다.
        private void RefreshPour(SmeltingGame game)
        {
            float max = Mathf.Max(0.01f, game.FillMax);
            _pourBand.anchorMin = new Vector2(game.FillLow / max, 0f);
            _pourBand.anchorMax = new Vector2(Mathf.Min(1f, game.FillHigh / max), 1f);

            float fill = Mathf.Clamp01(game.Fill / max);
            _pourFill.anchorMax = new Vector2(fill, 1f);
            bool low = game.Fill < game.FillLow;
            bool high = game.Fill > game.FillHigh;
            _pourFillImage.color = high ? new Color(1f, 0.4f, 0.3f, 0.95f) : (low ? new Color(1f, 0.78f, 0.3f, 0.95f) : new Color(0.45f, 0.95f, 0.5f, 0.95f));

            int fillPercent = Mathf.RoundToInt(game.Fill * 100f);
            if (fillPercent != _shownPourFill)
            {
                _shownPourFill = fillPercent;
                string state = high ? "넘침" : (low ? "부족" : "딱 좋아요");
                _pourFillText.text = $"채움 {fillPercent}%  <size=70%>{state}</size>";
                _pourFillText.color = high ? new Color(1f, 0.5f, 0.4f) : (low ? new Color(1f, 0.85f, 0.4f) : new Color(0.55f, 1f, 0.6f));
                _pourTargetText.text = $"목표 {Mathf.RoundToInt(game.FillLow * 100f)}~{Mathf.RoundToInt(game.FillHigh * 100f)}%";
            }

            int reserve = Mathf.RoundToInt(Mathf.Clamp01(game.Reserve / Mathf.Max(0.01f, game.ReserveStart)) * 100f);
            int info = reserve * 1000 + Mathf.RoundToInt(game.Spilled * 100f) * 10 + Mathf.RoundToInt(game.PreviewPurity * 10f);
            if (info != _shownPourInfo)
            {
                _shownPourInfo = info;
                _pourInfoText.text = $"도가니 남음 {reserve}%    흘린 양 {Mathf.RoundToInt(game.Spilled * 100f)}%    순도 {Mathf.RoundToInt(game.PreviewPurity * 100f)}%";
            }

            int accuracy = Mathf.RoundToInt(game.PreviewAccuracy * 100f);
            if (accuracy != _shownPourAccuracy)
            {
                _shownPourAccuracy = accuracy;
                _pourAccuracyText.text = $"지금 끝내면 정제 {accuracy}%";
                _pourAccuracyText.color = GradeColor(game.PreviewAccuracy);
            }

            string warn = string.Empty;
            if (game.Fill > 0f && !game.LandsInMold && game.Flow > 0.01f)
            {
                warn = "틀 밖에 붓는 중!";
            }
            else if (game.Clogged)
            {
                warn = "쇳물이 굳어 막혔다!";
            }
            else if (game.Fill >= game.FillHigh)
            {
                warn = "그만! 도가니를 내려요";
            }

            _pourWarnText.text = warn;
        }

        private static Color GradeColor(float value)
        {
            return value >= 0.75f ? new Color(0.5f, 0.92f, 0.45f) : (value >= 0.5f ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.55f, 0.45f));
        }

        private void AddPopup(string text, Color color, bool inWorld, Vector3 world, float heightFraction)
        {
            if (_popups == null || _popups.Length == 0)
            {
                return;
            }

            ref Popup popup = ref _popups[_nextPopup];
            _nextPopup = (_nextPopup + 1) % _popups.Length;
            popup.Active = true;
            popup.Start = Time.time;
            popup.InWorld = inWorld;
            popup.World = world;
            popup.HeightFraction = heightFraction;
            popup.Text.text = text;
            popup.Text.color = color;
            popup.Text.fontSize = inWorld ? 40f : 56f;
            popup.Text.gameObject.SetActive(true);
        }

        private void UpdatePopups()
        {
            for (int i = 0; i < _popups.Length; i++)
            {
                if (!_popups[i].Active)
                {
                    continue;
                }

                float duration = _popups[i].InWorld ? PopupTime : ScreenPopupTime;
                float age = (Time.time - _popups[i].Start) / duration;
                if (age >= 1f)
                {
                    _popups[i].Active = false;
                    _popups[i].Text.gameObject.SetActive(false);
                    continue;
                }

                var rect = (RectTransform)_popups[i].Text.transform;
                Color color = _popups[i].Text.color;
                color.a = 1f - age * age;
                _popups[i].Text.color = color;
                PlacePopup(rect, _popups[i], age);
            }
        }

        private void PlacePopup(RectTransform rect, Popup popup, float age)
        {
            if (popup.InWorld && _camera != null)
            {
                Vector3 screen = _camera.WorldToScreenPoint(popup.World);
                float canvasScale = ((RectTransform)transform).rect.height / Screen.height;
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.anchoredPosition = new Vector2(screen.x, screen.y) * canvasScale + Vector2.up * (WorldPopupRise * age);
                return;
            }

            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, popup.HeightFraction);
            rect.anchoredPosition = new Vector2(0f, 24f * age);
        }
    }
}
