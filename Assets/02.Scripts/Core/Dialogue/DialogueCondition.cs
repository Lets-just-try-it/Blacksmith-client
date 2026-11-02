using System;
using System.Collections.Generic;

namespace Blacksmith.Core.Dialogue
{
    // 대사 묶음 선택과 선택지 활성 조건이 읽는 값이다. 값을 채우는 쪽은 IDialogueStats 구현이다.
    // 항목을 추가하면 그 값을 채우는 구현(Dialogue/GameDialogueStats)에도 추가한다.
    public enum DialogueStat
    {
        FreeRosterSlots = 0,
    }

    public enum DialogueCompare
    {
        AtLeast = 0,
        AtMost = 1,
        Equal = 2,
        NotEqual = 3,
    }

    public interface IDialogueStats
    {
        int Get(DialogueStat stat);
    }

    // 예: FreeRosterSlots AtLeast 1 → 빈 관리 슬롯이 1개 이상
    [Serializable]
    public struct DialogueCondition
    {
        public DialogueStat Stat;
        public DialogueCompare Compare;
        public int Value;

        public bool IsMet(IDialogueStats stats)
        {
            int current = stats.Get(Stat);
            switch (Compare)
            {
                case DialogueCompare.AtLeast:
                    return current >= Value;
                case DialogueCompare.AtMost:
                    return current <= Value;
                case DialogueCompare.Equal:
                    return current == Value;
                case DialogueCompare.NotEqual:
                    return current != Value;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Compare), Compare, null);
            }
        }

        // 조건이 없으면 항상 참이다. 화면 표시와 호스트의 재확인이 모두 이 함수를 쓴다.
        public static bool AreMet(IReadOnlyList<DialogueCondition> conditions, IDialogueStats stats)
        {
            for (int i = 0; i < conditions.Count; i++)
            {
                if (!conditions[i].IsMet(stats))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
