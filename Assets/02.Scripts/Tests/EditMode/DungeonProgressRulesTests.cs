using Blacksmith.Core.Battle;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class DungeonProgressRulesTests
    {
        private const int StageCount = 3;

        private static BattleResult Result(bool isVictory, int stagesCleared)
        {
            return new BattleResult(isVictory, stagesCleared, 0, new AdventurerBattleResult[0]);
        }

        [Test]
        public void FirstTier_IsAlwaysUnlocked()
        {
            Assert.IsTrue(DungeonProgressRules.IsTierUnlocked(0, 0, 0));
        }

        [Test]
        public void NextTier_UnlocksOnlyAfterBossStageCleared()
        {
            Assert.IsFalse(DungeonProgressRules.IsTierUnlocked(1, 2, StageCount));
            Assert.IsTrue(DungeonProgressRules.IsTierUnlocked(1, 3, StageCount));
        }

        [Test]
        public void ClearedStagesAndNextStage_CanBeSelected()
        {
            // 1스테이지까지 클리어했으면 1·2스테이지(번호 0·1)를 고를 수 있고 3스테이지는 잠겨 있다.
            Assert.AreEqual(1, DungeonProgressRules.NextStage(1));
            Assert.IsTrue(DungeonProgressRules.CanSelectStage(1, StageCount, 0));
            Assert.IsTrue(DungeonProgressRules.CanSelectStage(1, StageCount, 1));
            Assert.IsFalse(DungeonProgressRules.CanSelectStage(1, StageCount, 2));
        }

        [Test]
        public void FreshTier_AllowsOnlyFirstStage()
        {
            Assert.IsTrue(DungeonProgressRules.CanSelectStage(0, StageCount, 0));
            Assert.IsFalse(DungeonProgressRules.CanSelectStage(0, StageCount, 1));
            Assert.IsFalse(DungeonProgressRules.CanSelectStage(0, StageCount, -1));
        }

        [Test]
        public void ClearedTier_AllowsEveryStage()
        {
            for (int stage = 0; stage < StageCount; stage++)
            {
                Assert.IsTrue(DungeonProgressRules.CanSelectStage(StageCount, StageCount, stage));
            }

            Assert.IsFalse(DungeonProgressRules.CanSelectStage(StageCount, StageCount, StageCount));
        }

        [Test]
        public void DefaultStage_IsNextStageOrBossWhenCleared()
        {
            Assert.AreEqual(0, DungeonProgressRules.DefaultStage(0, StageCount));
            Assert.AreEqual(2, DungeonProgressRules.DefaultStage(2, StageCount));
            Assert.AreEqual(2, DungeonProgressRules.DefaultStage(StageCount, StageCount));
        }

        [Test]
        public void DefeatInNextStage_KeepsProgress()
        {
            Assert.AreEqual(1, DungeonProgressRules.ApplyResult(1, 1, Result(false, 0)));
        }

        [Test]
        public void VictoryInNextStage_OpensFollowingStage()
        {
            Assert.AreEqual(2, DungeonProgressRules.ApplyResult(1, 1, Result(true, 1)));
        }

        [Test]
        public void VictoryInClearedStage_DoesNotAddProgress()
        {
            Assert.AreEqual(2, DungeonProgressRules.ApplyResult(2, 0, Result(true, 1)));
        }
    }
}
