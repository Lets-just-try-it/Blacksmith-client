using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 시위 걸기의 조작 안내 판. 3D 장면(BowStringStage)이 뒤에 보이도록 투명하게 두고, 마우스는 화면 어디서 시작해도 된다.
    // 감기: 처음 누른 자리를 가운데로 한 고리(점들)가 그려진다. 시계 방향으로 돌리면 점이 차오르고, 고리 색이 빠르기를 알려 준다(초록 알맞음, 파랑 느림, 붉음 미끄러짐).
    // 걸기: 오른쪽 세로 막대는 활이 휜 정도(아래로 끌수록 차오른다)이고 초록 띠가 고리가 밀리는 알맞은 구간이다(띠는 천천히 오르내린다).
    // 아래 가로 막대는 고리가 활 끝으로 나아간 정도다. 띠 안에 휘기를 두면서 옆으로 끌어 고리를 활 끝으로 민다.
    // 규칙은 건드리지 않고 BowStringGame의 값만 읽는다.
    public class BowStringBoardView : MiniBoardView
    {
        private const int Dots = 28;
        private const float RingRadius = 1.7f;

        private static readonly Color Shadow = new Color(0.05f, 0.05f, 0.07f);
        private static readonly Color Good = new Color(0.5f, 1f, 0.6f);
        private static readonly Color Slow = new Color(0.55f, 0.75f, 1f);
        private static readonly Color Fast = new Color(1f, 0.55f, 0.35f);
        private static readonly Color Slip = new Color(1f, 0.35f, 0.3f);

        private BowStringGame _game;

        protected override bool ClampToBoard => false;

        protected override void OnBind(MiniGame game)
        {
            _game = (BowStringGame)game;
        }

        protected override void OnUnbind()
        {
            _game = null;
        }

        protected override void Draw()
        {
            Canvas.Clear(Color.clear);
            if (_game.Phase == BowStringGame.PhaseKind.Wrap)
            {
                DrawWrapRing();
            }
            else if (_game.Phase == BowStringGame.PhaseKind.Flex)
            {
                DrawFlexGauge();
            }
        }

        private Color SpeedColor()
        {
            float speed = _game.Speed;
            if (speed > _game.SlipSpeed)
            {
                return Slip;
            }

            if (speed > _game.SpeedHi)
            {
                return Fast;
            }

            return speed >= _game.SpeedLo ? Good : Slow;
        }

        private void DrawWrapRing()
        {
            float centerX = _game.Holding ? _game.CenterX : (_game.PointerX > -50f ? _game.PointerX : 0f);
            float centerY = _game.Holding ? _game.CenterY : (_game.PointerX > -50f ? _game.PointerY : 0f);
            Vector2 center = ToCanvas(centerX, centerY);
            float radius = RingRadius * Unit;
            Color ring = _game.Holding ? SpeedColor() : new Color(1f, 1f, 1f, 0.5f);
            int filled = Mathf.FloorToInt(_game.Turn * Dots);
            for (int i = 0; i < Dots; i++)
            {
                float angle = i * Mathf.PI * 2f / Dots;
                Vector2 position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                bool on = _game.Holding && i < filled;
                Canvas.Circle(position, on ? 5f : 3f, on ? ring : new Color(ring.r, ring.g, ring.b, 0.45f));
            }

            // 도는 방향(시계 방향) 화살표
            Vector2 top = center + new Vector2(0f, -radius - 14f);
            Canvas.Line(top, top + new Vector2(-9f, 7f), 3f, new Color(1f, 1f, 1f, 0.6f));
            Canvas.Line(top, top + new Vector2(-9f, -7f), 3f, new Color(1f, 1f, 1f, 0.6f));
            Canvas.Line(top + new Vector2(-30f, 0f), top, 3f, new Color(1f, 1f, 1f, 0.6f));

            if (_game.PointerX > -50f)
            {
                Vector2 pointer = ToCanvas(_game.PointerX, _game.PointerY);
                Canvas.Circle(pointer, 8f, new Color(0f, 0f, 0f, 0.35f));
                Canvas.Circle(pointer, 5.5f, _game.Holding ? ring : new Color(0.9f, 0.92f, 1f));
            }
        }

        private void DrawFlexGauge()
        {
            // 휘기 세로 막대: 위가 0, 아래가 1(아래로 끌수록 휜다)
            Rect bar = new Rect(ToCanvas(7.1f, 0f).x, ToCanvas(0f, -3.2f).y, 26f, 6.4f * Unit);
            PixelShapes.Plate(Canvas, bar, 10f, new Color(0.08f, 0.08f, 0.1f), new Color(0.7f, 0.72f, 0.8f), Shadow);
            float inner = bar.height - 8f;
            float windowTop = bar.y + 4f + inner * Mathf.Clamp01(_game.WindowLo);
            float windowHeight = inner * (Mathf.Clamp01(_game.WindowHi) - Mathf.Clamp01(_game.WindowLo));
            Rect window = new Rect(bar.x + 2f, windowTop, bar.width - 4f, windowHeight);
            Canvas.Fill(window, new Color(0.4f, 0.9f, 0.55f, 0.6f));
            PixelShapes.Outline(Canvas, window, Good, 2f);
            Canvas.Fill(new Rect(bar.x + 6f, bar.y + 4f, bar.width - 12f, inner * _game.Flex), _game.OverFlexed ? new Color(1f, 0.45f, 0.35f, 0.85f) : new Color(1f, 0.75f, 0.4f, 0.85f));
            float needle = bar.y + 4f + inner * _game.Flex;
            Color needleColor = _game.InWindow ? Good : (_game.OverFlexed ? Slip : Color.white);
            Canvas.Line(new Vector2(bar.x - 8f, needle), new Vector2(bar.xMax + 8f, needle), 4f, needleColor);

            // 고리 진행 가로 막대와 미는 방향 화살표
            Rect slide = new Rect(ToCanvas(-3f, 0f).x, ToCanvas(0f, 4.6f).y, 6f * Unit, 18f);
            PixelShapes.Plate(Canvas, slide, 9f, new Color(0.08f, 0.08f, 0.1f), new Color(0.7f, 0.72f, 0.8f), Shadow);
            float width = (slide.width - 8f) * _game.Slide;
            float startX = _game.SlideDirection > 0 ? slide.x + 4f : slide.xMax - 4f - width;
            Canvas.Fill(new Rect(startX, slide.y + 4f, width, slide.height - 8f), _game.InWindow ? new Color(0.5f, 1f, 0.6f, 0.9f) : new Color(1f, 0.85f, 0.45f, 0.9f));
            float arrowX = _game.SlideDirection > 0 ? slide.xMax + 14f : slide.x - 14f;
            float sign = _game.SlideDirection;
            Vector2 tip = new Vector2(arrowX + sign * 12f, slide.center.y);
            Canvas.Line(new Vector2(arrowX - sign * 12f, slide.center.y), tip, 4f, new Color(1f, 1f, 1f, 0.7f));
            Canvas.Line(tip, tip + new Vector2(-sign * 9f, 7f), 4f, new Color(1f, 1f, 1f, 0.7f));
            Canvas.Line(tip, tip + new Vector2(-sign * 9f, -7f), 4f, new Color(1f, 1f, 1f, 0.7f));
        }
    }
}
