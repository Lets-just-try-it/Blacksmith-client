using System;
using System.Collections.Generic;

namespace Blacksmith.Core.Adventurers
{
    // 같은 시드와 규칙이면 같은 모험가가 나온다. 뽑는 순서를 바꾸면 기존 시드의 결과가 달라진다.
    public static class AdventurerGenerator
    {
        public static AdventurerData Generate(int seed, AdventurerGenerationRules rules)
        {
            Validate(rules);
            return Generate(new Random(seed), rules);
        }

        // 후보 수와 각 후보를 한 시드에서 이어서 뽑는다.
        public static List<AdventurerData> GenerateCandidates(int seed, AdventurerGenerationRules rules)
        {
            Validate(rules);
            var random = new Random(seed);
            int count = rules.CandidateCount.Pick(random);
            var candidates = new List<AdventurerData>(count);
            for (int i = 0; i < count; i++)
            {
                candidates.Add(Generate(random, rules));
            }

            return candidates;
        }

        private static AdventurerData Generate(Random random, AdventurerGenerationRules rules)
        {
            AdventurerJobRules job = rules.Jobs[random.Next(rules.Jobs.Count)];
            return new AdventurerData
            {
                AdventurerId = AdventurerData.NoAdventurerId,
                Name = rules.Names[random.Next(rules.Names.Count)],
                JobId = job.JobId,
                Level = rules.StartLevel,
                Appearance = new AdventurerAppearance
                {
                    HairPreset = random.Next(rules.HairPresetCount),
                    EyePreset = random.Next(rules.EyePresetCount),
                    MouthPreset = random.Next(rules.MouthPresetCount),
                    HairColor = random.Next(rules.HairColorCount),
                    SkinColor = random.Next(rules.SkinColorCount),
                },
                BaseStats = PickStats(random, job.BaseStats),
                GrowthStats = PickStats(random, job.GrowthStats),
                SkillId = job.SkillIds.Count > 0 ? job.SkillIds[random.Next(job.SkillIds.Count)] : AdventurerData.NoSkill,
                WeaponId = AdventurerData.NoEquipment,
                HelmetId = AdventurerData.NoEquipment,
                ArmorId = AdventurerData.NoEquipment,
                Condition = AdventurerCondition.Healthy,
            };
        }

        private static AdventurerStats PickStats(Random random, AdventurerStatRange range)
        {
            return new AdventurerStats
            {
                MaxHp = range.MaxHp.Pick(random),
                Attack = range.Attack.Pick(random),
                Defense = range.Defense.Pick(random),
            };
        }

        private static void Validate(AdventurerGenerationRules rules)
        {
            if (rules.Names == null || rules.Names.Count == 0)
            {
                throw new ArgumentException("이름 후보가 하나 이상 있어야 한다.", nameof(rules));
            }

            if (rules.Jobs == null || rules.Jobs.Count == 0)
            {
                throw new ArgumentException("직업이 하나 이상 있어야 한다.", nameof(rules));
            }

            if (rules.HairPresetCount < 1 || rules.EyePresetCount < 1 || rules.MouthPresetCount < 1
                || rules.HairColorCount < 1 || rules.SkinColorCount < 1)
            {
                throw new ArgumentException("외형 프리셋과 색은 하나 이상 있어야 한다.", nameof(rules));
            }
        }
    }
}
