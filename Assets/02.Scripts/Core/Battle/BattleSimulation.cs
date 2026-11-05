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

        private int _waveIndex = -1;

        public BattleSimulation(BattleSetup setup)
        {
            _setup = setup ?? throw new ArgumentNullException(nameof(setup));
            _rules = setup.Rules ?? throw new ArgumentException("BattleSetup.Rules가 비어 있다.", nameof(setup));
            _random = new Random(setup.Seed);
            SpawnAdventurers();
        }

        public IReadOnlyList<BattleUnit> Units => _units;
        public int Tick { get; private set; }
        public int WaveIndex => _waveIndex;
        public bool IsFinished => Result != null;
        public BattleResult Result { get; private set; }

        public IReadOnlyList<BattleEvent> Step()
        {
            _events.Clear();
            if (IsFinished)
            {
                return _events;
            }

            if (_waveIndex < 0)
            {
                if (!TryStartNextWave())
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

        // 근접은 앞줄, 원거리는 그 뒷줄에 나란히 선다. 한쪽만 있으면 그쪽만 선다.
        // 한 종류가 MaxRowSize명을 넘으면 그 종류끼리 줄을 나누되 고르게 나눈다(4명 → 2 / 2, 5명 → 3 / 2).
        // 예: 근접 4·원거리 1 → 2 / 2 / 1, 근접 1·원거리 4 → 1 / 2 / 2, 근접 3·원거리 2 → 3 / 2.
        // 줄 안에서는 사거리가 짧은 순, 같으면 파티 순서로 옆으로 늘어서고, 줄마다 길 가운데에 맞춘다.
        // 유닛 번호와 결과 순서는 파티 순서 그대로다.
        private void SpawnAdventurers()
        {
            int maxRowSize = Math.Max(1, _rules.MaxRowSize);
            int meleeCount = 0;
            foreach (AdventurerSetup adventurer in _setup.Adventurers)
            {
                meleeCount += IsMelee(adventurer) ? 1 : 0;
            }

            int rangedCount = _setup.Adventurers.Count - meleeCount;
            int meleeRows = GetRowCount(meleeCount, maxRowSize);
            for (int i = 0; i < _setup.Adventurers.Count; i++)
            {
                AdventurerSetup adventurer = _setup.Adventurers[i];
                bool isMelee = IsMelee(adventurer);
                int groupCount = isMelee ? meleeCount : rangedCount;
                int slot = GetGroupIndex(i, isMelee);
                int groupRow = 0;
                while (slot >= GetRowSize(groupCount, maxRowSize, groupRow))
                {
                    slot -= GetRowSize(groupCount, maxRowSize, groupRow);
                    groupRow++;
                }

                int row = isMelee ? groupRow : meleeRows + groupRow;
                int lane = GetCenteredLane(slot, GetRowSize(groupCount, maxRowSize, groupRow));
                _units.Add(new BattleUnit(_units.Count, BattleTeam.Adventurer, adventurer.AdventurerId, -1, -1, lane,
                    adventurer.Stats, new[] { adventurer.Skill }, default, -row * _rules.FormationSpacing));
            }
        }

        // count명이 나란히 설 때 slot번째의 자리(반 칸 단위, 0이 가운데).
        private static int GetCenteredLane(int slot, int count)
        {
            return slot * 2 - (count - 1);
        }

        private static int GetRowCount(int count, int maxRowSize)
        {
            return (count + maxRowSize - 1) / maxRowSize;
        }

        // count명을 GetRowCount개 줄에 고르게 나눌 때 row번째 줄의 인원. 남는 사람은 앞줄부터 하나씩 더 선다.
        private static int GetRowSize(int count, int maxRowSize, int row)
        {
            int rows = GetRowCount(count, maxRowSize);
            if (row >= rows)
            {
                return 0;
            }

            return count / rows + (row < count % rows ? 1 : 0);
        }

        private bool IsMelee(AdventurerSetup adventurer)
        {
            return adventurer.Stats.AttackRange <= _rules.MeleeRangeMax;
        }

        // 같은 종류(근접·원거리) 안에서 몇 번째인지. 사거리가 같으면 파티 순서를 따른다.
        private int GetGroupIndex(int index, bool isMelee)
        {
            int range = _setup.Adventurers[index].Stats.AttackRange;
            int rowIndex = 0;
            for (int i = 0; i < _setup.Adventurers.Count; i++)
            {
                AdventurerSetup other = _setup.Adventurers[i];
                int otherRange = other.Stats.AttackRange;
                if (IsMelee(other) == isMelee && (otherRange < range || (otherRange == range && i < index)))
                {
                    rowIndex++;
                }
            }

            return rowIndex;
        }

        private bool TryStartNextWave()
        {
            if (!HasAliveTeam(BattleTeam.Adventurer))
            {
                Finish(false);
                return false;
            }

            if (_waveIndex + 1 >= _setup.Waves.Count)
            {
                Finish(true);
                return false;
            }

            _waveIndex++;
            int spawnBase = FrontAdventurerPosition() + _rules.WaveSpawnDistance;
            WaveSetup wave = _setup.Waves[_waveIndex];
            for (int i = 0; i < wave.Monsters.Count; i++)
            {
                MonsterSetup monster = wave.Monsters[i];
                _units.Add(new BattleUnit(_units.Count, BattleTeam.Monster, monster.MonsterId, _waveIndex, i,
                    GetCenteredLane(i, wave.Monsters.Count), monster.Stats, monster.Skills, monster.Enrage, spawnBase + monster.SpawnOffset));
            }

            _events.Add(new BattleEvent(BattleEventType.WaveStarted, BattleEvent.NoUnit, BattleEvent.NoUnit, _waveIndex));
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
                CreditWaveClear();
                _events.Add(new BattleEvent(BattleEventType.WaveCleared, BattleEvent.NoUnit, BattleEvent.NoUnit, _waveIndex));
                TryStartNextWave();
                return;
            }

            if (Tick >= _rules.MaxTicks)
            {
                Finish(false);
            }
        }

        private void CreditWaveClear()
        {
            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i].Team == BattleTeam.Adventurer && _units[i].IsAlive)
                {
                    _units[i].WavesCleared++;
                }
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
            SkillData skill = unit.Skill;
            unit.AdvanceSkill();
            unit.Mana = 0;
            unit.AttackCooldown = unit.Stats.AttackIntervalTicks;
            _events.Add(new BattleEvent(BattleEventType.SkillCast, unit.UnitId, target.UnitId, (int)skill.Kind));

            switch (skill.Kind)
            {
                case SkillKind.SingleDamage:
                    DealDamage(unit, target, skill.PowerPercent);
                    break;
                case SkillKind.AreaDamage:
                    DealAreaDamage(unit, target, skill);
                    break;
                case SkillKind.HealLowestAlly:
                    BattleUnit ally = FindLowestHpAlly(unit);
                    if (ally != null)
                    {
                        Heal(unit, ally, skill.PowerPercent);
                    }

                    break;
                case SkillKind.HealAllAllies:
                    for (int i = 0; i < _units.Count; i++)
                    {
                        if (_units[i].Team == unit.Team && _units[i].IsAlive)
                        {
                            Heal(unit, _units[i], skill.PowerPercent);
                        }
                    }

                    break;
            }
        }

        private void DealAreaDamage(BattleUnit unit, BattleUnit center, SkillData skill)
        {
            int centerPosition = center.Position;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit other = _units[i];
                if (other.Team != unit.Team && other.IsAlive && Math.Abs(other.Position - centerPosition) <= skill.Radius)
                {
                    DealDamage(unit, other, skill.PowerPercent);
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
                return;
            }

            TryEnrage(target);
        }

        private void TryEnrage(BattleUnit unit)
        {
            if (unit.IsEnraged || !unit.Enrage.IsEnabled || (long)unit.Hp * 100 > (long)unit.Stats.MaxHp * unit.Enrage.HpPercent)
            {
                return;
            }

            unit.IsEnraged = true;
            unit.Stats = BattleFormulas.ApplyEnrage(unit.Stats, unit.Enrage);
            _events.Add(new BattleEvent(BattleEventType.Enraged, unit.UnitId, BattleEvent.NoUnit, 0));
        }

        private void Heal(BattleUnit source, BattleUnit target, int powerPercent)
        {
            int amount = Math.Min(BattleFormulas.Heal(source.Stats.Attack, powerPercent),
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
            int wavesCleared = isVictory ? _setup.Waves.Count : Math.Max(0, _waveIndex);
            // 모험가는 SpawnAdventurers에서 설정 순서대로 먼저 넣었으므로 _units의 앞쪽 번호가 설정 번호와 같다.
            var adventurers = new List<AdventurerBattleResult>();
            for (int i = 0; i < _setup.Adventurers.Count; i++)
            {
                BattleUnit unit = _units[i];
                bool isDead = unit.IsFallen && BattleFormulas.RollDeath(_rules.DeathChancePercent, _random);
                bool isAccessoryBroken = isDead && TryDeathImmunity(_setup.Adventurers[i].Accessory);
                adventurers.Add(new AdventurerBattleResult(unit.SourceId, unit.IsFallen, isDead && !isAccessoryBroken,
                    unit.DamageDealt, unit.WavesCleared, isAccessoryBroken));
            }

            Result = new BattleResult(isVictory, wavesCleared, Tick, adventurers);
            _events.Add(new BattleEvent(BattleEventType.BattleEnded, BattleEvent.NoUnit, BattleEvent.NoUnit, isVictory ? 1 : 0));
        }

        // 루비 목걸이: 사망으로 판정된 모험가를 확률로 살린다. 효과가 없으면 난수를 쓰지 않아 기존 결과가 바뀌지 않는다.
        private bool TryDeathImmunity(AccessoryEffect accessory)
        {
            int percent = accessory.PercentOf(AccessoryEffectKind.DeathImmunity);
            return percent > 0 && BattleFormulas.RollChance(percent, _random);
        }
    }
}
