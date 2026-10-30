namespace Blacksmith.Core.Battle
{
    public enum BattleTeam
    {
        Adventurer,
        Monster,
    }

    public sealed class BattleUnit
    {
        public int UnitId { get; }
        public BattleTeam Team { get; }
        // 모험가는 AdventurerId, 몬스터는 MonsterId다. 연출에서 외형과 이펙트를 찾는 데 쓴다.
        public int SourceId { get; }
        public int StageIndex { get; }
        public int Lane { get; }
        public UnitStats Stats { get; }
        public SkillData Skill { get; }

        public int Position { get; internal set; }
        public int Hp { get; internal set; }
        public int Mana { get; internal set; }
        public int AttackCooldown { get; internal set; }
        public bool IsFallen { get; internal set; }
        public int DamageDealt { get; internal set; }

        internal BattleUnit(int unitId, BattleTeam team, int sourceId, int stageIndex, int lane,
            UnitStats stats, SkillData skill, int position)
        {
            UnitId = unitId;
            Team = team;
            SourceId = sourceId;
            StageIndex = stageIndex;
            Lane = lane;
            Stats = stats;
            Skill = skill;
            Position = position;
            Hp = stats.MaxHp;
        }

        public bool IsAlive => !IsFallen;
        public bool IsManaFull => Stats.MaxMana > 0 && Mana >= Stats.MaxMana;
    }
}
