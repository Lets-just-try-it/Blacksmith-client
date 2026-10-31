using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Blacksmith.Network
{
    // 러너 프리팹에 붙어 로컬 입력 장치를 읽고 PlayerNetworkInput으로 채운다.
    public class PlayerInputCollector : MonoBehaviour, INetworkRunnerCallbacks
    {
        [SerializeField] private InputActionReference _move;
        [SerializeField] private InputActionReference _interact;

        private void OnEnable()
        {
            _move.action.Enable();
            _interact.action.Enable();
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            var data = new PlayerNetworkInput
            {
                MoveDirection = ToWorldDirection(_move.action.ReadValue<Vector2>()),
            };
            data.Buttons.Set(PlayerButton.Interact, _interact.action.IsPressed());
            input.Set(data);
        }

        // 호스트는 이 클라이언트의 카메라를 모르므로, 카메라 기준 방향을 월드 방향으로 바꿔 보낸다.
        private static Vector2 ToWorldDirection(Vector2 move)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return move;
            }

            Vector3 world = Quaternion.Euler(0f, camera.transform.eulerAngles.y, 0f) * new Vector3(move.x, 0f, move.y);
            return new Vector2(world.x, world.z);
        }

        void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }
        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
        void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner) { }
        void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
    }
}
