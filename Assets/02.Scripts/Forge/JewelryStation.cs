using System;
using Blacksmith.Core.Forge;
using Blacksmith.Network;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Blacksmith.Forge
{
    // 장신구대(보석 작업대). 내 캐릭터가 범위 안에서 상호작용 키를 누르면 제작 목록이 열리고, 제작을 누르면 장신구 미니게임을 진행한다.
    // 체인 잇기 → 보석 깎기 → 장착 → 광내기 네 단계를 한 번에 하고, 끝나면 네 단계 점수의 평균이 정확도가 된다.
    // 미니게임은 이 클라이언트에서만 돌리고 Update에서 시간을 넘긴다. 끝나면 정확도를 호스트에 보낸다.
    [RequireComponent(typeof(BoxCollider))]
    public class JewelryStation : MonoBehaviour
    {
        [SerializeField] private NecklaceRulesData _rulesData;
        [SerializeField] private InputActionReference _interact;
        [SerializeField] private InputActionReference _point;
        [SerializeField] private InputActionReference _click;
        [SerializeField] private NecklaceView _view;
        [SerializeField] private NecklaceBoardView _board;
        [SerializeField] private AudioSource _sfx;
        [SerializeField] private PlayerNetworkInputLock _inputLock;
        [SerializeField] private ForgeStationLink _link;

        private BoxCollider _range;
        private NecklaceRules _rules;
        private NecklaceGame _game;
        private NetworkPlayer _localPlayer;
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

        // 제작 목록이 열려 있는 동안에는 "[E] 키를 눌러 열기" 안내를 숨긴다.
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
            _game = new NecklaceGame(_rules, Environment.TickCount, _link.Difficulty);
            _inputLock.Lock(this);

            // 보석 그림은 제작 항목이 쓰는 보석의 색으로 칠한다.
            Color gemColor = _link.Entry != null && _link.Entry.Step.Input != null ? _link.Entry.Step.Input.Color : new Color(0.9f, 0.3f, 0.35f);

            // 마우스로 조각을 집고 보석을 끌어야 하므로 커서를 보이게 한다.
            _cursorWasVisible = Cursor.visible;
            _cursorLockState = Cursor.lockState;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // 얇은 선과 점선이 계단처럼 보이지 않도록 하는 동안만 카메라 안티앨리어싱을 켠다.
            Camera camera = Camera.main;
            _cameraData = camera.GetUniversalAdditionalCameraData();
            _previousAntialiasing = _cameraData.antialiasing;
            _cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            HidePlayer();
            _board.Bind(_game, gemColor);
            _view.ShowStart();
            _view.ShowPrompt(false);
        }

        private void End()
        {
            _game = null;
            _inputLock.Release(this);

            if (_cameraData != null)
            {
                _cameraData.antialiasing = _previousAntialiasing;
                _cameraData = null;
            }

            Cursor.visible = _cursorWasVisible;
            Cursor.lockState = _cursorLockState;

            ShowPlayer();
            _board.Unbind();
            _view.HidePanel();
            _view.ShowPrompt(_localPlayer != null && !_link.IsOpen);
        }

        // 내 캐릭터가 화면을 가리지 않도록 하는 동안 내 화면에서만 숨긴다. 다른 플레이어의 화면에는 그대로 보인다.
        private void HidePlayer()
        {
            if (_localPlayer == null)
            {
                return;
            }

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
            int arrow = MinigameKeys.ArrowPressed();
            int tool = MinigameKeys.ToolPressed();
            int arrowButton = _view.ConsumeArrow();
            int toolButton = _view.ConsumeTool();
            bool flipButton = _view.ConsumeFlip();
            return new MiniInput
            {
                ActionPressed = MinigameActionKey.WasPressed(_interact.action),
                ActionHeld = MinigameActionKey.IsHeld(_interact.action),
                ActionReleased = MinigameActionKey.WasReleased(_interact.action),
                PointerPressed = _click.action.WasPressedThisFrame(),
                PointerHeld = _click.action.IsPressed(),
                PointerX = mapped ? x : NecklaceGame.NoTarget,
                PointerY = mapped ? y : NecklaceGame.NoTarget,
                SecondaryPressed = MinigameKeys.SecondaryPressed() || flipButton,
                Arrow = arrow != 0 ? arrow : arrowButton,
                ToolNumber = tool != 0 ? tool : toolButton,
                FinishPressed = MinigameKeys.FinishPressed(),
            };
        }

        private void ShowEvents()
        {
            while (_game.Events.Count > 0)
            {
                MiniEvent miniEvent = _game.Events.Dequeue();
                HandleEvent(miniEvent);
                if (miniEvent.Kind == MiniEventKind.Complete)
                {
                    _view.ShowResult(_game);
                }
            }
        }

        // ------------------------------------------------------------------ 소리와 글자

        private void HandleEvent(MiniEvent e)
        {
            Vector2 top = NecklaceBoardView.ToLocal(new Vector2(NecklaceBoardView.CanvasWidth * 0.5f, 40f));
            Color good = new Color(0.7f, 1f, 0.75f);
            Color warn = new Color(1f, 0.55f, 0.45f);
            switch (e.Kind)
            {
                case MiniEventKind.Good:
                    HandleGood(e, top, good);
                    break;
                case MiniEventKind.Hit:
                    Play(ForgeProceduralSfx.Ding(740f + 60f * Mathf.Min(8f, e.Value)), 0.5f, 1f);
                    break;
                case MiniEventKind.Miss:
                    Play(ForgeProceduralSfx.Fail(), 0.4f, 1.3f);
                    _view.Flash(0.08f, new Color(1f, 0.3f, 0.25f));
                    if (_game.Step == 0)
                    {
                        _view.Popup("어긋난 연결!  (실수 " + Mathf.RoundToInt(e.Value) + "회)", warn, top);
                    }
                    else if (_game.Step == 1)
                    {
                        _view.Popup("너무 깎았다!  되돌릴 수 없다", warn, top);
                    }

                    break;
                case MiniEventKind.Charge:
                    Play(ForgeProceduralSfx.Clang(1), 0.35f, 1.5f);
                    break;
                case MiniEventKind.Spawn:
                    Play(ForgeProceduralSfx.Tick(), 0.4f, 1.1f);
                    break;
                case MiniEventKind.Tick:
                    Play(ForgeProceduralSfx.Tick(), 0.35f, 0.9f);
                    break;
                case MiniEventKind.Warn:
                    Play(ForgeProceduralSfx.Fail(), 0.35f, 1.4f);
                    _view.Popup(_game.Step == 0 ? "이미 조각이 있는 칸이다. 먼저 그 조각을 집어 빼자" : "맞지 않은 채로 눌렀다! 보석이 상한다", new Color(1f, 0.75f, 0.45f), top);
                    break;
                case MiniEventKind.Beat:
                    HandleBeat(e, top);
                    break;
                case MiniEventKind.PhaseChanged:
                    Play(ForgeProceduralSfx.Tick(), 0.4f, 0.8f);
                    break;
                case MiniEventKind.Perfect:
                    _board.Burst(_board.BurstPoint(2), 40, Color.white, 320f);
                    Play(ForgeProceduralSfx.Chime(1320f), 0.7f, 1f);
                    _view.Popup("딱 박혔다!", good, top);
                    break;
                case MiniEventKind.Complete:
                    Play(e.Value >= _game.SuccessScore ? ForgeProceduralSfx.Success() : ForgeProceduralSfx.Fail(), 0.7f, 1f);
                    break;
            }
        }

        private void HandleGood(MiniEvent e, Vector2 top, Color good)
        {
            if (_game.Step == 1)
            {
                Play(ForgeProceduralSfx.Ding(880f), 0.5f, 1f);
                _view.Popup("딱 맞게 깎았다!", good, top);
            }
            else if (_game.Step == 2)
            {
                Play(ForgeProceduralSfx.Ding(880f), 0.5f, 1f);
                _view.Popup("딱 맞았다!  꾹 눌러 박자", good, top);
            }
            else if (_game.Step == 3 && e.Index >= 0 && e.Index < _game.TargetFacets.Count)
            {
                _board.Burst(_board.FacetCenter(e.Index), 14, new Color(1f, 1f, 0.9f), 220f);
                Play(ForgeProceduralSfx.Ding(780f + 40f * _game.ShinedCount), 0.5f, 1f);
            }
        }

        private void HandleBeat(MiniEvent e, Vector2 top)
        {
            int step = Mathf.Clamp(e.Index, 0, 3);
            Play(ForgeProceduralSfx.Chime(880f + 110f * step), 0.6f, 1f);
            _view.Popup(NecklaceGame.StepNames[step] + " 완료  " + Mathf.RoundToInt(e.Value * 100f) + "%", Color.white, top);
            _board.Burst(_board.BurstPoint(step), 30, new Color(1f, 0.9f, 0.7f), 300f);
            _view.Flash(0.05f, new Color(1f, 0.95f, 0.7f));
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
    }
}
