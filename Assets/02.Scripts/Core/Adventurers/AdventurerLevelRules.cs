using System;

namespace Blacksmith.Core.Adventurers
{
    // 밸런스 수치는 ScriptableObject에서 읽어 넘긴다.
    public sealed class AdventurerLevelRules
    {
        // 1레벨에서 2레벨로 오르는 데 필요한 경험치
        public int BaseRequiredExperience { get; set; }
        // 레벨이 하나 오를 때마다 필요 경험치가 늘어나는 양
        public int RequiredExperiencePerLevel { get; set; }
    }

    public readonly struct AdventurerLevelProgress
    {
        public int Level { get; }
        public int Experience { get; }

        public AdventurerLevelProgress(int level, int experience)
        {
            Level = level;
            Experience = experience;
        }
    }

    public static class AdventurerLeveling
    {
        // [추정] 필요 경험치 = 기본값 + 증가분 × (현재 레벨 - 1) (docs/GAME_CONCEPT.md 6.4)
        public static int GetRequiredExperience(int level, AdventurerLevelRules rules)
        {
            int levelsGained = level > AdventurerStatRules.MinLevel ? level - AdventurerStatRules.MinLevel : 0;
            return Math.Max(1, rules.BaseRequiredExperience + rules.RequiredExperiencePerLevel * levelsGained);
        }

        // 남는 경험치는 다음 레벨로 이어지고, 한 번에 여러 레벨이 오를 수 있다.
        public static AdventurerLevelProgress AddExperience(int level, int experience, int gained, AdventurerLevelRules rules)
        {
            experience += Math.Max(0, gained);
            int required = GetRequiredExperience(level, rules);
            while (experience >= required)
            {
                experience -= required;
                level++;
                required = GetRequiredExperience(level, rules);
            }

            return new AdventurerLevelProgress(level, experience);
        }
    }
}
