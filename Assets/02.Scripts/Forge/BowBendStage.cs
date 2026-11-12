using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 활 몸체 휘기 작업대 위에 올라가는 줄인 3D 무대: 가운데(손잡이)를 바이스에 물린 달군 쇠막대와, 활 모양 형틀을 나타내는 푸른 유령 막대.
    // 막대는 양쪽 팔이 세 마디씩이고, 마디마다 휜 정도만큼 아래로 꺾인다. 누르는 마디 위에서 누름쇠가 내려온다.
    // 쇠는 뜨거울수록 붉게 달아오르고, 굳은 마디는 식은 쇠색이 된다. 모양은 임시 그레이박스라 코드로 만든다.
    // 진행과 입력은 BowBendStation이, 규칙은 BowBendGame이 맡는다.
    public class BowBendStage : MiniStage
    {
        private const float SegmentLength = 0.9f;
        private const float BarY = 1.3f;
        // 휜 정도 1일 때 마디가 꺾이는 각도
        private const float DegreesPerBend = 26f;

        private readonly Transform[] _pivots = new Transform[BowBendGame.SegmentCount];
        private readonly Transform[] _ghostPivots = new Transform[BowBendGame.SegmentCount];
        private readonly Renderer[] _bodies = new Renderer[BowBendGame.SegmentCount];
        private Transform _press;

        protected override float FocusHeight => 1.0f;
        protected override float ScreenShift => 0.05f;
        protected override float ViewSize => 4.6f;

        protected override void BuildProps()
        {
            Kit.Box(StageRoot, "Vise", new Vector3(0f, BarY - 0.35f, 0f), new Vector3(0.9f, 0.7f, 1.1f), new Color(0.22f, 0.22f, 0.26f));
            Kit.Box(StageRoot, "ViseBase", new Vector3(0f, 0.55f, 0f), new Vector3(1.4f, 0.5f, 1.4f), new Color(0.3f, 0.3f, 0.34f));
            Kit.Box(StageRoot, "Furnace", new Vector3(-3.1f, 0.65f, 0.9f), new Vector3(0.9f, 0.7f, 0.8f), new Color(0.35f, 0.15f, 0.1f));

            // 형틀(유령): 목표 휘기로 꺾인 푸른 가는 막대. 실제 막대 바로 뒤에 겹쳐 보인다.
            BuildChain("Ghost", 0.34f, new Color(0.35f, 0.8f, 1f), 0.5f, _ghostPivots, null);
            BuildChain("Bar", 0.16f, Color.gray, 0f, _pivots, _bodies);

            _press = Kit.Box(StageRoot, "Press", Vector3.zero, new Vector3(0.5f, 0.5f, 0.5f), new Color(0.3f, 0.3f, 0.34f)).transform;
            _press.gameObject.SetActive(false);
        }

        // 가운데에서 양쪽으로 마디가 이어진 사슬. 위 마디의 회전이 아래 마디로 이어지도록 계층으로 만든다.
        private void BuildChain(string name, float thickness, Color color, float depth, Transform[] pivots, Renderer[] bodies)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Transform parent = StageRoot;
                for (int step = 0; step < 3; step++)
                {
                    int index = side < 0 ? 2 - step : 3 + step;
                    var pivot = new GameObject(name + index).transform;
                    pivot.SetParent(parent, false);
                    pivot.localPosition = step == 0 ? new Vector3(0f, BarY, depth) : new Vector3(side * SegmentLength, 0f, 0f);
                    float width = thickness * (depth > 0f ? 0.6f : 1.5f);
                    GameObject body = Kit.Box(pivot, "Body", new Vector3(side * SegmentLength * 0.5f, 0f, 0f), new Vector3(SegmentLength * 0.96f, thickness, width), color);
                    pivots[index] = pivot;
                    if (bodies != null)
                    {
                        bodies[index] = body.GetComponent<Renderer>();
                    }

                    parent = pivot;
                }
            }
        }

        public override void End()
        {
            _press.gameObject.SetActive(false);
            Sparks(_press.position, 0f);
            base.End();
        }

        // 매 프레임 게임 값을 3D 장면에 옮긴다.
        public void Show(BowBendGame game, float deltaTime)
        {
            for (int i = 0; i < BowBendGame.SegmentCount; i++)
            {
                _pivots[i].localRotation = BendRotation(i, game.BendOf(i));
                _ghostPivots[i].localRotation = BendRotation(i, game.TargetOf(i));
                PaintSegment(game, i);
            }

            UpdatePress(game);
        }

        // 오른쪽 팔은 시계 방향(아래로), 왼쪽 팔은 반시계 방향으로 꺾여 둘 다 활 끝이 아래로 휜다.
        private static Quaternion BendRotation(int segment, float bend)
        {
            float sign = segment >= 3 ? -1f : 1f;
            return Quaternion.Euler(0f, 0f, sign * bend * DegreesPerBend);
        }

        private void PaintSegment(BowBendGame game, int segment)
        {
            if (game.IsLocked(segment))
            {
                bool good = Mathf.Abs(game.BendOf(segment) - game.TargetOf(segment)) <= game.Tolerance;
                Kit.Paint(_bodies[segment], good ? new Color(0.62f, 0.72f, 0.66f) : new Color(0.6f, 0.45f, 0.42f), Color.black);
                return;
            }

            Kit.PaintHot(_bodies[segment], Mathf.Clamp01(game.HeatOf(segment) * (game.Heating == segment ? 1.15f : 1f)));
        }

        // 누르는 마디(없으면 가리키는 마디) 위에서 누름쇠가 내려온다.
        private void UpdatePress(BowBendGame game)
        {
            // 달구는 중에는 그 마디에 불꽃이 일고, 누르는 중에는 누름쇠가 내려온다.
            if (game.Heating >= 0)
            {
                _press.gameObject.SetActive(false);
                Sparks(_bodies[game.Heating].transform.position, 55f);
                return;
            }

            int segment = game.Active >= 0 ? game.Active : game.Hovered;
            bool show = game.Phase == BowBendGame.PhaseKind.Bend && segment >= 0 && !game.IsLocked(segment);
            _press.gameObject.SetActive(show);
            if (!show)
            {
                Sparks(_press.position, 0f);
                return;
            }

            Transform body = _bodies[segment].transform;
            float lift = game.Active >= 0 ? 0.32f : 0.8f;
            _press.position = body.position + Vector3.up * (lift * Scale);
            Sparks(body.position, game.Active >= 0 ? 30f * game.HeatOf(segment) : 0f);
        }
    }
}
