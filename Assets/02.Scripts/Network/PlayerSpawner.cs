using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Blacksmith.Network
{
    // Game 씬 오브젝트다. 호스트만 플레이어 오브젝트를 스폰하고 지운다.
    public class PlayerSpawner : NetworkBehaviour, IPlayerJoined, IPlayerLeft
    {
        [SerializeField] private NetworkObject _playerPrefab;
        [SerializeField] private Transform[] _spawnPoints;

        private readonly Dictionary<PlayerRef, NetworkObject> _players = new Dictionary<PlayerRef, NetworkObject>();

        // 호스트 본인의 PlayerJoined는 Game 씬이 로드되기 전에 지나가므로, 이미 접속한 플레이어도 여기서 스폰한다.
        public override void Spawned()
        {
            if (!HasStateAuthority)
            {
                return;
            }

            foreach (PlayerRef player in Runner.ActivePlayers)
            {
                SpawnPlayer(player);
            }
        }

        public void PlayerJoined(PlayerRef player)
        {
            if (HasStateAuthority)
            {
                SpawnPlayer(player);
            }
        }

        public void PlayerLeft(PlayerRef player)
        {
            if (!HasStateAuthority || !_players.Remove(player, out NetworkObject playerObject))
            {
                return;
            }

            Runner.Despawn(playerObject);
        }

        private void SpawnPlayer(PlayerRef player)
        {
            if (_players.ContainsKey(player))
            {
                return;
            }

            Transform spawnPoint = _spawnPoints[_players.Count % _spawnPoints.Length];
            NetworkObject playerObject = Runner.Spawn(_playerPrefab, spawnPoint.position, spawnPoint.rotation, player);
            _players.Add(player, playerObject);
        }
    }
}
