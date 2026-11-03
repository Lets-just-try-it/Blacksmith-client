using System.Collections.Generic;
using Blacksmith.Core.Forge;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Forge
{
    // 단조 박자 레인. 오른쪽에서 박자가 다가와 왼쪽 판정선에 닿는 순간 누른다.
    // 작은 박자는 원 하나, 큰 박자는 시작 원과 끝 원을 막대로 잇는다. 친 박자는 판정 색으로 바뀌며 사라진다.
    public class ForgingLaneView : MonoBehaviour
    {
        private const float LineOffset = 90f;
        private const float RightMargin = 20f;
        private const float FadeTime = 0.5f;

        private sealed class BeatMark
        {
            public Image Bar;
            public Image Start;
            public Image End;
        }

        [SerializeField] private RectTransform _lane;
        // 박자 표시 크기 배율
        [SerializeField, Min(0.5f)] private float _markScale = 1f;
        [SerializeField] private RectTransform _line;
        [SerializeField] private RectTransform _weakBand;
        [SerializeField] private RectTransform _goodBand;
        [SerializeField] private RectTransform _perfectBand;
        [SerializeField] private Sprite _circleSprite;
        [SerializeField] private Color _lightColor = new Color(1f, 0.62f, 0.28f);
        [SerializeField] private Color _heavyColor = new Color(1f, 0.82f, 0.2f);
        [SerializeField] private Color _perfectColor = new Color(1f, 0.88f, 0.3f);
        [SerializeField] private Color _goodColor = new Color(0.5f, 0.92f, 0.45f);
        [SerializeField] private Color _weakColor = new Color(0.85f, 0.85f, 0.88f);
        [SerializeField] private Color _missColor = new Color(1f, 0.35f, 0.3f);

        private readonly List<BeatMark> _marks = new List<BeatMark>();
        private ForgingGame _game;
        private float _speed;

        public void Bind(ForgingGame game, ForgingRules rules)
        {
            _game = game;
            float width = _lane.rect.width;
            _speed = (width - LineOffset - RightMargin) / rules.TravelTime;

            _line.anchoredPosition = new Vector2(LineOffset, 0f);
            SetBand(_weakBand, game.WeakWindow);
            SetBand(_goodBand, game.GoodWindow);
            SetBand(_perfectBand, game.PerfectWindow);

            while (_marks.Count < game.Beats.Count)
            {
                _marks.Add(CreateMark());
            }
            for (int i = 0; i < _marks.Count; i++)
            {
                SetMarkVisible(_marks[i], false, false, false);
            }
        }

        public void Refresh()
        {
            if (_game == null)
            {
                return;
            }

            float width = _lane.rect.width;
            for (int i = 0; i < _game.Beats.Count; i++)
            {
                ForgingGame.Beat beat = _game.Beats[i];
                BeatMark mark = _marks[i];
                float endX = LineOffset + (beat.Time - _game.Clock) * _speed;
                float startX = LineOffset + (beat.PressTime - _game.Clock) * _speed;

                // 아직 나타나기 전이거나 지나간 박자는 숨긴다.
                if (startX > width - 6f || endX < -30f)
                {
                    SetMarkVisible(mark, false, false, false);
                    continue;
                }

                if (beat.IsHeavy)
                {
                    DrawHeavy(mark, beat, startX, endX);
                }
                else
                {
                    DrawLight(mark, beat, endX);
                }
            }
        }

        private void DrawLight(BeatMark mark, ForgingGame.Beat beat, float x)
        {
            if (!DrawResult(mark, beat, x, 30f * _markScale))
            {
                SetMarkVisible(mark, false, true, false);
                Place(mark.Start, x, 30f * _markScale, _lightColor);
            }
        }

        private void DrawHeavy(BeatMark mark, ForgingGame.Beat beat, float startX, float endX)
        {
            if (DrawResult(mark, beat, endX, 48f * _markScale))
            {
                return;
            }

            bool holding = _game.Holding == beat;
            // 누르는 동안 시작 표시는 판정선에 붙어 있다.
            float barStart = Mathf.Max(holding ? LineOffset : startX, 4f);
            SetMarkVisible(mark, true, !holding, true);

            RectTransform bar = mark.Bar.rectTransform;
            bar.anchoredPosition = new Vector2((barStart + endX) * 0.5f, 0f);
            bar.sizeDelta = new Vector2(Mathf.Max(0f, endX - barStart), 24f * _markScale);
            mark.Bar.color = new Color(_heavyColor.r, _heavyColor.g, _heavyColor.b, holding ? 0.9f : 0.35f);
            Place(mark.Start, startX, 34f * _markScale, _heavyColor);
            Place(mark.End, endX, 48f * _markScale, _heavyColor);
        }

        // 판정된 박자를 결과 색으로 보여 주고 서서히 지운다. 보여 주는 중이면 true를 돌려준다.
        private bool DrawResult(BeatMark mark, ForgingGame.Beat beat, float x, float size)
        {
            if (!beat.Judged)
            {
                return false;
            }

            float age = Mathf.Clamp01((_game.Clock - beat.Time) / FadeTime);
            if (age >= 1f)
            {
                SetMarkVisible(mark, false, false, false);
                return true;
            }

            SetMarkVisible(mark, false, false, true);
            Color color = ResultColor(beat.Quality);
            color.a = 1f - age;
            Place(mark.End, x, size + 26f * _markScale * age, color);
            return true;
        }

        private Color ResultColor(float quality)
        {
            if (quality >= 1f)
            {
                return _perfectColor;
            }
            if (quality >= 0.75f)
            {
                return _goodColor;
            }
            return quality > 0f ? _weakColor : _missColor;
        }

        private void SetBand(RectTransform band, float window)
        {
            band.anchoredPosition = new Vector2(LineOffset, 0f);
            band.sizeDelta = new Vector2(window * 2f * _speed, band.sizeDelta.y);
        }

        private static void Place(Image image, float x, float size, Color color)
        {
            RectTransform rect = image.rectTransform;
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(size, size);
            image.color = color;
        }

        private static void SetMarkVisible(BeatMark mark, bool bar, bool start, bool end)
        {
            mark.Bar.gameObject.SetActive(bar);
            mark.Start.gameObject.SetActive(start);
            mark.End.gameObject.SetActive(end);
        }

        private BeatMark CreateMark()
        {
            return new BeatMark
            {
                Bar = CreateImage("Bar", null),
                Start = CreateImage("Start", _circleSprite),
                End = CreateImage("End", _circleSprite),
            };
        }

        private Image CreateImage(string objectName, Sprite sprite)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(_lane, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            go.SetActive(false);
            return image;
        }
    }
}
