using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Battle;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class AdventurerSetupRulesTests
    {
        private static readonly JobBattleStats JobStats = new JobBattleStats
        {
            AttackRange = 120,
            AttackIntervalTicks = 20,
            MoveSpeed = 18,
            MaxMana = 1000,
            ManaPerTick = 10,
            ManaPerHit = 150,
        };

        private static readonly SkillData Skill = new SkillData { Kind = SkillKind.AreaDamage, PowerPercent = 220, Radius = 250 };

        private static AdventurerData CreateAdventurer(int level)
        {
            return new AdventurerData
            {
                AdventurerId = 7,
                Level = level,
                BaseStats = new AdventurerStats { MaxHp = 400, Attack = 40, Defense = 6 },
                GrowthStats = new AdventurerStats { MaxHp = 30, Attack = 3, Defense = 1 },
            };
        }

        [Test]
        public void Create_CopiesAdventurerId()
        {
            AdventurerSetup setup = AdventurerSetupRules.Create(CreateAdventurer(1), JobStats, Skill);

            Assert.AreEqual(7, setup.AdventurerId);
        }

        [Test]
        public void Create_UsesStatsAtAdventurerLevel()
        {
            AdventurerData adventurer = CreateAdventurer(5);
            AdventurerStats expected = AdventurerStatRules.GetCurrentStats(adventurer);

            UnitStats stats = AdventurerSetupRules.Create(adventurer, JobStats, Skill).Stats;

            Assert.AreEqual(expected.MaxHp, stats.MaxHp);
            Assert.AreEqual(expected.Attack, stats.Attack);
            Assert.AreEqual(expected.Defense, stats.Defense);
            Assert.AreEqual(520, stats.MaxHp);
        }

        [Test]
        public void Create_CopiesJobBattleStats()
        {
            UnitStats stats = AdventurerSetupRules.Create(CreateAdventurer(1), JobStats, Skill).Stats;

            Assert.AreEqual(JobStats.AttackRange, stats.AttackRange);
            Assert.AreEqual(JobStats.AttackIntervalTicks, stats.AttackIntervalTicks);
            Assert.AreEqual(JobStats.MoveSpeed, stats.MoveSpeed);
            Assert.AreEqual(JobStats.MaxMana, stats.MaxMana);
            Assert.AreEqual(JobStats.ManaPerTick, stats.ManaPerTick);
            Assert.AreEqual(JobStats.ManaPerHit, stats.ManaPerHit);
        }

        [Test]
        public void Create_UsesGivenSkill()
        {
            AdventurerSetup setup = AdventurerSetupRules.Create(CreateAdventurer(1), JobStats, Skill);

            Assert.AreEqual(Skill, setup.Skill);
        }

        [Test]
        public void Create_SameGeneratedAdventurer_GivesSameBattleResult()
        {
            var rules = new AdventurerGenerationRules
            {
                Names = new[] { "A" },
                Jobs = new[]
                {
                    new AdventurerJobRules
                    {
                        JobId = 1,
                        BaseStats = new AdventurerStatRange
                        {
                            MaxHp = new IntRange(400, 480),
                            Attack = new IntRange(34, 42),
                            Defense = new IntRange(5, 8),
                        },
                        GrowthStats = new AdventurerStatRange
                        {
                            MaxHp = new IntRange(20, 35),
                            Attack = new IntRange(2, 4),
                            Defense = new IntRange(1, 2),
                        },
                    },
                },
                StartLevel = 1,
                HairPresetCount = 1,
                EyePresetCount = 1,
                MouthPresetCount = 1,
                HairColorCount = 1,
                SkinColorCount = 1,
                CandidateCount = new IntRange(1, 1),
            };

            Assert.AreEqual(Simulate(rules), Simulate(rules));
        }

        private static string Simulate(AdventurerGenerationRules rules)
        {
            AdventurerData adventurer = AdventurerGenerator.Generate(42, rules);
            adventurer.AdventurerId = 1;
            var setup = new BattleSetup
            {
                Seed = 3,
                Rules = new BattleRules
                {
                    TicksPerSecond = 20,
                    MaxTicks = 20000,
                    MarchSpeed = 10,
                    FormationSpacing = 100,
                    StageSpawnDistance = 1000,
                    DamageVariancePercent = 10,
                    DeathChancePercent = 10,
                },
            };
            setup.Adventurers.Add(AdventurerSetupRules.Create(adventurer, JobStats, Skill));
            var stage = new StageSetup();
            stage.Monsters.Add(new MonsterSetup
            {
                MonsterId = 1,
                Stats = new UnitStats { MaxHp = 150, Attack = 15, Defense = 2, AttackRange = 100, AttackIntervalTicks = 25, MoveSpeed = 12 },
            });
            setup.Stages.Add(stage);

            BattleResult result = new BattleSimulation(setup).RunToEnd();
            return $"{result.IsVictory}/{result.StagesCleared}/{result.EndTick}/{result.Adventurers[0].DamageDealt}";
        }
    }
}
