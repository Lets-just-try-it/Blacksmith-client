using UnityEngine;

namespace Blacksmith.Forge
{
    // 작업대 미니게임 동안 카메라를 작업대 쪽으로 옮기고, 끝나면 시작 때의 시점으로 되돌린다.
    // 되돌리는 동안에도 IsActive가 참이라 카메라 추적(ForgeTestCameraFollow)은 끝난 뒤에 이어받는다.
    [RequireComponent(typeof(Camera))]
    public class ForgeCameraFocus : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float _blendTime = 0.6f;
        [SerializeField] private float _focusFov = 50f;

        private Camera _camera;
        private Pose _home;
        private float _homeFov;
        private float _homeOrthoSize;
        private bool _homeOrthographic;
        private Pose _target;
        private Pose _from;
        private float _move = 1f;
        private float _progress;
        private bool _focusing;
        private float _shakeAmplitude;
        private float _shakeDuration;
        private float _shakeLeft;
        private float _punch;

        public bool IsActive { get; private set; }
        public float FocusFov => _focusFov;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            enabled = false;
        }

        public void Focus(Vector3 position, Vector3 lookAt)
        {
            if (!IsActive)
            {
                _home = new Pose(transform.position, transform.rotation);
                _homeFov = _camera.fieldOfView;
                _homeOrthoSize = _camera.orthographicSize;
                _homeOrthographic = _camera.orthographic;
            }

            _camera.orthographic = false;
            var next = new Pose(position, Quaternion.LookRotation(lookAt - position));
            if (IsActive && _focusing)
            {
                // 이미 포커스 중이면 지금 시점에서 새 시점으로 부드럽게 옮겨 간다.
                _from = CurrentFocusPose();
                _move = 0f;
            }
            else
            {
                _from = next;
                _move = 1f;
            }

            _target = next;
            _focusing = true;
            IsActive = true;
            enabled = true;
        }

        public void Release()
        {
            _focusing = false;
        }

        // 맞는 순간 화면이 흔들린다. 진폭은 미터 단위이고 끝으로 갈수록 줄어든다.
        public void Shake(float amplitude, float seconds)
        {
            if (!MinigameFeedbackSwitch.Enabled)
            {
                return;
            }

            _shakeAmplitude = Mathf.Max(_shakeAmplitude * (_shakeLeft / Mathf.Max(0.01f, _shakeDuration)), amplitude);
            _shakeDuration = _shakeLeft = seconds;
        }

        // 맞는 순간 화면이 살짝 다가왔다 돌아온다. amount는 시야각이 줄어드는 비율이다.
        public void Punch(float amount)
        {
            if (!MinigameFeedbackSwitch.Enabled)
            {
                return;
            }

            _punch = Mathf.Max(_punch, amount);
        }

        private void LateUpdate()
        {
            float step = Time.deltaTime / _blendTime;
            _progress = Mathf.Clamp01(_progress + (_focusing ? step : -step));
            float t = Mathf.SmoothStep(0f, 1f, _progress);

            _move = Mathf.Clamp01(_move + step);
            Pose focus = CurrentFocusPose();
            transform.SetPositionAndRotation(
                Vector3.Lerp(_home.position, focus.position, t),
                Quaternion.Slerp(_home.rotation, focus.rotation, t));
            _camera.fieldOfView = Mathf.Lerp(_homeFov, _focusFov, t) * (1f - _punch);
            ApplyShake();
            _punch = Mathf.MoveTowards(_punch, 0f, Time.deltaTime * 0.5f);

            if (!_focusing && _progress <= 0f)
            {
                RestoreHome();
            }
        }

        private Pose CurrentFocusPose()
        {
            float m = Mathf.SmoothStep(0f, 1f, _move);
            return new Pose(
                Vector3.Lerp(_from.position, _target.position, m),
                Quaternion.Slerp(_from.rotation, _target.rotation, m));
        }

        private void ApplyShake()
        {
            if (_shakeLeft <= 0f)
            {
                return;
            }

            _shakeLeft -= Time.deltaTime;
            transform.position += Random.insideUnitSphere * (_shakeAmplitude * Mathf.Max(0f, _shakeLeft / _shakeDuration));
        }

        private void RestoreHome()
        {
            _shakeLeft = 0f;
            _punch = 0f;
            transform.SetPositionAndRotation(_home.position, _home.rotation);
            _camera.orthographic = _homeOrthographic;
            _camera.fieldOfView = _homeFov;
            _camera.orthographicSize = _homeOrthoSize;
            IsActive = false;
            enabled = false;
        }
    }
}
