using System;
using System.Collections.Generic;
using Blacksmith.Core.Characters;

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
                Appearance = HumanoidAppearanceGenerator.Generate(random, rules.Appearance),
                BaseStats = PickStats(random, job.BaseStats),
                GrowthStats = PickStats(random, job.GrowthStats),
                SkillId = job.SkillIds.Count > 0 ? job.SkillIds[random.Next(job.SkillIds.Count)] : AdventurerData.NoSkill,
                // 영입 후보는 직업의 기본무기만 들고, 나머지 칸은 비어 있다.
                WeaponId = job.DefaultWeaponId,
                WeaponGrade = job.DefaultWeaponGrade,
                HelmetId = AdventurerData.NoEquipment,
                ArmorId = AdventurerData.NoEquipment,
                AccessoryId = AdventurerData.NoEquipment,
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

            foreach (AdventurerJobRules job in rules.Jobs)
            {
                if (job.DefaultWeaponId == AdventurerData.NoEquipment)
                {
                    throw new ArgumentException($"직업에 기본무기가 없다: {job.JobId}", nameof(rules));
                }
            }

            HumanoidAppearanceGenerator.Validate(rules.Appearance);
        }
    }
}
