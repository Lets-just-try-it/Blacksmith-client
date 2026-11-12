using System;

namespace Blacksmith.Core.Forge
{
    // 밸런스 수치는 ScriptableObject에서 읽어 넘긴다.
    public sealed class BowBendRules
    {
        // 시작 안내, 모두 끝난 뒤 결과를 보여 주는 시간(초)
        public float IntroTime { get; set; }
        public float EndDelay { get; set; }

        // 마우스를 아래로 한 칸 끌면 휘는 정도(쇠가 가장 뜨거울 때). 식을수록 같은 거리를 끌어도 덜 휜다.
        public float BendPerCell { get; set; }
        // 다 식었을 때 끄는 거리에 곱하는 값
        public float ColdFactor { get; set; }
        // 달구는 속도(초당 열 회복)와 식는 속도(초당 열 감소). 식는 속도는 마디마다 조금씩 다르다.
        public float HeatRate { get; set; }
        public float CoolRate { get; set; }
        // 누르던 것을 놓으면 휘었던 정도가 이 비율만큼 되돌아온다(탄성). 식은 쇠는 더 되돌아와서 열이 0이면 (1 + SpringCold)배다.
        public float SpringBack { get; set; }
        public float SpringCold { get; set; }
        // 더 휠 수 없는 한계
        public float MaxBend { get; set; }
        // 오차가 허용 폭을 넘었을 때 점수가 0이 되는 추가 오차
        public float ErrorScale { get; set; }
        // 이 점수 이상이면 화면이 성공으로 알린다.
        public float SuccessScore { get; set; }

        public BowBendDifficulty[] Difficulties { get; set; }
    }

    // 재료 티어에서 정해진 난이도 하나의 수치
    [Serializable]
    public struct BowBendDifficulty
    {
        // 전체 제한시간(초), 맞았다고 보는 허용 폭, 형틀 목표 휘기가 흔들리는 비율
        public float TimeLimit;
        public float Tolerance;
        public float Jitter;
    }
}
