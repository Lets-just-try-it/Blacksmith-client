using Fusion;
using UnityEngine;

namespace Blacksmith.Dialogue
{
    // Player 프리팹에 붙는다. 다른 플레이어 화면에서 이 플레이어가 NPC와 대화 중이면 머리 위에 말풍선을 띄운다.
    // 함께 보는 대화는 모두가 같이 보므로 띄우지 않는다.
    public class DialogueBubbleView : NetworkBehaviour
    {
        [SerializeField] private DialoguePlayerActions _actions;
        [SerializeField] private GameObject _bubble;

        public override void Spawned()
        {
            _bubble.SetActive(false);
        }

        public override void Render()
        {
            _bubble.SetActive(!HasInputAuthority && _actions.IsTalking);
        }
    }
}
