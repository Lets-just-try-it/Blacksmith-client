using System;
using Blacksmith.Network;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Blacksmith.Forge
{
    // ForgeTest 씬에서 내 플레이어를 따라간다. 기본은 쿼터뷰이고, F1~F6 키로 쿼터뷰 시점을 고르고(숫자 키는 손에 들 슬롯 선택에 쓴다) V 키로 1인칭과 번갈아 본다.
    // 1인칭에서는 마우스로 둘러보고, 이동은 보는 방향 기준이다. Game 씬에 카메라 추적이 생기면 지운다.
    public class ForgeTestCameraFollow : MonoBehaviour
    {
        [Serializable]
        private struct ViewPreset
        {
            public string Name;
            public float Pitch;
            public float Distance;
            public float Fov;
            public bool Orthographic;
            public float OrthoSize;
        }

        // 이동은 카메라 방향 기준이라 방위(yaw)는 게임과 같은 45도로 고정하고, 높이·거리·투영만 바꾼다.
        [SerializeField] private ViewPreset[] _presets =
        {
            new ViewPreset { Name = "Default", Pitch = 35f, Distance = 8.5f, Fov = 60f },
            new ViewPreset { Name = "Wide", Pitch = 35f, Distance = 15f, Fov = 60f },
            new ViewPreset { Name = "High", Pitch = 55f, Distance = 11f, Fov = 60f },
            new ViewPreset { Name = "Low", Pitch = 22f, Distance = 8f, Fov = 60f },
            new ViewPreset { Name = "Close", Pitch = 40f, Distance = 5.5f, Fov = 50f },
            new ViewPreset { Name = "Ortho", Pitch = 35f, Distance = 20f, Orthographic = true, OrthoSize = 6f },
        };
        [SerializeField] private float _quarterYaw = 45f;
        [SerializeField, Min(0f)] private float _smoothTime = 0.2f;
        [Tooltip("대상이 이 거리보다 멀리 떨어지면(구역 이동 등 순간이동) 따라가지 않고 바로 옮긴다.")]
        [SerializeField, Min(0f)] private float _snapDistance = 15f;
        [SerializeField, Min(0.1f)] private float _searchInterval = 0.5f;
        [SerializeField] private bool _startFirstPerson;
        [SerializeField] private float _eyeHeight = 0.7f;
        [SerializeField] private float _firstPersonFov = 75f;
        [SerializeField, Min(0f)] private float _lookSensitivity = 0.1f;

        private Camera _camera;
        private ForgeCameraFocus _focus;
        private InputAction _toggle;
        private InputAction _look;
        private InputAction[] _presetKeys;
        private float _quarterNear;
        private Transform _target;
        private Vector3 _velocity;
        private float _nextSearchTime;
        private bool _firstPerson;
        private int _presetIndex;
        private float _yaw;
        private float _pitch;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _focus = GetComponent<ForgeCameraFocus>();
            _quarterNear = _camera.nearClipPlane;
            _toggle = new InputAction("ToggleView", binding: "<Keyboard>/v");
            _look = new InputAction("Look", binding: "<Mouse>/delta");
            _toggle.performed += _ => SetFirstPerson(!_firstPerson);

            _presetKeys = new InputAction[_presets.Length];
            for (int i = 0; i < _presets.Length; i++)
            {
                int index = i;
                _presetKeys[i] = new InputAction("Preset" + (i + 1), binding: "<Keyboard>/f" + (i + 1));
                _presetKeys[i].performed += _ => SelectPreset(index);
            }
        }

        private void Start()
        {
            ApplyPreset();
        }

        private void OnEnable()
        {
            _toggle.Enable();
            _look.Enable();
            foreach (InputAction key in _presetKeys)
            {
                key.Enable();
            }
        }

        private void OnDisable()
        {
            _toggle.Disable();
            _look.Disable();
            foreach (InputAction key in _presetKeys)
            {
                key.Disable();
            }
            SetFirstPerson(false);
        }

        private void OnDestroy()
        {
            _toggle.Dispose();
            _look.Dispose();
            foreach (InputAction key in _presetKeys)
            {
                key.Dispose();
            }
        }

        private void LateUpdate()
        {
            // 미니게임 포커스가 카메라를 잡고 있는 동안에는 따라가지 않는다.
            if (_focus != null && _focus.IsActive)
            {
                return;
            }

            if (_target == null)
            {
                FindLocalPlayer();
                return;
            }

            if (_firstPerson)
            {
                UpdateFirstPerson();
            }
            else
            {
                FollowQuarterView();
            }
        }

        private void OnGUI()
        {
            string text = "View ";
            for (int i = 0; i < _presets.Length; i++)
            {
                string label = (i + 1) + " " + _presets[i].Name;
                text += !_firstPerson && i == _presetIndex ? "[" + label + "]  " : label + "  ";
            }
            text += _firstPerson ? "[V FirstPerson]" : "V FirstPerson";
            GUI.Label(new Rect(10f, 8f, 900f, 24f), text);
        }

        public void SetFirstPerson(bool enable)
        {
            if (_firstPerson == enable)
            {
                return;
            }

            _firstPerson = enable;
            if (enable)
            {
                _yaw = _target != null ? _target.eulerAngles.y : transform.eulerAngles.y;
                _pitch = 5f;
                _camera.orthographic = false;
                _camera.fieldOfView = _firstPersonFov;
                _camera.nearClipPlane = 0.05f;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                ApplyPreset();
            }
        }

        public void SelectPreset(int index)
        {
            _presetIndex = Mathf.Clamp(index, 0, _presets.Length - 1);
            if (_firstPerson)
            {
                SetFirstPerson(false);
                return;
            }

            ApplyPreset();
        }

        private void ApplyPreset()
        {
            ViewPreset preset = _presets[_presetIndex];
            transform.rotation = Quaternion.Euler(preset.Pitch, _quarterYaw, 0f);
            _camera.nearClipPlane = _quarterNear;
            _camera.orthographic = preset.Orthographic;
            if (preset.Orthographic)
            {
                _camera.orthographicSize = preset.OrthoSize;
            }
            else
            {
                _camera.fieldOfView = preset.Fov;
            }
        }

        private void FollowQuarterView()
        {
            Vector3 goal = _target.position + QuarterOffset();
            if ((goal - transform.position).sqrMagnitude > _snapDistance * _snapDistance)
            {
                transform.position = goal;
                _velocity = Vector3.zero;
                return;
            }

            transform.position = Vector3.SmoothDamp(transform.position, goal, ref _velocity, _smoothTime);
        }

        private Vector3 QuarterOffset()
        {
            ViewPreset preset = _presets[_presetIndex];
            return Quaternion.Euler(preset.Pitch, _quarterYaw, 0f) * Vector3.back * preset.Distance;
        }

        private void UpdateFirstPerson()
        {
            Vector2 delta = _look.ReadValue<Vector2>() * _lookSensitivity;
            _yaw += delta.x;
            _pitch = Mathf.Clamp(_pitch - delta.y, -80f, 80f);
            transform.SetPositionAndRotation(_target.position + Vector3.up * _eyeHeight, Quaternion.Euler(_pitch, _yaw, 0f));
        }

        private void FindLocalPlayer()
        {
            if (Time.time < _nextSearchTime)
            {
                return;
            }

            _nextSearchTime = Time.time + _searchInterval;
            foreach (NetworkPlayer player in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
            {
                if (player.HasInputAuthority)
                {
                    _target = player.transform;
                    SetFirstPerson(_startFirstPerson);
                    return;
                }
            }
        }
    }
}
