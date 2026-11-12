using System;
using System.Collections.Generic;
using Blacksmith.Core.Battle;
using UnityEngine;
using UnityEngine.Serialization;

namespace Blacksmith.Battle
{
    // 던전 한 단계 = 잡몹 스테이지 2개 + 보스 스테이지 1개 (docs/GAME_CONCEPT.md 7.2)
    // 스테이지는 몬스터 무리 여러 개로 이뤄진다. 잡몹 무리의 몬스터는 전투 시드로 몹 풀에서 뽑고,
    // 보스 스테이지는 잡몹 무리를 모두 토벌하면 보스 무리가 나타난다 (docs/GAME_CONCEPT.md 7.1 "몬스터 무리").
    // 맵과 무리 스폰 지점도 같은 시드로 만든다.
    [CreateAssetMenu(fileName = "Dungeon", menuName = "Blacksmith/Battle/Dungeon")]
    public class DungeonData : ScriptableObject
    {
        [Serializable]
        private class StageEntry
        {
            [Tooltip("[추정] 잡몹 무리마다 나오는 몬스터 수. 무리 수만큼 맵에 스폰 지점을 둔다")]
            [FormerlySerializedAs("WaveMonsterCounts")]
            public List<int> GroupMonsterCounts = new List<int>();
            [Tooltip("잡몹 무리에 나올 수 있는 몬스터. 무리마다 전투 시드로 균등하게 뽑는다")]
            public List<MonsterData> MobPool = new List<MonsterData>();
            [Tooltip("켜면 잡몹 무리를 모두 토벌한 뒤 보스가 나타난다")]
            public bool IsBoss;
            public MonsterData Boss;
        }

        [SerializeField] private int _dungeonId;
        [SerializeField] private string _displayName;
        [Tooltip("[추정] 잡몹 한 마리를 쓰러뜨려 받는 보상. 그 무리를 토벌해야 받는다")]
        [SerializeField] private MonsterReward _mobReward = new MonsterReward();
        [Tooltip("[추정] 보스를 쓰러뜨려 받는 보상")]
        [SerializeField] private MonsterReward _bossReward = new MonsterReward();
        [Tooltip("[추정] 한 무리 안에서 몬스터끼리 앞뒤로 벌어지는 거리")]
        [SerializeField, Min(0)] private int _monsterSpacing = 150;
        [SerializeField] private List<StageEntry> _stages = new List<StageEntry>();
        [Tooltip("이 던전의 스테이지 맵 생성 수치와 바닥")]
        [SerializeField] private StageMapData _stageMap;

        public int DungeonId => _dungeonId;
        public string DisplayName => _displayName;
        public StageMapData StageMap => _stageMap;
        public int StageCount => _stages.Count;

        public bool IsBossStage(int stage)
        {
            return IsValid(stage) && _stages[stage].IsBoss;
        }

        // 모든 무리를 토벌하면 받는 골드다. 무리마다 몬스터 수가 정해져 있어 구성이 달라도 같다.
        public int GetStageGold(int stage)
        {
            if (!IsValid(stage))
            {
                return 0;
            }

            int gold = 0;
            foreach (int count in _stages[stage].GroupMonsterCounts)
            {
                gold += count * _mobReward.Gold;
            }

            return gold + (_stages[stage].IsBoss ? _bossReward.Gold : 0);
        }

        // setup.Seed로 스테이지 맵과 스폰 지점을 만들고, 잡몹을 뽑아 무리를 넣는다. 보스 무리는 마지막이다.
        public void AddGroupsTo(BattleSetup setup, int stage)
        {
            if (_stageMap == null)
            {
                throw new InvalidOperationException(name + "에 스테이지 맵 데이터가 없다.");
            }

            List<List<MonsterData>> groups = BuildGroups(stage, setup.Seed, out bool hasBossGroup);
            int mobGroupCount = hasBossGroup ? groups.Count - 1 : groups.Count;
            StageMap map = GenerateMap(setup.Seed);
            StageLayout layout = StageLayoutGenerator.Generate(map, setup.Seed, mobGroupCount, hasBossGroup, _stageMap.ToLayoutSettings());
            setup.Map = map;
            for (int g = 0; g < groups.Count; g++)
            {
                bool isBoss = hasBossGroup && g == groups.Count - 1;
                MapPoint spawn = isBoss ? layout.Boss : g < layout.MobGroups.Count ? layout.MobGroups[g] : map.Start;
                var group = new MonsterGroupSetup { IsBoss = isBoss, Spawn = spawn };
                List<MonsterData> monsters = groups[g];
                for (int i = 0; i < monsters.Count; i++)
                {
                    group.Monsters.Add(monsters[i].ToSetup(i * _monsterSpacing));
                }

                setup.Groups.Add(group);
            }
        }

        // 전투 시드로 스테이지 맵을 만든다. 다음 날 채집하는 맵도 같은 시드로 같은 맵을 만든다.
        public StageMap GenerateMap(int seed)
        {
            if (_stageMap == null)
            {
                throw new InvalidOperationException(name + "에 스테이지 맵 데이터가 없다.");
            }

            return StageMapGenerator.Generate(seed, _stageMap.ToSettings());
        }

        // AddGroupsTo와 같은 시드로 같은 구성을 만들어 무리 보상을 같은 순서로 돌려준다.
        public List<GroupReward> GetGroupRewards(int stage, int seed)
        {
            var rewards = new List<GroupReward>();
            foreach (List<MonsterData> monsters in BuildGroups(stage, seed, out _))
            {
                var monsterRewards = new List<MonsterReward>(monsters.Count);
                foreach (MonsterData monster in monsters)
                {
                    monsterRewards.Add(monster.IsBoss ? _bossReward : _mobReward);
                }

                rewards.Add(new GroupReward(monsterRewards));
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

        private List<List<MonsterData>> BuildGroups(int stage, int seed, out bool hasBossGroup)
        {
            StageEntry entry = _stages[stage];
            var ranges = new List<int>(entry.MobPool.Count);
            foreach (MonsterData monster in entry.MobPool)
            {
                ranges.Add(monster.AttackRange);
            }

            var groups = new List<List<MonsterData>>();
            foreach (int[] picks in BattleGroupRules.PickMobs(seed, entry.GroupMonsterCounts, ranges))
            {
                groups.Add(new List<MonsterData>(Array.ConvertAll(picks, index => entry.MobPool[index])));
            }

            hasBossGroup = entry.IsBoss && entry.Boss != null;
            if (hasBossGroup)
            {
                groups.Add(new List<MonsterData> { entry.Boss });
            }

            return groups;
        }
    }
}
