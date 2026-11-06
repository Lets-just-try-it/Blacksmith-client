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
        // 정제 쪼개기: 저주 균열을 넘겼다(Avoid), 균열을 눌렀다(Crack, Index 균열 번호), 아직 일찍 눌렀다(Early)
        // 정제 풀무질/붓기: 다 달궈졌다(Molten, Value는 묽기), 쇳물이 틀 밖으로 흘렀다(Spill, Value는 양),
        // 눈금을 넘쳤다(Overflow), 찌꺼기가 나올 때가 됐다(Warn), 쇳물이 굳었다(Solidify)
        Avoid,
        Crack,
        Early,
        Molten,
        Spill,
        Overflow,
        Warn,
        Solidify,
        // 연마: 날이 탔다(Hazard, Index는 닿은 칸, Value는 지금까지의 탄 벌점). 칸을 다 갈면 Good, 물에 담그면 Hit, 열이 경고 구간을 넘으면 Warn
        Hazard,
        // 판 성형: 다음 조각이 나왔다(Index는 조각 번호), 녹색 점을 잡고 오리기 시작했다(Grip)
        Spawn,
        Grip,
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
