using System.Collections.Generic;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Battle;
using Blacksmith.Core.Inventory;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class BattleSimulationTests
    {
        // 테스트 전용 수치다. 실제 밸런스는 ScriptableObject 데이터를 따른다.
        private static BattleRules CreateRules(int deathChancePercent = 30)
        {
            InjuryBalance injury = InjuryRulesTests.CreateBalance();
            injury.BaseDeathChancePercent = deathChancePercent;
            return new BattleRules
            {
                TicksPerSecond = 20,
                MaxTicks = 20000,
                MarchSpeed = 10,
                FormationSpacing = 100,
                LaneSpacing = 160,
                DamageVariancePercent = 10,
                Injury = injury,
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

        private static MonsterSetup Monster(int id, int hp, int attack, int offset = 0)
        {
            return new MonsterSetup { MonsterId = id, Stats = Stats(hp, attack), SpawnOffset = offset, SightRange = 1500 };
        }

        private static BattleSetup CreateSetup(int seed, int adventurerAttack, int monsterAttack)
        {
            var setup = new BattleSetup { Seed = seed, Rules = CreateRules(), Map = BattleTestMaps.Open() };
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
            setup.Groups.Add(BattleTestMaps.Group(false, 18.5f, Monster(10, 80, monsterAttack), Monster(10, 80, monsterAttack, 150)));
            setup.Groups.Add(BattleTestMaps.Group(false, 38.5f, Monster(11, 120, monsterAttack), Monster(11, 120, monsterAttack, 150)));
            setup.Groups.Add(BattleTestMaps.Group(true, 58.5f, Monster(20, 600, monsterAttack * 2)));
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

        private static BattleSimulation CreateFormation(params int[] ranges)
        {
            BattleRules rules = CreateRules();
            rules.MeleeRangeMax = 200;
            rules.MaxRowSize = 3;
            var setup = new BattleSetup { Seed = 1, Rules = rules, Map = BattleTestMaps.Open() };
            for (int i = 0; i < ranges.Length; i++)
            {
                setup.Adventurers.Add(new AdventurerSetup { AdventurerId = i + 1, Stats = Stats(100, 10, ranges[i]) });
            }

            setup.Groups.Add(BattleTestMaps.Group(false, 60.5f, Monster(10, 80, 5)));
            return new BattleSimulation(setup);
        }

        // 첫 무리가 +X 쪽에 있어 대열은 +X를 본다. 줄은 뒤(-X)로 100씩, 옆자리 반 칸은 -Z 쪽으로 80씩 벌어진다.
        private static void AssertSpot(BattleSimulation simulation, int partyIndex, int lane, int position)
        {
            BattleUnit unit = simulation.Units[partyIndex];
            Assert.AreEqual(partyIndex + 1, unit.SourceId);
            Assert.AreEqual(lane, unit.Lane, $"#{partyIndex + 1} lane");
            Assert.AreEqual(250 + position, unit.X, $"#{partyIndex + 1} x");
            Assert.AreEqual(1250 - lane * 80, unit.Z, $"#{partyIndex + 1} z");
        }

        [Test]
        public void Formation_PlacesMeleeInFrontRowAndRangedInBackRow()
        {
            BattleSimulation simulation = CreateFormation(450, 120, 400, 120);

            AssertSpot(simulation, 1, -1, 0);
            AssertSpot(simulation, 3, 1, 0);
            AssertSpot(simulation, 2, -1, -100);
            AssertSpot(simulation, 0, 1, -100);
        }

        [Test]
        public void Formation_SingleTypePartyStandsInOneFrontRow()
        {
            BattleSimulation simulation = CreateFormation(450, 400, 450);

            AssertSpot(simulation, 1, -2, 0);
            AssertSpot(simulation, 0, 0, 0);
            AssertSpot(simulation, 2, 2, 0);
        }

        [Test]
        public void Formation_CentersEachRowOnPath()
        {
            BattleSimulation simulation = CreateFormation(450, 450, 120, 400);

            AssertSpot(simulation, 2, 0, 0);
            AssertSpot(simulation, 3, -2, -100);
            AssertSpot(simulation, 0, 0, -100);
            AssertSpot(simulation, 1, 2, -100);
        }

        [Test]
        public void Formation_FourMeleeOneRanged_Forms221()
        {
            BattleSimulation simulation = CreateFormation(120, 120, 120, 120, 450);

            AssertSpot(simulation, 0, -1, 0);
            AssertSpot(simulation, 1, 1, 0);
            AssertSpot(simulation, 2, -1, -100);
            AssertSpot(simulation, 3, 1, -100);
            AssertSpot(simulation, 4, 0, -200);
        }

        [Test]
        public void Formation_OneMeleeFourRanged_Forms122()
        {
            BattleSimulation simulation = CreateFormation(450, 120, 400, 450, 450);

            AssertSpot(simulation, 1, 0, 0);
            AssertSpot(simulation, 2, -1, -100);
            AssertSpot(simulation, 0, 1, -100);
            AssertSpot(simulation, 3, -1, -200);
            AssertSpot(simulation, 4, 1, -200);
        }

        [Test]
        public void Formation_TwoMeleeThreeRanged_Forms23WithoutSplittingRanged()
        {
            BattleSimulation simulation = CreateFormation(450, 120, 400, 120, 450);

            AssertSpot(simulation, 1, -1, 0);
            AssertSpot(simulation, 3, 1, 0);
            AssertSpot(simulation, 2, -2, -100);
            AssertSpot(simulation, 0, 0, -100);
            AssertSpot(simulation, 4, 2, -100);
        }

        [Test]
        public void Formation_ThreeMeleeTwoRanged_Forms32()
        {
            BattleSimulation simulation = CreateFormation(120, 450, 120, 400, 120);

            AssertSpot(simulation, 0, -2, 0);
            AssertSpot(simulation, 2, 0, 0);
            AssertSpot(simulation, 4, 2, 0);
            AssertSpot(simulation, 3, -1, -100);
            AssertSpot(simulation, 1, 1, -100);
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
        public void StrongParty_ClearsAllGroups()
        {
            BattleResult result = new BattleSimulation(CreateSetup(1, 60, 5)).RunToEnd();

            Assert.IsTrue(result.IsVictory);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, result.ClearedGroups);
        }

        [Test]
        public void StrongParty_EveryAdventurerClearsAllGroups()
        {
            BattleResult result = new BattleSimulation(CreateSetup(1, 60, 5)).RunToEnd();

            foreach (AdventurerBattleResult adventurer in result.Adventurers)
            {
                Assert.IsFalse(adventurer.IsFallen);
                Assert.IsFalse(adventurer.IsDead);
                Assert.AreEqual(3, adventurer.GroupsCleared);
            }
        }

        [Test]
        public void InjuryBeforeBattle_AddsUpAndCanKillWithoutFalling()
        {
            BattleSetup setup = CreateSetup(1, 60, 5);
            setup.Rules = CreateRules(deathChancePercent: 100);
            setup.Adventurers[0].Injury = 95;

            BattleResult result = new BattleSimulation(setup).RunToEnd();

            Assert.IsFalse(result.Adventurers[0].IsFallen);
            Assert.GreaterOrEqual(result.Adventurers[0].Injury, 95);
            Assert.IsTrue(result.Adventurers[0].IsDead);
            Assert.IsFalse(result.Adventurers[1].IsDead);
        }

        [Test]
        public void AdventurerGroupsCleared_CountsOnlyClearsBeforeFalling()
        {
            // 이 시드에서는 모험가 1이 중간에 쓰러지고 모험가 2가 끝까지 남아 승리한다.
            var simulation = new BattleSimulation(CreateSetup(1, 15, 10));
            var unitToAdventurer = new Dictionary<int, int>();
            foreach (BattleUnit unit in simulation.Units)
            {
                unitToAdventurer[unit.UnitId] = unit.SourceId;
            }

            var fallen = new HashSet<int>();
            var expected = new Dictionary<int, int> { { 1, 0 }, { 2, 0 } };
            while (!simulation.IsFinished)
            {
                foreach (BattleEvent e in simulation.Step())
                {
                    if (e.Type == BattleEventType.Fell && unitToAdventurer.TryGetValue(e.TargetUnitId, out int fellId))
                    {
                        fallen.Add(fellId);
                    }

                    if (e.Type == BattleEventType.GroupCleared)
                    {
                        foreach (int adventurerId in new[] { 1, 2 })
                        {
                            if (!fallen.Contains(adventurerId))
                            {
                                expected[adventurerId]++;
                            }
                        }
                    }
                }
            }

            foreach (AdventurerBattleResult adventurer in simulation.Result.Adventurers)
            {
                Assert.AreEqual(expected[adventurer.AdventurerId], adventurer.GroupsCleared);
                Assert.LessOrEqual(adventurer.GroupsCleared, simulation.Result.GroupsCleared);
            }

            Assert.AreNotEqual(expected[1], expected[2]);
        }

        [Test]
        public void WeakParty_LosesWhenAllAdventurersFall()
        {
            BattleResult result = new BattleSimulation(CreateSetup(1, 3, 60)).RunToEnd();

            Assert.IsFalse(result.IsVictory);
            foreach (AdventurerBattleResult adventurer in result.Adventurers)
            {
                Assert.IsTrue(adventurer.IsFallen);
                Assert.AreEqual(InjuryRulesTests.CreateBalance().FallenInjury, adventurer.InjuryGained);
            }
        }

        [Test]
        public void DeathImmunity_AlwaysSavesAndBreaksAccessory()
        {
            BattleSetup setup = CreateSetup(1, 3, 60);
            setup.Rules = CreateRules(deathChancePercent: 100);
            setup.Adventurers[0].Accessory = new AccessoryEffect(AccessoryEffectKind.DeathImmunity, 100);

            BattleResult result = new BattleSimulation(setup).RunToEnd();

            Assert.IsFalse(result.Adventurers[0].IsDead);
            Assert.IsTrue(result.Adventurers[0].IsAccessoryBroken);
            Assert.IsTrue(result.Adventurers[1].IsDead);
            Assert.IsFalse(result.Adventurers[1].IsAccessoryBroken);
        }

        [Test]
        public void Durability_WeaponWearsPerAttack_ArmorPerHitTaken()
        {
            BattleSetup setup = CreateSetup(3, 30, 12);
            setup.Adventurers[0].Weapon = new EquipmentDurability(5, 100000, 3);
            setup.Adventurers[0].Helmet = new EquipmentDurability(0, 100000, 2);
            setup.Adventurers[0].Armor = new EquipmentDurability(0, 100000, 1);
            var simulation = new BattleSimulation(setup);
            int unitId = simulation.Units[0].UnitId;
            int hitsTaken = 0;
            while (!simulation.IsFinished)
            {
                foreach (BattleEvent e in simulation.Step())
                {
                    if (e.Type == BattleEventType.Damaged && e.TargetUnitId == unitId)
                    {
                        hitsTaken++;
                    }
                }
            }

            AdventurerBattleResult result = simulation.Result.Adventurers[0];
            int attacks = simulation.Units[0].AttackCount;
            Assert.Greater(attacks, 0);
            Assert.Greater(hitsTaken, 0);
            Assert.AreEqual(5 + attacks * 3, result.Weapon.Wear);
            Assert.AreEqual(hitsTaken * 2, result.Helmet.Wear);
            Assert.AreEqual(hitsTaken, result.Armor.Wear);
            Assert.IsFalse(simulation.Result.Adventurers[1].Weapon.HasDurability);
            Assert.AreEqual(0, simulation.Result.Adventurers[1].Weapon.Wear);
        }

        [Test]
        public void Durability_RunningOutDuringBattle_DoesNotChangeTheBattle()
        {
            BattleSetup worn = CreateSetup(3, 30, 12);
            foreach (AdventurerSetup adventurer in worn.Adventurers)
            {
                adventurer.Weapon = new EquipmentDurability(0, 1, DurabilityRules.WearUnitsPerDurability);
                adventurer.Armor = new EquipmentDurability(0, 1, DurabilityRules.WearUnitsPerDurability);
            }

            var simulation = new BattleSimulation(worn);
            List<string> log = Record(simulation);

            Assert.AreEqual(Record(new BattleSimulation(CreateSetup(3, 30, 12))), log);
            Assert.IsTrue(simulation.Result.Adventurers[0].Weapon.IsBroken);
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
        public void HpAndMana_CarryOverToNextGroup()
        {
            BattleSetup setup = CreateSetup(5, 30, 12);
            setup.Adventurers.RemoveAt(1);
            var simulation = new BattleSimulation(setup);
            while (!simulation.IsFinished)
            {
                foreach (BattleEvent e in simulation.Step())
                {
                    if (e.Type == BattleEventType.GroupEngaged && e.Amount > 0)
                    {
                        Assert.Less(simulation.Units[0].Hp, simulation.Units[0].Stats.MaxHp);
                        return;
                    }
                }
            }

            Assert.Fail("두 번째 무리와 싸우지 않았다.");
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

        // 쓰러지지 않는 모험가 한 명과 보스 한 마리만 싸운다. 보스는 UnitId 1이다.
        private static BattleSetup CreateBossSetup(int adventurerAttack, MonsterSetup boss)
        {
            var setup = new BattleSetup { Seed = 11, Rules = CreateRules(), Map = BattleTestMaps.Open() };
            setup.Adventurers.Add(new AdventurerSetup { AdventurerId = 1, Stats = Stats(100000, adventurerAttack, maxMana: 0) });
            setup.Groups.Add(BattleTestMaps.Group(true, 20.5f, boss));
            return setup;
        }

        [Test]
        public void BossSkills_CastInPatternOrder()
        {
            MonsterSetup boss = Monster(20, 100000, 10);
            boss.Skills = new[]
            {
                new SkillData { Kind = SkillKind.SingleDamage, PowerPercent = 200 },
                new SkillData { Kind = SkillKind.AreaDamage, PowerPercent = 150, Radius = 300 },
            };
            var simulation = new BattleSimulation(CreateBossSetup(5, boss));
            var casts = new List<int>();
            while (!simulation.IsFinished && casts.Count < 3)
            {
                foreach (BattleEvent e in simulation.Step())
                {
                    if (e.Type == BattleEventType.SkillCast && e.SourceUnitId == 1)
                    {
                        casts.Add(e.Amount);
                    }
                }
            }

            CollectionAssert.AreEqual(
                new[] { (int)SkillKind.SingleDamage, (int)SkillKind.AreaDamage, (int)SkillKind.SingleDamage }, casts);
        }

        [Test]
        public void Enrage_TriggersOnceBelowThresholdAndBoostsStats()
        {
            MonsterSetup boss = Monster(20, 1000, 10);
            boss.Enrage = new EnrageData { HpPercent = 50, AttackBonusPercent = 50, AttackIntervalReductionPercent = 50 };
            var simulation = new BattleSimulation(CreateBossSetup(40, boss));
            int enrageCount = 0;
            while (!simulation.IsFinished)
            {
                foreach (BattleEvent e in simulation.Step())
                {
                    if (e.Type == BattleEventType.Enraged)
                    {
                        enrageCount++;
                        BattleUnit unit = simulation.Units[e.SourceUnitId];
                        Assert.AreEqual(1, unit.UnitId);
                        Assert.LessOrEqual(unit.Hp * 100, unit.Stats.MaxHp * 50);
                        Assert.AreEqual(15, unit.Stats.Attack);
                        Assert.AreEqual(10, unit.Stats.AttackIntervalTicks);
                    }
                }
            }

            Assert.IsTrue(simulation.Result.IsVictory);
            Assert.AreEqual(1, enrageCount);
        }

        [Test]
        public void MonsterWithoutEnrage_NeverEnrages()
        {
            var simulation = new BattleSimulation(CreateBossSetup(40, Monster(20, 1000, 10)));
            while (!simulation.IsFinished)
            {
                foreach (BattleEvent e in simulation.Step())
                {
                    Assert.AreNotEqual(BattleEventType.Enraged, e.Type);
                }
            }

            Assert.IsFalse(simulation.Units[1].IsEnraged);
        }

        [Test]
        public void Party_GoesToNearestGroupFirst()
        {
            var setup = new BattleSetup { Seed = 2, Rules = CreateRules(), Map = BattleTestMaps.Open() };
            setup.Adventurers.Add(new AdventurerSetup { AdventurerId = 1, Stats = Stats(100000, 60, maxMana: 0) });
            setup.Groups.Add(BattleTestMaps.Group(false, 70.5f, Monster(10, 50, 1)));
            setup.Groups.Add(BattleTestMaps.Group(false, 25.5f, Monster(11, 50, 1)));

            BattleResult result = new BattleSimulation(setup).RunToEnd();

            Assert.IsTrue(result.IsVictory);
            CollectionAssert.AreEqual(new[] { 1, 0 }, result.ClearedGroups);
        }

        [Test]
        public void Group_WaitsAtSpawnUntilAdventurerInSight()
        {
            var simulation = new BattleSimulation(CreateSetup(4, 30, 12));
            BattleUnit far = simulation.Units[simulation.Units.Count - 1];
            int startX = far.X;
            while (!simulation.IsFinished)
            {
                foreach (BattleEvent e in simulation.Step())
                {
                    if (e.Type == BattleEventType.GroupEngaged && e.Amount == far.GroupIndex)
                    {
                        return;
                    }
                }

                Assert.AreEqual(startX, far.X);
                Assert.IsFalse(far.IsActive);
            }

            Assert.Fail("두 번째 무리와 싸우지 않았다.");
        }

        [Test]
        public void Boss_AppearsOnlyAfterMobGroupsCleared()
        {
            var simulation = new BattleSimulation(CreateSetup(1, 60, 5));
            int mobGroupsCleared = 0;
            while (!simulation.IsFinished)
            {
                foreach (BattleEvent e in simulation.Step())
                {
                    if (e.Type == BattleEventType.GroupCleared && e.Amount < 2)
                    {
                        mobGroupsCleared++;
                    }
                }

                bool hasBoss = simulation.Units.Count > 2 + 4;
                Assert.AreEqual(mobGroupsCleared == 2, hasBoss);
            }
        }

        [Test]
        public void Party_WalksAroundWallsAndStaysOnFloor()
        {
            // ㄱ자 맵: 아래쪽 가로 길(z 0~11)과 오른쪽 세로 길(x 48~59). 무리는 세로 길 위쪽 끝에 있다.
            var floor = new bool[60, 60];
            for (int x = 0; x < 60; x++)
            {
                for (int z = 0; z < 60; z++)
                {
                    floor[x, z] = z < 12 || x >= 48;
                }
            }

            var map = new StageMap(floor, new MapPoint(3.5f, 6.5f));
            var setup = new BattleSetup { Seed = 5, Rules = CreateRules(), Map = map };
            setup.Adventurers.Add(new AdventurerSetup { AdventurerId = 1, Stats = Stats(100000, 60, 120, 0) });
            setup.Adventurers.Add(new AdventurerSetup { AdventurerId = 2, Stats = Stats(100000, 60, 450, 0) });
            setup.Groups.Add(new MonsterGroupSetup { Spawn = new MapPoint(53.5f, 55.5f), Monsters = { Monster(10, 50, 1) } });

            var simulation = new BattleSimulation(setup);
            while (!simulation.IsFinished)
            {
                foreach (BattleEvent e in simulation.Step())
                {
                    // 모퉁이를 돈 뒤에도 뒤처진 모험가 없이 함께 무리를 만난다.
                    if (e.Type == BattleEventType.GroupEngaged)
                    {
                        long dx = simulation.Units[0].X - simulation.Units[1].X;
                        long dz = simulation.Units[0].Z - simulation.Units[1].Z;
                        Assert.LessOrEqual(dx * dx + dz * dz, 600L * 600L);
                    }
                }

                foreach (BattleUnit unit in simulation.Units)
                {
                    Assert.IsTrue(map.IsFloor(unit.X / BattleSpace.UnitsPerCell, unit.Z / BattleSpace.UnitsPerCell),
                        $"{simulation.Tick}틱 유닛 {unit.UnitId}: ({unit.X}, {unit.Z})");
                }
            }

            Assert.IsTrue(simulation.Result.IsVictory);
        }

        [Test]
        public void Adventurer_ClosesGapShorterThanOneStepAxis()
        {
            // 모험가(사거리 150)와 몬스터 사이가 비스듬히 151이라 한 걸음이 1이다. 내림하면 두 축 모두 0이 되어 멈췄다.
            var setup = new BattleSetup { Seed = 3, Rules = CreateRules(), Map = BattleTestMaps.Open() };
            setup.Adventurers.Add(new AdventurerSetup { AdventurerId = 1, Stats = Stats(100000, 60, 150, 0) });
            var monster = new MonsterSetup { MonsterId = 10, Stats = Stats(50, 1, 200), SightRange = 1500 };
            setup.Groups.Add(new MonsterGroupSetup { Spawn = new MapPoint(2.81f, BattleTestMaps.CenterZ + 1.48f), Monsters = { monster } });

            BattleResult result = new BattleSimulation(setup).RunToEnd();

            Assert.IsTrue(result.IsVictory);
            Assert.Less(result.EndTick, 200);
        }

        [Test]
        public void FallenMonsters_LeaveSpreadCorpsesNearWhereTheyFell()
        {
            var simulation = new BattleSimulation(CreateSetup(1, 60, 5));
            var falls = new List<BattleUnit>();
            var fallSpots = new List<(int X, int Z)>();
            while (!simulation.IsFinished)
            {
                foreach (BattleEvent e in simulation.Step())
                {
                    BattleUnit unit = e.Type == BattleEventType.Fell ? simulation.Units[e.TargetUnitId] : null;
                    if (unit != null && unit.Team == BattleTeam.Monster)
                    {
                        falls.Add(unit);
                        fallSpots.Add((unit.X, unit.Z));
                    }
                }
            }

            IReadOnlyList<BattleCorpse> corpses = simulation.Result.Corpses;
            Assert.AreEqual(5, corpses.Count);
            for (int i = 0; i < corpses.Count; i++)
            {
                Assert.AreEqual(falls[i].SourceId, corpses[i].MonsterId);
                Assert.AreEqual(falls[i].GroupIndex, corpses[i].GroupIndex);
                Assert.AreEqual(falls[i].GroupSlot, corpses[i].GroupSlot);
                long dx = corpses[i].X - fallSpots[i].X;
                long dz = corpses[i].Z - fallSpots[i].Z;
                Assert.LessOrEqual(dx * dx + dz * dz, 270L * 270L, $"시체 {i}가 쓰러진 자리에서 너무 멀다");
                for (int j = 0; j < i; j++)
                {
                    long gx = corpses[i].X - corpses[j].X;
                    long gz = corpses[i].Z - corpses[j].Z;
                    if (corpses[i].GroupIndex == corpses[j].GroupIndex)
                    {
                        Assert.GreaterOrEqual(gx * gx + gz * gz, 90L * 90L, $"시체 {j}·{i}가 겹친다");
                    }
                }
            }
        }

        [Test]
        public void PathField_StepsTowardTargetAroundWall()
        {
            var floor = new bool[5, 3];
            for (int x = 0; x < 5; x++)
            {
                floor[x, 0] = floor[x, 2] = true;
            }

            floor[4, 1] = true;
            var field = new StagePathField(new StageMap(floor, new MapPoint(0.5f, 0.5f)), 0, 2);

            Assert.AreEqual(0, field.CostAt(0, 2));
            Assert.AreEqual(StagePathField.Unreachable, field.CostAt(1, 1));
            Assert.IsTrue(field.TryNextCell(0, 0, out int nextX, out int nextZ));
            Assert.AreEqual(1, nextX);
            Assert.AreEqual(0, nextZ);
            Assert.AreEqual(100, field.CostAt(0, 0));
        }
    }
}
