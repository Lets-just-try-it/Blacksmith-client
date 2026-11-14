using Fusion;
using UnityEngine;

namespace Blacksmith.Dialogue
{
    // 말을 걸 수 있는 NPC에 붙는다. 한 번에 한 명만 대화할 수 있고, 누가 대화 중인지는 호스트가 관리한다.
    // 고른 선택지의 결과는 같은 오브젝트의 IDialogueChoiceHandler가 처리한다.
    public class DialogueSeat : NetworkBehaviour, IPlayerLeft
    {
        [SerializeField] private DialogueData _dialogue;
        [SerializeField] private DialogueChannel _channel;
        [Tooltip("내 캐릭터가 가장 가까이 있고 비어 있을 때 띄우는 상호작용 안내")]
        [SerializeField] private GameObject _prompt;
        [Tooltip("다른 플레이어가 대화 중일 때 띄우는 표시")]
        [SerializeField] private GameObject _busyMark;

        private IDialogueChoiceHandler _choiceHandler;
        private IDialogueSource _source;

        [Networked] public PlayerRef Talker { get; private set; }

        // 스폰 전이나 디스폰 후에는 [Networked] 값을 읽을 수 없다.
        public bool IsSpawned => Object != null && Object.IsValid;
        public DialogueData Dialogue => _source?.GetDialogue() ?? _dialogue;
        public bool IsBusy => Talker != PlayerRef.None;
        // 화자가 비어 있는 대사에 쓰는 이름이다. 각 PC가 복제된 값으로 직접 채운다.
        public string SpeakerName { get; set; } = "";

        public override void Spawned()
        {
            TryGetComponent(out _choiceHandler);
            TryGetComponent(out _source);
            SetPromptVisible(false);
            _busyMark.SetActive(false);
        }

        public override void Render()
        {
            _busyMark.SetActive(IsBusy && Talker != Runner.LocalPlayer);
        }

        public void SetPromptVisible(bool isVisible)
        {
            _prompt.SetActive(isVisible);
        }

        // 호스트 전용. 비어 있을 때만 성공한다.
        public bool TryOccupy(PlayerRef player)
        {
            if (!HasStateAuthority || IsBusy)
            {
                return false;
            }

            Talker = player;
            return true;
        }

        // 호스트 전용. 대화 중인 플레이어가 아니면 무시한다.
        public void Release(PlayerRef player)
        {
            if (HasStateAuthority && Talker == player)
            {
                Talker = PlayerRef.None;
            }
        }

        // 호스트 전용. 화면을 띄운 뒤 상태가 바뀌었을 수 있으므로 화면과 같은 조건 함수로 다시 확인한다.
        public void Choose(PlayerRef player, int choiceIndex)
        {
            DialogueData dialogue = Dialogue;
            if (!HasStateAuthority || Talker != player
                || choiceIndex < 0 || choiceIndex >= dialogue.Choices.Count)
            {
                return;
            }

            // 결과 처리(예: 영입되어 디스폰)보다 먼저 풀어 둔다.
            Release(player);
            if (_channel.Stats == null || !dialogue.IsChoiceEnabled(choiceIndex, _channel.Stats))
            {
                return;
            }

            string actionId = dialogue.Choices[choiceIndex].ActionId;
            if (!string.IsNullOrEmpty(actionId) && _choiceHandler != null)
            {
                _choiceHandler.OnDialogueChoice(actionId, player);
            }
        }

        public void PlayerLeft(PlayerRef player)
        {
            Release(player);
        }
    }
}
