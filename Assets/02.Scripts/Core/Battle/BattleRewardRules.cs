using System;
using System.Collections.Generic;
using Blacksmith.Core.Adventurers;

namespace Blacksmith.Core.Battle
{
    // System.Serializable이라 ScriptableObject 필드로도 쓸 수 있다. 재료 번호는 아이템 번호(ItemCatalog)다.
    [Serializable]
    public struct MaterialReward
    {
        public int MaterialId;
        public int Count;
    }

    // 몬스터 한 마리를 쓰러뜨려 받는 보상이다. 그 몬스터가 나온 웨이브를 클리어해야 받는다.
    // System.Serializable이라 ScriptableObject 필드로도 쓸 수 있다.
    [Serializable]
    public sealed class MonsterReward
    {
        public int Gold;
        // 이 몬스터가 나온 웨이브를 쓰러지지 않고 클리어한 모험가가 받는다.
        public int Experience;
        // 반드시 받는 재료(광석)
        public List<MaterialReward> Materials = new List<MaterialReward>();
        // 확률로 받는 부재료. 광석과 함께 에메랄드 효과를 받는다
        public List<ChanceDrop> ChanceDrops = new List<ChanceDrop>();
        // 확률로 받는 보석. 에메랄드 효과를 받지 않는다
        public List<ChanceDrop> GemDrops = new List<ChanceDrop>();
    }

    // 웨이브 하나의 보상이다. Monsters는 그 웨이브에 나온 몬스터와 같은 순서다.
    public sealed class WaveReward
    {
        public IReadOnlyList<MonsterReward> Monsters { get; }

        public WaveReward(IReadOnlyList<MonsterReward> monsters)
        {
            Monsters = monsters;
        }

        public int Gold
        {
            get
            {
                int gold = 0;
                foreach (MonsterReward monster in Monsters)
                {
                    gold += monster.Gold;
                }

                return gold;
            }
        }

        public int Experience
        {
            get
            {
                int experience = 0;
                foreach (MonsterReward monster in Monsters)
                {
                    experience += monster.Experience;
                }

                return experience;
            }
        }
    }

    public readonly struct AdventurerReward
    {
        public int AdventurerId { get; }
        // 사파이어 효과를 더한 값이다.
        public int ExperienceGained { get; }
        public int ExperienceBonus { get; }
        public int PreviousLevel { get; }
        public AdventurerLevelProgress Progress { get; }

        public AdventurerReward(int adventurerId, int experienceGained, int experienceBonus, int previousLevel,
            AdventurerLevelProgress progress)
        {
            AdventurerId = adventurerId;
            ExperienceGained = experienceGained;
            ExperienceBonus = experienceBonus;
            PreviousLevel = previousLevel;
            Progress = progress;
        }

        public bool IsLevelUp => Progress.Level > PreviousLevel;
    }

    public sealed class BattleRewards
    {
        // 토파즈 효과를 더한 값이다.
        public int Gold { get; }
        public int GoldBonus { get; }
        // 클리어한 웨이브의 재료와 에메랄드 효과로 늘어난 재료를 모두 합친 값이다.
        public IReadOnlyList<MaterialReward> Materials { get; }
        public IReadOnlyList<MaterialReward> BonusMaterials { get; }
        // 클리어한 웨이브마다 몬스터별로 나온 재료다. 전투 화면의 드랍 연출이 쓴다.
        public IReadOnlyList<WaveLoot> WaveLoots { get; }
        public IReadOnlyList<AdventurerReward> Adventurers { get; }

        public BattleRewards(int gold, int goldBonus, IReadOnlyList<MaterialReward> materials,
            IReadOnlyList<MaterialReward> bonusMaterials, IReadOnlyList<WaveLoot> waveLoots,
            IReadOnlyList<AdventurerReward> adventurers)
        {
            Gold = gold;
            GoldBonus = goldBonus;
            Materials = materials;
            BonusMaterials = bonusMaterials;
            WaveLoots = waveLoots;
            Adventurers = adventurers;
        }
    }

    // 전투 결과로 보상과 경험치를 정한다. 호스트가 계산한 값을 게임 상태에 반영한다. (docs/GAME_CONCEPT.md 7.1)
    public static class BattleRewardRules
    {
        // waves는 이번 전투에서 싸운 웨이브의 보상을 싸운 순서대로 담는다. setup은 그 전투의 설정이다(시드, 장신구 효과).
        // 패배해도 클리어한 웨이브까지의 보상을 주고, 경험치는 모험가마다 쓰러지기 전까지 클리어한 웨이브만큼 준다.
        // 장신구 효과: 토파즈·에메랄드는 파티 합산으로 늘리고, 사파이어는 입은 모험가 본인 경험치만 늘린다. 늘어난 수가 소수면 올림한다.
        public static BattleRewards Calculate(BattleSetup setup, IReadOnlyList<WaveReward> waves, BattleResult result,
            Func<int, AdventurerData> findAdventurer, AdventurerLevelRules levelRules)
        {
            IReadOnlyList<WaveLoot> allLoots = BattleDropRules.Roll(waves, setup.Seed);
            int clearedCount = Math.Min(result.WavesCleared, waves.Count);

            int baseGold = 0;
            var boostable = new List<MaterialReward>();
            var gems = new List<MaterialReward>();
            var waveLoots = new List<WaveLoot>(clearedCount);
            for (int i = 0; i < clearedCount; i++)
            {
                baseGold += waves[i].Gold;
                foreach (MonsterLoot loot in allLoots[i].Monsters)
                {
                    MaterialMath.AddAll(boostable, loot.Materials);
                    MaterialMath.AddAll(gems, loot.Gems);
                }

                waveLoots.Add(allLoots[i]);
            }

            int goldBonus = Bonus(baseGold, SumPartyPercent(setup, AccessoryEffectKind.GoldBonus));
            int materialPercent = SumPartyPercent(setup, AccessoryEffectKind.MaterialBonus);
            var bonusMaterials = new List<MaterialReward>();
            foreach (MaterialReward material in boostable)
            {
                MaterialMath.Add(bonusMaterials, material.MaterialId, Bonus(material.Count, materialPercent));
            }

            var materials = new List<MaterialReward>();
            MaterialMath.AddAll(materials, boostable);
            MaterialMath.AddAll(materials, bonusMaterials);
            MaterialMath.AddAll(materials, gems);

            var adventurers = new List<AdventurerReward>(result.Adventurers.Count);
            foreach (AdventurerBattleResult adventurerResult in result.Adventurers)
            {
                AdventurerData adventurer = findAdventurer(adventurerResult.AdventurerId);
                // 사망한 모험가는 관리 목록에서 빠지므로 경험치를 주지 않는다.
                int baseExperience = adventurerResult.IsDead ? 0 : SumExperience(waves, adventurerResult.WavesCleared);
                int experienceBonus = Bonus(baseExperience,
                    FindAccessory(setup, adventurerResult.AdventurerId).PercentOf(AccessoryEffectKind.ExperienceBonus));
                int gained = baseExperience + experienceBonus;
                AdventurerLevelProgress progress = AdventurerLeveling.AddExperience(adventurer.Level, adventurer.Experience,
                    gained, levelRules);
                adventurers.Add(new AdventurerReward(adventurer.AdventurerId, gained, experienceBonus, adventurer.Level, progress));
            }

            return new BattleRewards(baseGold + goldBonus, goldBonus, materials, bonusMaterials, waveLoots, adventurers);
        }

        // amount의 percent%를 올림한 값이다.
        public static int Bonus(int amount, int percent)
        {
            if (amount <= 0 || percent <= 0)
            {
                return 0;
            }

            return (int)(((long)amount * percent + 99) / 100);
        }

        private static int SumPartyPercent(BattleSetup setup, AccessoryEffectKind kind)
        {
            int percent = 0;
            foreach (AdventurerSetup adventurer in setup.Adventurers)
            {
                percent += adventurer.Accessory.PercentOf(kind);
            }

            return percent;
        }

        private static AccessoryEffect FindAccessory(BattleSetup setup, int adventurerId)
        {
            foreach (AdventurerSetup adventurer in setup.Adventurers)
            {
                if (adventurer.AdventurerId == adventurerId)
                {
                    return adventurer.Accessory;
                }
            }

            return default;
        }

        private static int SumExperience(IReadOnlyList<WaveReward> waves, int wavesCleared)
        {
            int experience = 0;
            for (int i = 0; i < wavesCleared && i < waves.Count; i++)
            {
                experience += waves[i].Experience;
            }

            return experience;
        }
    }
}
