using UnityEngine;

namespace Blacksmith.Dialogue
{
    // 시험 씬에서 함께 보는 대화를 확인하기 위한 도구다. 플레이 중 호스트 에디터에서
    // 이 컴포넌트의 컨텍스트 메뉴 "Play Shared Dialogue"를 누른다.
    public class SharedDialogueTestTrigger : MonoBehaviour
    {
        [SerializeField] private SharedDialogue _sharedDialogue;
        [SerializeField] private DialogueData _dialogue;

        [ContextMenu("Play Shared Dialogue")]
        private void Play()
        {
            if (!_sharedDialogue.Play(_dialogue))
            {
                Debug.LogWarning("함께 보는 대화를 열지 못했다. 호스트인지, 이미 진행 중인지, DialogueCatalog에 있는지 확인한다.", this);
            }
        }
    }
}
