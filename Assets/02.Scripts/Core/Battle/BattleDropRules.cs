using System;
using System.Collections.Generic;

namespace Blacksmith.Core.Battle
{
    // 성공할 때마다 재료 하나를 주는 확률 드랍이다. 재료가 여럿이면 그중 하나를 균등하게 고른다.
    // System.Serializable이라 ScriptableObject 필드로도 쓸 수 있다.
    [Serializable]
    public struct ChanceDrop
    {
        public int Rolls;
        public int ChancePercent;
        public int[] MaterialIds;
    }

    // 몬스터 한 마리의 시체에서 갈무리하는 재료다. Materials는 부산물, Gems는 보석이다.
    public sealed class MonsterLoot
    {
        public IReadOnlyList<MaterialReward> Materials { get; }
        public IReadOnlyList<MaterialReward> Gems { get; }

        public MonsterLoot(IReadOnlyList<MaterialReward> materials, IReadOnlyList<MaterialReward> gems)
        {
            Materials = materials;
            Gems = gems;
        }
    }

    // 몬스터 무리 하나에서 나온 재료다. Monsters는 그 무리의 몬스터와 같은 순서다.
    public sealed class GroupLoot
    {
        public IReadOnlyList<MonsterLoot> Monsters { get; }

        public GroupLoot(IReadOnlyList<MonsterLoot> monsters)
        {
            Monsters = monsters;
        }
    }

    // 몬스터마다 시체에서 갈무리할 재료를 굴린다 (docs/GAME_CONCEPT.md 7.4). 전투 시드로 굴려, 다음 날 채집 맵의 시체 더미가 같은 값을 쓴다.
    // 몬스터가 많고 무리가 많을수록 굴리는 횟수가 늘어 기대 드랍도 는다.
    public static class BattleDropRules
    {
        // 전투 시뮬레이션과 같은 시드를 쓰되 다른 난수열이 나오게 섞는 값이다. 밸런스 수치가 아니다.
        private const int SeedSalt = 0x5EED_D20B;

        // 스테이지의 모든 무리를 BattleSetup.Groups 순서로 굴린다.
        public static IReadOnlyList<GroupLoot> Roll(IReadOnlyList<GroupReward> groups, int seed)
        {
            var random = new Random(seed ^ SeedSalt);
            var loots = new List<GroupLoot>(groups.Count);
            foreach (GroupReward group in groups)
            {
                var monsters = new List<MonsterLoot>(group.Monsters.Count);
                foreach (MonsterReward monster in group.Monsters)
                {
                    monsters.Add(RollMonster(monster, random));
                }

                loots.Add(new GroupLoot(monsters));
            }

            return loots;
        }

        private static MonsterLoot RollMonster(MonsterReward monster, Random random)
        {
            var materials = new List<MaterialReward>();
            RollAll(materials, monster.ChanceDrops, random);

            var gems = new List<MaterialReward>();
            RollAll(gems, monster.GemDrops, random);
            return new MonsterLoot(materials, gems);
        }

        private static void RollAll(List<MaterialReward> total, IReadOnlyList<ChanceDrop> drops, Random random)
        {
            foreach (ChanceDrop drop in drops)
            {
                for (int i = 0; i < drop.Rolls; i++)
                {
                    bool isSuccess = random.Next(100) < drop.ChancePercent;
                    int index = drop.MaterialIds != null && drop.MaterialIds.Length > 0 ? random.Next(drop.MaterialIds.Length) : -1;
                    if (isSuccess && index >= 0)
                    {
                        MaterialMath.Add(total, drop.MaterialIds[index], 1);
                    }
                }
            }
        }
    }

    // 같은 재료를 한 줄로 합치는 계산이다.
    internal static class MaterialMath
    {
        public static void AddAll(List<MaterialReward> total, IReadOnlyList<MaterialReward> materials)
        {
            foreach (MaterialReward material in materials)
            {
                Add(total, material.MaterialId, material.Count);
            }
        }

        public static void Add(List<MaterialReward> total, int materialId, int count)
        {
            if (count <= 0)
            {
                return;
            }

            int index = total.FindIndex(existing => existing.MaterialId == materialId);
            if (index < 0)
            {
                total.Add(new MaterialReward { MaterialId = materialId, Count = count });
                return;
            }

            total[index] = new MaterialReward { MaterialId = materialId, Count = total[index].Count + count };
        }
    }
}
