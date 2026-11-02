using Fusion;

namespace Blacksmith.Dialogue
{
    // DialogueSeat과 같은 오브젝트에 붙어, 고른 선택지의 결과를 처리한다. 호스트에서만 불린다.
    // 호출 전에 요청자가 대화 중인 플레이어인지, 선택지를 고를 수 있는지는 DialogueSeat이 확인한다.
    public interface IDialogueChoiceHandler
    {
        void OnDialogueChoice(string actionId, PlayerRef player);
    }
}
