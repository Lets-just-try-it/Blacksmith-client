using Blacksmith.Battle;
using Blacksmith.Characters;
using Blacksmith.Core.Adventurers;
using Blacksmith.Dialogue;
using Blacksmith.Inventory;
using Fusion;
using TMPro;
using UnityEngine;

namespace Blacksmith.Plaza
{
    // 여관에 머무는 모험가 한 명의 모습이다. 호스트가 InnResidents로 관리 목록을 따라 스폰·디스폰한다.
    // 관리 목록이 바뀌면(장비, 레벨) 호스트가 Data를 갱신하고, 모든 참가자가 이름표와 외형을 다시 그린다.
    // 말을 걸면 같은 오브젝트의 DialogueSeat이 대화를 열고, 호스트는 대화가 시작될 때 InnResidents에 알려 친밀도를 올린다(6.6).
    // 대화에서 "장비 건네 주기"를 고르면 고른 플레이어 화면에 장비 착용 창을 열고,
    // "파티에 넣기"를 고르면 파견 파티에 넣고 결과에 맞는 대답을 고른 플레이어 화면에 보여 준다(6.3).
    // 이미 파티에 든 모험가는 "파티에 넣기" 대신 "파티에서 빼기"가 있는 대화를 쓴다.
    [RequireComponent(typeof(DialogueSeat))]
    public class InnResident : NetworkBehaviour, IDialogueChoiceHandler, IDialogueSource
    {
        // 여관 대화 데이터의 선택지 ActionId와 같아야 한다.
        public const string GiftActionId = "GiveEquipment";
        public const string JoinPartyActionId = "JoinParty";
        public const string LeavePartyActionId = "LeaveParty";

        [Tooltip("직업 이름을 찾는 데 쓴다")]
        [SerializeField] private AdventurerGenerationData _generation;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private AdventurerLook _look;
        [Tooltip("들고 있는 무기의 모델을 찾는 데 쓴다")]
        [SerializeField] private ItemCatalog _catalog;
        [SerializeField] private DialogueSeat _seat;
        [SerializeField] private InnChannel _innChannel;
        [SerializeField] private DialogueChannel _dialogueChannel;
        [Tooltip("파티에 든 모험가와의 대화. \"파티에서 빼기\" 선택지가 있다. 비어 있으면 DialogueSeat의 대화를 쓴다")]
        [SerializeField] private DialogueData _inPartyDialogue;
        [Header("파티에 넣기·빼기 대답")]
        [SerializeField] private DialogueData _joinedReply;
        [SerializeField] private DialogueData _alreadyInPartyReply;
        [Tooltip("파티가 가득 찼을 때. 오늘 밤은 쉬겠다고 한다")]
        [SerializeField] private DialogueData _partyFullReply;
        [Tooltip("부상이 심해 파견할 수 없을 때")]
        [SerializeField] private DialogueData _notDispatchableReply;
        [SerializeField] private DialogueData _leftReply;

        private ChangeDetector _changes;

        // 호스트에서만 쓴다.
        private InnResidents _inn;
        private bool _wasBusy;

        [Networked] public NetworkAdventurer Data { get; private set; }
        // 호스트가 아무도 대화하지 않을 때만 갱신한다. 대화 중에 바뀌면 화면과 호스트의 선택지 번호가 어긋난다.
        [Networked] private NetworkBool InParty { get; set; }

        public int AdventurerId => Data.AdventurerId;

        // 호스트 전용. 스폰 직전(onBeforeSpawned)과 관리 목록이 바뀔 때 부른다.
        public void SetData(AdventurerData data)
        {
            Data = NetworkAdventurer.FromData(data);
        }

        // 호스트 전용. 스폰 직전(onBeforeSpawned)에 불러, 대화 시작과 선택 결과를 inn에 넘기게 한다.
        public void SetInn(InnResidents inn)
        {
            _inn = inn;
        }

        public override void Spawned()
        {
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
            Refresh();
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
            {
                return;
            }

            bool isBusy = _seat.IsBusy;
            if (isBusy && !_wasBusy)
            {
                _inn?.Talk(AdventurerId);
            }
            else if (!isBusy && _inn != null)
            {
                InParty = _inn.IsInParty(AdventurerId);
            }

            _wasBusy = isBusy;
        }

        public override void Render()
        {
            foreach (string change in _changes.DetectChanges(this))
            {
                if (change == nameof(Data))
                {
                    Refresh();
                }
            }
        }

        // 호스트 전용.
        public void OnDialogueChoice(string actionId, PlayerRef player)
        {
            if (!HasStateAuthority || _inn == null)
            {
                return;
            }

            if (actionId == GiftActionId)
            {
                RpcOpenGift(player);
            }
            else if (actionId == JoinPartyActionId)
            {
                RpcReplyJoin(player, (int)_inn.JoinParty(AdventurerId));
            }
            else if (actionId == LeavePartyActionId && _inn.LeavePartyByTalk(AdventurerId))
            {
                RpcReplyLeft(player);
            }
        }

        public DialogueData GetDialogue()
        {
            return InParty && _inPartyDialogue != null ? _inPartyDialogue : null;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcOpenGift([RpcTarget] PlayerRef player)
        {
            _innChannel.RequestGift(AdventurerId);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcReplyJoin([RpcTarget] PlayerRef player, int result)
        {
            DialogueData reply = GetJoinReply((PartyJoinResult)result);
            if (reply != null)
            {
                _dialogueChannel.Play(new DialogueRequest(reply, _seat.SpeakerName));
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcReplyLeft([RpcTarget] PlayerRef player)
        {
            if (_leftReply != null)
            {
                _dialogueChannel.Play(new DialogueRequest(_leftReply, _seat.SpeakerName));
            }
        }

        private DialogueData GetJoinReply(PartyJoinResult result)
        {
            switch (result)
            {
                case PartyJoinResult.Joined:
                    return _joinedReply;
                case PartyJoinResult.AlreadyInParty:
                    return _alreadyInPartyReply;
                case PartyJoinResult.PartyFull:
                    return _partyFullReply;
                default:
                    return _notDispatchableReply;
            }
        }

        private void Refresh()
        {
            AdventurerData data = Data.ToData();
            name = $"InnResident {data.Name}";
            _nameLabel.text = $"{data.Name}\n{AdventurerLabels.GetJobName(_generation, data.JobId)} Lv.{data.Level}";
            _seat.SpeakerName = data.Name;
            _look.Show(data, _catalog);
        }
    }
}
