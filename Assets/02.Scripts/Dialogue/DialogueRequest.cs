using System;

namespace Blacksmith.Dialogue
{
    public enum DialogueCloseReason
    {
        // 선택지 없이 대사를 끝까지 넘겼다.
        Finished,
        // 선택지를 골랐다. ChoiceSelected가 먼저 불린다.
        Chosen,
        // 함께 보는 대화가 시작되어 닫혔다.
        Interrupted,
        // 대화를 연 쪽이 닫았다. (대화 상대가 사라짐, 함께 보는 대화가 모두 끝남 등)
        ClosedByOwner,
    }

    // 대화를 여는 쪽(NPC, 함께 보는 대화, 튜토리얼 등)이 DialogueChannel.Play에 넘기는 요청이다.
    // 다이얼로그 시스템은 누가 열었는지 모르고, 결과는 아래 콜백으로만 돌려준다.
    public sealed class DialogueRequest
    {
        public DialogueRequest(DialogueData data, string fallbackSpeakerName = "", bool isShared = false)
        {
            Data = data;
            FallbackSpeakerName = fallbackSpeakerName;
            IsShared = isShared;
        }

        public DialogueData Data { get; }
        // 화자가 비어 있는 대사에 쓰는 이름이다.
        public string FallbackSpeakerName { get; }
        // 함께 보는 대화는 선택지를 띄우지 않고, 다 읽으면 닫지 않고 다른 참가자를 기다린다.
        public bool IsShared { get; }

        public Action<int> ChoiceSelected { get; set; }
        // 함께 보는 대화에서 이 참가자가 마지막 대사를 넘겼을 때 불린다.
        public Action LinesFinished { get; set; }
        // 이유와 관계없이 창이 닫힐 때 한 번 불린다.
        public Action<DialogueCloseReason> Closed { get; set; }
    }
}
