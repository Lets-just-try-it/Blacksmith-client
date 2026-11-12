using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 시험 사격의 조준 화면. 3D 장면(BowRangeStage)이 뒤에 보이도록 투명하게 두고 조준 장치만 그린다.
    // 조준점은 마우스를 따라다니고, 시위를 당길수록 모이고 오래 버티면 떨린다. 낙차 눈금(아래쪽 가로 눈금, 점 하나가 5m)과
    // 바람 눈금(좌우 세로 눈금, 지금 거리에서 바람 1단위)이 있다. 아래에는 시위 당김 막대와 숨 막대가 있다.
    // 규칙은 건드리지 않고 BowRangeGame의 값만 읽는다.
    public class BowRangeBoardView : MiniBoardView
    {
        private static readonly Color Shadow = new Color(0.05f, 0.07f, 0.05f);
        private static readonly Color Good = new Color(0.5f, 1f, 0.6f);
        private static readonly Color Bad = new Color(1f, 0.4f, 0.35f);
        private static readonly Color Warm = new Color(1f, 0.85f, 0.45f);
        private static readonly Color Breath = new Color(0.55f, 0.8f, 1f);

        private BowRangeGame _game;

        protected override void OnBind(MiniGame game)
        {
            _game = (BowRangeGame)game;
        }

        protected override void OnUnbind()
        {
            _game = null;
        }

        protected override void Draw()
        {
            Canvas.Clear(Color.clear);
            DrawWind();
            DrawBars();
            DrawReticle();
        }

        // 낙차 눈금: 5m, 10m, 15m, 20m 거리에서 화살이 처지는 만큼 아래에 가로선을 긋는다(점 개수가 20m의 배수).
        // 바람 눈금: 지금 거리에서 바람 1단위가 미는 만큼씩 좌우에 세로선을 긋는다. 선이 하나 늘 때마다 바람이 1단위 더 센 것이다.
        private void DrawScaleTicks(Vector2 aim, Color color)
        {
            Color tick = new Color(color.r, color.g, color.b, 0.8f);
            for (int meters = 5; meters <= 20; meters += 5)
            {
                float y = aim.y + _game.DropAt(meters) * Unit;
                Canvas.Line(new Vector2(aim.x - 7f, y), new Vector2(aim.x + 7f, y), 2f, tick);
                for (int dot = 0; dot < meters / 5; dot++)
                {
                    Canvas.Circle(new Vector2(aim.x + 12f + dot * 5f, y), 1.5f, tick);
                }
            }

            float unit = _game.DriftAt(1f, _game.Distance) * Unit;
            for (int step = 1; step <= 3; step++)
            {
                float x = unit * step;
                Canvas.Line(new Vector2(aim.x - x, aim.y - 5f), new Vector2(aim.x - x, aim.y + 5f), 2f, tick);
                Canvas.Line(new Vector2(aim.x + x, aim.y - 5f), new Vector2(aim.x + x, aim.y + 5f), 2f, tick);
            }
        }

        // 바람 표시: 위쪽 왼편에 바람 방향과 세기를 화살표로 보여 준다.
        private void DrawWind()
        {
            Vector2 origin = ToCanvas(-8.2f, -5f);
            float wind = _game.Wind;
            Canvas.Circle(origin, 5f, new Color(1f, 1f, 1f, 0.8f));
            float length = Mathf.Clamp(wind, -3f, 3f) * 0.9f * Unit;
            Vector2 tip = origin + new Vector2(length, 0f);
            Canvas.Line(origin, tip, 4f, new Color(1f, 1f, 1f, 0.9f));
            float side = Mathf.Sign(length);
            Canvas.Line(tip, tip - new Vector2(side * 10f, 7f), 4f, new Color(1f, 1f, 1f, 0.9f));
            Canvas.Line(tip, tip - new Vector2(side * 10f, -7f), 4f, new Color(1f, 1f, 1f, 0.9f));
        }

        // 시위 당김 막대(다 당기면 초록, 오래 버티면 붉게)와 그 아래 숨 막대(참는 동안 줄고, 다하면 붉게)
        private void DrawBars()
        {
            if (_game.Phase == BowRangeGame.PhaseKind.Intro || _game.Phase == BowRangeGame.PhaseKind.Done)
            {
                return;
            }

            Rect bar = new Rect(ToCanvas(-3.5f, 0f).x, ToCanvas(0f, 5.0f).y, 7f * Unit, 16f);
            PixelShapes.Plate(Canvas, bar, 8f, new Color(0.08f, 0.08f, 0.1f), new Color(0.7f, 0.72f, 0.8f), Shadow);
            if (_game.Power > 0f)
            {
                bool tired = _game.DrawClock > 0.9f + 0.6f;
                Color fill = _game.Power >= 1f ? (tired ? Bad : Good) : Warm;
                Canvas.Fill(new Rect(bar.x + 4f, bar.y + 4f, (bar.width - 8f) * _game.Power, bar.height - 8f), fill);
            }

            Rect breath = new Rect(bar.x + bar.width * 0.25f, bar.yMax + 8f, bar.width * 0.5f, 10f);
            PixelShapes.Plate(Canvas, breath, 5f, new Color(0.08f, 0.08f, 0.1f), new Color(0.55f, 0.6f, 0.72f), Shadow);
            Color breathFill = _game.Gasping ? Bad : (_game.HoldingBreath ? Breath : new Color(0.5f, 0.7f, 0.9f, 0.7f));
            Canvas.Fill(new Rect(breath.x + 3f, breath.y + 3f, (breath.width - 6f) * _game.Breath, breath.height - 6f), breathFill);
        }

        // 조준점: 시위를 당길수록 작게 모이고, 떨릴 때는 떨리는 만큼 움직인다.
        private void DrawReticle()
        {
            if (_game.PointerX < -50f || _game.Phase == BowRangeGame.PhaseKind.Intro || _game.Phase == BowRangeGame.PhaseKind.Done)
            {
                return;
            }

            bool drawing = _game.Phase == BowRangeGame.PhaseKind.Draw;
            bool result = _game.Phase == BowRangeGame.PhaseKind.Result;
            float sx = drawing ? _game.SwayX : 0f;
            float sy = drawing ? _game.SwayY : 0f;
            Vector2 aim = ToCanvas(_game.PointerX + sx, _game.PointerY + sy);
            float size = Mathf.Lerp(1.3f, 0.45f, _game.Power) * Unit;
            Color color = drawing ? (_game.Power >= 1f ? Good : Warm) : new Color(1f, 1f, 1f, result ? 0.3f : 0.85f);
            Canvas.Ring(aim, size, color, 2.5f);
            Canvas.Line(aim + new Vector2(-size - 8f, 0f), aim + new Vector2(-size * 0.35f, 0f), 2.5f, color);
            Canvas.Line(aim + new Vector2(size + 8f, 0f), aim + new Vector2(size * 0.35f, 0f), 2.5f, color);
            Canvas.Line(aim + new Vector2(0f, -size - 8f), aim + new Vector2(0f, -size * 0.35f), 2.5f, color);
            Canvas.Line(aim + new Vector2(0f, size + 8f), aim + new Vector2(0f, size * 0.35f), 2.5f, color);
            Canvas.Circle(aim, 2.5f, color);
            if (!result)
            {
                DrawScaleTicks(aim, color);
            }
        }
    }
}
