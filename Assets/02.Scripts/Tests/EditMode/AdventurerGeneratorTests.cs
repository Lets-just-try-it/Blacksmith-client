using System;
using System.Collections.Generic;
using Blacksmith.Core.Adventurers;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class AdventurerGeneratorTests
    {
        // 테스트 전용 수치다. 실제 범위는 AdventurerGeneration 데이터를 따른다.
        private static AdventurerGenerationRules CreateRules()
        {
            return new AdventurerGenerationRules
            {
                Names = new[] { "가", "나", "다" },
                Jobs = new[]
                {
                    new AdventurerJobRules
                    {
                        JobId = 1,
                        BaseStats = CreateStatRange(new IntRange(100, 120), new IntRange(10, 12), new IntRange(8, 10)),
                        GrowthStats = CreateStatRange(new IntRange(3, 5), new IntRange(1, 2), new IntRange(1, 2)),
                        SkillIds = new[] { 11, 12 },
                    },
                    new AdventurerJobRules
                    {
                        JobId = 2,
                        BaseStats = CreateStatRange(new IntRange(60, 70), new IntRange(20, 25), new IntRange(2, 4)),
                        GrowthStats = CreateStatRange(new IntRange(1, 2), new IntRange(3, 4), new IntRange(0, 1)),
                    },
                },
                StartLevel = 1,
                HairPresetCount = 3,
                EyePresetCount = 4,
                MouthPresetCount = 2,
                HairColorCount = 12,
                SkinColorCount = 5,
                CandidateCount = new IntRange(1, 4),
            };
        }

        private static AdventurerStatRange CreateStatRange(IntRange maxHp, IntRange attack, IntRange defense)
        {
            return new AdventurerStatRange { MaxHp = maxHp, Attack = attack, Defense = defense };
        }

        [Test]
        public void Generate_SameSeed_ReturnsSameAdventurer()
        {
            AdventurerGenerationRules rules = CreateRules();
            for (int seed = 0; seed < 50; seed++)
            {
                AssertSame(AdventurerGenerator.Generate(seed, rules), AdventurerGenerator.Generate(seed, rules));
            }
        }

        [Test]
        public void GenerateCandidates_SameSeed_ReturnsSameCandidates()
        {
            AdventurerGenerationRules rules = CreateRules();
            List<AdventurerData> first = AdventurerGenerator.GenerateCandidates(42, rules);
            List<AdventurerData> second = AdventurerGenerator.GenerateCandidates(42, rules);

            Assert.AreEqual(first.Count, second.Count);
            for (int i = 0; i < first.Count; i++)
            {
                AssertSame(first[i], second[i]);
            }
        }

        [Test]
        public void GenerateCandidates_CountStaysInRangeAndCoversBounds()
        {
            AdventurerGenerationRules rules = CreateRules();
            var seen = new HashSet<int>();
            for (int seed = 0; seed < 200; seed++)
            {
                int count = AdventurerGenerator.GenerateCandidates(seed, rules).Count;
                Assert.That(count, Is.InRange(rules.CandidateCount.Min, rules.CandidateCount.Max));
                seen.Add(count);
            }

            Assert.That(seen, Does.Contain(rules.CandidateCount.Min));
            Assert.That(seen, Does.Contain(rules.CandidateCount.Max));
        }

        [Test]
        public void Generate_ValuesStayInJobRanges()
        {
            AdventurerGenerationRules rules = CreateRules();
            for (int seed = 0; seed < 200; seed++)
            {
                AdventurerData adventurer = AdventurerGenerator.Generate(seed, rules);
                AdventurerJobRules job = FindJob(rules, adventurer.JobId);

                AssertInRange(adventurer.BaseStats, job.BaseStats);
                AssertInRange(adventurer.GrowthStats, job.GrowthStats);
                Assert.That(adventurer.Appearance.HairPreset, Is.InRange(0, rules.HairPresetCount - 1));
                Assert.That(adventurer.Appearance.EyePreset, Is.InRange(0, rules.EyePresetCount - 1));
                Assert.That(adventurer.Appearance.MouthPreset, Is.InRange(0, rules.MouthPresetCount - 1));
                Assert.That(adventurer.Appearance.HairColor, Is.InRange(0, rules.HairColorCount - 1));
                Assert.That(adventurer.Appearance.SkinColor, Is.InRange(0, rules.SkinColorCount - 1));
                Assert.That(rules.Names, Does.Contain(adventurer.Name));
            }
        }

        [Test]
        public void Generate_PicksSkillFromJobOrNone()
        {
            AdventurerGenerationRules rules = CreateRules();
            for (int seed = 0; seed < 100; seed++)
            {
                AdventurerData adventurer = AdventurerGenerator.Generate(seed, rules);
                AdventurerJobRules job = FindJob(rules, adventurer.JobId);
                if (job.SkillIds.Count == 0)
                {
                    Assert.AreEqual(AdventurerData.NoSkill, adventurer.SkillId);
                }
                else
                {
                    Assert.That(job.SkillIds, Does.Contain(adventurer.SkillId));
                }
            }
        }

        [Test]
        public void Generate_StartsHealthyWithoutIdAndEquipment()
        {
            AdventurerData adventurer = AdventurerGenerator.Generate(7, CreateRules());

            Assert.AreEqual(AdventurerData.NoAdventurerId, adventurer.AdventurerId);
            Assert.AreEqual(1, adventurer.Level);
            Assert.AreEqual(AdventurerCondition.Healthy, adventurer.Condition);
            Assert.AreEqual(AdventurerData.NoEquipment, adventurer.WeaponId);
            Assert.AreEqual(AdventurerData.NoEquipment, adventurer.HelmetId);
            Assert.AreEqual(AdventurerData.NoEquipment, adventurer.ArmorId);
        }

        [Test]
        public void Generate_WithoutJobs_Throws()
        {
            AdventurerGenerationRules rules = CreateRules();
            rules.Jobs = new AdventurerJobRules[0];

            Assert.Throws<ArgumentException>(() => AdventurerGenerator.Generate(1, rules));
        }

        private static AdventurerJobRules FindJob(AdventurerGenerationRules rules, int jobId)
        {
            foreach (AdventurerJobRules job in rules.Jobs)
            {
                if (job.JobId == jobId)
                {
                    return job;
                }
            }

            Assert.Fail($"규칙에 없는 직업이다: {jobId}");
            return null;
        }

        private static void AssertInRange(AdventurerStats stats, AdventurerStatRange range)
        {
            Assert.That(stats.MaxHp, Is.InRange(range.MaxHp.Min, range.MaxHp.Max));
            Assert.That(stats.Attack, Is.InRange(range.Attack.Min, range.Attack.Max));
            Assert.That(stats.Defense, Is.InRange(range.Defense.Min, range.Defense.Max));
        }

        private static void AssertSame(AdventurerData expected, AdventurerData actual)
        {
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.JobId, actual.JobId);
            Assert.AreEqual(expected.Level, actual.Level);
            Assert.AreEqual(expected.Appearance, actual.Appearance);
            Assert.AreEqual(expected.BaseStats, actual.BaseStats);
            Assert.AreEqual(expected.GrowthStats, actual.GrowthStats);
            Assert.AreEqual(expected.SkillId, actual.SkillId);
        }
    }
}
