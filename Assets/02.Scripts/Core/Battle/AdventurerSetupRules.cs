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
        public static AdventurerSetup Create(AdventurerData adventurer, JobBattleStats jobStats, SkillData skill)
        {
            return Create(adventurer, jobStats, skill, default);
        }

        // 체력·공격력·방어력은 관리 목록 화면과 같은 AdventurerStatRules에 장비 보정을 더해 구한다.
        // equipmentBonus는 장비 착용 창과 같은 EquipmentStatRules로 구한 값이다.
        // [미정] 직업이 전투 능력치에 주는 보정은 아직 넣지 않는다 (docs/GAME_CONCEPT.md 6.4).
        public static AdventurerSetup Create(AdventurerData adventurer, JobBattleStats jobStats, SkillData skill, AdventurerStats equipmentBonus)
        {
            AdventurerStats stats = AdventurerStatRules.Add(AdventurerStatRules.GetCurrentStats(adventurer), equipmentBonus);
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
                Injury = adventurer.Injury,
            };
        }
    }
}
