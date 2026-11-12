using System;
using System.Collections.Generic;

namespace Blacksmith.Core.Battle
{
    // 채집 맵에서 캘 수 있는 것 (docs/GAME_CONCEPT.md 7.4)
    public enum GatheringNodeKind
    {
        // 몬스터 무리 하나의 시체 더미. 부산물과 보석이 나온다.
        CorpsePile,
        // 그 단계의 광석이 나온다.
        OreVein,
        // 목재가 나온다.
        Tree,
        // 바닥에 떨어진 광석 소량
        LooseOre,
        // 바닥에 떨어진 목재 소량
        LooseWood,
    }

    // 채집할 것 하나. 자리는 전투 좌표(BattleSpace)다. 시체 더미만 무리 번호(BattleSetup.Groups 순서)를 가진다.
    public readonly struct GatheringNode
    {
        public GatheringNodeKind Kind { get; }
        public int X { get; }
        public int Z { get; }
        public int GroupIndex { get; }

        public GatheringNode(GatheringNodeKind kind, int x, int z, int groupIndex = -1)
        {
            Kind = kind;
            X = x;
            Z = z;
            GroupIndex = groupIndex;
        }
    }

    // 채집할 것을 놓는 수치. 밸런스 데이터(GatheringRulesData)에서 읽어 넘긴다. 길이는 칸 단위다.
    public sealed class GatheringLayoutSettings
    {
        public int OreVeins { get; set; }
        public int Trees { get; set; }
        public int LooseOre { get; set; }
        public int LooseWood { get; set; }
        // 채집할 것끼리(시체 더미 포함) 떨어지는 최소 거리
        public float MinSpacing { get; set; }
        // 시작 지점(돌아가는 포털)에서 떨어지는 최소 거리
        public float StartClearance { get; set; }
        // 이 칸 수 안의 이웃이 모두 바닥인 칸에만 놓아 벽에 붙지 않게 한다.
        public int WallMargin { get; set; }
    }

    // 채집 맵에 채집할 것을 놓는다. 맵, 시체 자리, 시드가 같으면 같은 결과라 참가자마다 따로 만들어도 번호가 맞는다.
    // 시체 더미는 무리마다 하나로, 무리 번호 순서로 앞에 놓는다. 나머지는 바닥 칸에 무작위로 놓는다.
    public static class GatheringLayoutRules
    {
        // 전투·맵 생성과 같은 시드를 쓰되 다른 난수열이 나오게 섞는 값이다. 밸런스 수치가 아니다.
        private const int SeedSalt = 0x6A7E_4C11;

        public static List<GatheringNode> Generate(StageMap map, IReadOnlyList<BattleCorpse> corpses, int seed,
            GatheringLayoutSettings settings, int maxNodes)
        {
            var nodes = new List<GatheringNode>();
            AddCorpsePiles(nodes, corpses, maxNodes);

            List<(int X, int Z)> candidates = FindCandidates(map, settings);
            Shuffle(candidates, new Random(seed ^ SeedSalt));
            int next = 0;
            Place(GatheringNodeKind.OreVein, settings.OreVeins);
            Place(GatheringNodeKind.Tree, settings.Trees);
            Place(GatheringNodeKind.LooseOre, settings.LooseOre);
            Place(GatheringNodeKind.LooseWood, settings.LooseWood);
            return nodes;

            // 후보를 섞은 순서로 보며 다른 것과 충분히 떨어진 칸에 놓는다. 자리가 모자라면 덜 놓는다.
            void Place(GatheringNodeKind kind, int count)
            {
                int placed = 0;
                while (placed < count && next < candidates.Count && nodes.Count < maxNodes)
                {
                    (int x, int z) = candidates[next++];
                    int px = BattleSpace.ToUnits(x + 0.5f);
                    int pz = BattleSpace.ToUnits(z + 0.5f);
                    if (IsFarFromAll(nodes, px, pz, settings.MinSpacing))
                    {
                        nodes.Add(new GatheringNode(kind, px, pz));
                        placed++;
                    }
                }
            }
        }

        // 무리마다 시체 자리의 가운데에 더미 하나를 둔다.
        private static void AddCorpsePiles(List<GatheringNode> nodes, IReadOnlyList<BattleCorpse> corpses, int maxNodes)
        {
            var groups = new SortedDictionary<int, (long X, long Z, int Count)>();
            foreach (BattleCorpse corpse in corpses)
            {
                groups.TryGetValue(corpse.GroupIndex, out (long X, long Z, int Count) sum);
                groups[corpse.GroupIndex] = (sum.X + corpse.X, sum.Z + corpse.Z, sum.Count + 1);
            }

            foreach (KeyValuePair<int, (long X, long Z, int Count)> group in groups)
            {
                if (nodes.Count >= maxNodes)
                {
                    return;
                }

                (long x, long z, int count) = group.Value;
                nodes.Add(new GatheringNode(GatheringNodeKind.CorpsePile, (int)(x / count), (int)(z / count), group.Key));
            }
        }

        private static List<(int X, int Z)> FindCandidates(StageMap map, GatheringLayoutSettings settings)
        {
            var candidates = new List<(int X, int Z)>();
            float clearance = settings.StartClearance * settings.StartClearance;
            for (int x = 0; x < map.Width; x++)
            {
                for (int z = 0; z < map.Height; z++)
                {
                    float dx = x + 0.5f - map.Start.X;
                    float dz = z + 0.5f - map.Start.Z;
                    if (dx * dx + dz * dz >= clearance && IsAwayFromWall(map, x, z, settings.WallMargin))
                    {
                        candidates.Add((x, z));
                    }
                }
            }

            return candidates;
        }

        private static bool IsAwayFromWall(StageMap map, int x, int z, int margin)
        {
            for (int dx = -margin; dx <= margin; dx++)
            {
                for (int dz = -margin; dz <= margin; dz++)
                {
                    if (!map.IsFloor(x + dx, z + dz))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsFarFromAll(List<GatheringNode> nodes, int x, int z, float spacingCells)
        {
            long spacing = BattleSpace.ToUnits(spacingCells);
            long minSquared = spacing * spacing;
            foreach (GatheringNode node in nodes)
            {
                long dx = node.X - x;
                long dz = node.Z - z;
                if (dx * dx + dz * dz < minSquared)
                {
                    return false;
                }
            }

            return true;
        }

        private static void Shuffle<T>(List<T> list, Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }

    // 채집해서 얻는 양을 정하는 수치. 밸런스 데이터에서 읽어 넘긴다. 범위는 양 끝을 포함한다.
    public sealed class GatheringYieldSettings
    {
        // 그 던전 단계의 광석
        public int OreItemId { get; set; }
        public int WoodItemId { get; set; }
        public int OreVeinMin { get; set; }
        public int OreVeinMax { get; set; }
        public int TreeMin { get; set; }
        public int TreeMax { get; set; }
        public int LooseOreMin { get; set; }
        public int LooseOreMax { get; set; }
        public int LooseWoodMin { get; set; }
        public int LooseWoodMax { get; set; }
    }

    // 채집할 것 하나에서 나오는 재료를 정한다. 같은 시드와 번호면 같은 결과라, 미리 보이는 값과 실제로 받는 값이 같다.
    // 시체 더미는 그 무리 몬스터들의 드랍(BattleDropRules)을 모두 합친 것이다. 전투 보상에는 재료가 없다 (docs/GAME_CONCEPT.md 7.1).
    public static class GatheringYieldRules
    {
        private const int SeedSalt = 0x1EAF_0D5E;

        // corpseLoots는 이 맵 전투의 무리별 드랍(BattleDropRules.Roll)이다. 시체 더미가 아니면 쓰지 않는다.
        public static List<MaterialReward> Roll(GatheringNode node, int nodeIndex, int seed, GatheringYieldSettings settings,
            IReadOnlyList<GroupLoot> corpseLoots)
        {
            var materials = new List<MaterialReward>();
            switch (node.Kind)
            {
                case GatheringNodeKind.CorpsePile:
                    if (node.GroupIndex >= 0 && node.GroupIndex < corpseLoots.Count)
                    {
                        foreach (MonsterLoot monster in corpseLoots[node.GroupIndex].Monsters)
                        {
                            MaterialMath.AddAll(materials, monster.Materials);
                            MaterialMath.AddAll(materials, monster.Gems);
                        }
                    }

                    break;
                case GatheringNodeKind.OreVein:
                    AddRandom(settings.OreItemId, settings.OreVeinMin, settings.OreVeinMax);
                    break;
                case GatheringNodeKind.Tree:
                    AddRandom(settings.WoodItemId, settings.TreeMin, settings.TreeMax);
                    break;
                case GatheringNodeKind.LooseOre:
                    AddRandom(settings.OreItemId, settings.LooseOreMin, settings.LooseOreMax);
                    break;
                case GatheringNodeKind.LooseWood:
                    AddRandom(settings.WoodItemId, settings.LooseWoodMin, settings.LooseWoodMax);
                    break;
            }

            return materials;

            void AddRandom(int itemId, int min, int max)
            {
                var random = new Random(seed ^ SeedSalt ^ (nodeIndex * 7919));
                int high = Math.Max(min, max);
                MaterialMath.Add(materials, itemId, random.Next(min, high + 1));
            }
        }
    }
}
