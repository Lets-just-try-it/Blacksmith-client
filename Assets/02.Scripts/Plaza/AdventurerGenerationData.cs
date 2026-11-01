using System.Collections.Generic;
using Blacksmith.Core.Adventurers;
using UnityEngine;

namespace Blacksmith.Plaza
{
    [CreateAssetMenu(fileName = "AdventurerGeneration", menuName = "Blacksmith/Balance/Adventurer Generation")]
    public class AdventurerGenerationData : ScriptableObject
    {
        [SerializeField] private string[] _names = new string[0];
        [SerializeField] private AdventurerJobData[] _jobs = new AdventurerJobData[0];
        [SerializeField, Min(1)] private int _startLevel = 1;
        [SerializeField, Min(1)] private int _hairPresetCount = 1;
        [SerializeField, Min(1)] private int _eyePresetCount = 1;
        [SerializeField, Min(1)] private int _mouthPresetCount = 1;
        [SerializeField, Min(1)] private int _hairColorCount = 1;
        [SerializeField, Min(1)] private int _skinColorCount = 1;
        [Tooltip("광장에 한 번에 나오는 영입 후보 수 (최소~최대)")]
        [SerializeField] private IntRange _candidateCount = new IntRange(1, 4);

        public AdventurerGenerationRules ToRules()
        {
            var jobs = new List<AdventurerJobRules>(_jobs.Length);
            foreach (AdventurerJobData job in _jobs)
            {
                jobs.Add(job.ToRules());
            }

            return new AdventurerGenerationRules
            {
                Names = _names,
                Jobs = jobs,
                StartLevel = _startLevel,
                HairPresetCount = _hairPresetCount,
                EyePresetCount = _eyePresetCount,
                MouthPresetCount = _mouthPresetCount,
                HairColorCount = _hairColorCount,
                SkinColorCount = _skinColorCount,
                CandidateCount = _candidateCount,
            };
        }

        public AdventurerJobData FindJob(int jobId)
        {
            foreach (AdventurerJobData job in _jobs)
            {
                if (job.JobId == jobId)
                {
                    return job;
                }
            }

            return null;
        }

        private void OnValidate()
        {
            if (_candidateCount.Min < 0 || _candidateCount.Min > _candidateCount.Max)
            {
                Debug.LogWarning("영입 후보 수의 범위가 잘못되었다.", this);
            }

            var jobIds = new HashSet<int>();
            foreach (AdventurerJobData job in _jobs)
            {
                if (job != null && !jobIds.Add(job.JobId))
                {
                    Debug.LogWarning($"직업 번호가 겹친다: {job.JobId}", this);
                }
            }
        }
    }
}
