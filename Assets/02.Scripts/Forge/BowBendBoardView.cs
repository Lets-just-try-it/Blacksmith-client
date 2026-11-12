using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 활 몸체 휘기의 마디별 계기판. 3D 장면(BowBendStage)이 뒤에 보이도록 투명하게 두고, 화면 위쪽에 마디마다 세로 눈금을 그린다.
    // 막대 눈금은 지금 휜 정도, 노란 눈금선은 형틀 목표, 초록 띠는 허용 폭이다. 눌러서 채우고, 놓으면 조금 되돌아온다.
    // 마우스는 마디의 가로 칸 어디에 있어도 그 마디를 가리킨다. 규칙은 건드리지 않고 BowBendGame의 값만 읽는다.
    public class BowBendBoardView : MiniBoardView
    {
        private const float GaugeTop = -5.6f;
        private const float GaugeHeight = 3.4f;
        private const float GaugeWidth = 0.9f;

        private static readonly Color Back = new Color(0f, 0f, 0f, 0.28f);
        private static readonly Color Hover = new Color(1f, 1f, 1f, 0.12f);
        private static readonly Color Fill = new Color(1f, 0.6f, 0.25f);
        private static readonly Color Cool = new Color(0.55f, 0.7f, 0.95f);
        private static readonly Color Good = new Color(0.5f, 1f, 0.6f);
        private static readonly Color Bad = new Color(1f, 0.4f, 0.35f);
        private static readonly Color Mark = new Color(1f, 0.9f, 0.4f);

        private BowBendGame _game;

        // 마디를 누른 채 끌어 내리므로 마우스가 판 밖으로 나가도 계속 읽는다.
        protected override bool ClampToBoard => false;

        protected override void OnBind(MiniGame game)
        {
            _game = (BowBendGame)game;
        }

        protected override void OnUnbind()
        {
            _game = null;
        }

        protected override void Draw()
        {
            Canvas.Clear(Color.clear);
            float half = BowBendGame.SegmentCount * BowBendGame.SegmentWidth * 0.5f;
            for (int i = 0; i < BowBendGame.SegmentCount; i++)
            {
                float centerX = -half + (i + 0.5f) * BowBendGame.SegmentWidth;
                if (_game.Hovered == i)
                {
                    Vector2 a = ToCanvas(centerX - BowBendGame.SegmentWidth * 0.5f, -6f);
                    Vector2 b = ToCanvas(centerX + BowBendGame.SegmentWidth * 0.5f, 6f);
                    Canvas.Fill(new Rect(a.x, a.y, b.x - a.x, b.y - a.y), Hover);
                }

                DrawGauge(i, centerX);
            }
        }

        private void DrawGauge(int segment, float centerX)
        {
            float max = _game.MaxBend;
            Vector2 topLeft = ToCanvas(centerX - GaugeWidth * 0.5f, GaugeTop);
            float width = GaugeWidth * Unit;
            float height = GaugeHeight * Unit;
            Canvas.Fill(new Rect(topLeft.x, topLeft.y, width, height), Back);

            // 허용 폭과 목표 눈금
            float target = _game.TargetOf(segment);
            float lowRatio = Mathf.Clamp01((target - _game.Tolerance) / max);
            float highRatio = Mathf.Clamp01((target + _game.Tolerance) / max);
            Canvas.Fill(new Rect(topLeft.x, topLeft.y + height * (1f - highRatio), width, height * (highRatio - lowRatio)), new Color(0.4f, 0.9f, 0.55f, 0.45f));

            float ratio = Mathf.Clamp01(_game.BendOf(segment) / max);
            Color fill = _game.IsLocked(segment) ? (Mathf.Abs(_game.BendOf(segment) - target) <= _game.Tolerance ? Good : Bad) : Color.Lerp(Cool, Fill, _game.HeatOf(segment));
            Canvas.Fill(new Rect(topLeft.x + 4f, topLeft.y + height * (1f - ratio), width - 8f, height * ratio), fill);

            // 열 막대: 게이지 아래의 가는 막대가 마디의 열이다(식으면 푸르고 짧아진다). E를 누르면 차오른다.
            float heat = _game.HeatOf(segment);
            Rect heatBar = new Rect(topLeft.x, topLeft.y + height + 6f, width, 8f);
            Canvas.Fill(heatBar, Back);
            Canvas.Fill(new Rect(heatBar.x + 1f, heatBar.y + 1f, (heatBar.width - 2f) * heat, heatBar.height - 2f), _game.Heating == segment ? Mark : Color.Lerp(Cool, Fill, heat));

            float markY = topLeft.y + height * (1f - Mathf.Clamp01(target / max));
            Canvas.Line(new Vector2(topLeft.x - 6f, markY), new Vector2(topLeft.x + width + 6f, markY), 3f, Mark);
            PixelShapes.Outline(Canvas, new Rect(topLeft.x, topLeft.y, width, height), new Color(1f, 1f, 1f, 0.5f), 2f);
        }
    }
}
