using UnityEngine;

namespace Blacksmith.Forge
{
    // 작업대 위에 올라가는 줄인 무대의 공통 부분: 카메라 맞추기, 소리, 흔들림, 불똥.
    // 작업대마다 올려 둘 물건은 서브클래스가 BuildProps에서 임시 그레이박스로 만든다.
    public class MiniStage : MonoBehaviour
    {
        private const float HalfFov = 25f;
        // 그림판과 아래 패널이 위쪽을 차지하므로 무대를 화면 아래쪽으로 내려 놓는다.
        private const float CameraPitch = 40f;

        [SerializeField] private Transform _stageRoot;
        [SerializeField] private Transform _fxRoot;
        [SerializeField] private Material _bodyMaterial;
        [SerializeField] private Material _particleMaterial;
        [SerializeField] private AudioSource _sfx;

        private ForgeCameraFocus _camera;
        private ParticleSystem _sparks;
        private ParticleSystem _glint;

        protected ForgeGreybox Kit { get; private set; }
        protected Transform StageRoot => _stageRoot;
        protected float Scale => _stageRoot.localScale.x;
        protected AudioSource Sfx => _sfx;
        protected ForgeCameraFocus CameraFocus => _camera;
        protected Transform FxRoot => _fxRoot;
        // 카메라가 바라보는 무대 안 높이와, 무대를 화면 아래로 내리는 정도(화면 높이 비율). 서브클래스가 바꿀 수 있다.
        protected virtual float FocusHeight => 0.6f;
        protected virtual float ScreenShift => 0.3f;
        protected virtual float ViewSize => 4.8f;

        private void Awake()
        {
            Kit = new ForgeGreybox(_bodyMaterial, _particleMaterial);
            Kit.Box(_stageRoot, "Base", new Vector3(0f, 0.2f, 0.1f), new Vector3(7.4f, 0.3f, 3.4f), new Color(0.3f, 0.19f, 0.11f));
            _sparks = Kit.Particles(_fxRoot, "Sparks", new Color(1f, 0.8f, 0.4f), 3f * Scale, 0.06f * Scale, 0.45f, 1.4f * Scale, 50f);
            _glint = Kit.Particles(_fxRoot, "Glint", Color.white, 1.6f * Scale, 0.1f * Scale, 0.8f, -0.2f * Scale, 90f);
            BuildProps();
            SetVisible(false);
        }

        protected virtual void BuildProps()
        {
        }



        public void SetVisible(bool visible)
        {
            _stageRoot.gameObject.SetActive(visible);
        }

        public virtual void Begin(ForgeCameraFocus camera)
        {
            _camera = camera;
            SetVisible(true);
            FocusCamera();
        }

        public virtual void End()
        {
            ForgeGreybox.SetRate(_sparks, 0f);
            _camera = null;
            SetVisible(false);
        }

        public void Shake(float amplitude, float seconds)
        {
            if (_camera != null)
            {
                _camera.Shake(amplitude * Scale, seconds);
            }
        }

        // 맞는 순간 화면이 살짝 다가왔다 돌아온다(치명타 연출). amount는 시야각이 줄어드는 비율이다.
        public void Punch(float amount)
        {
            if (_camera != null)
            {
                _camera.Punch(amount);
            }
        }

        public void Play(AudioClip clip, float volume, float pitch)
        {
            if (!MinigameFeedbackSwitch.Enabled)
            {
                return;
            }

            _sfx.pitch = pitch;
            _sfx.PlayOneShot(clip, volume);
        }

        public void Burst(int count)
        {
            ForgeGreybox.Burst(_glint, count);
        }

        public void Sparks(Vector3 worldPosition, float rate)
        {
            _sparks.transform.position = worldPosition;
            ForgeGreybox.SetRate(_sparks, rate);
        }

        // 무대를 앞에서 내려다본다. 무대를 화면 아래쪽에 두려고 바라보는 곳을 위로 옮긴다.
        protected virtual void FocusCamera()
        {
            if (_camera == null)
            {
                return;
            }

            float pitch = CameraPitch * Mathf.Deg2Rad;
            Vector3 toCamera = _stageRoot.TransformDirection(new Vector3(0f, Mathf.Sin(pitch), -Mathf.Cos(pitch)));
            Vector3 cameraUp = _stageRoot.TransformDirection(new Vector3(0f, Mathf.Cos(pitch), Mathf.Sin(pitch)));
            Vector3 target = _stageRoot.TransformPoint(new Vector3(0f, FocusHeight, 0f)) + cameraUp * (2f * ViewSize * Scale * ScreenShift);
            float distance = ViewSize * Scale / Mathf.Tan(HalfFov * Mathf.Deg2Rad);
            _camera.Focus(target + toCamera * distance, target);
        }
    }
}
