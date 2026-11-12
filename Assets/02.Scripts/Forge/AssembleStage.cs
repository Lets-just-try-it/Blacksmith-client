using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 조립 작업대 위에 올라가는 줄인 무대: 판 위에서 망치가 리벳을 내려치고, 칠 때마다 불똥이 튄다.
    // 방어구 그림은 AssembleBoardView가 화면에 그리고, 이 무대는 그 아래쪽에 보이는 3D 장면과 소리·흔들림을 맡는다.
    // 모양은 임시 그레이박스라 코드로 만든다. 진행과 입력은 AssembleStation이, 규칙은 AssembleGame이 맡는다.
    public class AssembleStage : MonoBehaviour
    {
        private const float HalfFov = 25f;
        // 방어구 화면과 아래 패널이 위쪽을 차지하므로 무대를 화면 아래쪽으로 내려 놓는다.
        private const float ViewSize = 4.8f;
        private const float ScreenShift = 0.3f;
        private const float CameraPitch = 40f;
        private const float SheetTop = 0.46f;

        [SerializeField] private Transform _stageRoot;
        [SerializeField] private Transform _fxRoot;
        [SerializeField] private Material _bodyMaterial;
        [SerializeField] private Material _particleMaterial;
        [SerializeField] private AudioSource _sfx;

        private ForgeGreybox _kit;
        private ParticleSystem _sparks;
        private ParticleSystem _glint;
        private Transform _hammer;
        private AssembleGame _game;
        private AssembleView _view;
        private AssembleBoardView _board;
        private ForgeCameraFocus _camera;
        private float _swing;

        private float Scale => _stageRoot.localScale.x;

        private void Awake()
        {
            _kit = new ForgeGreybox(_bodyMaterial, _particleMaterial);
            _kit.Box(_stageRoot, "Base", new Vector3(0f, 0.2f, 0.1f), new Vector3(7.4f, 0.3f, 3.4f), new Color(0.3f, 0.19f, 0.11f));
            _kit.Box(_stageRoot, "Sheet", new Vector3(0f, SheetTop - 0.05f, 0f), new Vector3(5.2f, 0.1f, 2.8f), new Color(0.55f, 0.58f, 0.64f));
            BuildHammer();
            _sparks = _kit.Particles(_fxRoot, "Sparks", new Color(1f, 0.8f, 0.4f), 3f * Scale, 0.06f * Scale, 0.45f, 1.4f * Scale, 50f);
            _glint = _kit.Particles(_fxRoot, "Glint", Color.white, 1.6f * Scale, 0.1f * Scale, 0.8f, -0.2f * Scale, 90f);
        }

        // 손잡이와 머리로 된 망치. swing 값이 1에서 0으로 줄어들며 내려친다.
        private void BuildHammer()
        {
            var group = new GameObject("Hammer");
            group.transform.SetParent(_stageRoot, false);
            group.transform.localPosition = new Vector3(0f, SheetTop + 0.1f, 0f);
            _hammer = group.transform;
            _kit.Box(_hammer, "Handle", new Vector3(0.5f, 0f, 0f), new Vector3(1.0f, 0.08f, 0.1f), new Color(0.25f, 0.15f, 0.09f));
            _kit.Box(_hammer, "Head", new Vector3(-0.1f, 0f, 0f), new Vector3(0.3f, 0.3f, 0.3f), new Color(0.35f, 0.35f, 0.4f));
        }

        public void SetVisible(bool visible)
        {
            _stageRoot.gameObject.SetActive(visible);
        }

        public void Begin(AssembleGame game, ForgeCameraFocus camera, AssembleView view, AssembleBoardView board)
        {
            _game = game;
            _camera = camera;
            _view = view;
            _board = board;
            _swing = 0f;
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
            _swing = Mathf.MoveTowards(_swing, 0f, deltaTime * 5f);
            _hammer.localRotation = Quaternion.Euler(0f, 0f, 40f * _swing);
            ForgeGreybox.SetRate(_sparks, 0f);
        }

        public void HandleEvent(MiniEvent miniEvent)
        {
            switch (miniEvent.Kind)
            {
                case MiniEventKind.PhaseChanged:
                    Play(ForgeProceduralSfx.Tick(), 0.4f, 1.2f);
                    break;
                case MiniEventKind.Pin:
                    Play(ForgeProceduralSfx.Clang(0), 0.4f, 1.5f);
                    Popup("고정!  맞춤 " + Mathf.RoundToInt(miniEvent.Value * 100f) + "%", new Color(0.5f, 1f, 0.6f), HoleCenter(miniEvent.Index));
                    break;
                case MiniEventKind.Misalign:
                    Play(ForgeProceduralSfx.Fail(), 0.3f, 1.4f);
                    Shake(0.01f, 0.06f);
                    break;
                case MiniEventKind.Rivet:
                    Play(ForgeProceduralSfx.Tick(), 0.5f, 0.9f);
                    break;
                case MiniEventKind.Perfect:
                    Strike(miniEvent, "완벽!", new Color(1f, 0.9f, 0.4f), 1f);
                    break;
                case MiniEventKind.Hit:
                    Strike(miniEvent, "좋아", new Color(0.7f, 1f, 0.8f), 0.8f);
                    break;
                case MiniEventKind.Miss:
                    Play(ForgeProceduralSfx.Fail(), 0.35f, 1.4f);
                    Shake(0.015f, 0.08f);
                    break;
                case MiniEventKind.Good:
                    Play(ForgeProceduralSfx.Ding(880f), 0.5f, 1f);
                    ForgeGreybox.Burst(_glint, 14);
                    Popup("리벳 완성  " + Mathf.RoundToInt(miniEvent.Value * 100f) + "%", new Color(0.5f, 1f, 0.6f), HoleCenter(miniEvent.Index));
                    break;
                case MiniEventKind.SeamDone:
                    Play(ForgeProceduralSfx.Chime(660f), 0.5f, 1f);
                    if (_view != null && _board != null)
                    {
                        _view.Popup("이음새 완료  " + Mathf.RoundToInt(miniEvent.Value * 100f) + "%", new Color(1f, 0.95f, 0.6f), Vector2.zero);
                        _view.Flash(0.05f, new Color(1f, 0.95f, 0.7f));
                    }

                    break;
                case MiniEventKind.Complete:
                    PlayComplete(miniEvent.Value);
                    break;
            }
        }

        private void Strike(MiniEvent miniEvent, string text, Color color, float strength)
        {
            _swing = 1f;
            Play(ForgeProceduralSfx.Clang(miniEvent.Index % 3), 0.5f * strength, 1.2f + 0.2f * strength);
            Shake(0.01f * strength, 0.07f);
            ForgeGreybox.Burst(_glint, 8);
            Popup(text, color, HoleCenter(miniEvent.Index));
        }

        private void Popup(string text, Color color, Vector2 position)
        {
            if (_view != null)
            {
                _view.Popup(text, color, position);
            }
        }

        // 지금 이음새의 구멍이 보드 위에서 놓인 화면 위치
        private Vector2 HoleCenter(int holeIndex)
        {
            AssembleGame.Seam seam = _game?.Active;
            if (seam == null || _board == null || holeIndex < 0 || holeIndex >= seam.Holes.Length)
            {
                return Vector2.zero;
            }

            AssembleGame.Hole hole = seam.Holes[holeIndex];
            return _board.ToLocal(hole.X, hole.Y + 0.9f);
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

        // 판을 앞에서 내려다본다. 무대를 화면 아래쪽에 두려고 바라보는 곳을 위로 옮긴다.
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
