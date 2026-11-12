using System;
using Blacksmith.Core.Forge;
using Blacksmith.Network;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Blacksmith.Forge
{
    // 활 휘기, 마석 박기처럼 같은 틀을 쓰는 작업대의 공통 동작. 내 캐릭터가 범위 안에서 상호작용 키를 누르면 카메라를 작업대로 옮기고 미니게임을 진행한다.
    // 서브클래스는 게임을 만들고 화면 글자와 효과를 채운다.
    // 미니게임은 이 클라이언트에서만 돌리고 Update에서 시간을 넘긴다. 끝나면 정확도를 호스트에 보낸다.
    [RequireComponent(typeof(BoxCollider))]
    public abstract class MiniStation<TGame> : MonoBehaviour where TGame : MiniGame
    {
        [SerializeField] private InputActionReference _interact;
        [SerializeField] private InputActionReference _point;
        [SerializeField] private InputActionReference _click;
        [SerializeField] private MiniPanelView _view;
        [SerializeField] private MiniBoardView _board;
        [SerializeField] private MiniStage _stage;
        [SerializeField] private PlayerNetworkInputLock _inputLock;
        [SerializeField] private ForgeStationLink _link;

        private BoxCollider _range;
        private TGame _game;
        private NetworkPlayer _localPlayer;
        private ForgeCameraFocus _cameraFocus;
        private UniversalAdditionalCameraData _cameraData;
        private AntialiasingMode _previousAntialiasing;
        private Renderer[] _hiddenRenderers = Array.Empty<Renderer>();
        private bool _cursorWasVisible;
        private CursorLockMode _cursorLockState;

        protected MiniStage Stage => _stage;
        protected MiniBoardView Board => _board;

        public event Action<float> Completed;

        private void Awake()
        {
            _range = GetComponent<BoxCollider>();
            OnAwake();
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

            _game.Tick(Time.deltaTime, ReadInput());
            ShowEvents();
            _board.Tick(Time.deltaTime);
            Refresh(_game, _view);

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
            _game = CreateGame(Environment.TickCount, _link.Difficulty);
            _inputLock.Lock(this);

            Camera camera = Camera.main;
            _cameraFocus = camera.GetComponent<ForgeCameraFocus>();

            _cursorWasVisible = Cursor.visible;
            _cursorLockState = Cursor.lockState;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // 얇은 외곽선과 점선이 계단처럼 보이지 않도록 하는 동안만 카메라 안티앨리어싱을 켠다.
            _cameraData = camera.GetUniversalAdditionalCameraData();
            _previousAntialiasing = _cameraData.antialiasing;
            _cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            HidePlayer();
            _board.Bind(_game);
            _view.ShowStart(Objective);
            _stage.Begin(_cameraFocus);
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
            _board.Unbind();
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
            bool mapped = _board.TryMap(_point.action.ReadValue<Vector2>(), out float x, out float y);
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
                HandleEvent(_game, miniEvent, _stage, _view, _board);
                if (miniEvent.Kind == MiniEventKind.Complete)
                {
                    _view.ShowResult(ResultText(_game));
                }
            }
        }

        protected virtual void OnAwake()
        {
        }

        // 이번 제작의 재료 티어가 정한 난이도로 게임을 만든다. 시드는 호출한 쪽이 정한다.
        protected abstract TGame CreateGame(int seed, int difficulty);

        // 시작할 때 방법 창에 보여 줄 안내 글
        protected abstract string Objective { get; }

        // 매 프레임 글자와 게이지를 채운다.
        protected abstract void Refresh(TGame game, MiniPanelView view);

        // 게임이 알려 주는 사건마다 소리·말풍선·흔들림을 만든다.
        protected abstract void HandleEvent(TGame game, MiniEvent miniEvent, MiniStage stage, MiniPanelView view, MiniBoardView board);

        protected abstract string ResultText(TGame game);
    }
}
