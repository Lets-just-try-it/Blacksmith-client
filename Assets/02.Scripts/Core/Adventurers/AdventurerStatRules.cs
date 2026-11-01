namespace Blacksmith.Core.Adventurers
{
    public static class AdventurerStatRules
    {
        public const int MinLevel = 1;

        // 레벨이 하나 오를 때마다 타고난 성장값만큼 늘어난다. 화면 표시와 전투 계산이 같은 함수를 쓴다.
        public static AdventurerStats GetStatsAtLevel(AdventurerStats baseStats, AdventurerStats growthStats, int level)
        {
            int levelsGained = level > MinLevel ? level - MinLevel : 0;
            return new AdventurerStats
            {
                MaxHp = baseStats.MaxHp + growthStats.MaxHp * levelsGained,
                Attack = baseStats.Attack + growthStats.Attack * levelsGained,
                Defense = baseStats.Defense + growthStats.Defense * levelsGained,
            };
        }

        public static AdventurerStats GetCurrentStats(AdventurerData adventurer)
        {
            return GetStatsAtLevel(adventurer.BaseStats, adventurer.GrowthStats, adventurer.Level);
        }
    }
}
