using System;
using System.Collections.Generic;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Battle;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 모험가를 전투 유닛으로 바꿀 때 쓰는 직업별 전투 수치와 스킬 번호 표다.
    // 직업 번호와 스킬 번호는 광장의 직업 데이터(AdventurerJobData)와 같은 번호를 쓴다.
    // [미정] 직업별 공격 방식이 정해지면 직업 항목에 추가한다.
    [CreateAssetMenu(fileName = "AdventurerBattle", menuName = "Blacksmith/Battle/Adventurer Battle")]
    public class AdventurerBattleData : ScriptableObject
    {
        [Serializable]
        private class JobEntry
        {
            public int JobId;
            public JobBattleStats Stats;
            public BattleUnitView ViewPrefab;
        }

        [Serializable]
        private class SkillEntry
        {
            public int SkillId;
            public SkillData Skill;
        }

        [SerializeField] private List<JobEntry> _jobs = new List<JobEntry>();
        [SerializeField] private List<SkillEntry> _skills = new List<SkillEntry>();

        public AdventurerSetup ToSetup(AdventurerData adventurer)
        {
            JobEntry job = FindJob(adventurer.JobId);
            if (job == null)
            {
                throw new ArgumentException($"전투 수치가 없는 직업이다: {adventurer.JobId}", nameof(adventurer));
            }

            return AdventurerSetupRules.Create(adventurer, job.Stats, FindSkill(adventurer.SkillId));
        }

        public BattleUnitView GetViewPrefab(int jobId)
        {
            return FindJob(jobId)?.ViewPrefab;
        }

        private JobEntry FindJob(int jobId)
        {
            return _jobs.Find(job => job.JobId == jobId);
        }

        // 스킬이 없거나 표에 없는 번호면 스킬을 쓰지 않는다.
        private SkillData FindSkill(int skillId)
        {
            if (skillId == AdventurerData.NoSkill)
            {
                return default;
            }

            SkillEntry entry = _skills.Find(skill => skill.SkillId == skillId);
            if (entry == null)
            {
                Debug.LogWarning($"스킬 표에 없는 스킬 번호다: {skillId}", this);
                return default;
            }

            return entry.Skill;
        }

        private void OnValidate()
        {
            var jobIds = new HashSet<int>();
            foreach (JobEntry job in _jobs)
            {
                if (!jobIds.Add(job.JobId))
                {
                    Debug.LogWarning($"직업 번호가 겹친다: {job.JobId}", this);
                }
            }

            var skillIds = new HashSet<int>();
            foreach (SkillEntry skill in _skills)
            {
                if (!skillIds.Add(skill.SkillId))
                {
                    Debug.LogWarning($"스킬 번호가 겹친다: {skill.SkillId}", this);
                }
            }
        }
    }
}
