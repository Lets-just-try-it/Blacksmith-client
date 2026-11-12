using System;
using Blacksmith.UI;
using Fusion;
using UnityEngine;

namespace Blacksmith.Network
{
    // Player 프리팹에 붙는 구역 이동 컴포넌트다. 화면이 완전히 검어진 뒤 호스트에 이동을 요청하고,
    // 호스트가 옮긴 결과가 도착하면 화면을 다시 밝힌다. 이동하는 동안 캐릭터 조작을 잠근다.
    // 규칙은 docs/NETWORK_DESIGN.md의 "구역 이동"을 따른다.
    public class PlayerAreaTravel : NetworkBehaviour
    {
        [SerializeField] private NetworkCharacterController _controller;
        [SerializeField] private PlayerNetworkInputLock _inputLock;
        [SerializeField] private ScreenFadeChannel _screenFade;

        // 호스트가 옮길 때마다 늘린다. 요청한 클라이언트는 이 값이 바뀌면 도착한 것으로 본다.
        [Networked] private int TravelCount { get; set; }

        private ChangeDetector _changes;
        // 입력 권한을 가진 클라이언트에서만 쓴다.
        private bool _isTraveling;
        // 호스트에서만 쓴다. RPC로 받은 이동을 다음 틱에 적용한다.
        private bool _hasPendingTravel;
        private Vector3 _pendingPosition;
        private Quaternion _pendingRotation;

        public override void Spawned()
        {
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (_isTraveling)
            {
                _isTraveling = false;
                _inputLock.Release(this);
            }
        }

        // 입력 권한을 가진 클라이언트 전용. 이동 중에 다시 불리면 무시한다.
        // onScreenDark는 화면이 다 어두워진 뒤 이동을 요청하기 직전에 불린다. 도착지를 준비하는 무거운 일을 화면이 가려진 동안 한다.
        public async void Travel(Vector3 position, Quaternion rotation, Action onScreenDark = null)
        {
            if (!HasInputAuthority || _isTraveling)
            {
                return;
            }

            _isTraveling = true;
            _inputLock.Lock(this);
            await _screenFade.FadeOut();

            // 화면이 어두워지는 사이 세션이 끝났을 수 있다.
            if (Object == null || !Object.IsValid)
            {
                return;
            }

            onScreenDark?.Invoke();
            RpcRequestTravel(position, rotation);
        }

        // 호스트 전용. 화면이 이미 가려진 동안(전투 씬이 떠 있을 때 등) 페이드 없이 바로 옮긴다.
        public void TravelByHost(Vector3 position, Quaternion rotation)
        {
            if (HasStateAuthority)
            {
                QueueTravel(position, rotation);
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcRequestTravel(Vector3 position, Quaternion rotation)
        {
            QueueTravel(position, rotation);
        }

        private void QueueTravel(Vector3 position, Quaternion rotation)
        {
            _pendingPosition = position;
            _pendingRotation = rotation;
            _hasPendingTravel = true;
        }

        // NetworkCharacterController의 순간이동은 시뮬레이션 안에서 해야 보간이 끊기지 않는다.
        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || !_hasPendingTravel)
            {
                return;
            }

            _hasPendingTravel = false;
            _controller.Teleport(_pendingPosition, _pendingRotation);
            // 포털로 걸어 들어오던 속도가 남으면 도착한 뒤에도 미끄러져 반대편 포털에 다시 닿는다.
            _controller.Velocity = Vector3.zero;
            TravelCount++;
        }

        public override void Render()
        {
            foreach (string change in _changes.DetectChanges(this))
            {
                if (change == nameof(TravelCount) && _isTraveling)
                {
                    Arrive();
                }
            }
        }

        private void Arrive()
        {
            _isTraveling = false;
            _inputLock.Release(this);
            _ = _screenFade.FadeIn();
        }
    }
}
