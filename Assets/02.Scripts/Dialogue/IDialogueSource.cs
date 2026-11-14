namespace Blacksmith.Dialogue
{
    // DialogueSeat과 같은 오브젝트에 붙어, 상태에 따라 다른 대화를 고르게 한다.
    // 호스트와 모든 PC가 같은 복제 값으로 계산해야 하고, 대화 중에는 결과가 바뀌지 않아야 선택지 번호가 맞는다.
    public interface IDialogueSource
    {
        // null이면 DialogueSeat에 넣어 둔 기본 대화를 쓴다.
        DialogueData GetDialogue();
    }
}
