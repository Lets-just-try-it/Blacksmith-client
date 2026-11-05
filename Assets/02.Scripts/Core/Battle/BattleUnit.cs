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
        public int StageIndex { get; }
        // 옆으로 선 자리. 연출만 쓴다. 반 칸 단위이고 0이 길 가운데라, 줄마다 가운데에 맞춰 설 수 있다(2명 → -1, 1 / 3명 → -2, 0, 2).
        public int Lane { get; }
        // 분노하면 공격력과 공격 간격이 바뀐다.
        public UnitStats Stats { get; internal set; }
        public EnrageData Enrage { get; }

        public int Position { get; internal set; }
        public int Hp { get; internal set; }
        public int Mana { get; internal set; }
        public int AttackCooldown { get; internal set; }
        public bool IsFallen { get; internal set; }
        public bool IsEnraged { get; internal set; }
        public int DamageDealt { get; internal set; }
        // 쓰러지지 않은 채 클리어한 스테이지 수다. 경험치 계산에 쓴다.
        public int StagesCleared { get; internal set; }

        internal BattleUnit(int unitId, BattleTeam team, int sourceId, int stageIndex, int lane,
            UnitStats stats, IReadOnlyList<SkillData> skills, EnrageData enrage, int position)
        {
            UnitId = unitId;
            Team = team;
            SourceId = sourceId;
            StageIndex = stageIndex;
            Lane = lane;
            Stats = stats;
            _skills = skills ?? Array.Empty<SkillData>();
            Enrage = enrage;
            Position = position;
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
