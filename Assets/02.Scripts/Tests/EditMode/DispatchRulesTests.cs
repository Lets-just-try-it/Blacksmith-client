using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Battle;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class DispatchRulesTests
    {
        private const int MaxPartySize = 5;
        private const int StageCount = 3;

        [Test]
        public void AdventurerBelowInjuryLimit_CanBeDispatched()
        {
            InjuryBalance injury = InjuryRulesTests.CreateBalance();

            Assert.IsTrue(DispatchRules.CanDispatch(new AdventurerData { Injury = 0 }, injury));
            Assert.IsTrue(DispatchRules.CanDispatch(new AdventurerData { Injury = 49 }, injury));
            Assert.IsFalse(DispatchRules.CanDispatch(new AdventurerData { Injury = 50 }, injury));
        }

        [Test]
        public void Party_CannotExceedMaxSize()
        {
            Assert.IsTrue(DispatchRules.CanAddToParty(MaxPartySize - 1, MaxPartySize));
            Assert.IsFalse(DispatchRules.CanAddToParty(MaxPartySize, MaxPartySize));
        }

        [Test]
        public void Party_NeedsAtLeastOneAdventurer()
        {
            Assert.IsFalse(DispatchRules.IsValidPartySize(0, MaxPartySize));
            Assert.IsTrue(DispatchRules.IsValidPartySize(1, MaxPartySize));
            Assert.IsTrue(DispatchRules.IsValidPartySize(MaxPartySize, MaxPartySize));
            Assert.IsFalse(DispatchRules.IsValidPartySize(MaxPartySize + 1, MaxPartySize));
        }

        [Test]
        public void ClampStage_KeepsSelectableStage()
        {
            Assert.AreEqual(0, DispatchRules.ClampStage(1, StageCount, 0));
            Assert.AreEqual(1, DispatchRules.ClampStage(1, StageCount, 1));
        }

        [Test]
        public void ClampStage_MovesLockedStageToDefault()
        {
            Assert.AreEqual(1, DispatchRules.ClampStage(1, StageCount, 2));
            Assert.AreEqual(0, DispatchRules.ClampStage(0, StageCount, 5));
            Assert.AreEqual(2, DispatchRules.ClampStage(StageCount, StageCount, -1));
        }

        [Test]
        public void ClampStage_EmptyTier_HasNoStage()
        {
            Assert.AreEqual(-1, DispatchRules.ClampStage(0, 0, 0));
        }

        [Test]
        public void NextTier_SelectableOnlyAfterPreviousBossCleared()
        {
            int[] stageCounts = { StageCount, StageCount };
            Assert.IsTrue(DispatchRules.IsTierSelectable(0, new[] { 0, 0 }, stageCounts));
            Assert.IsFalse(DispatchRules.IsTierSelectable(1, new[] { 2, 0 }, stageCounts));
            Assert.IsTrue(DispatchRules.IsTierSelectable(1, new[] { StageCount, 0 }, stageCounts));
        }

        [Test]
        public void ClearedTier_StaysSelectable()
        {
            int[] stageCounts = { StageCount, StageCount };
            Assert.IsTrue(DispatchRules.IsTierSelectable(0, new[] { StageCount, 0 }, stageCounts));
            Assert.IsFalse(DispatchRules.IsTierSelectable(2, new[] { StageCount, StageCount }, stageCounts));
        }
    }
}
