using System.Collections.Generic;
using UnityEngine;

namespace Blacksmith.Dialogue
{
    // 함께 보는 대화를 RPC로 보낼 번호표. ScriptableObject는 RPC로 보낼 수 없어서 목록의 번호를 보낸다.
    // 모든 참가자가 같은 목록을 써야 하므로 순서를 바꾸면 같은 빌드끼리만 맞는다.
    [CreateAssetMenu(fileName = "DialogueCatalog", menuName = "Blacksmith/Dialogue/Catalog")]
    public class DialogueCatalog : ScriptableObject
    {
        [SerializeField] private List<DialogueData> _dialogues = new List<DialogueData>();

        // 없으면 -1이다.
        public int IndexOf(DialogueData dialogue)
        {
            return _dialogues.IndexOf(dialogue);
        }

        public bool TryGet(int index, out DialogueData dialogue)
        {
            bool isValid = index >= 0 && index < _dialogues.Count;
            dialogue = isValid ? _dialogues[index] : null;
            return isValid;
        }
    }
}
