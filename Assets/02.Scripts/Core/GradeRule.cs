using System;
using System.Collections.Generic;

namespace Blacksmith.Core
{
    public static class GradeRule
    {
        public const int MinGrade = 1;

        /// <summary>
        /// 각 미니게임 점수(0~1)의 평균으로 등급을 구한다.
        /// </summary>
        public static int Evaluate(IReadOnlyList<float> stageScores, IReadOnlyList<float> lowerBounds)
        {
            return FromAccuracy(Average(stageScores), lowerBounds);
        }

        /// <summary>
        /// lowerBounds[i]는 (i + 2)등급의 하한(이상)이다. 오름차순이어야 하며, 길이가 N이면 등급은 1~N+1이다.
        /// </summary>
        public static int FromAccuracy(float accuracy, IReadOnlyList<float> lowerBounds)
        {
            int grade = MinGrade;
            for (int i = 0; i < lowerBounds.Count && accuracy >= lowerBounds[i]; i++)
            {
                grade++;
            }
            return grade;
        }

        public static float Average(IReadOnlyList<float> scores)
        {
            if (scores.Count == 0)
            {
                throw new ArgumentException("점수가 하나 이상 있어야 한다.", nameof(scores));
            }

            float sum = 0f;
            for (int i = 0; i < scores.Count; i++)
            {
                sum += scores[i];
            }
            return sum / scores.Count;
        }
    }
}
