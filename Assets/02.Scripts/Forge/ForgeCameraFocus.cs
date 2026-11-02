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
        private float _progress;
        private bool _focusing;

        public bool IsActive { get; private set; }

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
            _target = new Pose(position, Quaternion.LookRotation(lookAt - position));
            _focusing = true;
            IsActive = true;
            enabled = true;
        }

        public void Release()
        {
            _focusing = false;
        }

        private void LateUpdate()
        {
            float step = Time.deltaTime / _blendTime;
            _progress = Mathf.Clamp01(_progress + (_focusing ? step : -step));
            float t = Mathf.SmoothStep(0f, 1f, _progress);

            transform.SetPositionAndRotation(
                Vector3.Lerp(_home.position, _target.position, t),
                Quaternion.Slerp(_home.rotation, _target.rotation, t));
            _camera.fieldOfView = Mathf.Lerp(_homeFov, _focusFov, t);

            if (!_focusing && _progress <= 0f)
            {
                RestoreHome();
            }
        }

        private void RestoreHome()
        {
            transform.SetPositionAndRotation(_home.position, _home.rotation);
            _camera.orthographic = _homeOrthographic;
            _camera.fieldOfView = _homeFov;
            _camera.orthographicSize = _homeOrthoSize;
            IsActive = false;
            enabled = false;
        }
    }
}
