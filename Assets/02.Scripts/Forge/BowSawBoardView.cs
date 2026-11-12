using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 활 깎기의 마우스 안내 판. 3D 장면(BowSawStage)이 뒤에 보이도록 투명하게 두고, 마우스를 움직일 범위만 그린다.
    // 마우스는 화면 어디서 시작해도 된다. 왼쪽 레일에서 손잡이가 처음 누른 높이에서 벗어난 만큼 오르내린다. 위로 밀고 아래로 당기는 것이 톱질 한 번이다.
    // 규칙은 건드리지 않고 BowSawGame의 값만 읽는다.
    public class BowSawBoardView : MiniBoardView
    {
        private const float RailX = -8.4f;
        private const float RailHalf = 3.4f;
        private const float StrokeHalf = 1.5f;

        private static readonly Color Rail = new Color(1f, 1f, 1f, 0.22f);
        private static readonly Color Mark = new Color(1f, 0.9f, 0.55f, 0.75f);
        private static readonly Color Good = new Color(0.5f, 1f, 0.6f);
        private static readonly Color Bad = new Color(1f, 0.4f, 0.35f);

        private BowSawGame _game;

        protected override bool ClampToBoard => false;

        protected override void OnBind(MiniGame game)
        {
            _game = (BowSawGame)game;
        }

        protected override void OnUnbind()
        {
            _game = null;
        }

        protected override void Draw()
        {
            Canvas.Clear(Color.clear);
            Vector2 top = ToCanvas(RailX, -RailHalf);
            Vector2 bottom = ToCanvas(RailX, RailHalf);
            Canvas.Line(top, bottom, 6f, Rail);

            // 한 번의 톱질로 인정받는 길이를 보여 주는 위아래 표시
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Vector2 mark = ToCanvas(RailX, sign * StrokeHalf);
                Canvas.Line(mark + new Vector2(-14f, 0f), mark + new Vector2(14f, 0f), 3f, Mark);
            }

            DrawArrow(ToCanvas(RailX, -RailHalf - 0.5f), -1f);
            DrawArrow(ToCanvas(RailX, RailHalf + 0.5f), 1f);

            if (_game.PointerX > -50f)
            {
                float y = Mathf.Clamp(_game.RelativeY, -RailHalf, RailHalf);
                Color knob = _game.BindLeft > 0f ? Bad : (_game.Holding ? Good : new Color(0.9f, 0.92f, 1f));
                Vector2 center = ToCanvas(RailX, y);
                Canvas.Circle(center, 13f, new Color(0f, 0f, 0f, 0.35f));
                Canvas.Circle(center, 10f, knob);
            }
        }

        private void DrawArrow(Vector2 tip, float direction)
        {
            Vector2 back = tip + new Vector2(0f, -direction * 12f);
            Canvas.Line(tip, back + new Vector2(-9f, 0f), 3f, Mark);
            Canvas.Line(tip, back + new Vector2(9f, 0f), 3f, Mark);
        }
    }
}
