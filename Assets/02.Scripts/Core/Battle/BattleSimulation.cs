using System;
using System.Collections.Generic;
using Blacksmith.Core.Adventurers;

namespace Blacksmith.Core.Battle
{
    // 같은 BattleSetup이면 같은 결과가 나온다. 호스트는 RunToEnd로 결과를 정하고,
    // 각 참가자는 같은 설정으로 Step을 틱마다 불러 연출을 재생한다.
    // 스테이지 맵 위에서 싸운다 (docs/GAME_CONCEPT.md 7.1).
    // 1. 잡몹 무리는 처음부터 스폰 지점에 서 있다. 보스 무리는 잡몹 무리를 모두 토벌하면 보스 자리에 나타난다.
    // 2. 처음에는 첫 무리 쪽을 보고 대열로 선다. 싸울 무리가 없으면 남은 무리 중 걸어서 가장 가까운 무리 쪽으로
    //    모험가가 각자 8방향 격자 경로를 따라 걷는다. 대열은 지키지 않고 서로 부딪히지 않게만 비켜 선다.
    // 3. 모험가가 무리의 시야에 들어오면 그 무리가 다가오고, 가장 가까운 적을 노려 싸운다.
    // 4. 무리를 모두 토벌하면 승리, 모험가가 모두 쓰러지면 패배다.
    public sealed class BattleSimulation
    {
        private const int DirectionScale = 1000;
        // 같은 무리의 시체끼리 떨어지는 거리. 연출 수치라 데이터로 빼지 않는다.
        private const int CorpseSpacing = 90;
        // 8방향 단위 벡터(DirectionScale 길이)
        private static readonly int[] CorpseOffsetX = { 1000, 707, 0, -707, -1000, -707, 0, 707 };
        private static readonly int[] CorpseOffsetZ = { 0, 707, 1000, 707, 0, -707, -1000, -707 };
        // 걸을 때 격자 경로를 몇 칸 앞까지 내다보고 곧게 갈 수 있는 가장 먼 칸을 노릴지. 칸마다 꺾이지 않게 한다.
        private const int LookAheadCells = 8;
        // 경로가 곧게 이어지는지 확인할 때 선분 위를 살피는 간격
        private const int LineCheckStep = 50;

        private readonly BattleSetup _setup;
        private readonly BattleRules _rules;
        private readonly StageMap _map;
        private readonly Random _random;
        private readonly List<BattleUnit> _units = new List<BattleUnit>();
        private readonly List<BattleEvent> _events = new List<BattleEvent>();
        private readonly List<int> _clearedGroups = new List<int>();
        private readonly List<BattleCorpse> _corpses = new List<BattleCorpse>();
        private readonly GroupState[] _groups;

        // 전투 시작 때 대열을 세울 기준점(앞줄 가운데)과 보는 방향(DirectionScale 길이)
        private int _anchorX;
        private int _anchorZ;
        private int _headingX = DirectionScale;
        private int _headingZ;
        // 걸어가는 목표 무리. 토벌하기 전까지 바꾸지 않아 두 무리 사이에서 오락가락하지 않는다.
        private int _marchGroup = -1;

        private sealed class GroupState
        {
            public bool IsSpawned;
            public bool IsEngaged;
            public bool IsCleared;
            public StagePathField Field;
        }

        public BattleSimulation(BattleSetup setup)
        {
            _setup = setup ?? throw new ArgumentNullException(nameof(setup));
            _rules = setup.Rules ?? throw new ArgumentException("BattleSetup.Rules가 비어 있다.", nameof(setup));
            _map = setup.Map ?? throw new ArgumentException("BattleSetup.Map이 비어 있다.", nameof(setup));
            _random = new Random(setup.Seed);
            _groups = new GroupState[setup.Groups.Count];
            for (int i = 0; i < _groups.Length; i++)
            {
                _groups[i] = new GroupState();
            }

            _anchorX = BattleSpace.ToUnits(_map.Start.X);
            _anchorZ = BattleSpace.ToUnits(_map.Start.Z);
            // 모험가가 앞 번호를 쓰도록, 첫 무리 쪽을 보고 모험가를 먼저 세운 뒤 몬스터를 넣는다.
            bool hasMobGroup = setup.Groups.Exists(group => !group.IsBoss);
            for (int i = 0; i < _groups.Length; i++)
            {
                if (setup.Groups[i].IsBoss != hasMobGroup)
                {
                    PrepareGroup(i);
                }
            }

            FaceNextStep();
            SpawnAdventurers();
            for (int i = 0; i < _groups.Length; i++)
            {
                if (_groups[i].IsSpawned)
                {
                    AddGroupUnits(i);
                }
            }
        }

        public IReadOnlyList<BattleUnit> Units => _units;
        public int Tick { get; private set; }
        public bool IsFinished => Result != null;
        public BattleResult Result { get; private set; }

        public IReadOnlyList<BattleEvent> Step()
        {
            _events.Clear();
            if (IsFinished)
            {
                return _events;
            }

            Tick++;
            EngageGroupsInSight();
            bool isFighting = HasActiveMonster();
            if (isFighting)
            {
                for (int i = 0; i < _units.Count; i++)
                {
                    if (_units[i].IsAlive && _units[i].IsActive)
                    {
                        Act(_units[i]);
                    }
                }
            }
            else
            {
                March();
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
        // 줄 안에서는 사거리가 짧은 순, 같으면 파티 순서로 옆으로 늘어서고, 줄마다 가운데에 맞춘다.
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
                var unit = new BattleUnit(_units.Count, BattleTeam.Adventurer, adventurer.AdventurerId, -1, -1, row, lane,
                    adventurer.Stats, new[] { adventurer.Skill }, default, 0, 0);
                GetFormationSpot(unit, out int x, out int z);
                bool isOnFloor = IsFloor(x, z);
                unit.X = isOnFloor ? x : _anchorX;
                unit.Z = isOnFloor ? z : _anchorZ;
                _units.Add(unit);
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

        // 기준점에서 걷는 방향 뒤로 줄 간격만큼, 옆으로 옆자리 간격만큼 떨어진 대열 자리
        private void GetFormationSpot(BattleUnit unit, out int x, out int z)
        {
            int back = unit.Row * _rules.FormationSpacing;
            int side = unit.Lane * _rules.LaneSpacing / 2;
            x = _anchorX + (-_headingX * back + _headingZ * side) / DirectionScale;
            z = _anchorZ + (-_headingZ * back - _headingX * side) / DirectionScale;
        }

        private void PrepareGroup(int groupIndex)
        {
            MapPoint spawn = _setup.Groups[groupIndex].Spawn;
            _groups[groupIndex].IsSpawned = true;
            _groups[groupIndex].Field = new StagePathField(_map, (int)Math.Floor(spawn.X), (int)Math.Floor(spawn.Z));
        }

        // 무리는 스폰 지점에서 시작 지점 쪽을 보고 선다. 사거리가 짧은 몹이 앞에 서고, 옆으로는 가운데에 맞춘다.
        private void AddGroupUnits(int groupIndex)
        {
            MonsterGroupSetup group = _setup.Groups[groupIndex];
            int spawnX = BattleSpace.ToUnits(group.Spawn.X);
            int spawnZ = BattleSpace.ToUnits(group.Spawn.Z);
            Normalize(BattleSpace.ToUnits(_map.Start.X) - spawnX, BattleSpace.ToUnits(_map.Start.Z) - spawnZ, out int faceX, out int faceZ);
            for (int i = 0; i < group.Monsters.Count; i++)
            {
                MonsterSetup monster = group.Monsters[i];
                int side = GetCenteredLane(i, group.Monsters.Count) * _rules.LaneSpacing / 2;
                int x = spawnX + (-faceX * monster.SpawnOffset + faceZ * side) / DirectionScale;
                int z = spawnZ + (-faceZ * monster.SpawnOffset - faceX * side) / DirectionScale;
                bool isOnFloor = IsFloor(x, z);
                _units.Add(new BattleUnit(_units.Count, BattleTeam.Monster, monster.MonsterId, groupIndex, i, 0, 0,
                    monster.Stats, monster.Skills, monster.Enrage, isOnFloor ? x : spawnX, isOnFloor ? z : spawnZ));
            }
        }

        private void SpawnBossIfReady()
        {
            for (int i = 0; i < _groups.Length; i++)
            {
                if (!_setup.Groups[i].IsBoss && !_groups[i].IsCleared)
                {
                    return;
                }
            }

            for (int i = 0; i < _groups.Length; i++)
            {
                if (_setup.Groups[i].IsBoss && !_groups[i].IsSpawned)
                {
                    PrepareGroup(i);
                    AddGroupUnits(i);
                }
            }
        }

        // 아직 다가오지 않은 무리의 시야 안에 쓰러지지 않은 모험가가 있으면 그 무리 전체가 움직이기 시작한다.
        private void EngageGroupsInSight()
        {
            for (int g = 0; g < _groups.Length; g++)
            {
                if (!_groups[g].IsSpawned || _groups[g].IsEngaged || !IsGroupInSight(g))
                {
                    continue;
                }

                _groups[g].IsEngaged = true;
                foreach (BattleUnit unit in _units)
                {
                    if (unit.Team == BattleTeam.Monster && unit.GroupIndex == g)
                    {
                        unit.IsActive = true;
                    }
                }

                _events.Add(new BattleEvent(BattleEventType.GroupEngaged, BattleEvent.NoUnit, BattleEvent.NoUnit, g));
            }
        }

        private bool IsGroupInSight(int groupIndex)
        {
            int sight = 0;
            foreach (MonsterSetup monster in _setup.Groups[groupIndex].Monsters)
            {
                sight = Math.Max(sight, monster.SightRange);
            }

            long sightSquared = (long)sight * sight;
            foreach (BattleUnit monster in _units)
            {
                if (monster.Team != BattleTeam.Monster || monster.GroupIndex != groupIndex || !monster.IsAlive)
                {
                    continue;
                }

                foreach (BattleUnit adventurer in _units)
                {
                    if (adventurer.Team == BattleTeam.Adventurer && adventurer.IsAlive && SquaredDistance(monster, adventurer) <= sightSquared)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // 싸울 무리가 없으면 모험가는 대열 없이 각자 목표 무리 쪽 격자 경로를 따라 걷고, 서로 LaneSpacing보다 가까우면 비켜 선다.
        private void March()
        {
            if (!IsMarchGroupValid())
            {
                GetPartyCenterCell(out int cellX, out int cellZ);
                _marchGroup = FindNearestGroup(cellX, cellZ);
            }

            if (_marchGroup < 0)
            {
                return;
            }

            GetLineUp(out int rearMelee, out int frontRanged);
            foreach (BattleUnit unit in _units)
            {
                if (unit.Team != BattleTeam.Adventurer || !unit.IsAlive)
                {
                    continue;
                }

                int speed = GetMarchSpeed(unit, rearMelee, frontRanged);
                GetPathTarget(unit, out int targetX, out int targetZ);
                Normalize(targetX - unit.X, targetZ - unit.Z, out int dirX, out int dirZ);
                GetSeparation(unit, out int pushX, out int pushZ);
                if (dirX + pushX == 0 && dirZ + pushZ == 0)
                {
                    continue;
                }

                Normalize(dirX + pushX, dirZ + pushZ, out dirX, out dirZ);
                int distance = BattleSpace.Sqrt((long)(targetX - unit.X) * (targetX - unit.X) + (long)(targetZ - unit.Z) * (targetZ - unit.Z));
                int step = pushX == 0 && pushZ == 0 ? Math.Min(speed, distance) : speed;
                MoveToward(unit, unit.X + dirX, unit.Z + dirZ, step);
            }
        }

        // 목표 무리까지 남은 걷는 비용으로 본 가장 뒤의 근접과 가장 앞의 원거리. 한쪽이 없으면 -1이다.
        private void GetLineUp(out int rearMelee, out int frontRanged)
        {
            rearMelee = -1;
            frontRanged = -1;
            foreach (BattleUnit unit in _units)
            {
                if (unit.Team != BattleTeam.Adventurer || !unit.IsAlive)
                {
                    continue;
                }

                int cost = GetMarchCost(unit);
                if (IsMelee(unit))
                {
                    rearMelee = Math.Max(rearMelee, cost);
                }
                else if (frontRanged < 0 || cost < frontRanged)
                {
                    frontRanged = cost;
                }
            }
        }

        // 원거리가 근접보다 앞서거나 FormationSpacing만큼 뒤에 있지 않으면, 그 원거리는 느리게, 그보다 뒤처진 근접은 빠르게 걸어
        // 근접이 앞에 서게 한다. 줄이 맞으면 모두 같은 속도로 걷는다.
        private int GetMarchSpeed(BattleUnit unit, int rearMelee, int frontRanged)
        {
            int gap = _rules.FormationSpacing * StagePathField.StraightCost / BattleSpace.UnitsPerCell;
            if (rearMelee < 0 || frontRanged < 0 || frontRanged >= rearMelee + gap)
            {
                return _rules.MarchSpeed;
            }

            int cost = GetMarchCost(unit);
            int change = _rules.MarchSpeed * _rules.LineUpSpeedPercent / 100;
            if (IsMelee(unit))
            {
                return cost + gap > frontRanged ? _rules.MarchSpeed + change : _rules.MarchSpeed;
            }

            return cost < rearMelee + gap ? _rules.MarchSpeed - change : _rules.MarchSpeed;
        }

        private int GetMarchCost(BattleUnit unit)
        {
            return _groups[_marchGroup].Field.CostAt(unit.X / BattleSpace.UnitsPerCell, unit.Z / BattleSpace.UnitsPerCell);
        }

        private bool IsMelee(BattleUnit unit)
        {
            return unit.Stats.AttackRange <= _rules.MeleeRangeMax;
        }

        private bool IsMarchGroupValid()
        {
            return _marchGroup >= 0 && _groups[_marchGroup].IsSpawned && !_groups[_marchGroup].IsCleared;
        }

        private void GetPartyCenterCell(out int cellX, out int cellZ)
        {
            long sumX = 0;
            long sumZ = 0;
            int count = 0;
            foreach (BattleUnit unit in _units)
            {
                if (unit.Team == BattleTeam.Adventurer && unit.IsAlive)
                {
                    sumX += unit.X;
                    sumZ += unit.Z;
                    count++;
                }
            }

            int x = count > 0 ? (int)(sumX / count) : _anchorX;
            int z = count > 0 ? (int)(sumZ / count) : _anchorZ;
            if (!IsFloor(x, z) && count > 0)
            {
                x = _units[0].X;
                z = _units[0].Z;
            }

            cellX = x / BattleSpace.UnitsPerCell;
            cellZ = z / BattleSpace.UnitsPerCell;
        }

        // 격자 경로를 LookAheadCells칸까지 따라가며 지금 자리에서 곧게 갈 수 있는 가장 먼 칸의 가운데. 무리 칸에 닿으면 스폰 지점이다.
        private void GetPathTarget(BattleUnit unit, out int x, out int z)
        {
            StagePathField field = _groups[_marchGroup].Field;
            int cellX = unit.X / BattleSpace.UnitsPerCell;
            int cellZ = unit.Z / BattleSpace.UnitsPerCell;
            x = BattleSpace.ToUnits(_setup.Groups[_marchGroup].Spawn.X);
            z = BattleSpace.ToUnits(_setup.Groups[_marchGroup].Spawn.Z);
            for (int i = 0; i < LookAheadCells; i++)
            {
                if (!field.TryNextCell(cellX, cellZ, out cellX, out cellZ))
                {
                    if (i > 0 && !HasClearLine(unit.X, unit.Z, x, z))
                    {
                        GetCellCenter(cellX, cellZ, out x, out z);
                    }

                    return;
                }

                GetCellCenter(cellX, cellZ, out int centerX, out int centerZ);
                if (i > 0 && !HasClearLine(unit.X, unit.Z, centerX, centerZ))
                {
                    return;
                }

                x = centerX;
                z = centerZ;
            }
        }

        private static void GetCellCenter(int cellX, int cellZ, out int x, out int z)
        {
            x = cellX * BattleSpace.UnitsPerCell + BattleSpace.UnitsPerCell / 2;
            z = cellZ * BattleSpace.UnitsPerCell + BattleSpace.UnitsPerCell / 2;
        }

        private bool HasClearLine(int fromX, int fromZ, int toX, int toZ)
        {
            int length = BattleSpace.Sqrt((long)(toX - fromX) * (toX - fromX) + (long)(toZ - fromZ) * (toZ - fromZ));
            for (int d = LineCheckStep; d < length; d += LineCheckStep)
            {
                if (!IsFloor(fromX + (int)((long)(toX - fromX) * d / length), fromZ + (int)((long)(toZ - fromZ) * d / length)))
                {
                    return false;
                }
            }

            return true;
        }

        // 가까운 모험가에게서 멀어지는 방향(DirectionScale 단위). 겹칠수록 세게 밀고, 같은 자리면 번호로 정한 쪽으로 민다.
        private void GetSeparation(BattleUnit unit, out int x, out int z)
        {
            x = 0;
            z = 0;
            int spacing = Math.Max(1, _rules.LaneSpacing);
            foreach (BattleUnit other in _units)
            {
                if (other == unit || other.Team != BattleTeam.Adventurer || !other.IsAlive)
                {
                    continue;
                }

                int dx = unit.X - other.X;
                int dz = unit.Z - other.Z;
                int distance = BattleSpace.Sqrt((long)dx * dx + (long)dz * dz);
                if (distance >= spacing)
                {
                    continue;
                }

                if (distance == 0)
                {
                    dx = unit.UnitId < other.UnitId ? 1 : -1;
                    dz = 0;
                    distance = 1;
                }

                int strength = (int)((long)(spacing - distance) * DirectionScale * 2 / spacing);
                x += (int)((long)dx * strength / distance);
                z += (int)((long)dz * strength / distance);
            }
        }

        // 기준점이 서 있는 칸에서 가장 가까운 무리 쪽으로 한 걸음 간 칸의 가운데. 무리 칸에 닿았으면 무리 스폰 지점이다.
        private bool TryGetMarchTarget(out int x, out int z)
        {
            x = _anchorX;
            z = _anchorZ;
            int cellX = _anchorX / BattleSpace.UnitsPerCell;
            int cellZ = _anchorZ / BattleSpace.UnitsPerCell;
            int target = FindNearestGroup(cellX, cellZ);
            _marchGroup = target;
            if (target < 0)
            {
                return false;
            }

            if (_groups[target].Field.TryNextCell(cellX, cellZ, out int nextX, out int nextZ))
            {
                x = nextX * BattleSpace.UnitsPerCell + BattleSpace.UnitsPerCell / 2;
                z = nextZ * BattleSpace.UnitsPerCell + BattleSpace.UnitsPerCell / 2;
                return true;
            }

            x = BattleSpace.ToUnits(_setup.Groups[target].Spawn.X);
            z = BattleSpace.ToUnits(_setup.Groups[target].Spawn.Z);
            return true;
        }

        // 아직 토벌하지 않은 무리 중 이 칸에서 걸어서 가장 가까운 무리. 같으면 번호가 작은 무리다.
        private int FindNearestGroup(int cellX, int cellZ)
        {
            int nearest = -1;
            int nearestCost = StagePathField.Unreachable;
            for (int g = 0; g < _groups.Length; g++)
            {
                if (!_groups[g].IsSpawned || _groups[g].IsCleared)
                {
                    continue;
                }

                int cost = _groups[g].Field.CostAt(cellX, cellZ);
                if (nearest < 0 || cost < nearestCost)
                {
                    nearest = g;
                    nearestCost = cost;
                }
            }

            return nearest;
        }

        // 전투 시작 때 첫 무리 쪽을 보고 대열을 세운다.
        private void FaceNextStep()
        {
            if (TryGetMarchTarget(out int x, out int z) && (x != _anchorX || z != _anchorZ))
            {
                Normalize(x - _anchorX, z - _anchorZ, out _headingX, out _headingZ);
            }
        }

        private void UpdateBattleState()
        {
            if (!HasAliveTeam(BattleTeam.Adventurer))
            {
                Finish(false);
                return;
            }

            for (int g = 0; g < _groups.Length; g++)
            {
                if (_groups[g].IsEngaged && !_groups[g].IsCleared && !HasAliveMonster(g))
                {
                    ClearGroup(g);
                }
            }

            SpawnBossIfReady();
            if (_clearedGroups.Count >= _groups.Length)
            {
                Finish(true);
                return;
            }

            if (Tick >= _rules.MaxTicks)
            {
                Finish(false);
            }
        }

        private void ClearGroup(int groupIndex)
        {
            _groups[groupIndex].IsCleared = true;
            _clearedGroups.Add(groupIndex);
            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i].Team == BattleTeam.Adventurer && _units[i].IsAlive)
                {
                    _units[i].GroupsCleared++;
                }
            }

            _events.Add(new BattleEvent(BattleEventType.GroupCleared, BattleEvent.NoUnit, BattleEvent.NoUnit, groupIndex));
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
                return;
            }

            GainMana(unit, unit.Stats.ManaPerTick);

            int distance = BattleSpace.Sqrt(SquaredDistance(unit, target));
            if (distance > unit.Stats.AttackRange)
            {
                MoveToward(unit, target.X, target.Z, Math.Min(unit.Stats.MoveSpeed, distance - unit.Stats.AttackRange));
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

        // 목표 쪽으로 최대 step만큼 곧게 걷는다. 그 자리가 바닥이 아니면 한 축으로만 미끄러지고, 그것도 안 되면 멈춘다.
        // 곧게 걸었거나 이미 닿아 있으면 참, 벽에 막혀 미끄러지거나 멈췄으면 거짓이다.
        private bool MoveToward(BattleUnit unit, int targetX, int targetZ, int step)
        {
            int dx = targetX - unit.X;
            int dz = targetZ - unit.Z;
            int distance = BattleSpace.Sqrt((long)dx * dx + (long)dz * dz);
            if (distance == 0 || step <= 0)
            {
                return true;
            }

            step = Math.Min(step, distance);
            int x = unit.X + DivideRounded((long)dx * step, distance);
            int z = unit.Z + DivideRounded((long)dz * step, distance);
            if (IsFloor(x, z))
            {
                unit.X = x;
                unit.Z = z;
                return true;
            }

            if (IsFloor(x, unit.Z))
            {
                unit.X = x;
            }
            else if (IsFloor(unit.X, z))
            {
                unit.Z = z;
            }

            return false;
        }

        private void BasicAttack(BattleUnit unit, BattleUnit target)
        {
            unit.AttackCooldown = unit.Stats.AttackIntervalTicks;
            _events.Add(new BattleEvent(BattleEventType.BasicAttack, unit.UnitId, target.UnitId, 0));
            unit.AttackCount++;
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
                    unit.AttackCount++;
                    DealDamage(unit, target, skill.PowerPercent);
                    break;
                case SkillKind.AreaDamage:
                    unit.AttackCount++;
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
                        if (_units[i].Team == unit.Team && _units[i].IsAlive && _units[i].IsActive)
                        {
                            Heal(unit, _units[i], skill.PowerPercent);
                        }
                    }

                    break;
            }
        }

        private void DealAreaDamage(BattleUnit unit, BattleUnit center, SkillData skill)
        {
            long radiusSquared = (long)skill.Radius * skill.Radius;
            int centerX = center.X;
            int centerZ = center.Z;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit other = _units[i];
                long dx = other.X - centerX;
                long dz = other.Z - centerZ;
                if (other.Team != unit.Team && other.IsAlive && other.IsActive && dx * dx + dz * dz <= radiusSquared)
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
            target.HitsTaken++;
            source.DamageDealt += damage;
            _events.Add(new BattleEvent(BattleEventType.Damaged, source.UnitId, target.UnitId, damage));

            if (target.Hp == 0)
            {
                target.IsFallen = true;
                target.Mana = 0;
                if (target.Team == BattleTeam.Monster)
                {
                    AddCorpse(target);
                }

                _events.Add(new BattleEvent(BattleEventType.Fell, source.UnitId, target.UnitId, 0));
                return;
            }

            TryEnrage(target);
        }

        // 쓰러진 자리에 시체를 남긴다. 몬스터가 한곳에 몰려 쓰러져도 시체가 흩어져 보이도록,
        // 같은 무리의 시체와 CorpseSpacing보다 가까우면 둘레의 바닥 자리로 비켜 놓는다.
        private void AddCorpse(BattleUnit monster)
        {
            int x = monster.X;
            int z = monster.Z;
            for (int ring = 1; ring <= 3 && IsNearCorpse(monster.GroupIndex, x, z); ring++)
            {
                for (int i = 0; i < 8; i++)
                {
                    // 방향은 무리 안 순서로 돌려 시작해, 같은 무리 시체가 한쪽으로만 쌓이지 않게 한다.
                    int direction = (i + monster.GroupSlot * 3) % 8;
                    int candidateX = monster.X + CorpseOffsetX[direction] * CorpseSpacing * ring / DirectionScale;
                    int candidateZ = monster.Z + CorpseOffsetZ[direction] * CorpseSpacing * ring / DirectionScale;
                    if (IsFloor(candidateX, candidateZ) && !IsNearCorpse(monster.GroupIndex, candidateX, candidateZ))
                    {
                        x = candidateX;
                        z = candidateZ;
                        break;
                    }
                }
            }

            _corpses.Add(new BattleCorpse(monster.SourceId, monster.GroupIndex, monster.GroupSlot, x, z));
        }

        private bool IsNearCorpse(int groupIndex, int x, int z)
        {
            foreach (BattleCorpse corpse in _corpses)
            {
                long dx = corpse.X - x;
                long dz = corpse.Z - z;
                if (corpse.GroupIndex == groupIndex && dx * dx + dz * dz < (long)CorpseSpacing * CorpseSpacing)
                {
                    return true;
                }
            }

            return false;
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

        // 다가오기 시작한 무리의 몬스터와 쓰러지지 않은 모험가만 노린다.
        private BattleUnit FindNearestEnemy(BattleUnit unit)
        {
            BattleUnit nearest = null;
            long nearestDistance = long.MaxValue;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit other = _units[i];
                if (other.Team == unit.Team || !other.IsAlive || !other.IsActive)
                {
                    continue;
                }

                long distance = SquaredDistance(unit, other);
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
                if (other.Team != unit.Team || !other.IsAlive || !other.IsActive)
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

        private bool HasActiveMonster()
        {
            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i].Team == BattleTeam.Monster && _units[i].IsAlive && _units[i].IsActive)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasAliveMonster(int groupIndex)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i].Team == BattleTeam.Monster && _units[i].GroupIndex == groupIndex && _units[i].IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsFloor(int x, int z)
        {
            return x >= 0 && z >= 0 && _map.IsFloor(x / BattleSpace.UnitsPerCell, z / BattleSpace.UnitsPerCell);
        }

        private static long SquaredDistance(BattleUnit a, BattleUnit b)
        {
            long dx = a.X - b.X;
            long dz = a.Z - b.Z;
            return dx * dx + dz * dz;
        }

        // 반올림 나눗셈. 내림하면 남은 거리가 짧을 때 한 걸음이 0이 되어 멈춘다. divisor는 양수다.
        private static int DivideRounded(long value, int divisor)
        {
            return (int)(value >= 0 ? (value + divisor / 2) / divisor : -((-value + divisor / 2) / divisor));
        }

        // (dx, dz)를 길이 DirectionScale로 맞춘다. 길이가 0이면 +X를 본다.
        private static void Normalize(int dx, int dz, out int x, out int z)
        {
            int length = BattleSpace.Sqrt((long)dx * dx + (long)dz * dz);
            if (length == 0)
            {
                x = DirectionScale;
                z = 0;
                return;
            }

            x = (int)((long)dx * DirectionScale / length);
            z = (int)((long)dz * DirectionScale / length);
        }

        private void Finish(bool isVictory)
        {
            // 모험가는 SpawnAdventurers에서 설정 순서대로 앞 번호를 받았으므로 _units의 앞쪽 번호가 설정 번호와 같다.
            var adventurers = new List<AdventurerBattleResult>();
            for (int i = 0; i < _setup.Adventurers.Count; i++)
            {
                BattleUnit unit = _units[i];
                AdventurerSetup adventurer = _setup.Adventurers[i];
                int injuryGained = InjuryRules.GetBattleInjury(unit.Hp, unit.Stats.MaxHp, unit.IsFallen, _rules.Injury);
                int injury = InjuryRules.AddInjury(adventurer.Injury, injuryGained, _rules.Injury);
                // 사망 판정 대상이 아니면 난수를 쓰지 않아 다른 모험가의 판정이 바뀌지 않는다.
                int deathChance = InjuryRules.GetDeathChancePercent(injury, _rules.Injury);
                bool isDead = deathChance > 0 && BattleFormulas.RollDeath(deathChance, _random);
                bool isAccessoryBroken = isDead && TryDeathImmunity(adventurer.Accessory);
                adventurers.Add(new AdventurerBattleResult(unit.SourceId, unit.IsFallen, injury, injuryGained,
                    isDead && !isAccessoryBroken, unit.DamageDealt, unit.GroupsCleared, isAccessoryBroken,
                    adventurer.Weapon.AddUses(unit.AttackCount), adventurer.Helmet.AddUses(unit.HitsTaken),
                    adventurer.Armor.AddUses(unit.HitsTaken)));
            }

            Result = new BattleResult(isVictory, _clearedGroups.ToArray(), Tick, adventurers, _corpses.ToArray());
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
