using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 연마 작업대 위에 올라가는 줄인 무대: 빙글빙글 도는 숫돌 바퀴에 칼날의 날 끝을 대고, 두 손이 눌러 간다.
    // 확대한 숫돌 표면 그림은 PolishLensView가 화면에 그리고, 이 무대는 그 아래쪽에 보이는 3D 장면을 맡는다.
    // 숫돌 바퀴는 제자리에서 돌기만 하고, 칼날이 옆으로 미끄러져 마우스가 가리킨 자리가 바퀴 밑에 오게 한다.
    // 날 끝에는 칸마다 작은 조각이 있어 선 정도에 따라 빛나고, 칼날은 열이 오르면 달아오른다.
    // 모양은 임시 그레이박스라 코드로 만든다. 진행과 입력은 PolishStation이, 규칙은 PolishGame이 맡는다.
    public class PolishStage : MonoBehaviour
    {
        private const float HalfFov = 25f;
        // 확대 화면과 아래 패널이 위쪽을 차지하므로 무대를 화면 아래쪽으로 내려 놓는다.
        private const float ViewSize = 4.8f;
        private const float ScreenShift = 0.22f;
        // 칼날 끝이 놓인 높이와, 바퀴의 크기
        private const float EdgeY = 0.62f;
        private const float WheelRadius = 0.9f;
        private const float WheelLength = 1.0f;
        private const float BladeLength = 5f;
        // 카메라는 앞(-z)에서 이 각도로 내려다본다.
        private const float CameraPitch = 35f;

        [SerializeField] private Transform _stageRoot;
        [SerializeField] private Transform _fxRoot;
        [SerializeField] private Material _bodyMaterial;
        [SerializeField] private Material _particleMaterial;
        [SerializeField] private AudioSource _sfx;
        [SerializeField] private ForgeNoiseSynth _grindSound;
        [SerializeField] private Light _glow;

        private ForgeGreybox _kit;
        private PolishFeedback _feedback;
        private Transform _wheelGroup;
        private Transform _wheelSpin;
        private Renderer _wheel;
        private Transform _blade;
        private Renderer _bladeBody;
        private Transform _bucket;
        private Transform _water;
        private readonly Renderer[] _edge = new Renderer[PolishGame.SegmentCount];
        private readonly int[] _edgeKeys = new int[PolishGame.SegmentCount];
        private Transform _handA;
        private Transform _handB;
        private PolishGame _game;
        private PolishLensView _lens;
        private ForgeCameraFocus _camera;
        private float _slide;
        private float _spin;
        private float _press;
        private float _pulse;
        private int _lastSegment = -1;

        private float Scale => _stageRoot.localScale.x;

        private void Awake()
        {
            _kit = new ForgeGreybox(_bodyMaterial, _particleMaterial);
            BuildBase();
            BuildWheel();
            BuildBlade();
            BuildBucket();
            _feedback = new PolishFeedback(_kit, _stageRoot, _fxRoot, Scale, _sfx, _grindSound, _glow);
            _glow.intensity = 0f;
        }

        private void BuildBase()
        {
            _kit.Box(_stageRoot, "Base", new Vector3(0f, 0.2f, 0.1f), new Vector3(7.4f, 0.3f, 3.4f), new Color(0.3f, 0.19f, 0.11f));
        }

        // 날 끝 위에서 도는 숫돌 바퀴. 축이 가로이고 둘레가 앞을 향해 세로로 돌아가며, 제자리에서 돈다.
        private void BuildWheel()
        {
            var group = new GameObject("WheelGroup");
            group.transform.SetParent(_stageRoot, false);
            group.transform.localPosition = new Vector3(0f, EdgeY + WheelRadius, 0f);
            _wheelGroup = group.transform;

            var spin = new GameObject("WheelSpin");
            spin.transform.SetParent(_wheelGroup, false);
            _wheelSpin = spin.transform;

            GameObject disc = _kit.Cylinder(_wheelSpin, "Wheel", Vector3.zero, new Vector3(WheelRadius * 2f, WheelLength * 0.5f, WheelRadius * 2f), new Color(0.46f, 0.42f, 0.4f));
            disc.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            _wheel = disc.GetComponent<Renderer>();
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f;
                GameObject mark = _kit.Box(_wheelSpin, "Mark" + i, Quaternion.Euler(angle, 0f, 0f) * new Vector3(0f, WheelRadius, 0f), new Vector3(WheelLength + 0.04f, 0.07f, 0.2f), new Color(0.2f, 0.18f, 0.17f));
                mark.transform.localRotation = Quaternion.Euler(angle, 0f, 0f);
            }

            // 양옆의 쇠 받침판과 굵은 축
            for (int side = -1; side <= 1; side += 2)
            {
                _kit.Cylinder(_wheelGroup, "Flange" + side, new Vector3(side * (WheelLength * 0.5f + 0.06f), 0f, 0f), new Vector3(WheelRadius * 0.9f, 0.06f, WheelRadius * 0.9f), new Color(0.25f, 0.25f, 0.28f)).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            _kit.Cylinder(_wheelGroup, "Axle", Vector3.zero, new Vector3(0.2f, WheelLength * 0.5f + 0.55f, 0.2f), new Color(0.2f, 0.2f, 0.23f)).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        // 옆으로 미끄러지는 칼날: 날 끝은 z=0에 있고 몸통은 앞(-z)으로 뻗는다. 날 끝에는 칸마다 조각이 있다.
        private void BuildBlade()
        {
            var group = new GameObject("BladeGroup");
            group.transform.SetParent(_stageRoot, false);
            group.transform.localPosition = new Vector3(0f, EdgeY, 0f);
            _blade = group.transform;

            Color steel = new Color(0.62f, 0.65f, 0.7f);
            _bladeBody = _kit.Box(_blade, "Blade", new Vector3(0f, 0f, -0.75f), new Vector3(BladeLength, 0.07f, 1.5f), steel).GetComponent<Renderer>();
            _kit.Box(_blade, "Spine", new Vector3(0f, 0.04f, -1.45f), new Vector3(BladeLength, 0.12f, 0.12f), steel * 0.75f);
            _kit.Box(_blade, "Handle", new Vector3(-BladeLength * 0.5f - 0.7f, 0.03f, -0.75f), new Vector3(1.4f, 0.16f, 0.4f), new Color(0.25f, 0.15f, 0.09f));

            float cell = BladeLength / PolishGame.SegmentCount;
            for (int i = 0; i < PolishGame.SegmentCount; i++)
            {
                float x = -BladeLength * 0.5f + (i + 0.5f) * cell;
                _edge[i] = _kit.Box(_blade, "Edge" + i, new Vector3(x, -0.01f, 0.02f), new Vector3(cell * 0.9f, 0.06f, 0.08f), Color.gray).GetComponent<Renderer>();
            }

            // 칼날을 누르는 두 손
            Color skin = new Color(0.86f, 0.64f, 0.5f);
            _handA = _kit.Box(_blade, "HandA", new Vector3(-1.1f, 0.22f, -1f), new Vector3(0.55f, 0.22f, 0.4f), skin).transform;
            _handB = _kit.Box(_blade, "HandB", new Vector3(1.1f, 0.22f, -1f), new Vector3(0.55f, 0.22f, 0.4f), skin).transform;
        }

        // 식히는 물통: 칼날이 미끄러지는 범위 밖, 바퀴 뒤쪽에 둔다.
        private void BuildBucket()
        {
            _bucket = _kit.Cylinder(_stageRoot, "Bucket", new Vector3(2.9f, 0.65f, 1.2f), new Vector3(1.0f, 0.4f, 1.0f), new Color(0.3f, 0.26f, 0.22f)).transform;
            _water = _kit.Cylinder(_stageRoot, "Water", new Vector3(2.9f, 1.04f, 1.2f), new Vector3(0.86f, 0.02f, 0.86f), new Color(0.3f, 0.6f, 0.85f)).transform;
        }

        public void SetVisible(bool visible)
        {
            _stageRoot.gameObject.SetActive(visible);
        }

        public void Begin(PolishGame game, ForgeCameraFocus camera, PolishView view, PolishLensView lens)
        {
            _game = game;
            _camera = camera;
            _lens = lens;
            _slide = 0f;
            _press = 0f;
            _lastSegment = -1;
            for (int i = 0; i < _edgeKeys.Length; i++)
            {
                _edgeKeys[i] = -1;
            }
            _feedback.Begin(camera, view, lens);
            SetVisible(true);
            FocusCamera();
        }

        public void End()
        {
            _feedback.End();
            _game = null;
            _camera = null;
            _lens = null;
            SetVisible(false);
        }

        public void Tick(float deltaTime)
        {
            _pulse += deltaTime;

            // 칼날은 마우스가 가리킨 자리가 바퀴 밑에 오도록 미끄러진다.
            float wantSlide = _game.PointerX > -50f ? Mathf.Clamp(_game.PointerX, -1f, 1f) * BladeLength * 0.5f : 0f;
            _slide = Mathf.Lerp(_slide, wantSlide, 1f - Mathf.Exp(-14f * deltaTime));

            int segment = _game.Grinding ? PolishGame.SegmentAt(_game.PointerX) : -1;
            if (segment >= 0 && segment != _lastSegment)
            {
                _feedback.PlayStep(Mathf.Clamp01(_game.Sharp[segment] / _game.SegmentGoal));
            }
            _lastSegment = segment;

            float wantPress = _game.Grinding ? 1f : 0f;
            _press = Mathf.Lerp(_press, wantPress, 1f - Mathf.Exp(-18f * deltaTime));
            float shake = _game.Grinding ? (0.01f + 0.02f * (1f - _game.Quality)) * Mathf.Sin(_pulse * 55f) : 0f;
            _wheelGroup.localPosition = new Vector3(0f, EdgeY + WheelRadius - 0.02f * _press + shake, 0f);
            _blade.localPosition = new Vector3(-_slide, EdgeY, 0f);

            // 닿으면 바퀴가 조금 느려진다.
            _spin += (_game.Grinding ? 320f : 520f) * deltaTime;
            _wheelSpin.localRotation = Quaternion.Euler(_spin, 0f, 0f);
            _kit.Paint(_wheel, Color.Lerp(new Color(0.42f, 0.38f, 0.36f), new Color(0.6f, 0.3f, 0.2f), _game.Heat * 0.5f), ForgeGreybox.HeatColor(_game.Heat) * (_game.Heat * _game.Heat * 0.6f));

            // 칼날 각도는 마우스 높이를 따른다. 위로 올리면 몸통이 올라간다.
            float tilt = 15f - Mathf.Clamp((_game.PointerX > -50f ? _game.PointerY : _game.TargetAngle) * 80f, -30f, 30f);
            _blade.localRotation = Quaternion.Euler(tilt, 0f, 0f);
            _kit.PaintHot(_bladeBody, _game.Heat * 0.85f, 0.6f);
            UpdateEdge();

            float handY = 0.3f - 0.08f * _press;
            _handA.localPosition = new Vector3(-1.1f, handY, -1f);
            _handB.localPosition = new Vector3(1.1f, handY, -1f);

            float ripple = _game.Dipping ? 1f + 0.05f * Mathf.Sin(_pulse * 30f) : 1f;
            _water.localScale = new Vector3(0.86f * ripple, 0.02f, 0.86f * ripple);

            _feedback.Tick(_game, ContactPoint(), _stageRoot.TransformPoint(_bucket.localPosition + Vector3.up * 0.45f));
        }

        // 날 끝 조각을 선 정도에 따라 칠한다. 바뀐 조각만 다시 칠한다.
        private void UpdateEdge()
        {
            for (int i = 0; i < _edge.Length; i++)
            {
                float ratio = Mathf.Clamp01(_game.Sharp[i] / _game.SegmentGoal);
                bool done = _game.Sharp[i] >= _game.SegmentGoal;
                int key = Mathf.RoundToInt(ratio * 20f) * 4 + (done ? 1 : 0) + (_game.Burnt[i] ? 2 : 0);
                if (key == _edgeKeys[i])
                {
                    continue;
                }

                _edgeKeys[i] = key;
                Color color = Color.Lerp(new Color(0.18f, 0.18f, 0.2f), new Color(0.7f, 0.75f, 0.85f), ratio);
                Color glow = done ? new Color(0.5f, 0.7f, 1f) * 0.9f : Color.black;
                if (_game.Burnt[i])
                {
                    color = Color.Lerp(color, new Color(0.3f, 0.07f, 0.05f), 0.7f);
                    glow = Color.black;
                }
                _kit.Paint(_edge[i], color, glow);
            }
        }

        private Vector3 ContactPoint()
        {
            return _stageRoot.TransformPoint(new Vector3(0f, EdgeY + 0.03f, 0f));
        }

        public void HandleEvent(MiniEvent miniEvent)
        {
            switch (miniEvent.Kind)
            {
                case MiniEventKind.PhaseChanged:
                    _feedback.PlayPhase();
                    break;
                case MiniEventKind.Good:
                    _feedback.PlayDone(miniEvent.Index, (int)miniEvent.Value, ContactPoint());
                    break;
                case MiniEventKind.Hit:
                    _feedback.PlayDip(miniEvent.Value);
                    break;
                case MiniEventKind.Warn:
                    _feedback.PlayWarn();
                    break;
                case MiniEventKind.Hazard:
                    _feedback.PlayBurn();
                    break;
                case MiniEventKind.Complete:
                    _feedback.PlayComplete(miniEvent.Value, _game.SuccessScore);
                    break;
            }
        }

        // 바퀴와 칼날을 앞에서 내려다본다. 무대를 화면 아래쪽에 두려고 바라보는 곳을 위로 옮긴다.
        private void FocusCamera()
        {
            if (_camera == null)
            {
                return;
            }

            float pitch = CameraPitch * Mathf.Deg2Rad;
            Vector3 toCamera = _stageRoot.TransformDirection(new Vector3(0f, Mathf.Sin(pitch), -Mathf.Cos(pitch)));
            Vector3 cameraUp = _stageRoot.TransformDirection(new Vector3(0f, Mathf.Cos(pitch), Mathf.Sin(pitch)));
            Vector3 target = _stageRoot.TransformPoint(new Vector3(0f, 0.9f, 0.2f)) + cameraUp * (2f * ViewSize * Scale * ScreenShift);
            float distance = ViewSize * Scale / Mathf.Tan(HalfFov * Mathf.Deg2Rad);
            _camera.Focus(target + toCamera * distance, target);
        }
    }
}
