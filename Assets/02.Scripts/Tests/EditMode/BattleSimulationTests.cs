using System.Collections.Generic;
using Blacksmith.Core.Battle;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class BattleSimulationTests
    {
        // 테스트 전용 수치다. 실제 밸런스는 ScriptableObject 데이터를 따른다.
        private static BattleRules CreateRules()
        {
            return new BattleRules
            {
                TicksPerSecond = 20,
                MaxTicks = 20000,
                MarchSpeed = 10,
                FormationSpacing = 100,
                StageSpawnDistance = 1000,
                DamageVariancePercent = 10,
                DeathChancePercent = 30,
            };
        }

        private static UnitStats Stats(int hp, int attack, int range = 100, int maxMana = 1000)
        {
            return new UnitStats
            {
                MaxHp = hp,
                Attack = attack,
                Defense = 2,
                AttackRange = range,
                AttackIntervalTicks = 20,
                MoveSpeed = 15,
                MaxMana = maxMana,
                ManaPerTick = 5,
                ManaPerHit = 100,
            };
        }

        private static StageSetup Stage(bool isBoss, params MonsterSetup[] monsters)
        {
            var stage = new StageSetup { IsBoss = isBoss };
            stage.Monsters.AddRange(monsters);
            return stage;
        }

        private static MonsterSetup Monster(int id, int hp, int attack, int offset = 0)
        {
            return new MonsterSetup { MonsterId = id, Stats = Stats(hp, attack), SpawnOffset = offset };
        }

        private static BattleSetup CreateSetup(int seed, int adventurerAttack, int monsterAttack)
        {
            var setup = new BattleSetup { Seed = seed, Rules = CreateRules() };
            setup.Adventurers.Add(new AdventurerSetup
            {
                AdventurerId = 1,
                Stats = Stats(300, adventurerAttack),
                Skill = new SkillData { Kind = SkillKind.AreaDamage, PowerPercent = 250, Radius = 200 },
            });
            setup.Adventurers.Add(new AdventurerSetup
            {
                AdventurerId = 2,
                Stats = Stats(200, adventurerAttack, 400),
                Skill = new SkillData { Kind = SkillKind.HealLowestAlly, PowerPercent = 150 },
            });
            setup.Stages.Add(Stage(false, Monster(10, 80, monsterAttack), Monster(10, 80, monsterAttack, 150)));
            setup.Stages.Add(Stage(false, Monster(11, 120, monsterAttack), Monster(11, 120, monsterAttack, 150)));
            setup.Stages.Add(Stage(true, Monster(20, 600, monsterAttack * 2)));
            return setup;
        }

        private static List<string> Record(BattleSimulation simulation)
        {
            var log = new List<string>();
            while (!simulation.IsFinished)
            {
                foreach (BattleEvent e in simulation.Step())
                {
                    log.Add($"{simulation.Tick}:{e.Type}:{e.SourceUnitId}:{e.TargetUnitId}:{e.Amount}");
                }
            }

            return log;
        }

        [Test]
        public void SameSeed_ProducesSameEventsAndResult()
        {
            var first = new BattleSimulation(CreateSetup(42, 30, 12));
            var second = new BattleSimulation(CreateSetup(42, 30, 12));

            CollectionAssert.AreEqual(Record(first), Record(second));
            Assert.AreEqual(first.Result.IsVictory, second.Result.IsVictory);
            Assert.AreEqual(first.Result.EndTick, second.Result.EndTick);
        }

        [Test]
        public void StepPlayback_MatchesRunToEnd()
        {
            var host = new BattleSimulation(CreateSetup(7, 30, 12));
            var client = new BattleSimulation(CreateSetup(7, 30, 12));

            BattleResult hostResult = host.RunToEnd();
            Record(client);

            Assert.AreEqual(hostResult.EndTick, client.Result.EndTick);
            Assert.AreEqual(hostResult.IsVictory, client.Result.IsVictory);
            for (int i = 0; i < hostResult.Adventurers.Count; i++)
            {
                Assert.AreEqual(hostResult.Adventurers[i].IsDead, client.Result.Adventurers[i].IsDead);
                Assert.AreEqual(hostResult.Adventurers[i].DamageDealt, client.Result.Adventurers[i].DamageDealt);
            }
        }

        [Test]
        public void StrongParty_ClearsAllStages()
        {
            BattleResult result = new BattleSimulation(CreateSetup(1, 60, 5)).RunToEnd();

            Assert.IsTrue(result.IsVictory);
            Assert.AreEqual(3, result.StagesCleared);
        }

        [Test]
        public void WeakParty_LosesWhenAllAdventurersFall()
        {
            BattleResult result = new BattleSimulation(CreateSetup(1, 3, 60)).RunToEnd();

            Assert.IsFalse(result.IsVictory);
            foreach (AdventurerBattleResult adventurer in result.Adventurers)
            {
                Assert.IsTrue(adventurer.IsInjured);
            }
        }

        [Test]
        public void FullMana_CastsSkillAndResetsMana()
        {
            var simulation = new BattleSimulation(CreateSetup(3, 30, 12));
            bool skillCast = false;
            while (!simulation.IsFinished && !skillCast)
            {
                foreach (BattleEvent e in simulation.Step())
                {
                    if (e.Type == BattleEventType.SkillCast)
                    {
                        skillCast = true;
                        Assert.AreEqual(0, simulation.Units[e.SourceUnitId].Mana);
                    }
                }
            }

            Assert.IsTrue(skillCast);
        }

        [Test]
        public void HpAndMana_CarryOverToNextStage()
        {
            BattleSetup setup = CreateSetup(5, 30, 12);
            setup.Adventurers.RemoveAt(1);
            var simulation = new BattleSimulation(setup);
            while (!simulation.IsFinished)
            {
                foreach (BattleEvent e in simulation.Step())
                {
                    if (e.Type == BattleEventType.StageStarted && e.Amount > 0)
                    {
                        Assert.Less(simulation.Units[0].Hp, simulation.Units[0].Stats.MaxHp);
                        return;
                    }
                }
            }

            Assert.Fail("두 번째 스테이지가 시작되지 않았다.");
        }

        [Test]
        public void FallenAdventurer_StaysFallenAndIsNotTargeted()
        {
            var simulation = new BattleSimulation(CreateSetup(9, 3, 60));
            var fallen = new HashSet<int>();
            while (!simulation.IsFinished)
            {
                foreach (BattleEvent e in simulation.Step())
                {
                    if (e.Type == BattleEventType.Damaged || e.Type == BattleEventType.BasicAttack)
                    {
                        Assert.IsFalse(fallen.Contains(e.TargetUnitId));
                        Assert.IsFalse(fallen.Contains(e.SourceUnitId));
                    }

                    if (e.Type == BattleEventType.Fell)
                    {
                        fallen.Add(e.TargetUnitId);
                    }
                }
            }

            Assert.IsNotEmpty(fallen);
        }
    }
}
