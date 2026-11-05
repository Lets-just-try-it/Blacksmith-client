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

    // 스테이지 하나에서 나온 재료다. Materials는 에메랄드 효과를 받는 광석·부재료, Gems는 받지 않는 보석이다.
    public sealed class StageLoot
    {
        public IReadOnlyList<MaterialReward> Materials { get; }
        public IReadOnlyList<MaterialReward> Gems { get; }

        public StageLoot(IReadOnlyList<MaterialReward> materials, IReadOnlyList<MaterialReward> gems)
        {
            Materials = materials;
            Gems = gems;
        }
    }

    // 스테이지 재료 드랍을 정한다 (docs/GAME_CONCEPT.md 7.1 "보상과 경험치").
    // 같은 시드면 같은 결과라, 결과창 보상과 전투 화면의 드랍 연출이 같은 값을 쓴다.
    public static class BattleDropRules
    {
        // 전투 시뮬레이션과 같은 시드를 쓰되 다른 난수열이 나오게 섞는 값이다. 밸런스 수치가 아니다.
        private const int SeedSalt = 0x5EED_D20B;

        // 싸운 모든 스테이지를 순서대로 굴린다. 클리어하지 못한 스테이지의 결과는 받는 쪽이 버린다.
        public static IReadOnlyList<StageLoot> Roll(IReadOnlyList<StageReward> stages, int seed)
        {
            var random = new Random(seed ^ SeedSalt);
            var loots = new List<StageLoot>(stages.Count);
            foreach (StageReward stage in stages)
            {
                var materials = new List<MaterialReward>();
                MaterialMath.AddAll(materials, stage.Materials);
                RollAll(materials, stage.ChanceDrops, random);

                var gems = new List<MaterialReward>();
                RollAll(gems, stage.GemDrops, random);
                loots.Add(new StageLoot(materials, gems));
            }

            return loots;
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
