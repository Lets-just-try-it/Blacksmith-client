using System;
using System.Collections.Generic;
using Blacksmith.Core.Characters;

namespace Blacksmith.Core.Adventurers
{
    // 최솟값과 최댓값을 모두 포함한다.
    [Serializable]
    public struct IntRange
    {
        public int Min;
        public int Max;

        public IntRange(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public int Pick(Random random)
        {
            return random.Next(Min, Max + 1);
        }
    }

    [Serializable]
    public struct AdventurerStatRange
    {
        public IntRange MaxHp;
        public IntRange Attack;
        public IntRange Defense;
    }

    // 직업마다 타고나는 능력치·성장값 범위와 스킬 후보가 다르다.
    public sealed class AdventurerJobRules
    {
        public int JobId { get; set; }
        public AdventurerStatRange BaseStats { get; set; }
        public AdventurerStatRange GrowthStats { get; set; }
        // 비어 있으면 스킬 없이 생성한다.
        public IReadOnlyList<int> SkillIds { get; set; } = Array.Empty<int>();
        // 영입 후보가 처음부터 들고 있는 무기의 아이템 번호와 등급. 모험가는 맨손으로 생성되지 않는다.
        public int DefaultWeaponId { get; set; }
        public int DefaultWeaponGrade { get; set; }
    }

    // 밸런스 수치는 ScriptableObject에서 읽어 넘긴다.
    public sealed class AdventurerGenerationRules
    {
        public IReadOnlyList<string> Names { get; set; }
        public IReadOnlyList<AdventurerJobRules> Jobs { get; set; }
        public int StartLevel { get; set; }
        public HumanoidAppearanceRules Appearance { get; set; }
        // 광장에 한 번에 나오는 영입 후보 수
        public IntRange CandidateCount { get; set; }
    }
}
