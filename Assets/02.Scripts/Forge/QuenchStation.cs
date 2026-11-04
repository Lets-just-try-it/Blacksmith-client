using System;
using Blacksmith.Core.Forge;
using Blacksmith.Network;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Blacksmith.Forge
{
    // 담금질 작업대. 내 캐릭터가 범위 안에서 상호작용 키를 누르면 카메라를 작업대로 옮기고 담금질을 진행한다.
    // 미니게임은 이 클라이언트에서만 돌리고 Update에서 시간을 넘긴다. 끝난 정확도는 Completed로 알린다.
    // 작업대 사용권과 결과를 호스트에 보내는 일은 아직 없다.
    [RequireComponent(typeof(BoxCollider))]
    public class QuenchStation : MonoBehaviour
    {
        [SerializeField] private QuenchRulesData _rulesData;
        [SerializeField] private InputActionReference _interact;
        [SerializeField] private Transform _cameraAnchor;
        [SerializeField] private Transform _focusPoint;
        [SerializeField] private QuenchView _view;
        [SerializeField] private QuenchBladeVisual _blade;
        [SerializeField] private QuenchFeedback _feedback;
        [SerializeField] private PlayerNetworkInputLock _inputLock;
        [SerializeField] private ForgeStationLink _link;

        private BoxCollider _range;
        private QuenchRules _rules;
        private QuenchGame _game;
        private NetworkPlayer _localPlayer;
        private ForgeCameraFocus _cameraFocus;
        private bool _hasSplashed;
        private Renderer[] _hiddenRenderers = Array.Empty<Renderer>();

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
            _interact.action.Enable();
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
            ShowState();

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
            _game = new QuenchGame(_rules, Environment.TickCount, _link.Difficulty);
            _inputLock.Lock(this);
            HidePlayer();

            _cameraFocus = Camera.main.GetComponent<ForgeCameraFocus>();
            if (_cameraFocus != null)
            {
                _cameraFocus.Focus(_cameraAnchor.position, _focusPoint.position);
            }

            _blade.SetVisible(true);
            _hasSplashed = false;
            _feedback.Begin(_cameraFocus, _blade.WaterPoint);
            _view.ShowPrompt(false);
            _view.ShowReady(_game.Target);
            _blade.Apply(_game, _rules.CarryTime);
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

            _feedback.End();
            ShowPlayer();
            _blade.SetVisible(false);
            _view.HidePanel();
            _view.ShowPrompt(_localPlayer != null);
        }

        // 내 캐릭터가 카메라 앞을 가리지 않도록, 하는 동안 내 화면에서만 숨긴다. 다른 플레이어의 화면에는 그대로 보인다.
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
                if (miniEvent.Kind == MiniEventKind.PhaseChanged && miniEvent.Index == (int)QuenchGame.PhaseKind.Dunk)
                {
                    _view.ShowDunk();
                    _feedback.PlayCarry();
                }
                else if (miniEvent.Kind == MiniEventKind.SecondTick)
                {
                    _feedback.PlaySecond((int)miniEvent.Value);
                }
                else if (miniEvent.Kind == MiniEventKind.Pull)
                {
                    _view.ShowResult(_game);
                    _feedback.PlayPull(_game.TimedOut);
                    _feedback.PlayResult(_game.Accuracy, !_game.TimedOut && _game.Accuracy >= _rules.PerfectEventAccuracy, _game.TimedOut);
                }
                else if (miniEvent.Kind == MiniEventKind.Perfect)
                {
                    _view.MarkPerfect();
                }
            }
        }

        private void ShowState()
        {
            if (_game.Phase == QuenchGame.PhaseKind.Dunk)
            {
                _view.ShowTime(_game.Elapsed, _game.IsCountVisible);

                // 칼날이 수조까지 옮겨져 물에 닿는 순간
                if (!_hasSplashed && _game.PhaseClock >= _rules.CarryTime)
                {
                    _hasSplashed = true;
                    _feedback.PlaySplash();
                }
            }
            _blade.Apply(_game, _rules.CarryTime);
        }
    }
}
