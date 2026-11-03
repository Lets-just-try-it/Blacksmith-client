using Blacksmith.Network;
using Fusion;
using UnityEngine;

namespace Blacksmith.Dialogue
{
    // Player 프리팹에 붙는 대화 행동 컴포넌트다. 가까운 DialogueSeat을 찾아 상호작용 키로 대화를 요청하고,
    // 호스트가 대화 상대를 정해 주면 내 화면에 대화를 연다. 선택 결과와 함께 보는 대화의 종료도 여기서 호스트에 알린다.
    public class DialoguePlayerActions : NetworkBehaviour
    {
        [SerializeField] private DialogueChannel _channel;
        [SerializeField, Min(0f)] private float _interactRadius = 1.5f;

        private readonly Collider[] _hits = new Collider[16];
        private DialogueSeat _promptedSeat;
        private DialogueRequest _seatRequest;
        private NetworkId _openedTarget;

        [Networked] private NetworkButtons PreviousButtons { get; set; }
        // 대화 중인 DialogueSeat. 호스트만 바꾼다. 다른 플레이어 화면의 "대화 중" 말풍선도 이 값을 본다.
        [Networked] public NetworkId DialogueTarget { get; private set; }

        public bool IsTalking => DialogueTarget.IsValid;

        public override void Spawned()
        {
            if (HasInputAuthority)
            {
                _channel.Register(this);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            ShowPromptOn(null);
            if (_seatRequest != null)
            {
                // 디스폰 중에는 RPC를 보낼 수 없다. 점유는 호스트가 플레이어 퇴장으로 푼다.
                DialogueRequest request = _seatRequest;
                _seatRequest = null;
                request.Closed = null;
                _channel.Close(request);
            }

            _channel.Unregister(this);
        }

        public override void FixedUpdateNetwork()
        {
            // 대화 중이던 NPC가 사라지면(예: 후보 교체) 대화 상대를 비운다.
            if (HasStateAuthority && IsTalking && !Runner.TryFindObject(DialogueTarget, out _))
            {
                DialogueTarget = default;
            }

            if (!GetInput(out PlayerNetworkInput input))
            {
                return;
            }

            bool interactPressed = input.Buttons.WasPressed(PreviousButtons, PlayerButton.Interact);
            PreviousButtons = input.Buttons;

            // 예측 재계산 중에 같은 요청을 다시 보내지 않도록 처음 계산하는 틱에서만 보낸다.
            if (!interactPressed || !HasInputAuthority || !Runner.IsForward || IsTalking || _channel.IsOpen)
            {
                return;
            }

            DialogueSeat seat = FindNearestSeat();
            if (seat != null && !seat.IsBusy)
            {
                RpcRequestTalk(seat.Object.Id);
            }
        }

        public override void Render()
        {
            if (!HasInputAuthority)
            {
                return;
            }

            DialogueSeat nearest = IsTalking || _channel.IsOpen ? null : FindNearestSeat();
            ShowPromptOn(nearest != null && !nearest.IsBusy ? nearest : null);
            SyncSeatDialogue();
        }

        // 함께 보는 대화를 이 PC에서 다 읽었을 때 SharedDialogue가 부른다.
        public void SendSharedFinished(int playId)
        {
            RpcFinishShared(playId);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcRequestTalk(NetworkId seatId)
        {
            if (!IsTalking && TryFindSeat(seatId, out DialogueSeat seat) && seat.TryOccupy(Object.InputAuthority))
            {
                DialogueTarget = seatId;
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcRelease(NetworkId seatId)
        {
            if (DialogueTarget != seatId)
            {
                return;
            }

            DialogueTarget = default;
            if (TryFindSeat(seatId, out DialogueSeat seat))
            {
                seat.Release(Object.InputAuthority);
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcChoose(NetworkId seatId, int choiceIndex)
        {
            if (DialogueTarget != seatId)
            {
                return;
            }

            DialogueTarget = default;
            if (TryFindSeat(seatId, out DialogueSeat seat))
            {
                seat.Choose(Object.InputAuthority, choiceIndex);
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcFinishShared(int playId)
        {
            if (_channel.Shared != null)
            {
                _channel.Shared.MarkFinished(Object.InputAuthority, playId);
            }
        }

        // 호스트가 정해 준 대화 상대가 바뀌면 내 화면의 대화를 열거나 닫는다.
        private void SyncSeatDialogue()
        {
            if (DialogueTarget == _openedTarget)
            {
                return;
            }

            if (_seatRequest != null)
            {
                _channel.Close(_seatRequest);
            }

            _openedTarget = DialogueTarget;
            if (!IsTalking || !TryFindSeat(DialogueTarget, out DialogueSeat seat))
            {
                return;
            }

            NetworkId seatId = DialogueTarget;
            var request = new DialogueRequest(seat.Dialogue, seat.SpeakerName);
            bool hasChosen = false;
            request.ChoiceSelected = choiceIndex =>
            {
                hasChosen = true;
                RpcChoose(seatId, choiceIndex);
            };
            request.Closed = reason =>
            {
                _seatRequest = null;
                // 씬을 내리며 대화창이 먼저 꺼지는 경우처럼 이미 세션이 끝났으면 보내지 않는다.
                if (!hasChosen && Object != null && Object.IsValid)
                {
                    RpcRelease(seatId);
                }
            };

            _seatRequest = request;
            if (!_channel.Play(request))
            {
                _seatRequest = null;
                RpcRelease(seatId);
            }
        }

        private bool TryFindSeat(NetworkId seatId, out DialogueSeat seat)
        {
            seat = null;
            return Runner.TryFindObject(seatId, out NetworkObject seatObject) && seatObject.TryGetComponent(out seat);
        }

        private DialogueSeat FindNearestSeat()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, _interactRadius, _hits,
                Physics.AllLayers, QueryTriggerInteraction.Collide);

            DialogueSeat nearest = null;
            float nearestSqrDistance = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                // 디스폰된 직후(예: 영입된 후보)에도 그 프레임에는 콜라이더가 잡히므로 스폰 상태를 함께 확인한다.
                if (!_hits[i].TryGetComponent(out DialogueSeat seat) || !seat.IsSpawned)
                {
                    continue;
                }

                float sqrDistance = (seat.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearest = seat;
                    nearestSqrDistance = sqrDistance;
                }
            }

            return nearest;
        }

        private void ShowPromptOn(DialogueSeat seat)
        {
            if (seat == _promptedSeat)
            {
                return;
            }

            if (_promptedSeat != null)
            {
                _promptedSeat.SetPromptVisible(false);
            }

            _promptedSeat = seat;
            if (_promptedSeat != null)
            {
                _promptedSeat.SetPromptVisible(true);
            }
        }
    }
}
