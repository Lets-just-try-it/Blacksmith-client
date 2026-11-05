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

        // 웨이브 골드·경험치는 그 웨이브 몬스터의 합이다: 100/40, 150/60, 300/120
        private static List<WaveReward> Waves()
        {
            return new List<WaveReward>
            {
                Wave(new MonsterReward { Gold = 100, Experience = 40, Materials = { Material(1, 1) } }),
                Wave(new MonsterReward { Gold = 75, Experience = 30 }, new MonsterReward { Gold = 75, Experience = 30 }),
                Wave(new MonsterReward { Gold = 300, Experience = 120, Materials = { Material(1, 2), Material(2, 1) } }),
            };
        }

        private static WaveReward Wave(params MonsterReward[] monsters)
        {
            return new WaveReward(monsters);
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

        private static BattleRewards Calculate(BattleResult result, AccessoryEffect accessory1 = default,
            AccessoryEffect accessory2 = default)
        {
            Dictionary<int, AdventurerData> party = Party();
            return BattleRewardRules.Calculate(Setup(accessory1, accessory2), Waves(), result, id => party[id], LevelRules);
        }

        private static BattleSetup Setup(AccessoryEffect accessory1, AccessoryEffect accessory2)
        {
            var setup = new BattleSetup { Seed = 7 };
            setup.Adventurers.Add(new AdventurerSetup { AdventurerId = 1, Accessory = accessory1 });
            setup.Adventurers.Add(new AdventurerSetup { AdventurerId = 2, Accessory = accessory2 });
            return setup;
        }

        private static AccessoryEffect Effect(AccessoryEffectKind kind, int percent)
        {
            return new AccessoryEffect(kind, percent);
        }

        private static ChanceDrop Drop(int rolls, int chancePercent, params int[] materialIds)
        {
            return new ChanceDrop { Rolls = rolls, ChancePercent = chancePercent, MaterialIds = materialIds };
        }

        private static int CountOf(IReadOnlyList<MaterialReward> materials, int materialId)
        {
            int count = 0;
            foreach (MaterialReward material in materials)
            {
                if (material.MaterialId == materialId)
                {
                    count += material.Count;
                }
            }

            return count;
        }

        private static BattleResult Result(bool isVictory, int wavesCleared, params AdventurerBattleResult[] adventurers)
        {
            return new BattleResult(isVictory, wavesCleared, 100, adventurers);
        }

        private static AdventurerBattleResult Adventurer(int id, int wavesCleared, bool isInjured = false, bool isDead = false)
        {
            return new AdventurerBattleResult(id, isInjured, isDead, 0, wavesCleared);
        }

        [Test]
        public void Victory_GivesAllWaveGoldAndMergedMaterials()
        {
            BattleRewards rewards = Calculate(Result(true, 3, Adventurer(1, 3), Adventurer(2, 3)));

            Assert.AreEqual(550, rewards.Gold);
            Assert.AreEqual(2, rewards.Materials.Count);
            Assert.AreEqual(Material(1, 3), rewards.Materials[0]);
            Assert.AreEqual(Material(2, 1), rewards.Materials[1]);
        }

        [Test]
        public void Defeat_GivesRewardsOnlyForClearedWaves()
        {
            BattleRewards rewards = Calculate(Result(false, 1, Adventurer(1, 1, true), Adventurer(2, 0, true)));

            Assert.AreEqual(100, rewards.Gold);
            Assert.AreEqual(1, rewards.Materials.Count);
            Assert.AreEqual(Material(1, 1), rewards.Materials[0]);
        }

        [Test]
        public void Experience_CountsWavesClearedBeforeFalling()
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
        public void GoldAccessories_SumAcrossPartyAndRoundUp()
        {
            // 클리어 골드 550에 10% + 3% = 13% → 71.5를 올려 72
            BattleRewards rewards = Calculate(Result(true, 3, Adventurer(1, 3), Adventurer(2, 3)),
                Effect(AccessoryEffectKind.GoldBonus, 10), Effect(AccessoryEffectKind.GoldBonus, 3));

            Assert.AreEqual(72, rewards.GoldBonus);
            Assert.AreEqual(622, rewards.Gold);
        }

        [Test]
        public void MaterialAccessory_BoostsOreButNotGems()
        {
            var waves = new List<WaveReward>
            {
                Wave(new MonsterReward { Materials = { Material(13, 10) }, GemDrops = { Drop(3, 100, 18) } }),
            };
            Dictionary<int, AdventurerData> party = Party();
            BattleRewards rewards = BattleRewardRules.Calculate(Setup(Effect(AccessoryEffectKind.MaterialBonus, 2), default),
                waves, Result(true, 1, Adventurer(1, 1), Adventurer(2, 1)), id => party[id], LevelRules);

            Assert.AreEqual(11, CountOf(rewards.Materials, 13));
            Assert.AreEqual(1, CountOf(rewards.BonusMaterials, 13));
            Assert.AreEqual(3, CountOf(rewards.Materials, 18));
            Assert.AreEqual(0, CountOf(rewards.BonusMaterials, 18));
        }

        [Test]
        public void ExperienceAccessory_BoostsOnlyWearer()
        {
            BattleRewards rewards = Calculate(Result(true, 3, Adventurer(1, 3), Adventurer(2, 3)),
                Effect(AccessoryEffectKind.ExperienceBonus, 15));

            Assert.AreEqual(220 + 33, rewards.Adventurers[0].ExperienceGained);
            Assert.AreEqual(33, rewards.Adventurers[0].ExperienceBonus);
            Assert.AreEqual(220, rewards.Adventurers[1].ExperienceGained);
        }

        [Test]
        public void Defeat_KeepsLootOfClearedWavesOnly()
        {
            BattleRewards rewards = Calculate(Result(false, 1, Adventurer(1, 1, true), Adventurer(2, 1, true)));

            Assert.AreEqual(1, rewards.WaveLoots.Count);
        }

        [Test]
        public void DropRoll_GivesOneLootPerMonster()
        {
            IReadOnlyList<WaveLoot> loots = BattleDropRules.Roll(Waves(), 3);

            Assert.AreEqual(3, loots.Count);
            Assert.AreEqual(1, loots[0].Monsters.Count);
            Assert.AreEqual(2, loots[1].Monsters.Count);
            Assert.AreEqual(1, CountOf(loots[0].Monsters[0].Materials, 1));
        }

        [Test]
        public void DropRoll_MoreMonstersRollMoreTimes()
        {
            var monster = new MonsterReward { ChanceDrops = { Drop(1, 100, 15) } };
            var waves = new List<WaveReward> { Wave(monster, monster), Wave(monster, monster, monster) };
            Dictionary<int, AdventurerData> party = Party();

            BattleRewards rewards = BattleRewardRules.Calculate(Setup(default, default), waves,
                Result(true, 2, Adventurer(1, 2), Adventurer(2, 2)), id => party[id], LevelRules);

            Assert.AreEqual(5, CountOf(rewards.Materials, 15));
        }

        [Test]
        public void DropRoll_SameSeedGivesSameLoot()
        {
            var monster = new MonsterReward { ChanceDrops = { Drop(2, 50, 15), Drop(2, 50, 16) }, GemDrops = { Drop(2, 30, 18, 19, 20, 21) } };
            var waves = new List<WaveReward> { Wave(monster, monster), Wave(monster) };

            IReadOnlyList<WaveLoot> first = BattleDropRules.Roll(waves, 42);
            IReadOnlyList<WaveLoot> second = BattleDropRules.Roll(waves, 42);

            for (int i = 0; i < waves.Count; i++)
            {
                for (int j = 0; j < waves[i].Monsters.Count; j++)
                {
                    CollectionAssert.AreEqual(first[i].Monsters[j].Materials, second[i].Monsters[j].Materials);
                    CollectionAssert.AreEqual(first[i].Monsters[j].Gems, second[i].Monsters[j].Gems);
                }
            }
        }

        [Test]
        public void DropRoll_ChanceBoundsAreRespected()
        {
            var waves = new List<WaveReward>
            {
                Wave(new MonsterReward { ChanceDrops = { Drop(4, 100, 15), Drop(4, 0, 16) }, GemDrops = { Drop(2, 100, 18, 19) } }),
            };

            MonsterLoot loot = BattleDropRules.Roll(waves, 1)[0].Monsters[0];

            Assert.AreEqual(4, CountOf(loot.Materials, 15));
            Assert.AreEqual(0, CountOf(loot.Materials, 16));
            Assert.AreEqual(2, CountOf(loot.Gems, 18) + CountOf(loot.Gems, 19));
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
