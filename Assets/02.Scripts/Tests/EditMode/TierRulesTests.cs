using Blacksmith.Core.Forge;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class TierRulesTests
    {
        // 테스트 전용 수치다. 실제 값은 TierRules 데이터를 따른다.
        private static TierRules Create()
        {
            return new TierRules
            {
                Tiers = new[]
                {
                    new TierInfo { BasePrice = 1000, Difficulty = 0 },
                    new TierInfo { BasePrice = 5000, Difficulty = 1 },
                    new TierInfo { BasePrice = 25000, Difficulty = 2 },
                },
                GradeMultipliers = new[] { 0.5f, 0.75f, 1f, 1.25f, 1.5f },
            };
        }

        [TestCase(1, 1, 500)]
        [TestCase(1, 2, 750)]
        [TestCase(1, 3, 1000)]
        [TestCase(1, 4, 1250)]
        [TestCase(1, 5, 1500)]
        [TestCase(2, 1, 2500)]
        [TestCase(2, 3, 5000)]
        [TestCase(2, 5, 7500)]
        [TestCase(3, 3, 25000)]
        public void SalePrice_IsTierBaseTimesGradeMultiplier(int tier, int grade, int expected)
        {
            Assert.AreEqual(expected, Create().SalePrice(tier, grade));
        }

        [Test]
        public void SalePrice_ClampsOutOfRangeTierAndGrade()
        {
            TierRules rules = Create();
            Assert.AreEqual(500, rules.SalePrice(0, 0));
            Assert.AreEqual(rules.SalePrice(3, 5), rules.SalePrice(9, 9));
        }

        [Test]
        public void SalePrice_RoundsToWholeNumber()
        {
            var rules = new TierRules
            {
                Tiers = new[] { new TierInfo { BasePrice = 333, Difficulty = 0 } },
                GradeMultipliers = new[] { 0.5f },
            };
            Assert.AreEqual(167, rules.SalePrice(1, 1));
        }

        [TestCase(1, 0)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        public void DifficultyIndex_FollowsTier(int tier, int expected)
        {
            Assert.AreEqual(expected, Create().DifficultyIndex(tier));
        }

        [Test]
        public void DifficultyIndex_ClampsOutOfRangeTier()
        {
            TierRules rules = Create();
            Assert.AreEqual(2, rules.DifficultyIndex(9));
            Assert.AreEqual(0, rules.DifficultyIndex(0));
        }
    }
}
