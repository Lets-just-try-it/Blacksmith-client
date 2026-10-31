using System;
using Blacksmith.Core;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class GradeRuleTests
    {
        // 테스트 전용 수치다. 실제 경계값은 GradeThresholds 데이터를 따른다.
        private static readonly float[] LowerBounds = { 0.40f, 0.60f, 0.75f, 0.90f };

        [TestCase(0f, 1)]
        [TestCase(0.39f, 1)]
        [TestCase(0.40f, 2)]
        [TestCase(0.59f, 2)]
        [TestCase(0.60f, 3)]
        [TestCase(0.74f, 3)]
        [TestCase(0.75f, 4)]
        [TestCase(0.89f, 4)]
        [TestCase(0.90f, 5)]
        [TestCase(1f, 5)]
        public void FromAccuracy_UsesInclusiveLowerBounds(float accuracy, int expectedGrade)
        {
            Assert.AreEqual(expectedGrade, GradeRule.FromAccuracy(accuracy, LowerBounds));
        }

        [Test]
        public void FromAccuracy_WithoutBounds_ReturnsMinGrade()
        {
            Assert.AreEqual(GradeRule.MinGrade, GradeRule.FromAccuracy(1f, new float[0]));
        }

        [Test]
        public void Average_ReturnsMeanOfScores()
        {
            Assert.AreEqual(0.5f, GradeRule.Average(new[] { 0.2f, 0.5f, 0.8f }), 0.0001f);
        }

        [Test]
        public void Average_WithoutScores_Throws()
        {
            Assert.Throws<ArgumentException>(() => GradeRule.Average(new float[0]));
        }

        [Test]
        public void Evaluate_GradesByAverageNotByEachScore()
        {
            // 평균 0.625: 한 단계가 0.9 이상이어도 평균 구간(3등급)을 따른다.
            var scores = new[] { 0.95f, 0.30f, 0.60f, 0.65f };

            Assert.AreEqual(3, GradeRule.Evaluate(scores, LowerBounds));
        }
    }
}
