using System;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Blacksmith.Network
{
    // 로비에서 세션을 시작한다. 혼자 플레이도 참가자가 없는 Host로 시작하며 GameMode.Single은 쓰지 않는다.
    public class NetworkBootstrap : MonoBehaviour
    {
        [SerializeField] private NetworkRunner _runnerPrefab;
        [SerializeField] private string _gameScenePath = "Assets/01.Scenes/Game.unity";
        [Tooltip("호스트를 포함한 최대 참가 인원")]
        [SerializeField, Min(1)] private int _maxPlayers = 4;

        private NetworkRunner _runner;

        public event Action<string> StatusChanged;

        public Task<bool> StartHost(string sessionName)
        {
            return StartSession(GameMode.Host, sessionName);
        }

        public Task<bool> StartClient(string sessionName)
        {
            return StartSession(GameMode.Client, sessionName);
        }

        private async Task<bool> StartSession(GameMode mode, string sessionName)
        {
            if (_runner != null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(sessionName))
            {
                StatusChanged?.Invoke("Enter a session name.");
                return false;
            }

            int gameSceneIndex = SceneUtility.GetBuildIndexByScenePath(_gameScenePath);
            if (gameSceneIndex < 0)
            {
                Debug.LogError($"빌드 설정에 Game 씬이 없다: {_gameScenePath}");
                return false;
            }

            _runner = Instantiate(_runnerPrefab);
            DontDestroyOnLoad(_runner.gameObject);
            _runner.ProvideInput = true;

            StatusChanged?.Invoke(mode == GameMode.Host ? "Starting host..." : "Joining session...");
            StartGameResult result = await _runner.StartGame(new StartGameArgs
            {
                GameMode = mode,
                SessionName = sessionName.Trim(),
                PlayerCount = _maxPlayers,
                Scene = CreateSceneInfo(mode, gameSceneIndex),
                SceneManager = _runner.GetComponent<INetworkSceneManager>(),
                ObjectProvider = _runner.GetComponent<INetworkObjectProvider>(),
            });

            // 성공하면 Game 씬으로 바뀌며 이 오브젝트는 사라진다. 실패하면 러너는 스스로 종료된다.
            if (result.Ok)
            {
                return true;
            }

            _runner = null;
            StatusChanged?.Invoke($"Failed: {result.ShutdownReason}");
            return false;
        }

        // 씬은 호스트가 정하고, 클라이언트는 접속하면 호스트의 씬을 따라간다.
        private static NetworkSceneInfo CreateSceneInfo(GameMode mode, int gameSceneIndex)
        {
            var sceneInfo = new NetworkSceneInfo();
            if (mode == GameMode.Host)
            {
                sceneInfo.AddSceneRef(SceneRef.FromIndex(gameSceneIndex), LoadSceneMode.Single);
            }

            return sceneInfo;
        }
    }
}
