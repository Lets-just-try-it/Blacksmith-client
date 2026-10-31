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
        }

        [SerializeField] private int _dungeonId;
        [SerializeField] private string _displayName;
        [SerializeField] private List<StageEntry> _stages = new List<StageEntry>();

        public int DungeonId => _dungeonId;
        public string DisplayName => _displayName;
        public int StageCount => _stages.Count;

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
