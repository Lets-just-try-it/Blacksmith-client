using System.Collections.Generic;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Battle;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class BattleRewardRulesTests
    {
        // 테스트 전용 수치다. 실제 밸런스는 ScriptableObject 데이터를 따른다.
        private static readonly AdventurerLevelRules LevelRules = new AdventurerLevelRules
        {
            BaseRequiredExperience = 100,
            RequiredExperiencePerLevel = 50,
        };

        private static List<StageReward> Stages()
        {
            return new List<StageReward>
            {
                new StageReward { Gold = 100, Experience = 40, Materials = new[] { Material(1, 1) } },
                new StageReward { Gold = 150, Experience = 60 },
                new StageReward { Gold = 300, Experience = 120, Materials = new[] { Material(1, 2), Material(2, 1) } },
            };
        }

        private static MaterialReward Material(int id, int count)
        {
            return new MaterialReward { MaterialId = id, Count = count };
        }

        private static Dictionary<int, AdventurerData> Party()
        {
            return new Dictionary<int, AdventurerData>
            {
                { 1, new AdventurerData { AdventurerId = 1, Level = 1, Experience = 0 } },
                { 2, new AdventurerData { AdventurerId = 2, Level = 1, Experience = 90 } },
            };
        }

        private static BattleRewards Calculate(BattleResult result)
        {
            Dictionary<int, AdventurerData> party = Party();
            return BattleRewardRules.Calculate(Stages(), result, id => party[id], LevelRules);
        }

        private static BattleResult Result(bool isVictory, int stagesCleared, params AdventurerBattleResult[] adventurers)
        {
            return new BattleResult(isVictory, stagesCleared, 100, adventurers);
        }

        private static AdventurerBattleResult Adventurer(int id, int stagesCleared, bool isInjured = false, bool isDead = false)
        {
            return new AdventurerBattleResult(id, isInjured, isDead, 0, stagesCleared);
        }

        [Test]
        public void Victory_GivesAllStageGoldAndMergedMaterials()
        {
            BattleRewards rewards = Calculate(Result(true, 3, Adventurer(1, 3), Adventurer(2, 3)));

            Assert.AreEqual(550, rewards.Gold);
            Assert.AreEqual(2, rewards.Materials.Count);
            Assert.AreEqual(Material(1, 3), rewards.Materials[0]);
            Assert.AreEqual(Material(2, 1), rewards.Materials[1]);
        }

        [Test]
        public void Defeat_GivesRewardsOnlyForClearedStages()
        {
            BattleRewards rewards = Calculate(Result(false, 1, Adventurer(1, 1, true), Adventurer(2, 0, true)));

            Assert.AreEqual(100, rewards.Gold);
            Assert.AreEqual(1, rewards.Materials.Count);
            Assert.AreEqual(Material(1, 1), rewards.Materials[0]);
        }

        [Test]
        public void Experience_CountsStagesClearedBeforeFalling()
        {
            BattleRewards rewards = Calculate(Result(true, 3, Adventurer(1, 3), Adventurer(2, 1, true)));

            Assert.AreEqual(220, rewards.Adventurers[0].ExperienceGained);
            Assert.AreEqual(40, rewards.Adventurers[1].ExperienceGained);
        }

        [Test]
        public void DeadAdventurer_GetsNoExperience()
        {
            BattleRewards rewards = Calculate(Result(false, 2, Adventurer(1, 2, true, true), Adventurer(2, 2)));

            Assert.AreEqual(0, rewards.Adventurers[0].ExperienceGained);
            Assert.IsFalse(rewards.Adventurers[0].IsLevelUp);
        }

        [Test]
        public void Experience_LevelsUpAndCarriesOver()
        {
            // 모험가 2: 90 + 40 = 130 → 100을 써서 2레벨, 30이 남는다.
            BattleRewards rewards = Calculate(Result(true, 3, Adventurer(1, 0, true), Adventurer(2, 1)));

            AdventurerReward reward = rewards.Adventurers[1];
            Assert.IsTrue(reward.IsLevelUp);
            Assert.AreEqual(1, reward.PreviousLevel);
            Assert.AreEqual(2, reward.Progress.Level);
            Assert.AreEqual(30, reward.Progress.Experience);
        }

        [Test]
        public void AddExperience_CanGainSeveralLevelsAtOnce()
        {
            // 1→2에 100, 2→3에 150, 3→4에 200이 필요하다.
            AdventurerLevelProgress progress = AdventurerLeveling.AddExperience(1, 0, 460, LevelRules);

            Assert.AreEqual(4, progress.Level);
            Assert.AreEqual(10, progress.Experience);
        }

        [Test]
        public void GetRequiredExperience_GrowsPerLevel()
        {
            Assert.AreEqual(100, AdventurerLeveling.GetRequiredExperience(1, LevelRules));
            Assert.AreEqual(150, AdventurerLeveling.GetRequiredExperience(2, LevelRules));
            Assert.AreEqual(550, AdventurerLeveling.GetRequiredExperience(10, LevelRules));
        }
    }
}
