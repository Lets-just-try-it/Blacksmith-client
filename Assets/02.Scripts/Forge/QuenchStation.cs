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
        [SerializeField, Min(0)] private int _difficulty;
        [SerializeField] private InputActionReference _interact;
        [SerializeField] private Transform _cameraAnchor;
        [SerializeField] private Transform _focusPoint;
        [SerializeField] private QuenchView _view;
        [SerializeField] private QuenchBladeVisual _blade;

        private BoxCollider _range;
        private QuenchRules _rules;
        private QuenchGame _game;
        private NetworkPlayer _localPlayer;
        private PlayerInputCollector _collector;
        private ForgeCameraFocus _cameraFocus;

        public event Action<float> Completed;

        private void Awake()
        {
            _range = GetComponent<BoxCollider>();
            _rules = _rulesData.ToRules();
            _blade.SetVisible(false);
        }

        private void OnEnable()
        {
            _interact.action.Enable();
        }

        private void OnDisable()
        {
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
                    Begin();
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
            _game = new QuenchGame(_rules, Environment.TickCount, _difficulty);

            _collector = _localPlayer.Runner.GetComponent<PlayerInputCollector>();
            if (_collector != null)
            {
                _collector.MovementLocked = true;
            }

            _cameraFocus = Camera.main.GetComponent<ForgeCameraFocus>();
            if (_cameraFocus != null)
            {
                _cameraFocus.Focus(_cameraAnchor.position, _focusPoint.position);
            }

            _blade.SetVisible(true);
            _view.ShowPrompt(false);
            _view.ShowReady(_game.Target);
            _blade.Apply(_game, _rules.CarryTime);
        }

        private void End()
        {
            _game = null;
            if (_collector != null)
            {
                _collector.MovementLocked = false;
                _collector = null;
            }

            if (_cameraFocus != null)
            {
                _cameraFocus.Release();
                _cameraFocus = null;
            }

            _blade.SetVisible(false);
            _view.HidePanel();
            _view.ShowPrompt(_localPlayer != null);
        }

        private MiniInput ReadInput()
        {
            InputAction action = _interact.action;
            return new MiniInput
            {
                ActionPressed = action.WasPressedThisFrame(),
                ActionHeld = action.IsPressed(),
                ActionReleased = action.WasReleasedThisFrame(),
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
                }
                else if (miniEvent.Kind == MiniEventKind.Pull)
                {
                    _view.ShowResult(_game);
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
            }
            _blade.Apply(_game, _rules.CarryTime);
        }
    }
}
