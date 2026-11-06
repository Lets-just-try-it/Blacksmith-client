using System;
using Blacksmith.Core.Forge;
using Blacksmith.Network;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Blacksmith.Forge
{
    // 단조 작업대. 내 캐릭터가 범위 안에서 상호작용 키를 누르면 카메라를 작업대로 옮기고 단조를 진행한다.
    // 미니게임은 이 클라이언트에서만 돌리고 Update에서 시간을 넘긴다. 끝난 정확도는 Completed로 알린다.
    // 작업대 사용권과 결과를 호스트에 보내는 일은 아직 없다.
    [RequireComponent(typeof(BoxCollider))]
    public class ForgingStation : MonoBehaviour
    {
        [SerializeField] private ForgingRulesData _rulesData;
        [SerializeField] private InputActionReference _interact;
        [SerializeField] private Transform _cameraAnchor;
        [SerializeField] private Transform _focusPoint;
        [SerializeField] private ForgingView _view;
        [SerializeField] private ForgingBladeVisual _blade;
        [SerializeField] private ForgingFeedback _feedback;
        [SerializeField] private PlayerNetworkInputLock _inputLock;
        [SerializeField] private ForgeStationLink _link;

        private BoxCollider _range;
        private ForgingRules _rules;
        private ForgingGame _game;
        private NetworkPlayer _localPlayer;
        private ForgeCameraFocus _cameraFocus;
        private Renderer[] _hiddenRenderers = Array.Empty<Renderer>();
        private float _freezeUntil;
        private bool _bufferedPress;
        private bool _bufferedRelease;

        public event Action<float> Completed;

        private void Awake()
        {
            _range = GetComponent<BoxCollider>();
            _rules = _rulesData.ToRules();
            _blade.SetVisible(false);
        }

        private void OnEnable()
        {
            _link.Approved += Begin;
            _link.Opened += OnMenuOpened;
            _link.Closed += OnMenuClosed;
            _interact.action.Enable();
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

            TickGame();
            ShowEvents();
            _view.Refresh(_game);
            _blade.Tick(Time.deltaTime);
            _feedback.UpdateCharge(_game.Holding != null, _game.HoldProgress);
            _feedback.Tick(Time.deltaTime, _blade.Power);

            if (_game.Finished)
            {
                float accuracy = _game.Accuracy;
                End();
                _link.SubmitResult(accuracy);
                Completed?.Invoke(accuracy);
            }
        }

        // 맞는 순간 규칙 시간을 아주 잠깐 멈춰 타격감을 준다. 멈춘 동안 누르거나 뗀 입력은 모아 뒀다가 다음 틱에 넣는다.
        private void TickGame()
        {
            MiniInput input = ReadInput();
            if (Time.unscaledTime < _freezeUntil)
            {
                _bufferedPress |= input.ActionPressed;
                _bufferedRelease |= input.ActionReleased;
                return;
            }

            input.ActionPressed |= _bufferedPress;
            input.ActionReleased |= _bufferedRelease;
            _bufferedPress = false;
            _bufferedRelease = false;
            _game.Tick(Time.deltaTime, input);
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
            _game = new ForgingGame(_rules, Environment.TickCount, _link.Difficulty);
            _inputLock.Lock(this);

            _cameraFocus = Camera.main.GetComponent<ForgeCameraFocus>();
            if (_cameraFocus != null)
            {
                _cameraFocus.Focus(_cameraAnchor.position, _focusPoint.position);
            }

            _freezeUntil = 0f;
            _bufferedPress = false;
            _bufferedRelease = false;
            HidePlayer();
            _feedback.Begin(_cameraFocus, _view);
            _blade.SetVisible(true);
            _blade.Bind(_game);
            _view.ShowPrompt(false);
            _view.ShowStart(_game, _rules);
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

            ShowPlayer();
            _feedback.End();
            _blade.SetVisible(false);
            _view.HidePanel();
            _view.ShowPrompt(_localPlayer != null && !_link.IsOpen);
        }

        // 내 캐릭터가 작업대와 카메라 사이에 서 있으면 칼날과 망치를 가리므로, 하는 동안 내 화면에서만 숨긴다.
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
            return new MiniInput
            {
                ActionPressed = MinigameActionKey.WasPressed(action),
                ActionHeld = MinigameActionKey.IsHeld(action),
                ActionReleased = MinigameActionKey.WasReleased(action),
            };
        }

        private void ShowEvents()
        {
            while (_game.Events.Count > 0)
            {
                MiniEvent miniEvent = _game.Events.Dequeue();
                switch (miniEvent.Kind)
                {
                    case MiniEventKind.Charge:
                        _feedback.PlayCharge();
                        break;
                    case MiniEventKind.Swing:
                        _blade.StartSwing();
                        break;
                    case MiniEventKind.Perfect:
                    case MiniEventKind.Good:
                    case MiniEventKind.Hit:
                        ShowHit(miniEvent);
                        break;
                    case MiniEventKind.Miss:
                        ShowMiss(miniEvent);
                        break;
                    case MiniEventKind.Complete:
                        _view.ShowResult(miniEvent.Value, _game.MeanOffset);
                        _feedback.PlayEnd(miniEvent.Value);
                        break;
                }
            }
        }

        private void ShowHit(MiniEvent miniEvent)
        {
            float quality = QualityOf(miniEvent.Kind);
            bool heavy = miniEvent.Value >= 2f;
            float strength = ForgingFeedback.Strength(quality, heavy, _blade.Power);

            _blade.Impact(miniEvent.Index, quality, strength);
            _view.ShowJudgement(miniEvent, _game);
            float hitStop = _feedback.PlayImpact(_blade.ImpactPoint(miniEvent.Index), miniEvent.Index, quality, heavy, _blade.Power);
            _freezeUntil = Mathf.Max(_freezeUntil, Time.unscaledTime + hitStop);
        }

        private void ShowMiss(MiniEvent miniEvent)
        {
            // 허공을 친 경우는 Value가 음수이고, 놓친 박자는 내려치던 동작을 멈춘다.
            if (miniEvent.Value < 0f)
            {
                _feedback.PlayAirSwing();
            }
            else
            {
                _blade.CancelSwing();
            }
            _view.ShowJudgement(miniEvent, _game);
        }

        private float QualityOf(MiniEventKind kind)
        {
            switch (kind)
            {
                case MiniEventKind.Perfect:
                    return _rules.PerfectQuality;
                case MiniEventKind.Good:
                    return _rules.GoodQuality;
                default:
                    return _rules.WeakQuality;
            }
        }
    }
}
