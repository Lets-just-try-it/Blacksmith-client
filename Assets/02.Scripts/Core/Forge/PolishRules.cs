using System;

namespace Blacksmith.Core.Forge
{
    // 밸런스 수치는 ScriptableObject에서 읽어 넘긴다.
    public sealed class PolishRules
    {
        // 준비 시간과, 끝난 뒤 결과를 보여 주는 시간
        public float IntroTime { get; set; }
        public float EndDelay { get; set; }

        // 한 칸의 날이 이만큼 서면 다 갈린 것이다(0~1).
        public float SegmentGoal { get; set; }
        // 각도가 알맞을 때 날이 초당 서는 양과, 단단한 칸의 배율
        public float GrindRate { get; set; }
        public float HardRate { get; set; }
        // 숫돌에 닿는 폭의 절반(날 길이 -1~1 기준)
        public float ContactRadius { get; set; }

        // 알맞은 각도가 위아래로 흔들리는 폭과 속도 범위(rad/초). 흔들림 모양은 시드로 정한다.
        public float AngleAmplitude { get; set; }
        public float AngleSpeedMin { get; set; }
        public float AngleSpeedMax { get; set; }

        // 열이 오르는 속도(초당), 각도가 어긋날수록 더해지는 배율, 가만히 두면 식는 속도, 물에 담그면 식는 속도
        public float HeatUp { get; set; }
        public float OffAngleHeat { get; set; }
        public float HeatCool { get; set; }
        public float DipCool { get; set; }
        // 이 열에 닿으면 날이 탄다. 탄 뒤에는 ResumeHeat까지 식어야 다시 갈 수 있다.
        public float BurnHeat { get; set; }
        public float ResumeHeat { get; set; }
        // 이 열을 넘으면 경고한다.
        public float WarnHeat { get; set; }
        // 탈 때마다 깎이는 점수와, 닿은 칸이 잃는 날
        public float BurnPenalty { get; set; }
        public float BurnSharpLoss { get; set; }

        // 이미 다 간 곳을 계속 갈 때 초당 깎이는 점수와 그 한도
        public float ThinLossPerSecond { get; set; }
        public float MaxThin { get; set; }

        // 처음 날의 범위. 앞 단계 평균 점수가 PriorThreshold보다 낮으면 PriorDrop만큼 덜 서 있다.
        public float InitialMin { get; set; }
        public float InitialMax { get; set; }
        public float PriorThreshold { get; set; }
        public float PriorDrop { get; set; }

        // 보너스 = min(ComboBonusMax, 최고 콤보 * ComboBonusPer)
        public float ComboBonusPer { get; set; }
        public float ComboBonusMax { get; set; }
        // 이 점수 이상이면 결과 사건을 성공으로 알린다.
        public float SuccessScore { get; set; }

        public PolishDifficulty[] Difficulties { get; set; }
    }

    // 광석 등급 하나의 수치. 귀한 광석일수록 알맞은 각도 폭과 제한시간이 빠듯하고 열이 빨리 오르며 단단한 칸이 많다.
    [Serializable]
    public struct PolishDifficulty
    {
        // 알맞은 각도의 허용 폭(위아래 반폭)
        public float AngleTolerance;
        public float TimeLimit;
        public float HeatScale;
        // 단단한 칸이 될 확률
        public float HardChance;
    }
}
