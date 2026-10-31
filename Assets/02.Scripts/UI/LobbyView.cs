using System;
using System.Threading.Tasks;
using Blacksmith.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.UI
{
    // 한글 폰트 에셋이 추가되기 전까지 문구는 영어로 둔다.
    public class LobbyView : MonoBehaviour
    {
        [SerializeField] private NetworkBootstrap _bootstrap;
        [SerializeField] private TMP_InputField _sessionNameInput;
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private TMP_Text _statusText;

        private void Awake()
        {
            _hostButton.onClick.AddListener(HandleHostClicked);
            _joinButton.onClick.AddListener(HandleJoinClicked);
            _bootstrap.StatusChanged += ShowStatus;
            _statusText.text = string.Empty;
        }

        private void OnDestroy()
        {
            _hostButton.onClick.RemoveListener(HandleHostClicked);
            _joinButton.onClick.RemoveListener(HandleJoinClicked);
            _bootstrap.StatusChanged -= ShowStatus;
        }

        private async void HandleHostClicked()
        {
            await StartSession(_bootstrap.StartHost);
        }

        private async void HandleJoinClicked()
        {
            await StartSession(_bootstrap.StartClient);
        }

        private async Task StartSession(Func<string, Task<bool>> start)
        {
            SetButtonsInteractable(false);
            bool started = await start(_sessionNameInput.text);

            // 성공하면 Game 씬으로 넘어가며 이 화면은 사라진다.
            if (!started && this != null)
            {
                SetButtonsInteractable(true);
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            _hostButton.interactable = interactable;
            _joinButton.interactable = interactable;
        }

        private void ShowStatus(string message)
        {
            _statusText.text = message;
        }
    }
}
