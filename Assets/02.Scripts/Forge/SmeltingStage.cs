using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 정제 작업대 위에 올라가는 줄인 무대. 원석, 화덕, 크레인과 틀을 만들고 단계마다 카메라 시점을 알려 준다.
    // 모양은 임시 그레이박스라 코드로 만든다. 진행과 입력은 SmeltingStation이, 규칙은 SmeltingGame이 맡는다.
    public class SmeltingStage : MonoBehaviour
    {
        // 카메라 세로 시야각의 절반. ForgeCameraFocus의 시야각과 맞춘다.
        private const float HalfFov = 25f;
        // 누르고 올릴 때 도가니가 끝까지 기울어지는 마우스 이동량(1080p 기준 픽셀)
        private const float TiltPixels = 260f;
        // 화면 UI를 피하려고 시야를 넓히는 배율과, 무대를 화면 높이의 이 비율만큼 위로 올리는 값
        private const float ViewMargin = 1.1f;
        private const float ScreenShift = 0.06f;

        [SerializeField] private Transform _stageRoot;
        [SerializeField] private Transform _fxRoot;
        [SerializeField] private Material _bodyMaterial;
        [SerializeField] private Material _particleMaterial;
        [SerializeField] private Material _lineMaterial;
        [SerializeField] private AudioSource _sfx;
        [SerializeField] private ForgeNoiseSynth _fireSound;
        [SerializeField] private ForgeNoiseSynth _pourSound;
        [SerializeField] private Light _glow;

        private SmeltingGreybox _kit;
        private SmeltingBoulderVisual _boulder;
        private SmeltingFurnaceVisual _furnace;
        private SmeltingPourVisual _pour;
        private SmeltingCrackMarkers _markers;
        private SmeltingFeedback _feedback;
        private SmeltingGame _game;
        private SmeltingGame.ActKind _shownAct;
        private ForgeCameraFocus _camera;
        private Vector2 _lastPointer;
        private float _grabReference;
        private int _arrived;

        private float Scale => _stageRoot.localScale.x;

        private void Awake()
        {
            _kit = new SmeltingGreybox(_bodyMaterial, _particleMaterial);
            _furnace = new SmeltingFurnaceVisual(_kit, _stageRoot);
            _boulder = new SmeltingBoulderVisual(_kit, _stageRoot);
            _pour = new SmeltingPourVisual(_kit, _stageRoot, _furnace);
            _markers = new SmeltingCrackMarkers(_kit, _stageRoot, _lineMaterial);
            _feedback = new SmeltingFeedback(_kit, _stageRoot, _fxRoot, Scale, _sfx, _fireSound, _pourSound, _glow);

            _boulder.Impacted += _feedback.PlayImpact;
            _boulder.NuggetLanded += OnNuggetLanded;
            _glow.intensity = 0f;
        }

        public void SetVisible(bool visible)
        {
            _stageRoot.gameObject.SetActive(visible);
        }

        public void Begin(SmeltingGame game, ForgeCameraFocus camera, SmeltingView view)
        {
            _game = game;
            _camera = camera;
            _arrived = 0;
            _grabReference = 0f;

            _furnace.Bind(game);
            _boulder.Bind(game);
            _pour.Bind(game);
            _markers.Bind(game);
            _boulder.ResetVisual(game.Difficulty);
            _furnace.ResetVisual();
            _pour.ResetVisual();
            _feedback.Begin(camera, view);
            SetVisible(true);

            _shownAct = SmeltingGame.ActKind.Crack;
            FocusOn(_shownAct);
        }

        public void End()
        {
            _feedback.End();
            _markers.Hide();
            _game = null;
            _camera = null;
            SetVisible(false);
        }

        public void Tick(float deltaTime, Vector2 arenaPointer)
        {
            if (_game.Act != _shownAct && _game.Act != SmeltingGame.ActKind.Done)
            {
                _shownAct = _game.Act;
                FocusOn(_shownAct);
            }

            _lastPointer = arenaPointer;
            _boulder.SetPointer(arenaPointer.x, arenaPointer.y);
            _boulder.Tick(deltaTime);
            _furnace.Tick(deltaTime);
            _pour.Tick(deltaTime);
            _markers.Tick(_game);
            _feedback.Tick(_game, _furnace, _pour);
        }

        // 단계마다 카메라가 그 단계의 장면을 앞에서 내려다본다.
        private void FocusOn(SmeltingGame.ActKind act)
        {
            if (_camera == null)
            {
                return;
            }

            Vector3 focus;
            float size;
            switch (act)
            {
                case SmeltingGame.ActKind.Crack:
                    focus = SmeltingLayout.CrackFocus;
                    size = SmeltingLayout.CrackSize;
                    break;
                case SmeltingGame.ActKind.Bellows:
                    focus = SmeltingLayout.BellowsFocus;
                    size = SmeltingLayout.BellowsSize;
                    break;
                default:
                    focus = SmeltingLayout.PourFocus;
                    size = SmeltingLayout.PourSize;
                    break;
            }

            // 아래쪽 패널과 위쪽 단계 표시에 가리지 않도록 조금 멀리서 보고, 무대를 화면 위쪽으로 올려 놓는다.
            size *= ViewMargin;
            Vector3 target = _stageRoot.TransformPoint(focus);
            Vector3 up = _stageRoot.TransformDirection(SmeltingLayout.ArenaUp);
            target -= up * (2f * size * Scale * ScreenShift);
            Vector3 toCamera = _stageRoot.TransformDirection(SmeltingLayout.ArenaNormal);
            float distance = size * Scale / Mathf.Tan(HalfFov * Mathf.Deg2Rad);
            _camera.Focus(target + toCamera * distance, target);
        }

        // 화면의 마우스 위치를 지금 단계의 게임 좌표로 바꾼다.
        // 쪼개기: 원석 위 좌표. 붓기: x는 도가니의 좌우 위치, y는 누른 채 올린 정도(0~1).
        public bool TryMapPointer(Camera camera, Vector2 screen, bool held, out float x, out float y)
        {
            x = -100f;
            y = -100f;
            if (_game == null || camera == null)
            {
                return false;
            }

            Ray ray = camera.ScreenPointToRay(screen);
            switch (_game.Act)
            {
                case SmeltingGame.ActKind.Crack:
                    return MapCrack(ray, out x, out y);
                case SmeltingGame.ActKind.Pour:
                    return MapPour(ray, screen, held, out x, out y);
                default:
                    return false;
            }
        }

        private bool MapCrack(Ray ray, out float x, out float y)
        {
            x = -100f;
            y = -100f;
            var plane = new Plane(_stageRoot.TransformDirection(SmeltingLayout.ArenaNormal), _stageRoot.TransformPoint(SmeltingLayout.ArenaOrigin));
            if (!plane.Raycast(ray, out float distance))
            {
                return false;
            }

            Vector3 local = _stageRoot.InverseTransformPoint(ray.GetPoint(distance)) - SmeltingLayout.ArenaOrigin;
            x = Vector3.Dot(local, SmeltingLayout.ArenaRight) / SmeltingLayout.ArenaUnit;
            y = -Vector3.Dot(local, SmeltingLayout.ArenaUp) / SmeltingLayout.ArenaUnit;
            return true;
        }

        private bool MapPour(Ray ray, Vector2 screen, bool held, out float x, out float y)
        {
            x = -100f;
            y = 0f;
            var plane = new Plane(_stageRoot.TransformDirection(Vector3.up), _stageRoot.TransformPoint(new Vector3(0f, SmeltingLayout.HangY, 0f)));
            if (!plane.Raycast(ray, out float distance))
            {
                return false;
            }

            x = _stageRoot.InverseTransformPoint(ray.GetPoint(distance)).x;
            // 누르고 있는 동안, 누른 자리에서 위로 올린 만큼 도가니가 기울어진다.
            if (!held)
            {
                _grabReference = screen.y;
            }
            y = held ? Mathf.Clamp01((screen.y - _grabReference) / (TiltPixels * Screen.height / 1080f)) : 0f;
            return true;
        }

        public Vector3 TargetWorld(int index)
        {
            SmeltingGame.Target target = _game.Targets[index];
            return _stageRoot.TransformPoint(SmeltingLayout.ArenaToLocal(target.X, target.Y));
        }

        // 쪼개기 화면 표시에 쓰는 원석 좌표 평면의 위치, 오른쪽 방향, 1.0이 월드에서 갖는 길이
        public Vector3 ArenaWorld(float x, float y)
        {
            return _stageRoot.TransformPoint(SmeltingLayout.ArenaToLocal(x, y));
        }

        public float ArenaWorldUnit => SmeltingLayout.ArenaUnit * Scale;

        public void HandleEvent(MiniEvent miniEvent)
        {
            switch (miniEvent.Kind)
            {
                case MiniEventKind.Perfect:
                case MiniEventKind.Good:
                case MiniEventKind.Hit:
                    OnStrike(miniEvent);
                    break;
                case MiniEventKind.Miss:
                    OnTargetMissed(miniEvent.Index);
                    break;
                case MiniEventKind.Avoid:
                    _markers.MarkResult(miniEvent.Index, -2f);
                    _feedback.PlayAvoid(LocalTarget(miniEvent.Index));
                    break;
                case MiniEventKind.Crack:
                    OnCurse(miniEvent.Index);
                    break;
                case MiniEventKind.Swing:
                    _boulder.Swing();
                    _feedback.PlayStray();
                    break;
                case MiniEventKind.Early:
                    _feedback.PlayEarly(SmeltingLayout.ArenaToLocal(_lastPointer.x, _lastPointer.y));
                    break;
                case MiniEventKind.PhaseChanged:
                    OnPhase(miniEvent);
                    break;
                case MiniEventKind.Beat:
                    _feedback.PlayBeat(miniEvent.Value > 0.5f);
                    break;
                case MiniEventKind.Molten:
                    _feedback.PlayMolten(miniEvent.Value, miniEvent.Index == 1);
                    break;
                case MiniEventKind.Spill:
                    _feedback.PlaySpill();
                    break;
                case MiniEventKind.Overflow:
                    _feedback.PlayOverflow();
                    break;
                case MiniEventKind.Solidify:
                    _feedback.PlaySolidify();
                    break;
                case MiniEventKind.Warn:
                    _feedback.PlayWarn();
                    break;
                case MiniEventKind.Complete:
                    _pour.StartCooling();
                    _feedback.PlayIngot(_stageRoot.TransformPoint(_pour.MoldCenter));
                    _feedback.PlayResult(miniEvent.Value);
                    break;
            }
        }

        private Vector3 LocalTarget(int index)
        {
            SmeltingGame.Target target = _game.Targets[Mathf.Clamp(index, 0, _game.Targets.Count - 1)];
            return SmeltingLayout.ArenaToLocal(target.X, target.Y);
        }

        private void OnStrike(MiniEvent miniEvent)
        {
            _markers.MarkResult(miniEvent.Index, miniEvent.Value);
            _boulder.OnStrike(miniEvent.Index, miniEvent.Value);
            _feedback.ShowStrike(LocalTarget(miniEvent.Index), miniEvent.Value, _game.Combo);
        }

        private void OnTargetMissed(int index)
        {
            if (_game.Act != SmeltingGame.ActKind.Crack)
            {
                return;
            }

            _markers.MarkResult(index, 0f);
            _feedback.PlayMiss(LocalTarget(index));
        }

        private void OnCurse(int index)
        {
            _markers.MarkResult(index, -1f);
            _boulder.Swing();
            _feedback.PlayCurse(LocalTarget(index));
        }

        private void OnPhase(MiniEvent miniEvent)
        {
            switch (miniEvent.Index)
            {
                case 0:
                    _boulder.Split();
                    _feedback.PlaySplit(_boulder.BoulderCenter, miniEvent.Value);
                    break;
                case 2:
                    _feedback.PlayPourStart();
                    break;
            }
        }

        private void OnNuggetLanded(Vector3 local)
        {
            _furnace.AddNugget();
            _arrived++;
            _feedback.PlayNuggetLanded(local, _arrived);
        }
    }
}
