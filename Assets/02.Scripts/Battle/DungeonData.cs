using System;
using System.Collections.Generic;
using Blacksmith.Core.Battle;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 던전 한 단계 = 잡몹 스테이지 2개 + 보스 스테이지 1개 (docs/GAME_CONCEPT.md 7.2)
    // 스테이지는 웨이브 여러 개로 이뤄진다. 잡몹 웨이브의 몬스터는 전투 시드로 몹 풀에서 뽑고,
    // 보스 스테이지는 잡몹 웨이브를 모두 이긴 뒤 보스 웨이브를 싸운다 (docs/GAME_CONCEPT.md 7.1 "웨이브").
    [CreateAssetMenu(fileName = "Dungeon", menuName = "Blacksmith/Battle/Dungeon")]
    public class DungeonData : ScriptableObject
    {
        [Serializable]
        private class StageEntry
        {
            [Tooltip("[추정] 잡몹 웨이브마다 나오는 몬스터 수. 위에서부터 차례로 싸운다")]
            public List<int> WaveMonsterCounts = new List<int>();
            [Tooltip("잡몹 웨이브에 나올 수 있는 몬스터. 웨이브마다 전투 시드로 균등하게 뽑는다")]
            public List<MonsterData> MobPool = new List<MonsterData>();
            [Tooltip("켜면 잡몹 웨이브를 모두 이긴 뒤 보스 웨이브를 싸운다")]
            public bool IsBoss;
            public MonsterData Boss;
        }

        [SerializeField] private int _dungeonId;
        [SerializeField] private string _displayName;
        [Tooltip("[추정] 잡몹 한 마리를 쓰러뜨려 받는 보상. 그 웨이브를 클리어해야 받는다")]
        [SerializeField] private MonsterReward _mobReward = new MonsterReward();
        [Tooltip("[추정] 보스를 쓰러뜨려 받는 보상")]
        [SerializeField] private MonsterReward _bossReward = new MonsterReward();
        [Tooltip("[추정] 한 웨이브 안에서 몬스터끼리 앞뒤로 벌어지는 거리")]
        [SerializeField, Min(0)] private int _monsterSpacing = 150;
        [SerializeField] private List<StageEntry> _stages = new List<StageEntry>();

        public int DungeonId => _dungeonId;
        public string DisplayName => _displayName;
        public int StageCount => _stages.Count;

        public bool IsBossStage(int stage)
        {
            return IsValid(stage) && _stages[stage].IsBoss;
        }

        // 모든 웨이브를 이기면 받는 골드다. 웨이브마다 몬스터 수가 정해져 있어 구성이 달라도 같다.
        public int GetStageGold(int stage)
        {
            if (!IsValid(stage))
            {
                return 0;
            }

            int gold = 0;
            foreach (int count in _stages[stage].WaveMonsterCounts)
            {
                gold += count * _mobReward.Gold;
            }

            return gold + (_stages[stage].IsBoss ? _bossReward.Gold : 0);
        }

        // 고른 스테이지의 웨이브를 싸우는 순서대로 넣는다. setup.Seed로 잡몹을 뽑는다.
        public void AddWavesTo(BattleSetup setup, int stage)
        {
            List<List<MonsterData>> waves = BuildWaves(stage, setup.Seed, out bool hasBossWave);
            for (int w = 0; w < waves.Count; w++)
            {
                List<MonsterData> monsters = waves[w];
                var wave = new WaveSetup { IsBoss = hasBossWave && w == waves.Count - 1 };
                for (int i = 0; i < monsters.Count; i++)
                {
                    wave.Monsters.Add(monsters[i].ToSetup(i * _monsterSpacing));
                }

                setup.Waves.Add(wave);
            }
        }

        // AddWavesTo와 같은 시드로 같은 구성을 만들어 웨이브 보상을 싸우는 순서대로 돌려준다.
        public List<WaveReward> GetWaveRewards(int stage, int seed)
        {
            var rewards = new List<WaveReward>();
            foreach (List<MonsterData> monsters in BuildWaves(stage, seed, out _))
            {
                var monsterRewards = new List<MonsterReward>(monsters.Count);
                foreach (MonsterData monster in monsters)
                {
                    monsterRewards.Add(monster.IsBoss ? _bossReward : _mobReward);
                }

                rewards.Add(new WaveReward(monsterRewards));
            }

            return rewards;
        }

        public MonsterData FindMonster(int monsterId)
        {
            foreach (StageEntry entry in _stages)
            {
                if (entry.Boss != null && entry.Boss.MonsterId == monsterId)
                {
                    return entry.Boss;
                }

                MonsterData found = entry.MobPool.Find(monster => monster != null && monster.MonsterId == monsterId);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private bool IsValid(int stage)
        {
            return stage >= 0 && stage < _stages.Count;
        }

        private List<List<MonsterData>> BuildWaves(int stage, int seed, out bool hasBossWave)
        {
            StageEntry entry = _stages[stage];
            var ranges = new List<int>(entry.MobPool.Count);
            foreach (MonsterData monster in entry.MobPool)
            {
                ranges.Add(monster.AttackRange);
            }

            var waves = new List<List<MonsterData>>();
            foreach (int[] picks in BattleWaveRules.PickMobs(seed, entry.WaveMonsterCounts, ranges))
            {
                waves.Add(new List<MonsterData>(Array.ConvertAll(picks, index => entry.MobPool[index])));
            }

            hasBossWave = entry.IsBoss && entry.Boss != null;
            if (hasBossWave)
            {
                waves.Add(new List<MonsterData> { entry.Boss });
            }

            return waves;
        }
    }
}
