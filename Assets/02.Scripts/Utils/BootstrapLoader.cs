using UnityEngine;
using UnityEngine.SceneManagement;

namespace Blacksmith.Utils
{
    // 앱 시작 씬이다. 지금은 미리 불러올 리소스가 없어 바로 로비로 넘어간다.
    public class BootstrapLoader : MonoBehaviour
    {
        [SerializeField] private string _lobbyScenePath = "Assets/01.Scenes/Lobby.unity";

        private void Start()
        {
            SceneManager.LoadScene(_lobbyScenePath);
        }
    }
}
