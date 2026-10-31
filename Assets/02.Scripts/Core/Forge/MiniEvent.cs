namespace Blacksmith.Core.Forge
{
    public enum MiniEventKind
    {
        PhaseChanged,
        SecondTick,
        Pull,
        Perfect,
        Finish,
    }

    // 화면이 효과음과 연출을 만들 수 있도록 미니게임이 알려 주는 사건이다.
    // Value는 점수나 초, Index는 단계 번호처럼 종류에 따라 다르게 쓰고, Extra는 덧붙이는 값이다.
    public readonly struct MiniEvent
    {
        public MiniEventKind Kind { get; }
        public float Value { get; }
        public int Index { get; }
        public float Extra { get; }

        public MiniEvent(MiniEventKind kind, float value, int index, float extra)
        {
            Kind = kind;
            Value = value;
            Index = index;
            Extra = extra;
        }
    }
}
