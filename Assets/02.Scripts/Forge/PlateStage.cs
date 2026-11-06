using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 판 성형 작업대 위에 올라가는 줄인 무대: 쇠판 위를 가위가 따라다니며 자르고, 오리는 곳에서 불똥이 튄다.
    // 쇠판 그림은 PlateBoardView가 화면에 그리고, 이 무대는 그 아래쪽에 보이는 3D 장면과 소리·흔들림을 맡는다.
    // 모양은 임시 그레이박스라 코드로 만든다. 진행과 입력은 PlateStation이, 규칙은 PlateGame이 맡는다.
    public class PlateStage : MonoBehaviour
    {
        private const float HalfFov = 25f;
        // 쇠판 화면과 아래 패널이 위쪽을 차지하므로 무대를 화면 아래쪽으로 내려 놓는다.
        private const float ViewSize = 4.8f;
        private const float ScreenShift = 0.3f;
        private const float CameraPitch = 40f;
        private const float SheetWidth = 5.2f;
        private const float SheetDepth = 2.8f;
        private const float SheetTop = 0.46f;
        private const float ScrapeInterval = 0.11f;

        [SerializeField] private Transform _stageRoot;
        [SerializeField] private Transform _fxRoot;
        [SerializeField] private Material _bodyMaterial;
        [SerializeField] private Material _particleMaterial;
        [SerializeField] private AudioSource _sfx;

        private ForgeGreybox _kit;
        private ParticleSystem _sparks;
        private ParticleSystem _glint;
        private Transform _shears;
        private Transform _shearBlade;
        private PlateGame _game;
        private PlateView _view;
        private PlateBoardView _board;
        private ForgeCameraFocus _camera;
        private Vector3 _shearsTarget;
        private float _snip;
        private float _scrapeTimer;
        private int _lastHead;

        private float Scale => _stageRoot.localScale.x;

        private void Awake()
        {
            _kit = new ForgeGreybox(_bodyMaterial, _particleMaterial);
            _kit.Box(_stageRoot, "Base", new Vector3(0f, 0.2f, 0.1f), new Vector3(7.4f, 0.3f, 3.4f), new Color(0.3f, 0.19f, 0.11f));
            _kit.Box(_stageRoot, "Sheet", new Vector3(0f, SheetTop - 0.05f, 0f), new Vector3(SheetWidth, 0.1f, SheetDepth), new Color(0.55f, 0.58f, 0.64f));
            BuildShears();
            _sparks = _kit.Particles(_fxRoot, "Sparks", new Color(1f, 0.8f, 0.4f), 3f * Scale, 0.06f * Scale, 0.45f, 1.4f * Scale, 50f);
            _glint = _kit.Particles(_fxRoot, "Glint", Color.white, 1.6f * Scale, 0.1f * Scale, 0.8f, -0.2f * Scale, 90f);
        }

        // 두 날이 겹친 가위. 위 날이 snip 값에 따라 벌어졌다 닫힌다.
        private void BuildShears()
        {
            var group = new GameObject("Shears");
            group.transform.SetParent(_stageRoot, false);
            group.transform.localPosition = new Vector3(SheetWidth * 0.5f - 0.4f, SheetTop + 0.12f, -0.6f);
            _shears = group.transform;

            Color steel = new Color(0.3f, 0.3f, 0.35f);
            _kit.Box(_shears, "LowerBlade", new Vector3(0f, 0f, 0f), new Vector3(1.0f, 0.06f, 0.14f), steel);
            _shearBlade = _kit.Box(_shears, "UpperBlade", new Vector3(0f, 0.08f, 0f), new Vector3(1.0f, 0.06f, 0.14f), steel * 1.3f).transform;
            _kit.Box(_shears, "Handle", new Vector3(0.9f, 0.04f, 0f), new Vector3(0.8f, 0.12f, 0.2f), new Color(0.25f, 0.15f, 0.09f));
            _shearsTarget = _shears.localPosition;
        }

        public void SetVisible(bool visible)
        {
            _stageRoot.gameObject.SetActive(visible);
        }

        public void Begin(PlateGame game, ForgeCameraFocus camera, PlateView view, PlateBoardView board)
        {
            _game = game;
            _camera = camera;
            _view = view;
            _board = board;
            _snip = 0f;
            _lastHead = 0;
            SetVisible(true);
            FocusCamera();
        }

        public void End()
        {
            ForgeGreybox.SetRate(_sparks, 0f);
            _game = null;
            _camera = null;
            _view = null;
            _board = null;
            SetVisible(false);
        }

        public void Tick(float deltaTime)
        {
            // 가위는 오리는 점을 따라 쇠판 위를 움직인다.
            bool onBoard = _game.PointerX > -50f && _game.Phase == PlateGame.PhaseKind.Cut;
            if (onBoard)
            {
                float x = (_game.PointerX / PlateGame.Width - 0.5f) * SheetWidth;
                float z = (0.5f - _game.PointerY / PlateGame.Height) * SheetDepth;
                _shearsTarget = new Vector3(x + 0.55f, SheetTop + 0.12f, z);
            }

            _shears.localPosition = Vector3.Lerp(_shears.localPosition, _shearsTarget, 1f - Mathf.Exp(-16f * deltaTime));
            _snip = Mathf.MoveTowards(_snip, 0f, deltaTime * 4f);
            _shearBlade.localRotation = Quaternion.Euler(0f, 6f * Mathf.Sin(_snip * 12f), 0f);

            bool cutting = _game.Phase == PlateGame.PhaseKind.Cut && _game.Cutting && !_game.OffBand && _game.CurrentPart != null;
            _sparks.transform.position = _shears.position;
            ForgeGreybox.SetRate(_sparks, cutting ? 40f : 0f);
            if (cutting)
            {
                TickScrape(deltaTime);
            }
            else if (_game.CurrentPart != null)
            {
                _lastHead = _game.CurrentPart.Head;
            }
        }

        // 오려 나가는 동안 사각사각 소리
        private void TickScrape(float deltaTime)
        {
            int head = _game.CurrentPart.Head;
            _scrapeTimer -= deltaTime;
            if (head != _lastHead && _scrapeTimer <= 0f)
            {
                _scrapeTimer = ScrapeInterval;
                Play(ForgeProceduralSfx.Tick(), 0.18f, 1.6f + 0.2f * Mathf.Sin(head * 0.2f));
                _snip = Mathf.Max(_snip, 0.4f);
            }

            _lastHead = head;
        }

        public void HandleEvent(MiniEvent miniEvent)
        {
            switch (miniEvent.Kind)
            {
                case MiniEventKind.Spawn:
                    Play(ForgeProceduralSfx.Tick(), 0.5f, 1.3f);
                    break;
                case MiniEventKind.Grip:
                    Play(ForgeProceduralSfx.Tick(), 0.35f, 0.9f);
                    break;
                case MiniEventKind.Hit:
                    PlayPartDone(miniEvent);
                    break;
                case MiniEventKind.Miss:
                    Play(ForgeProceduralSfx.Fail(), 0.35f, 1.4f);
                    Shake(0.02f, 0.08f);
                    break;
                case MiniEventKind.Complete:
                    PlayComplete(miniEvent.Value);
                    break;
            }
        }

        // 조각 하나를 다 오렸다.
        private void PlayPartDone(MiniEvent miniEvent)
        {
            _snip = 1f;
            Play(ForgeProceduralSfx.Clang(miniEvent.Index % 3), 0.5f, 1.3f);
            Shake(0.012f, 0.08f);
            ForgeGreybox.Burst(_glint, 14);
            if (_view == null || _game == null)
            {
                return;
            }

            PlateGame.Part part = _game.Parts[Mathf.Clamp(miniEvent.Index, 0, _game.Parts.Count - 1)];
            Color color = miniEvent.Value >= 0.8f ? new Color(0.5f, 1f, 0.6f) : new Color(1f, 0.9f, 0.5f);
            _view.Popup(part.Name + " 완성!  정확도 " + Mathf.RoundToInt(miniEvent.Value * 100f) + "%", color, _board.ToLocal(PlateGame.Width * 0.5f, 0.6f));
            _view.Flash(0.05f, new Color(1f, 0.95f, 0.7f));
        }

        private void PlayComplete(float score)
        {
            if (score >= _game.SuccessScore)
            {
                Play(ForgeProceduralSfx.Success(), 0.7f, 1f);
                ForgeGreybox.Burst(_glint, 40);
            }
            else
            {
                Play(ForgeProceduralSfx.Fail(), 0.7f, 1f);
            }
        }

        private void Shake(float amplitude, float seconds)
        {
            if (_camera != null)
            {
                _camera.Shake(amplitude * Scale, seconds);
            }
        }

        private void Play(AudioClip clip, float volume, float pitch)
        {
            if (!MinigameFeedbackSwitch.Enabled)
            {
                return;
            }

            _sfx.pitch = pitch;
            _sfx.PlayOneShot(clip, volume);
        }

        // 쇠판을 앞에서 내려다본다. 무대를 화면 아래쪽에 두려고 바라보는 곳을 위로 옮긴다.
        private void FocusCamera()
        {
            if (_camera == null)
            {
                return;
            }

            float pitch = CameraPitch * Mathf.Deg2Rad;
            Vector3 toCamera = _stageRoot.TransformDirection(new Vector3(0f, Mathf.Sin(pitch), -Mathf.Cos(pitch)));
            Vector3 cameraUp = _stageRoot.TransformDirection(new Vector3(0f, Mathf.Cos(pitch), Mathf.Sin(pitch)));
            Vector3 target = _stageRoot.TransformPoint(new Vector3(0f, 0.6f, 0f)) + cameraUp * (2f * ViewSize * Scale * ScreenShift);
            float distance = ViewSize * Scale / Mathf.Tan(HalfFov * Mathf.Deg2Rad);
            _camera.Focus(target + toCamera * distance, target);
        }
    }
}
