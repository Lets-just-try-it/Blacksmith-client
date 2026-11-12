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

    // 몬스터 한 마리를 쓰러뜨려 받는 보상이다. 그 몬스터의 무리를 토벌해야 받는다.
    // System.Serializable이라 ScriptableObject 필드로도 쓸 수 있다.
    [Serializable]
    public sealed class MonsterReward
    {
        public int Gold;
        // 이 몬스터의 무리를 쓰러지지 않고 토벌한 모험가가 받는다.
        public int Experience;
        // 다음 날 이 몬스터의 시체를 갈무리해 확률로 얻는 부산물(가죽, 힘줄, 마석 등). 전투 보상이 아니다 (docs/GAME_CONCEPT.md 7.4)
        public List<ChanceDrop> ChanceDrops = new List<ChanceDrop>();
        // 다음 날 시체를 갈무리해 확률로 얻는 보석
        public List<ChanceDrop> GemDrops = new List<ChanceDrop>();
    }

    // 몬스터 무리 하나의 보상이다. Monsters는 그 무리의 몬스터와 같은 순서다.
    public sealed class GroupReward
    {
        public IReadOnlyList<MonsterReward> Monsters { get; }

        public GroupReward(IReadOnlyList<MonsterReward> monsters)
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
        public IReadOnlyList<AdventurerReward> Adventurers { get; }

        public BattleRewards(int gold, int goldBonus, IReadOnlyList<AdventurerReward> adventurers)
        {
            Gold = gold;
            GoldBonus = goldBonus;
            Adventurers = adventurers;
        }
    }

    // 전투 결과로 보상과 경험치를 정한다. 호스트가 계산한 값을 게임 상태에 반영한다. (docs/GAME_CONCEPT.md 7.1)
    // 보상은 골드와 경험치뿐이다. 재료는 다음 날 채집으로 얻는다(GatheringYieldRules).
    public static class BattleRewardRules
    {
        // groups는 이번 전투의 무리 보상을 BattleSetup.Groups 순서로 담는다. setup은 그 전투의 설정이다(장신구 효과).
        // 패배해도 토벌한 무리의 보상을 주고, 경험치는 모험가마다 쓰러지기 전까지 토벌한 무리만큼 준다.
        // 장신구 효과: 토파즈는 파티 합산으로 골드를 늘리고, 사파이어는 입은 모험가 본인 경험치만 늘린다. 늘어난 수가 소수면 올림한다.
        public static BattleRewards Calculate(BattleSetup setup, IReadOnlyList<GroupReward> groups, BattleResult result,
            Func<int, AdventurerData> findAdventurer, AdventurerLevelRules levelRules)
        {
            int baseGold = 0;
            foreach (int group in result.ClearedGroups)
            {
                baseGold += groups[group].Gold;
            }

            int goldBonus = Bonus(baseGold, SumPartyPercent(setup, AccessoryEffectKind.GoldBonus));

            var adventurers = new List<AdventurerReward>(result.Adventurers.Count);
            foreach (AdventurerBattleResult adventurerResult in result.Adventurers)
            {
                AdventurerData adventurer = findAdventurer(adventurerResult.AdventurerId);
                // 사망한 모험가는 관리 목록에서 빠지므로 경험치를 주지 않는다.
                int baseExperience = adventurerResult.IsDead ? 0 : SumExperience(groups, result.ClearedGroups, adventurerResult.GroupsCleared);
                int experienceBonus = Bonus(baseExperience,
                    FindAccessory(setup, adventurerResult.AdventurerId).PercentOf(AccessoryEffectKind.ExperienceBonus));
                int gained = baseExperience + experienceBonus;
                AdventurerLevelProgress progress = AdventurerLeveling.AddExperience(adventurer.Level, adventurer.Experience,
                    gained, levelRules);
                adventurers.Add(new AdventurerReward(adventurer.AdventurerId, gained, experienceBonus, adventurer.Level, progress));
            }

            return new BattleRewards(baseGold + goldBonus, goldBonus, adventurers);
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

        // 쓰러지기 전까지 토벌한 무리는 토벌 순서의 앞에서부터 groupsCleared개다.
        private static int SumExperience(IReadOnlyList<GroupReward> groups, IReadOnlyList<int> clearedGroups, int groupsCleared)
        {
            int experience = 0;
            for (int i = 0; i < groupsCleared && i < clearedGroups.Count; i++)
            {
                experience += groups[clearedGroups[i]].Experience;
            }

            return experience;
        }
    }
}
