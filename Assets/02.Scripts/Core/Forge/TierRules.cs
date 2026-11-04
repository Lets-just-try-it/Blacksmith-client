using System;

namespace Blacksmith.Core.Forge
{
    // 재료 티어(1부터, 높을수록 귀한 재료) 하나의 수치다.
    [Serializable]
    public struct TierInfo
    {
        // 이 티어 재료로 만든 장비의 기준 판매가(중급 기준)
        public int BasePrice;
        // 이 티어 재료로 만들 때의 미니게임 난이도 번호(0부터)
        public int Difficulty;
    }

    // 재료 티어와 장비 등급으로 판매가와 미니게임 난이도를 정한다. 네트워크와 Unity에 의존하지 않는다.
    //  - 판매가 = 티어의 기준 판매가 × 등급 배율 (등급 1~5, 중급이 1배)
    //  - 난이도 = 티어가 정한 번호. 상위 재료를 쓰는 장비일수록 미니게임이 어렵다.
    public sealed class TierRules
    {
        public TierInfo[] Tiers { get; set; }
        // 1등급부터 순서대로의 판매가 배율
        public float[] GradeMultipliers { get; set; }

        public int SalePrice(int tier, int grade)
        {
            float multiplier = GradeMultipliers[Clamp(grade, 1, GradeMultipliers.Length) - 1];
            return (int)Math.Round(GetTier(tier).BasePrice * multiplier, MidpointRounding.AwayFromZero);
        }

        // 난이도 번호가 각 미니게임의 난이도 개수보다 크면 미니게임 쪽에서 마지막 난이도로 자른다.
        public int DifficultyIndex(int tier)
        {
            return Math.Max(0, GetTier(tier).Difficulty);
        }

        private TierInfo GetTier(int tier)
        {
            return Tiers[Clamp(tier, 1, Tiers.Length) - 1];
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }
}
