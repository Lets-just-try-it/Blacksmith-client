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
        public void Battle_StartsFromFirstUnclearedStage()
        {
            // 1스테이지까지 클리어한 뒤 3스테이지를 고르면 2·3스테이지를 싸운다.
            Assert.AreEqual(1, DungeonProgressRules.FirstStageToFight(1));
            Assert.IsTrue(DungeonProgressRules.CanSelectStage(1, StageCount, 2));
        }

        [Test]
        public void ClearedStage_CannotBeSelected()
        {
            Assert.IsFalse(DungeonProgressRules.CanSelectStage(2, StageCount, 0));
            Assert.IsFalse(DungeonProgressRules.CanSelectStage(2, StageCount, 1));
            Assert.IsTrue(DungeonProgressRules.CanSelectStage(2, StageCount, 2));
        }

        [Test]
        public void UnlockedTier_AllowsAnyStage()
        {
            for (int stage = 0; stage < StageCount; stage++)
            {
                Assert.IsTrue(DungeonProgressRules.CanSelectStage(0, StageCount, stage));
            }

            Assert.IsFalse(DungeonProgressRules.CanSelectStage(0, StageCount, StageCount));
        }

        [Test]
        public void DefeatInFirstStage_KeepsProgress()
        {
            Assert.AreEqual(0, DungeonProgressRules.ApplyResult(0, Result(false, 0)));
        }

        [Test]
        public void StagesClearedBeforeDefeat_AreKept()
        {
            Assert.AreEqual(2, DungeonProgressRules.ApplyResult(1, Result(false, 1)));
        }

        [Test]
        public void Victory_AddsAllFoughtStages()
        {
            Assert.AreEqual(3, DungeonProgressRules.ApplyResult(1, Result(true, 2)));
        }
    }
}
