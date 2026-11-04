using Blacksmith.Core.Forge;
using Blacksmith.Core.Inventory;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class CraftingRulesTests
    {
        // 테스트 전용 수치다. 실제 경계값은 GradeThresholds 데이터를 따른다.
        private static readonly float[] LowerBounds = { 0.40f, 0.60f, 0.75f, 0.90f };

        [Test]
        public void FirstStage_UsesAccuracyAndIgnoresPreviousValue()
        {
            Assert.AreEqual(83, CraftingRules.AccumulatePercent(99, 0, 0.83f));
        }

        [Test]
        public void LaterStages_KeepRunningAverage()
        {
            int afterSecond = CraftingRules.AccumulatePercent(80, 1, 0.60f);
            Assert.AreEqual(70, afterSecond);
            int afterThird = CraftingRules.AccumulatePercent(afterSecond, 2, 1f);
            Assert.AreEqual(80, afterThird);
        }

        [Test]
        public void Accumulate_ClampsInputs()
        {
            Assert.AreEqual(100, CraftingRules.AccumulatePercent(0, 0, 2f));
            Assert.AreEqual(0, CraftingRules.AccumulatePercent(0, 0, -1f));
            Assert.AreEqual(50, CraftingRules.AccumulatePercent(500, 1, 0f));
            Assert.AreEqual(50, CraftingRules.AccumulatePercent(-10, 1, 1f));
        }

        [Test]
        public void IntermediateOutput_CarriesPercentInAccuracy()
        {
            ItemStack output = CraftingRules.MakeOutput(7, 1, false, 80, 0.60f, LowerBounds);
            Assert.AreEqual(7, output.ItemId);
            Assert.AreEqual(70, output.Accuracy);
            Assert.AreEqual(0, output.Grade);
            Assert.AreEqual(1, output.Count);
        }

        [TestCase(0.39f, 1)]
        [TestCase(0.40f, 2)]
        [TestCase(0.60f, 3)]
        [TestCase(0.75f, 4)]
        [TestCase(0.90f, 5)]
        public void FinalOutput_TurnsAverageIntoGrade(float average, int expectedGrade)
        {
            // 앞 세 단계의 평균이 average이고 마지막 단계도 average이면 전체 평균도 average다.
            int previous = (int)(average * 100f + 0.5f);
            ItemStack output = CraftingRules.MakeOutput(3, 3, true, previous, average, LowerBounds);
            Assert.AreEqual(3, output.ItemId);
            Assert.AreEqual(expectedGrade, output.Grade);
            Assert.AreEqual(0, output.Accuracy);
        }

        [Test]
        public void FullWeaponChain_AveragesAllFourStages()
        {
            float[] scores = { 1f, 0.8f, 0.6f, 0.6f };
            int percent = 0;
            ItemStack item = ItemStack.Empty;
            for (int stage = 0; stage < scores.Length; stage++)
            {
                bool isFinal = stage == scores.Length - 1;
                item = CraftingRules.MakeOutput(10 + stage, stage, isFinal, percent, scores[stage], LowerBounds);
                percent = item.Accuracy;
            }

            // 평균 0.75이므로 상급(4등급)
            Assert.AreEqual(13, item.ItemId);
            Assert.AreEqual(4, item.Grade);
        }
    }
}
