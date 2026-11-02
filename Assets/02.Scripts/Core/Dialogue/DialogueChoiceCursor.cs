using System;

namespace Blacksmith.Core.Dialogue
{
    // 선택지 강조 규칙이다. 연타로 원치 않는 선택지가 골라지지 않도록 강조 없이 시작하고,
    // 강조된 활성 선택지에서만 확정할 수 있다.
    public sealed class DialogueChoiceCursor
    {
        public const int NoHighlight = -1;
        // 숫자키 1~9로 고를 수 있는 만큼만 둔다.
        public const int MaxChoices = 9;

        private readonly bool[] _enabled;

        public DialogueChoiceCursor(bool[] enabled)
        {
            if (enabled.Length == 0 || enabled.Length > MaxChoices)
            {
                throw new ArgumentException($"선택지는 1~{MaxChoices}개여야 한다.", nameof(enabled));
            }

            _enabled = enabled;
        }

        public int Count => _enabled.Length;
        public int Highlighted { get; private set; } = NoHighlight;

        public bool IsEnabled(int index)
        {
            return _enabled[index];
        }

        // 숫자키는 강조만 한다. 없는 번호면 그대로 둔다.
        public void HighlightNumber(int number)
        {
            int index = number - 1;
            if (index >= 0 && index < Count)
            {
                Highlighted = index;
            }
        }

        // 강조가 없으면 위·아래 어느 쪽이든 첫 선택지부터 강조한다. 끝에서는 반대쪽으로 넘어가지 않는다.
        public void Move(int direction)
        {
            if (Highlighted == NoHighlight)
            {
                Highlighted = 0;
                return;
            }

            Highlighted = Math.Clamp(Highlighted + Math.Sign(direction), 0, Count - 1);
        }

        public bool CanConfirm => Highlighted != NoHighlight && _enabled[Highlighted];
    }
}
