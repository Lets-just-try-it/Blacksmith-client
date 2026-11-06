using System;
using Blacksmith.Core.Forge;
using Blacksmith.Network;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Blacksmith.Forge
{
    // 정제 작업대. 내 캐릭터가 범위 안에서 상호작용 키를 누르면 카메라를 작업대로 옮기고 정제를 진행한다.
    // ① 원석 쪼개기(마우스 클릭) → ② 풀무질(행동 키 또는 마우스 꾹) → ③ 쇳물 붓기(마우스로 도가니를 옮기고 클릭한 채 올림, 행동 키로 마무리).
    // 미니게임은 이 클라이언트에서만 돌리고 Update에서 시간을 넘긴다. 끝난 정확도는 Completed로 알린다.
    // 작업대 사용권과 결과를 호스트에 보내는 일은 아직 없다.
    [RequireComponent(typeof(BoxCollider))]
    public class SmeltingStation : MonoBehaviour
    {
        [SerializeField] private SmeltingRulesData _rulesData;
        [SerializeField] private InputActionReference _interact;
        [SerializeField] private InputActionReference _point;
        [SerializeField] private InputActionReference _click;
        [SerializeField] private SmeltingView _view;
        [SerializeField] private SmeltingStage _stage;
        [SerializeField] private PlayerNetworkInputLock _inputLock;
        [SerializeField] private ForgeStationLink _link;

        private BoxCollider _range;
        private SmeltingRules _rules;
        private SmeltingGame _game;
        private NetworkPlayer _localPlayer;
        private ForgeCameraFocus _cameraFocus;
        private Camera _camera;
        private Renderer[] _hiddenRenderers = Array.Empty<Renderer>();
        private bool _cursorWasVisible;
        private CursorLockMode _cursorLockState;
        private Vector2 _arenaPointer;
        private UniversalAdditionalCameraData _cameraData;
        private AntialiasingMode _previousAntialiasing;

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
            _link.Opened += OnMenuOpened;
            _link.Closed += OnMenuClosed;
            _interact.action.Enable();
            _point.action.Enable();
            _click.action.Enable();
        }

        private void OnDisable()
        {
            _link.Approved -= Begin;
            _link.Opened -= OnMenuOpened;
            _link.Closed -= OnMenuClosed;
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
            _view.ShowPrompt(_game == null && !_link.IsOpen);
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
                if (_localPlayer != null && !_link.IsOpen && !IsInRange(_localPlayer.transform.position))
                {
                    LeaveRange();
                }

                if (_localPlayer != null && !_link.IsOpen && _interact.action.WasPressedThisFrame())
                {
                    _link.RequestOpen();
                }
                return;
            }

            MiniInput input = ReadInput();
            _game.Tick(Time.deltaTime, input);
            ShowEvents();
            _stage.Tick(Time.deltaTime, _arenaPointer);
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

        // 제작 목록이 열려 있는 동안에는 "[E] 키를 눌러 시작" 안내를 숨긴다.
        private void OnMenuOpened()
        {
            _view.ShowPrompt(false);
        }

        private void OnMenuClosed()
        {
            _view.ShowPrompt(_localPlayer != null && _game == null);
        }

        private void Begin()
        {
            _game = new SmeltingGame(_rules, Environment.TickCount, _link.Difficulty);
            _inputLock.Lock(this);

            _camera = Camera.main;
            _cameraFocus = _camera.GetComponent<ForgeCameraFocus>();

            // 균열을 클릭하고 도가니를 쥐어야 하므로 마우스 커서를 보이게 한다.
            _cursorWasVisible = Cursor.visible;
            _cursorLockState = Cursor.lockState;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // 얇은 고리와 선이 계단처럼 보이지 않도록 하는 동안만 카메라 안티앨리어싱을 켠다.
            _cameraData = _camera.GetUniversalAdditionalCameraData();
            _previousAntialiasing = _cameraData.antialiasing;
            _cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            HidePlayer();
            _arenaPointer = Vector2.zero;
            _stage.Begin(_game, _cameraFocus, _view);
            _view.ShowPrompt(false);
            _view.ShowStart(_camera);
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

            Cursor.visible = _cursorWasVisible;
            Cursor.lockState = _cursorLockState;

            if (_cameraData != null)
            {
                _cameraData.antialiasing = _previousAntialiasing;
                _cameraData = null;
            }

            ShowPlayer();
            _stage.End();
            _view.HidePanel();
            _view.ShowPrompt(_localPlayer != null && !_link.IsOpen);
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
            InputAction action = _interact.action;
            bool held = _click.action.IsPressed();
            _stage.TryMapPointer(_camera, _point.action.ReadValue<Vector2>(), held, out float x, out float y);
            if (_game.Act == SmeltingGame.ActKind.Crack && x > -50f)
            {
                _arenaPointer = new Vector2(x, y);
            }

            return new MiniInput
            {
                ActionPressed = MinigameActionKey.WasPressed(action),
                ActionHeld = MinigameActionKey.IsHeld(action),
                ActionReleased = MinigameActionKey.WasReleased(action),
                PointerPressed = _click.action.WasPressedThisFrame(),
                PointerHeld = held,
                PointerX = x,
                PointerY = y,
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
