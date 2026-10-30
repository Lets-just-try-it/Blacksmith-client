using System;
using System.Collections.Generic;

namespace Blacksmith.Core.Battle
{
    // 같은 BattleSetup이면 같은 결과가 나온다. 호스트는 RunToEnd로 결과를 정하고,
    // 각 참가자는 같은 설정으로 Step을 틱마다 불러 연출을 재생한다.
    public sealed class BattleSimulation
    {
        private readonly BattleSetup _setup;
        private readonly BattleRules _rules;
        private readonly Random _random;
        private readonly List<BattleUnit> _units = new List<BattleUnit>();
        private readonly List<BattleEvent> _events = new List<BattleEvent>();

        private int _stageIndex = -1;

        public BattleSimulation(BattleSetup setup)
        {
            _setup = setup ?? throw new ArgumentNullException(nameof(setup));
            _rules = setup.Rules ?? throw new ArgumentException("BattleSetup.Rules가 비어 있다.", nameof(setup));
            _random = new Random(setup.Seed);
            SpawnAdventurers();
        }

        public IReadOnlyList<BattleUnit> Units => _units;
        public int Tick { get; private set; }
        public int StageIndex => _stageIndex;
        public bool IsFinished => Result != null;
        public BattleResult Result { get; private set; }

        public IReadOnlyList<BattleEvent> Step()
        {
            _events.Clear();
            if (IsFinished)
            {
                return _events;
            }

            if (_stageIndex < 0)
            {
                if (!TryStartNextStage())
                {
                    return _events;
                }
            }

            Tick++;
            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i].IsAlive)
                {
                    Act(_units[i]);
                }
            }

            UpdateBattleState();
            return _events;
        }

        public BattleResult RunToEnd()
        {
            while (!IsFinished)
            {
                Step();
            }

            return Result;
        }

        private void SpawnAdventurers()
        {
            for (int i = 0; i < _setup.Adventurers.Count; i++)
            {
                AdventurerSetup adventurer = _setup.Adventurers[i];
                _units.Add(new BattleUnit(_units.Count, BattleTeam.Adventurer, adventurer.AdventurerId, -1, i,
                    adventurer.Stats, adventurer.Skill, -i * _rules.FormationSpacing));
            }
        }

        private bool TryStartNextStage()
        {
            if (!HasAliveTeam(BattleTeam.Adventurer))
            {
                Finish(false);
                return false;
            }

            if (_stageIndex + 1 >= _setup.Stages.Count)
            {
                Finish(true);
                return false;
            }

            _stageIndex++;
            int spawnBase = FrontAdventurerPosition() + _rules.StageSpawnDistance;
            StageSetup stage = _setup.Stages[_stageIndex];
            for (int i = 0; i < stage.Monsters.Count; i++)
            {
                MonsterSetup monster = stage.Monsters[i];
                _units.Add(new BattleUnit(_units.Count, BattleTeam.Monster, monster.MonsterId, _stageIndex, i,
                    monster.Stats, monster.Skill, spawnBase + monster.SpawnOffset));
            }

            _events.Add(new BattleEvent(BattleEventType.StageStarted, BattleEvent.NoUnit, BattleEvent.NoUnit, _stageIndex));
            return true;
        }

        private void UpdateBattleState()
        {
            if (!HasAliveTeam(BattleTeam.Adventurer))
            {
                Finish(false);
                return;
            }

            if (!HasAliveTeam(BattleTeam.Monster))
            {
                _events.Add(new BattleEvent(BattleEventType.StageCleared, BattleEvent.NoUnit, BattleEvent.NoUnit, _stageIndex));
                TryStartNextStage();
                return;
            }

            if (Tick >= _rules.MaxTicks)
            {
                Finish(false);
            }
        }

        private void Act(BattleUnit unit)
        {
            if (unit.AttackCooldown > 0)
            {
                unit.AttackCooldown--;
            }

            BattleUnit target = FindNearestEnemy(unit);
            if (target == null)
            {
                if (unit.Team == BattleTeam.Adventurer)
                {
                    unit.Position += _rules.MarchSpeed;
                }

                return;
            }

            GainMana(unit, unit.Stats.ManaPerTick);

            int distance = Math.Abs(target.Position - unit.Position);
            if (distance > unit.Stats.AttackRange)
            {
                MoveToward(unit, target, distance);
                return;
            }

            if (unit.IsManaFull && unit.Skill.Kind != SkillKind.None)
            {
                CastSkill(unit, target);
                return;
            }

            if (unit.AttackCooldown == 0)
            {
                BasicAttack(unit, target);
            }
        }

        private void MoveToward(BattleUnit unit, BattleUnit target, int distance)
        {
            int step = Math.Min(unit.Stats.MoveSpeed, distance - unit.Stats.AttackRange);
            unit.Position += target.Position > unit.Position ? step : -step;
        }

        private void BasicAttack(BattleUnit unit, BattleUnit target)
        {
            unit.AttackCooldown = unit.Stats.AttackIntervalTicks;
            _events.Add(new BattleEvent(BattleEventType.BasicAttack, unit.UnitId, target.UnitId, 0));
            DealDamage(unit, target, 100);
            GainMana(unit, unit.Stats.ManaPerHit);
        }

        private void CastSkill(BattleUnit unit, BattleUnit target)
        {
            unit.Mana = 0;
            unit.AttackCooldown = unit.Stats.AttackIntervalTicks;
            _events.Add(new BattleEvent(BattleEventType.SkillCast, unit.UnitId, target.UnitId, (int)unit.Skill.Kind));

            switch (unit.Skill.Kind)
            {
                case SkillKind.SingleDamage:
                    DealDamage(unit, target, unit.Skill.PowerPercent);
                    break;
                case SkillKind.AreaDamage:
                    DealAreaDamage(unit, target);
                    break;
                case SkillKind.HealLowestAlly:
                    BattleUnit ally = FindLowestHpAlly(unit);
                    if (ally != null)
                    {
                        Heal(unit, ally);
                    }

                    break;
                case SkillKind.HealAllAllies:
                    for (int i = 0; i < _units.Count; i++)
                    {
                        if (_units[i].Team == unit.Team && _units[i].IsAlive)
                        {
                            Heal(unit, _units[i]);
                        }
                    }

                    break;
            }
        }

        private void DealAreaDamage(BattleUnit unit, BattleUnit center)
        {
            int centerPosition = center.Position;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit other = _units[i];
                if (other.Team != unit.Team && other.IsAlive && Math.Abs(other.Position - centerPosition) <= unit.Skill.Radius)
                {
                    DealDamage(unit, other, unit.Skill.PowerPercent);
                }
            }
        }

        private void DealDamage(BattleUnit source, BattleUnit target, int powerPercent)
        {
            int damage = BattleFormulas.Damage(source.Stats.Attack, powerPercent, target.Stats.Defense,
                _rules.DamageVariancePercent, _random);
            damage = Math.Min(damage, target.Hp);
            target.Hp -= damage;
            source.DamageDealt += damage;
            _events.Add(new BattleEvent(BattleEventType.Damaged, source.UnitId, target.UnitId, damage));

            if (target.Hp == 0)
            {
                target.IsFallen = true;
                target.Mana = 0;
                _events.Add(new BattleEvent(BattleEventType.Fell, source.UnitId, target.UnitId, 0));
            }
        }

        private void Heal(BattleUnit source, BattleUnit target)
        {
            int amount = Math.Min(BattleFormulas.Heal(source.Stats.Attack, source.Skill.PowerPercent),
                target.Stats.MaxHp - target.Hp);
            target.Hp += amount;
            _events.Add(new BattleEvent(BattleEventType.Healed, source.UnitId, target.UnitId, amount));
        }

        private static void GainMana(BattleUnit unit, int amount)
        {
            unit.Mana = Math.Min(unit.Stats.MaxMana, unit.Mana + amount);
        }

        private BattleUnit FindNearestEnemy(BattleUnit unit)
        {
            BattleUnit nearest = null;
            int nearestDistance = int.MaxValue;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit other = _units[i];
                if (other.Team == unit.Team || !other.IsAlive)
                {
                    continue;
                }

                int distance = Math.Abs(other.Position - unit.Position);
                if (distance < nearestDistance)
                {
                    nearest = other;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private BattleUnit FindLowestHpAlly(BattleUnit unit)
        {
            BattleUnit lowest = null;
            long lowestRatio = long.MaxValue;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit other = _units[i];
                if (other.Team != unit.Team || !other.IsAlive)
                {
                    continue;
                }

                long ratio = (long)other.Hp * 1000 / other.Stats.MaxHp;
                if (ratio < lowestRatio)
                {
                    lowest = other;
                    lowestRatio = ratio;
                }
            }

            return lowest;
        }

        private bool HasAliveTeam(BattleTeam team)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i].Team == team && _units[i].IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        private int FrontAdventurerPosition()
        {
            int front = int.MinValue;
            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i].Team == BattleTeam.Adventurer && _units[i].IsAlive && _units[i].Position > front)
                {
                    front = _units[i].Position;
                }
            }

            return front;
        }

        private void Finish(bool isVictory)
        {
            int stagesCleared = isVictory ? _setup.Stages.Count : Math.Max(0, _stageIndex);
            var adventurers = new List<AdventurerBattleResult>();
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit unit = _units[i];
                if (unit.Team != BattleTeam.Adventurer)
                {
                    continue;
                }

                bool isDead = unit.IsFallen && BattleFormulas.RollDeath(_rules.DeathChancePercent, _random);
                adventurers.Add(new AdventurerBattleResult(unit.SourceId, unit.IsFallen, isDead, unit.DamageDealt));
            }

            Result = new BattleResult(isVictory, stagesCleared, Tick, adventurers);
            _events.Add(new BattleEvent(BattleEventType.BattleEnded, BattleEvent.NoUnit, BattleEvent.NoUnit, isVictory ? 1 : 0));
        }
    }
}
