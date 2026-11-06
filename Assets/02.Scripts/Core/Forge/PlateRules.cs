using System;

namespace Blacksmith.Core.Forge
{
    // 판 성형이 만드는 방어구 종류. 번호는 방어구 레시피와 결과물을 고르는 데 쓰인다.
    public enum ArmorKind
    {
        Helmet,
        Chestplate,
    }

    public static class ArmorKinds
    {
        public const int Count = 2;

        private static readonly string[] Names = { "투구", "갑옷" };

        public static string NameOf(int kind)
        {
            return Names[Math.Max(0, Math.Min(Count - 1, kind))];
        }

    }

    // 밸런스 수치는 ScriptableObject에서 읽어 넘긴다.
    public sealed class PlateRules
    {
        // 조각 하나를 마치고 다음 조각을 꺼내기까지의 시간과, 끝난 뒤 결과를 보여 주는 시간
        public float PartPause { get; set; }
        public float EndDelay { get; set; }

        // 한 번에 따라잡을 수 있는 앞쪽 범위(외곽선 표본 수)
        public int Ahead { get; set; }
        // 불순물 구간에서 띠가 좁아지는 배율과 구간 길이(표본 수)
        public float NarrowMul { get; set; }
        public int NarrowLen { get; set; }
        // 녹색 점을 눌러 시작할 때 허용 거리 = 띠 폭 * StartReachScale + StartReachPad
        public float StartReachScale { get; set; }
        public float StartReachPad { get; set; }
        // 처음 누른 자리의 오차 비율(0~1)
        public float StartErr { get; set; }

        // 띠를 벗어날 때마다 깎이는 점수와 그 한도, 다 오렸을 때 남은 시간에 따른 보너스의 최대치
        public float SlipPenalty { get; set; }
        public float MaxSlipPenalty { get; set; }
        public float TimeBonus { get; set; }
        // 선에서 가장 멀 때 깎이는 비율(0~1)
        public float FitLoss { get; set; }
        // 이 점수 이상이면 결과 사건을 성공으로 알린다.
        public float SuccessScore { get; set; }

        // 앞 단계 평균 점수에 따라 띠 폭이 바뀌는 범위. 폭 = 기본 폭 * (PriorMin + PriorSpan * 앞 단계 점수)
        public float PriorMin { get; set; }
        public float PriorSpan { get; set; }

        public PlateDifficulty[] Difficulties { get; set; }
    }

    // 재료 티어에서 정해진 난이도 하나의 수치. 어려울수록 띠가 좁고, 불순물 구간과 조각이 많고, 제한시간이 빠듯하다.
    [Serializable]
    public struct PlateDifficulty
    {
        // 띠의 기본 폭(칸)
        public float BandWidth;
        // 조각마다 만들어지는 불순물 구간 수
        public int NarrowCount;
        // 기본 조각 말고 더해지는 보강 조각 수(최대 2)
        public int ExtraParts;
        // 제한시간 = 기본 시간 + 둘레 / 이 값
        public float PerimeterPerSecond;
        public float BaseTime;
    }
}
