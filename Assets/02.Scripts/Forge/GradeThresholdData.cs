using System.Collections.Generic;
using Blacksmith.Core;
using UnityEngine;

namespace Blacksmith.Forge
{
    [CreateAssetMenu(fileName = "GradeThresholds", menuName = "Blacksmith/Balance/Grade Thresholds")]
    public class GradeThresholdData : ScriptableObject
    {
        [Tooltip("1등급부터 순서대로의 이름")]
        [SerializeField] private string[] _gradeNames = { "최하급", "하급", "중급", "상급", "최상급" };

        [Tooltip("2등급부터 순서대로의 정확도 하한(이상). 오름차순, 0~1")]
        [SerializeField] private float[] _lowerBounds = { 0.40f, 0.60f, 0.75f, 0.90f };

        public int GradeCount => _lowerBounds.Length + GradeRule.MinGrade;

        public int Evaluate(IReadOnlyList<float> stageScores)
        {
            return GradeRule.Evaluate(stageScores, _lowerBounds);
        }

        public string GetGradeName(int grade)
        {
            return _gradeNames[grade - GradeRule.MinGrade];
        }

        private void OnValidate()
        {
            if (_gradeNames.Length != GradeCount)
            {
                Debug.LogWarning($"등급 이름은 {GradeCount}개여야 한다.", this);
            }

            for (int i = 1; i < _lowerBounds.Length; i++)
            {
                if (_lowerBounds[i] <= _lowerBounds[i - 1])
                {
                    Debug.LogWarning("정확도 하한은 오름차순이어야 한다.", this);
                    break;
                }
            }
        }
    }
}
