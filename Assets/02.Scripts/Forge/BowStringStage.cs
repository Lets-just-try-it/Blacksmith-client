using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 시위 걸기 작업대 위에 올라가는 줄인 3D 무대: 작업대에 눕혀 놓은 활과 힘줄 시위.
    // 카메라는 활 끝(몸대)에 바짝 다가가 힘줄이 감기는 모습을 크게 보여 준다.
    // 감기: 힘줄 타래에서 뻗은 실이 활 끝을 빙글 돌면서(마우스 원 그리기) 몸대에 한 바퀴씩 촘촘히 감긴다. 도는 각도만큼 감긴 마디가 늘어난다.
    // 걸기: 마우스를 아래로 끌면 그쪽 활채가 휘고, 알맞게 휜 채 옆으로 끌면 시위 고리가 활채를 타고 활 끝의 홈으로 미끄러져 간다. 처음은 왼쪽, 다음은 오른쪽 끝이다.
    // 너무 휘면 활채가 붉어지고 고리가 뒤로 밀린다. 고리는 알맞게 휜 동안 초록으로 빛난다.
    // 모양은 임시 그레이박스라 코드로 만든다. 진행과 입력은 BowStringStation이, 규칙은 BowStringGame이 맡는다.
    public class BowStringStage : MiniStage
    {
        private const float BowY = 1.35f;
        private const float LimbLength = 2.3f;
        private const float LimbThick = 0.2f;
        private const float LimbDepth = 0.26f;
        private const float BandThick = 0.045f;
        private const float BandWidth = 0.09f;
        private const float WrapHeight = 0.12f;
        private const float WrapWidth = 0.16f;
        private const float RingSpacing = 0.1f;
        private const int MaxRings = 6;
        private const int SegmentsPerRing = 4;
        private const float CameraPitch = 38f;

        private static readonly Color Sinew = new Color(0.9f, 0.82f, 0.62f);
        private static readonly Color Wood = new Color(0.55f, 0.36f, 0.2f);
        private static readonly Color Metal = new Color(0.7f, 0.7f, 0.76f);

        private readonly Transform[] _pivots = new Transform[2];
        private readonly Transform[,] _coil = new Transform[MaxRings, SegmentsPerRing];
        private Transform _string;
        private Transform _thread;
        private Transform _loop;
        private Transform _threadEnd;
        private Transform _spool;
        private Renderer _loopBody;
        private readonly Renderer[] _limbBodies = new Renderer[2];
        private int _focusSide = int.MinValue;

        protected override float ViewSize => 3.2f;

        protected override void BuildProps()
        {
            Kit.Box(StageRoot, "Bench", new Vector3(0f, 0.75f, 0f), new Vector3(6f, 0.3f, 1.8f), new Color(0.3f, 0.2f, 0.12f));
            Kit.Box(StageRoot, "Rest", new Vector3(0f, 1.0f, 0f), new Vector3(0.7f, 0.3f, 0.9f), new Color(0.22f, 0.15f, 0.09f));
            _spool = Kit.Cylinder(StageRoot, "Spool", new Vector3(2.5f, 1.05f, 0.75f), new Vector3(0.5f, 0.2f, 0.5f), Sinew).transform;
            Kit.Cylinder(StageRoot, "SpoolCore", new Vector3(2.5f, 1.05f, 0.75f), new Vector3(0.2f, 0.28f, 0.2f), new Color(0.5f, 0.35f, 0.2f));

            // 활: 손잡이는 가운데 고정이고 양쪽 활채(몸대)가 각자의 받침점에서 위로 휜다.
            Kit.Box(StageRoot, "Grip", new Vector3(0f, BowY, 0f), new Vector3(0.5f, LimbThick, 0.28f), new Color(0.3f, 0.2f, 0.12f));
            for (int side = -1; side <= 1; side += 2)
            {
                var pivot = new GameObject(side < 0 ? "LimbLeft" : "LimbRight").transform;
                pivot.SetParent(StageRoot, false);
                pivot.localPosition = new Vector3(side * 0.25f, BowY, 0f);
                _pivots[side < 0 ? 0 : 1] = pivot;
                _limbBodies[side < 0 ? 0 : 1] = Kit.Box(pivot, "Limb", new Vector3(side * LimbLength * 0.5f, 0f, 0f), new Vector3(LimbLength, LimbThick, 0.26f), Wood).GetComponent<Renderer>();
                Kit.Box(pivot, "Tip", new Vector3(side * LimbLength, 0.04f, 0f), new Vector3(0.14f, 0.3f, 0.3f), Metal);
            }

            _string = Kit.Box(StageRoot, "String", Vector3.zero, Vector3.one, Sinew).transform;
            _thread = Kit.Box(StageRoot, "Thread", Vector3.zero, Vector3.one, Sinew).transform;
            _loop = Kit.Box(StageRoot, "Loop", Vector3.zero, new Vector3(0.14f, 0.4f, 0.4f), Sinew).transform;
            _loopBody = _loop.GetComponent<Renderer>();
            for (int ring = 0; ring < MaxRings; ring++)
            {
                for (int segment = 0; segment < SegmentsPerRing; segment++)
                {
                    _coil[ring, segment] = Kit.Box(StageRoot, "Wrap" + ring + "_" + segment, Vector3.zero, new Vector3(0.09f, 0.07f, 0.2f), new Color(0.95f, 0.88f, 0.68f)).transform;
                }
            }

            _threadEnd = Kit.Box(StageRoot, "ThreadEnd", Vector3.zero, new Vector3(0.12f, 0.085f, 0.085f), new Color(1f, 0.95f, 0.75f)).transform;
        }

        // 활 끝(몸대) 쪽으로 바짝 다가간다. 감기는 오른쪽 끝, 걸기는 처음 왼쪽 끝과 다음 오른쪽 끝을 비춘다.
        private void FocusSide(int side)
        {
            if (CameraFocus == null)
            {
                return;
            }

            _focusSide = side;
            float pitch = CameraPitch * Mathf.Deg2Rad;
            Vector3 toCamera = StageRoot.TransformDirection(new Vector3(0f, Mathf.Sin(pitch), -Mathf.Cos(pitch)));
            Vector3 up = StageRoot.TransformDirection(new Vector3(0f, Mathf.Cos(pitch), Mathf.Sin(pitch)));
            Vector3 target = StageRoot.TransformPoint(new Vector3(side * 1.45f, BowY, 0f)) + up * (2f * ViewSize * Scale * 0.06f);
            float distance = ViewSize * Scale / Mathf.Tan(25f * Mathf.Deg2Rad);
            CameraFocus.Focus(target + toCamera * distance, target);
        }

        protected override void FocusCamera()
        {
            FocusSide(1);
        }

        // 매 프레임 게임 값을 3D 장면에 옮긴다.
        public void Show(BowStringGame game, float deltaTime)
        {
            bool flexing = game.Phase == BowStringGame.PhaseKind.Flex;
            int activeSide = game.FlexIndex == 0 ? 0 : 1;
            int wantedSide = flexing && game.FlexIndex == 0 ? -1 : 1;
            if (wantedSide != _focusSide)
            {
                FocusSide(wantedSide);
            }

            float flex = flexing ? game.Flex : 0f;
            for (int i = 0; i < _pivots.Length; i++)
            {
                float sign = i == 0 ? -1f : 1f;
                float angle = (i == activeSide ? flex : 0f) * 20f + (game.FlexIndex > i ? 6f : 0f);
                _pivots[i].localRotation = Quaternion.Euler(0f, 0f, sign * -angle);
            }

            Vector3 left = _pivots[0].TransformPoint(new Vector3(-LimbLength, 0.04f, 0f));
            Vector3 right = _pivots[1].TransformPoint(new Vector3(LimbLength, 0.04f, 0f));
            bool taut = game.Phase == BowStringGame.PhaseKind.Done || game.FlexIndex >= BowStringGame.FlexCount;
            PlaceBar(_string, left, right, taut ? 0f : 0.45f, 0.06f);

            UpdateCoils(game, right);
            UpdateLoop(game, right, activeSide, flexing);
        }

        // 두 점(월드 좌표) 사이에 막대(상자)를 늘여 놓는다. sag(무대 안 길이)만큼 가운데가 처진다.
        private void PlaceBar(Transform bar, Vector3 from, Vector3 to, float sag, float thickness)
        {
            Vector3 delta = to - from;
            bar.position = (from + to) * 0.5f + Vector3.down * (sag * Scale);
            bar.rotation = delta.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(delta, Vector3.up) : Quaternion.identity;
            bar.localScale = new Vector3(thickness, thickness, delta.magnitude / Scale + sag * 0.6f);
        }

        // 몸대에 감긴 힘줄(바퀴마다 둘레를 도는 마디들)과, 지금 돌고 있는 힘줄 끝, 타래에서 힘줄 끝까지 팽팽한 실
        private void UpdateCoils(BowStringGame game, Vector3 rightTipWorld)
        {
            // 무대 뿌리가 돌아가 있거나 줄여져 있어도 맞도록 무대 안 좌표로 계산한다.
            Vector3 rightTip = StageRoot.InverseTransformPoint(rightTipWorld);
            Vector3 limbAxis = Vector3.left;
            int doneRings = Mathf.Min(MaxRings, game.WrapsDone);
            bool wrapping = game.Phase == BowStringGame.PhaseKind.Wrap;
            int partial = wrapping ? Mathf.FloorToInt(game.Turn * SegmentsPerRing + 0.0001f) : 0;
            for (int ring = 0; ring < MaxRings; ring++)
            {
                bool full = ring < doneRings;
                bool current = wrapping && ring == doneRings;
                for (int segment = 0; segment < SegmentsPerRing; segment++)
                {
                    bool visible = full || (current && segment < partial);
                    Transform piece = _coil[ring, segment];
                    piece.gameObject.SetActive(visible);
                    if (!visible)
                    {
                        continue;
                    }

                    float angle = segment * Mathf.PI * 2f / SegmentsPerRing;
                    // 몸대 단면을 감싸는 사각 띠: 위, 앞, 아래, 뒤 순서로 한 변씩 채워진다.
                    Vector3 offset;
                    Vector3 size;
                    switch (segment)
                    {
                        case 0:
                            offset = new Vector3(0f, LimbThick * 0.5f + BandThick * 0.5f, 0f);
                            size = new Vector3(BandWidth, BandThick, LimbDepth + BandThick * 2f);
                            break;
                        case 1:
                            offset = new Vector3(0f, 0f, -(LimbDepth * 0.5f + BandThick * 0.5f));
                            size = new Vector3(BandWidth, LimbThick + BandThick * 2f, BandThick);
                            break;
                        case 2:
                            offset = new Vector3(0f, -(LimbThick * 0.5f + BandThick * 0.5f), 0f);
                            size = new Vector3(BandWidth, BandThick, LimbDepth + BandThick * 2f);
                            break;
                        default:
                            offset = new Vector3(0f, 0f, LimbDepth * 0.5f + BandThick * 0.5f);
                            size = new Vector3(BandWidth, LimbThick + BandThick * 2f, BandThick);
                            break;
                    }

                    piece.localPosition = rightTip + limbAxis * (0.26f + RingSpacing * ring) + offset;
                    piece.localRotation = Quaternion.identity;
                    piece.localScale = size;
                }
            }

            _threadEnd.gameObject.SetActive(wrapping);
            _thread.gameObject.SetActive(wrapping);
            if (!wrapping)
            {
                return;
            }

            float spin = game.Angle;
            Vector3 hub = rightTip + limbAxis * (0.26f + RingSpacing * Mathf.Min(MaxRings - 1, doneRings));
            float extra = game.Holding ? 0.02f : 0.12f;
            Vector3 end = hub + new Vector3(0f, Mathf.Sin(spin) * (WrapHeight + extra), Mathf.Cos(spin) * (WrapWidth + extra));
            _threadEnd.localPosition = end;
            _threadEnd.localRotation = Quaternion.identity;
            PlaceBar(_thread, _spool.position + StageRoot.TransformVector(Vector3.up * 0.3f), StageRoot.TransformPoint(end), game.Holding ? 0.02f : 0.25f, 0.035f);
        }

        // 걸기 단계의 시위 고리: 활채를 따라 활 가운데쯤에서 활 끝의 홈으로 미끄러진다. 알맞게 휜 동안 초록으로 빛나고, 너무 휘면 활채가 붉어진다.
        private void UpdateLoop(BowStringGame game, Vector3 right, int activeSide, bool flexing)
        {
            _loop.gameObject.SetActive(flexing || game.Phase == BowStringGame.PhaseKind.Done);
            for (int i = 0; i < _limbBodies.Length; i++)
            {
                bool over = flexing && i == activeSide && game.OverFlexed;
                Kit.Paint(_limbBodies[i], over ? new Color(0.85f, 0.3f, 0.25f) : Wood, over ? new Color(0.7f, 0.1f, 0.05f) : Color.black);
            }

            if (!flexing)
            {
                _loop.position = right + Vector3.left * 0.12f;
                return;
            }

            // 고리는 활채 위(위쪽 면)를 가운데쯤(0.9)에서 끝 홈(LimbLength)까지 간다.
            float sign = activeSide == 0 ? -1f : 1f;
            float along = Mathf.Lerp(0.9f, LimbLength - 0.04f, game.Slide);
            _loop.position = _pivots[activeSide].TransformPoint(new Vector3(sign * along, LimbThick * 0.5f + 0.14f, 0f));
            _loop.rotation = _pivots[activeSide].rotation;
            Kit.Paint(_loopBody, game.InWindow ? new Color(0.6f, 1f, 0.7f) : Sinew, game.InWindow ? new Color(0.2f, 0.8f, 0.3f) : Color.black);
        }
    }
}
