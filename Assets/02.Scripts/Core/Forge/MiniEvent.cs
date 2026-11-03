namespace Blacksmith.Core.Forge
{
    public enum MiniEventKind
    {
        PhaseChanged,
        SecondTick,
        Pull,
        Perfect,
        Finish,
        // 단조: 박자 소리(Value 1 작은 박자, 2 큰 박자 시작, 3 큰 박자 끝), 힘 모으기 시작, 망치 내려치기, 판정(Index는 부위 번호, Extra는 박자와의 차이)
        Beat,
        Charge,
        Swing,
        Good,
        Hit,
        Miss,
        // 단조: 마지막 박자까지 판정이 끝났다. Value는 정확도이고, 화면이 결과를 보여 줄 시간 뒤에 Finish가 온다.
        Complete,
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
