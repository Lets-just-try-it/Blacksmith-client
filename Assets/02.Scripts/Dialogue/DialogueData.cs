using System;
using System.Collections.Generic;
using Blacksmith.Core.Dialogue;
using UnityEngine;

namespace Blacksmith.Dialogue
{
    // 대사 한 편. 조건이 맞는 첫 묶음의 대사를 차례로 보여 주고, 선택지가 있으면 마지막에 띄운다.
    [CreateAssetMenu(fileName = "Dialogue", menuName = "Blacksmith/Dialogue/Dialogue")]
    public class DialogueData : ScriptableObject
    {
        [Serializable]
        public class Line
        {
            [Tooltip("비워 두면 대화를 연 쪽이 넘긴 이름(예: 영입 후보 이름)을 쓴다")]
            public DialogueSpeaker Speaker;
            [TextArea(2, 4)]
            public string Text;
        }

        [Serializable]
        public class Entry
        {
            [Tooltip("모두 맞아야 이 묶음을 쓴다. 비워 두면 항상 맞는다")]
            public List<DialogueCondition> Conditions = new List<DialogueCondition>();
            public List<Line> Lines = new List<Line>();
        }

        [Serializable]
        public class Choice
        {
            public string Text;
            [Tooltip("모두 맞아야 고를 수 있다. 비워 두면 항상 고를 수 있다")]
            public List<DialogueCondition> EnableConditions = new List<DialogueCondition>();
            [Tooltip("고를 수 없을 때 선택지 옆에 보여 줄 짧은 이유")]
            public string DisabledText;
            [Tooltip("호스트에서 대화를 연 쪽(IDialogueChoiceHandler)에 넘기는 값. 비워 두면 대화만 닫는다")]
            public string ActionId;
        }

        [Tooltip("위에서부터 확인해 조건이 맞는 첫 묶음을 쓴다. 마지막 묶음은 조건 없이 두는 것을 권한다")]
        [SerializeField] private List<Entry> _entries = new List<Entry>();
        [Tooltip("함께 보는 대화에서는 쓰지 않는다")]
        [SerializeField] private List<Choice> _choices = new List<Choice>();

        public IReadOnlyList<Choice> Choices => _choices;

        // 맞는 묶음이 없으면 null이다.
        public Entry PickEntry(IDialogueStats stats)
        {
            foreach (Entry entry in _entries)
            {
                if (DialogueCondition.AreMet(entry.Conditions, stats))
                {
                    return entry;
                }
            }

            return null;
        }

        public bool IsChoiceEnabled(int index, IDialogueStats stats)
        {
            return DialogueCondition.AreMet(_choices[index].EnableConditions, stats);
        }

        private void OnValidate()
        {
            if (_choices.Count > DialogueChoiceCursor.MaxChoices)
            {
                Debug.LogWarning($"선택지는 숫자키로 고를 수 있는 {DialogueChoiceCursor.MaxChoices}개까지만 둔다.", this);
            }
        }
    }
}
