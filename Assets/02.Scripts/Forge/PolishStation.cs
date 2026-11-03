using System;
using Blacksmith.Core.Forge;
using Blacksmith.Network;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Blacksmith.Forge
{
    // 연마 작업대. 내 캐릭터가 범위 안에서 상호작용 키를 누르면 카메라를 작업대로 옮기고 연마를 진행한다.
    // 마우스 버튼을 누른 채 도는 숫돌 위에서 알맞은 각도(초록 띠)에 맞춰 날을 밀어 갈고, 상호작용 키를 누르는 동안 물에 식힌다.
    // 미니게임은 이 클라이언트에서만 돌리고 Update에서 시간을 넘긴다. 끝난 정확도는 Completed로 알린다.
    // 작업대 사용권과 결과를 호스트에 보내는 일은 아직 없다.
    [RequireComponent(typeof(BoxCollider))]
    public class PolishStation : MonoBehaviour
    {
        [SerializeField] private PolishRulesData _rulesData;
        [SerializeField] private InputActionReference _interact;
        [SerializeField] private InputActionReference _point;
        [SerializeField] private InputActionReference _click;
        [SerializeField] private PolishView _view;
        [SerializeField] private PolishLensView _lens;
        [SerializeField] private PolishStage _stage;
        [SerializeField] private PlayerNetworkInputLock _inputLock;
        [SerializeField] private ForgeStationLink _link;

        private BoxCollider _range;
        private PolishRules _rules;
        private PolishGame _game;
        private NetworkPlayer _localPlayer;
        private ForgeCameraFocus _cameraFocus;
        private UniversalAdditionalCameraData _cameraData;
        private AntialiasingMode _previousAntialiasing;
        private Renderer[] _hiddenRenderers = Array.Empty<Renderer>();
        private bool _cursorWasVisible;
        private CursorLockMode _cursorLockState;

        public event Action<float> Completed;

        private void Awake()
        {
            _range = GetComponent<BoxCollider>();
            _rules = _rulesData.ToRules();
            _stage.SetVisible(false);
        }

        private void OnEnable()
        {
            _link.Approved += Begin;
            _interact.action.Enable();
            _point.action.Enable();
            _click.action.Enable();
        }

        private void OnDisable()
        {
            _link.Approved -= Begin;
            if (_game != null)
            {
                End();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            NetworkPlayer player = other.GetComponentInParent<NetworkPlayer>();
            if (player == null || !player.HasInputAuthority)
            {
                return;
            }

            _localPlayer = player;
            _view.ShowPrompt(_game == null);
        }

        private void OnTriggerExit(Collider other)
        {
            NetworkPlayer player = other.GetComponentInParent<NetworkPlayer>();
            if (player == null || player != _localPlayer)
            {
                return;
            }

            LeaveRange();
        }

        private void Update()
        {
            if (_game == null)
            {
                // OnTriggerExit가 오지 않는 경우(순간이동 등)에도 범위를 벗어나면 시작하지 못하도록 직접 확인한다.
                if (_localPlayer != null && !IsInRange(_localPlayer.transform.position))
                {
                    LeaveRange();
                }

                if (_localPlayer != null && _interact.action.WasPressedThisFrame())
                {
                    _link.RequestStart();
                }
                return;
            }

            _game.Tick(Time.deltaTime, ReadInput());
            ShowEvents();
            _stage.Tick(Time.deltaTime);
            _lens.Tick(Time.deltaTime);
            _view.Refresh(_game);

            if (_game.Finished)
            {
                float accuracy = _game.Accuracy;
                End();
                _link.SubmitResult(accuracy);
                Completed?.Invoke(accuracy);
            }
        }

        // 높이는 보지 않고 바닥 기준 범위만 확인한다.
        private bool IsInRange(Vector3 position)
        {
            Bounds bounds = _range.bounds;
            return bounds.Contains(new Vector3(position.x, bounds.center.y, position.z));
        }

        private void LeaveRange()
        {
            _localPlayer = null;
            _view.ShowPrompt(false);
        }

        private void Begin()
        {
            _game = new PolishGame(_rules, Environment.TickCount, _link.Difficulty, _link.PriorAccuracy);
            _inputLock.Lock(this);

            Camera camera = Camera.main;
            _cameraFocus = camera.GetComponent<ForgeCameraFocus>();

            // 숫돌을 문질러야 하므로 마우스 커서를 보이게 한다.
            _cursorWasVisible = Cursor.visible;
            _cursorLockState = Cursor.lockState;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // 얇은 선과 흠집이 계단처럼 보이지 않도록 하는 동안만 카메라 안티앨리어싱을 켠다.
            _cameraData = camera.GetUniversalAdditionalCameraData();
            _previousAntialiasing = _cameraData.antialiasing;
            _cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            HidePlayer();
            _lens.Bind(_game);
            _view.ShowStart();
            _stage.Begin(_game, _cameraFocus, _view, _lens);
            _view.ShowPrompt(false);
        }

        private void End()
        {
            _game = null;
            _inputLock.Release(this);

            if (_cameraFocus != null)
            {
                _cameraFocus.Release();
                _cameraFocus = null;
            }

            if (_cameraData != null)
            {
                _cameraData.antialiasing = _previousAntialiasing;
                _cameraData = null;
            }

            Cursor.visible = _cursorWasVisible;
            Cursor.lockState = _cursorLockState;

            ShowPlayer();
            _stage.End();
            _lens.Unbind();
            _view.HidePanel();
            _view.ShowPrompt(_localPlayer != null);
        }

        // 내 캐릭터가 작업대와 카메라 사이에 서 있으면 가리므로, 하는 동안 내 화면에서만 숨긴다.
        // 다른 플레이어의 화면에는 그대로 보인다.
        private void HidePlayer()
        {
            _hiddenRenderers = _localPlayer.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in _hiddenRenderers)
            {
                renderer.enabled = false;
            }
        }

        private void ShowPlayer()
        {
            foreach (Renderer renderer in _hiddenRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }
            _hiddenRenderers = Array.Empty<Renderer>();
        }

        private MiniInput ReadInput()
        {
            bool mapped = _lens.TryMap(_point.action.ReadValue<Vector2>(), out float x, out float y);
            return new MiniInput
            {
                ActionPressed = MinigameActionKey.WasPressed(_interact.action),
                ActionHeld = MinigameActionKey.IsHeld(_interact.action),
                ActionReleased = MinigameActionKey.WasReleased(_interact.action),
                PointerPressed = _click.action.WasPressedThisFrame(),
                PointerHeld = _click.action.IsPressed(),
                PointerX = mapped ? x : -100f,
                PointerY = mapped ? y : -100f,
            };
        }

        private void ShowEvents()
        {
            while (_game.Events.Count > 0)
            {
                MiniEvent miniEvent = _game.Events.Dequeue();
                _stage.HandleEvent(miniEvent);
                if (miniEvent.Kind == MiniEventKind.Complete)
                {
                    _view.ShowResult(_game);
                }
            }
        }
    }
}
