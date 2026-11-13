using System;
using System.Collections.Generic;

namespace Blacksmith.Core.Battle
{
    public enum BattleTeam
    {
        Adventurer,
        Monster,
    }

    public sealed class BattleUnit
    {
        private readonly IReadOnlyList<SkillData> _skills;
        private int _nextSkillIndex;

        public int UnitId { get; }
        public BattleTeam Team { get; }
        // 모험가는 AdventurerId, 몬스터는 MonsterId다. 연출에서 외형과 이펙트를 찾는 데 쓴다.
        public int SourceId { get; }
        // 몬스터가 속한 무리 번호(BattleSetup.Groups 순서). 모험가는 -1이다.
        public int GroupIndex { get; }
        // 무리 안에서 몇 번째 몬스터인지. 몬스터별 드랍을 찾는 데 쓴다. 모험가는 -1이다.
        public int GroupSlot { get; }
        // 대열의 줄(0이 앞줄)과 옆자리. 옆자리는 반 칸 단위이고 0이 가운데라, 줄마다 가운데에 맞춰 설 수 있다(2명 → -1, 1 / 3명 → -2, 0, 2).
        public int Row { get; }
        public int Lane { get; }
        // 분노하면 공격력과 공격 간격이 바뀐다.
        public UnitStats Stats { get; internal set; }
        public EnrageData Enrage { get; }

        // 맵 위치(BattleSpace 단위)
        public int X { get; internal set; }
        public int Z { get; internal set; }
        // 몬스터는 무리가 모험가를 발견해야 움직이고 싸운다. 모험가는 늘 참이다.
        public bool IsActive { get; internal set; }
        public int Hp { get; internal set; }
        public int Mana { get; internal set; }
        public int AttackCooldown { get; internal set; }
        public bool IsFallen { get; internal set; }
        public bool IsEnraged { get; internal set; }
        public int DamageDealt { get; internal set; }
        // 공격한 횟수(기본 공격과 피해 스킬)와 공격받은 횟수. 장비 내구도 계산에 쓴다.
        public int AttackCount { get; internal set; }
        public int HitsTaken { get; internal set; }
        // 쓰러지지 않은 채 토벌한 무리 수다. 경험치 계산에 쓴다.
        public int GroupsCleared { get; internal set; }

        internal BattleUnit(int unitId, BattleTeam team, int sourceId, int groupIndex, int groupSlot, int row, int lane,
            UnitStats stats, IReadOnlyList<SkillData> skills, EnrageData enrage, int x, int z)
        {
            UnitId = unitId;
            Team = team;
            SourceId = sourceId;
            GroupIndex = groupIndex;
            GroupSlot = groupSlot;
            Row = row;
            Lane = lane;
            Stats = stats;
            _skills = skills ?? Array.Empty<SkillData>();
            Enrage = enrage;
            X = x;
            Z = z;
            IsActive = team == BattleTeam.Adventurer;
            Hp = stats.MaxHp;
        }

        public bool IsAlive => !IsFallen;
        public bool IsManaFull => Stats.MaxMana > 0 && Mana >= Stats.MaxMana;
        // 다음에 마나가 차면 쓸 스킬이다. 스킬이 없으면 Kind가 None이다.
        public SkillData Skill => _skills.Count > 0 ? _skills[_nextSkillIndex] : default;

        internal void AdvanceSkill()
        {
            if (_skills.Count > 0)
            {
                _nextSkillIndex = (_nextSkillIndex + 1) % _skills.Count;
            }
        }
    }
}
