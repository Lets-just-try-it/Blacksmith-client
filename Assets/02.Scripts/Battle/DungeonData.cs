using System;
using System.Collections.Generic;
using Blacksmith.Core.Battle;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 던전 한 단계 = 잡몹 스테이지 2개 + 보스 스테이지 1개 (docs/GAME_CONCEPT.md 7.2)
    [CreateAssetMenu(fileName = "Dungeon", menuName = "Blacksmith/Battle/Dungeon")]
    public class DungeonData : ScriptableObject
    {
        [Serializable]
        private class MonsterSpawn
        {
            public MonsterData Monster;
            public int SpawnOffset;
        }

        [Serializable]
        private class StageEntry
        {
            public bool IsBoss;
            public List<MonsterSpawn> Spawns = new List<MonsterSpawn>();
            [Tooltip("이 스테이지를 클리어하면 받는 골드")]
            public int Gold;
            [Tooltip("이 스테이지를 쓰러지지 않고 클리어한 모험가가 받는 경험치")]
            public int Experience;
            [Tooltip("클리어하면 반드시 받는 재료(광석)")]
            public List<MaterialReward> Materials = new List<MaterialReward>();
            [Tooltip("확률로 받는 부재료. 굴릴 때마다 성공하면 1개. 에메랄드 효과를 받는다")]
            public List<ChanceDrop> ChanceDrops = new List<ChanceDrop>();
            [Tooltip("확률로 받는 보석. 성공하면 목록 중 하나를 균등하게 고른다. 에메랄드 효과를 받지 않는다")]
            public List<ChanceDrop> GemDrops = new List<ChanceDrop>();
        }

        [SerializeField] private int _dungeonId;
        [SerializeField] private string _displayName;
        [SerializeField] private List<StageEntry> _stages = new List<StageEntry>();

        public int DungeonId => _dungeonId;
        public string DisplayName => _displayName;
        public int StageCount => _stages.Count;

        public bool IsBossStage(int stage)
        {
            return stage >= 0 && stage < _stages.Count && _stages[stage].IsBoss;
        }

        public int GetStageGold(int stage)
        {
            return stage >= 0 && stage < _stages.Count ? _stages[stage].Gold : 0;
        }

        // firstStage부터 lastStage까지(0부터 센 번호, 양 끝 포함) 이어서 싸울 스테이지를 넣는다.
        public void AddStagesTo(BattleSetup setup, int firstStage, int lastStage)
        {
            for (int i = firstStage; i <= lastStage; i++)
            {
                StageEntry entry = _stages[i];
                var stage = new StageSetup { IsBoss = entry.IsBoss };
                foreach (MonsterSpawn spawn in entry.Spawns)
                {
                    stage.Monsters.Add(spawn.Monster.ToSetup(spawn.SpawnOffset));
                }

                setup.Stages.Add(stage);
            }
        }

        // AddStagesTo와 같은 범위의 스테이지 보상을 싸우는 순서대로 돌려준다.
        public List<StageReward> GetStageRewards(int firstStage, int lastStage)
        {
            var rewards = new List<StageReward>();
            for (int i = firstStage; i <= lastStage; i++)
            {
                StageEntry entry = _stages[i];
                rewards.Add(new StageReward
                {
                    Gold = entry.Gold,
                    Experience = entry.Experience,
                    Materials = entry.Materials,
                    ChanceDrops = entry.ChanceDrops,
                    GemDrops = entry.GemDrops,
                });
            }

            return rewards;
        }

        public MonsterData FindMonster(int monsterId)
        {
            foreach (StageEntry entry in _stages)
            {
                foreach (MonsterSpawn spawn in entry.Spawns)
                {
                    if (spawn.Monster != null && spawn.Monster.MonsterId == monsterId)
                    {
                        return spawn.Monster;
                    }
                }
            }

            return null;
        }
    }
}
