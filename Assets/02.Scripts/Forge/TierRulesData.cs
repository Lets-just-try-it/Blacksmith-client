using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 재료 티어별 기준 판매가·미니게임 난이도와 등급별 판매가 배율. 던전이 늘어 재료가 많아지면 이 표를 조정한다.
    [CreateAssetMenu(fileName = "TierRules", menuName = "Blacksmith/Balance/Tier Rules")]
    public class TierRulesData : ScriptableObject
    {
        [Tooltip("티어 1, 2, 3 순서. 기준 판매가는 중급 장비의 값이고, 난이도 번호는 각 미니게임 규칙의 난이도 등급(0부터)이다.")]
        [SerializeField] private TierInfo[] _tiers =
        {
            new TierInfo { BasePrice = 1000, Difficulty = 0 },
            new TierInfo { BasePrice = 5000, Difficulty = 1 },
            new TierInfo { BasePrice = 25000, Difficulty = 2 },
        };

        [Tooltip("1등급(최하급)부터 5등급(최상급)까지 순서대로의 판매가 배율. 중급(3등급)이 1배다.")]
        [SerializeField] private float[] _gradeMultipliers = { 0.5f, 0.75f, 1f, 1.25f, 1.5f };

        private TierRules _rules;

        public TierRules Rules => _rules ??= new TierRules { Tiers = _tiers, GradeMultipliers = _gradeMultipliers };

        public int SalePrice(int tier, int grade)
        {
            return Rules.SalePrice(tier, grade);
        }

        public int DifficultyIndex(int tier)
        {
            return Rules.DifficultyIndex(tier);
        }

        private void OnValidate()
        {
            _rules = null;
            if (_tiers.Length == 0 || _gradeMultipliers.Length == 0)
            {
                Debug.LogWarning("티어와 등급 배율이 하나 이상 있어야 한다.", this);
            }
        }
    }
}
