using System;
using Blacksmith.Core.Adventurers;

namespace Blacksmith.Core.Battle
{
    // 모험가 정보에 없는 전투 전용 수치다. 직업마다 하나씩 둔다.
    [Serializable]
    public struct JobBattleStats
    {
        public int AttackRange;
        public int AttackIntervalTicks;
        public int MoveSpeed;
        public int MaxMana;
        public int ManaPerTick;
        public int ManaPerHit;
    }

    public static class AdventurerSetupRules
    {
        // 체력·공격력·방어력은 관리 목록 화면과 같은 AdventurerStatRules로 구한다.
        // [미정] 장비와 직업이 전투 능력치에 주는 보정은 아직 넣지 않는다 (docs/GAME_CONCEPT.md 6.4).
        public static AdventurerSetup Create(AdventurerData adventurer, JobBattleStats jobStats, SkillData skill)
        {
            AdventurerStats stats = AdventurerStatRules.GetCurrentStats(adventurer);
            return new AdventurerSetup
            {
                AdventurerId = adventurer.AdventurerId,
                Stats = new UnitStats
                {
                    MaxHp = stats.MaxHp,
                    Attack = stats.Attack,
                    Defense = stats.Defense,
                    AttackRange = jobStats.AttackRange,
                    AttackIntervalTicks = jobStats.AttackIntervalTicks,
                    MoveSpeed = jobStats.MoveSpeed,
                    MaxMana = jobStats.MaxMana,
                    ManaPerTick = jobStats.ManaPerTick,
                    ManaPerHit = jobStats.ManaPerHit,
                },
                Skill = skill,
            };
        }
    }
}
