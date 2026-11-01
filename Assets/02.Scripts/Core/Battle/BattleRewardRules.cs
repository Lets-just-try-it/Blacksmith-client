using System;
using System.Collections.Generic;
using Blacksmith.Core.Adventurers;

namespace Blacksmith.Core.Battle
{
    // System.Serializable이라 ScriptableObject 필드로도 쓸 수 있다.
    // 재료 번호는 대장간의 재료 데이터가 생기면 그 번호를 쓴다.
    [Serializable]
    public struct MaterialReward
    {
        public int MaterialId;
        public int Count;
    }

    public sealed class StageReward
    {
        public int Gold { get; set; }
        public int Experience { get; set; }
        public IReadOnlyList<MaterialReward> Materials { get; set; } = Array.Empty<MaterialReward>();
    }

    public readonly struct AdventurerReward
    {
        public int AdventurerId { get; }
        public int ExperienceGained { get; }
        public int PreviousLevel { get; }
        public AdventurerLevelProgress Progress { get; }

        public AdventurerReward(int adventurerId, int experienceGained, int previousLevel, AdventurerLevelProgress progress)
        {
            AdventurerId = adventurerId;
            ExperienceGained = experienceGained;
            PreviousLevel = previousLevel;
            Progress = progress;
        }

        public bool IsLevelUp => Progress.Level > PreviousLevel;
    }

    public sealed class BattleRewards
    {
        public int Gold { get; }
        public IReadOnlyList<MaterialReward> Materials { get; }
        public IReadOnlyList<AdventurerReward> Adventurers { get; }

        public BattleRewards(int gold, IReadOnlyList<MaterialReward> materials, IReadOnlyList<AdventurerReward> adventurers)
        {
            Gold = gold;
            Materials = materials;
            Adventurers = adventurers;
        }
    }

    // 전투 결과로 보상과 경험치를 정한다. 호스트가 계산한 값을 게임 상태에 반영한다. (docs/GAME_CONCEPT.md 7.1)
    public static class BattleRewardRules
    {
        // stages는 이번 전투에서 싸운 스테이지의 보상을 싸운 순서대로 담는다.
        // 패배해도 클리어한 스테이지까지의 보상을 주고, 경험치는 모험가마다 쓰러지기 전까지 클리어한 스테이지만큼 준다.
        public static BattleRewards Calculate(IReadOnlyList<StageReward> stages, BattleResult result,
            Func<int, AdventurerData> findAdventurer, AdventurerLevelRules levelRules)
        {
            int gold = 0;
            var materials = new List<MaterialReward>();
            for (int i = 0; i < result.StagesCleared && i < stages.Count; i++)
            {
                gold += stages[i].Gold;
                AddMaterials(materials, stages[i].Materials);
            }

            var adventurers = new List<AdventurerReward>(result.Adventurers.Count);
            foreach (AdventurerBattleResult adventurerResult in result.Adventurers)
            {
                AdventurerData adventurer = findAdventurer(adventurerResult.AdventurerId);
                // 사망한 모험가는 관리 목록에서 빠지므로 경험치를 주지 않는다.
                int gained = adventurerResult.IsDead ? 0 : SumExperience(stages, adventurerResult.StagesCleared);
                AdventurerLevelProgress progress = AdventurerLeveling.AddExperience(adventurer.Level, adventurer.Experience,
                    gained, levelRules);
                adventurers.Add(new AdventurerReward(adventurer.AdventurerId, gained, adventurer.Level, progress));
            }

            return new BattleRewards(gold, materials, adventurers);
        }

        private static int SumExperience(IReadOnlyList<StageReward> stages, int stagesCleared)
        {
            int experience = 0;
            for (int i = 0; i < stagesCleared && i < stages.Count; i++)
            {
                experience += stages[i].Experience;
            }

            return experience;
        }

        // 같은 재료는 한 줄로 합친다.
        private static void AddMaterials(List<MaterialReward> total, IReadOnlyList<MaterialReward> materials)
        {
            foreach (MaterialReward material in materials)
            {
                int index = total.FindIndex(existing => existing.MaterialId == material.MaterialId);
                if (index < 0)
                {
                    total.Add(material);
                }
                else
                {
                    total[index] = new MaterialReward
                    {
                        MaterialId = material.MaterialId,
                        Count = total[index].Count + material.Count,
                    };
                }
            }
        }
    }
}
