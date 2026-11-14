using System.Collections.Generic;
using Blacksmith.Core.Inventory;

namespace Blacksmith.Core.Battle
{
    public readonly struct AdventurerBattleResult
    {
        public int AdventurerId { get; }
        public bool IsFallen { get; }
        // 전투 뒤의 부상수치와 이번 전투에서 늘어난 값이다. 사망했어도 판정에 쓴 값이 남는다.
        public int Injury { get; }
        public int InjuryGained { get; }
        public bool IsDead { get; }
        public int DamageDealt { get; }
        // 쓰러지기 전까지 토벌한 무리 수다. BattleResult.ClearedGroups의 앞에서부터 이 수만큼의 경험치를 받는다.
        public int GroupsCleared { get; }
        // 루비 목걸이가 사망 판정을 막고 부서졌다.
        public bool IsAccessoryBroken { get; }
        // 전투 뒤의 장비 내구도. IsBroken이면 결과를 반영할 때 장비가 파괴된다.
        public EquipmentDurability Weapon { get; }
        public EquipmentDurability Helmet { get; }
        public EquipmentDurability Armor { get; }
        // 친밀도 전투 이벤트가 일어났는지. 빠른 회복은 InjuryGained를 줄였고, 사망 면제는 IsDead를 막았다(목걸이보다 먼저 본다).
        public bool IsFastRecovered { get; }
        public bool IsSkillBoosted { get; }
        public bool IsDeathExempted { get; }

        public AdventurerBattleResult(int adventurerId, bool isFallen, int injury, int injuryGained, bool isDead, int damageDealt,
            int groupsCleared, bool isAccessoryBroken = false, EquipmentDurability weapon = default, EquipmentDurability helmet = default,
            EquipmentDurability armor = default, bool isFastRecovered = false, bool isSkillBoosted = false, bool isDeathExempted = false)
        {
            IsFastRecovered = isFastRecovered;
            IsSkillBoosted = isSkillBoosted;
            IsDeathExempted = isDeathExempted;
            IsAccessoryBroken = isAccessoryBroken;
            Weapon = weapon;
            Helmet = helmet;
            Armor = armor;
            AdventurerId = adventurerId;
            IsFallen = isFallen;
            Injury = injury;
            InjuryGained = injuryGained;
            IsDead = isDead;
            DamageDealt = damageDealt;
            GroupsCleared = groupsCleared;
        }
    }

    // 쓰러진 몬스터 하나가 남긴 시체. 한 무리의 시체가 모여 시체 더미가 되고, 다음 날 같은 자리에서 채집한다 (docs/GAME_CONCEPT.md 7.4).
    public readonly struct BattleCorpse
    {
        public int MonsterId { get; }
        // BattleSetup.Groups 순서의 무리 번호와 무리 안의 순서
        public int GroupIndex { get; }
        public int GroupSlot { get; }
        // 쓰러진 자리(BattleSpace 단위)
        public int X { get; }
        public int Z { get; }

        public BattleCorpse(int monsterId, int groupIndex, int groupSlot, int x, int z)
        {
            MonsterId = monsterId;
            GroupIndex = groupIndex;
            GroupSlot = groupSlot;
            X = x;
            Z = z;
        }
    }

    public sealed class BattleResult
    {
        public bool IsVictory { get; }
        // 토벌한 무리 번호(BattleSetup.Groups 순서)를 토벌한 순서대로 담는다.
        public IReadOnlyList<int> ClearedGroups { get; }
        public int GroupsCleared => ClearedGroups.Count;
        public int EndTick { get; }
        public IReadOnlyList<AdventurerBattleResult> Adventurers { get; }
        // 쓰러진 몬스터의 시체를 쓰러진 순서대로 담는다.
        public IReadOnlyList<BattleCorpse> Corpses { get; }

        public BattleResult(bool isVictory, IReadOnlyList<int> clearedGroups, int endTick, IReadOnlyList<AdventurerBattleResult> adventurers,
            IReadOnlyList<BattleCorpse> corpses = null)
        {
            Corpses = corpses ?? new BattleCorpse[0];
            IsVictory = isVictory;
            ClearedGroups = clearedGroups;
            EndTick = endTick;
            Adventurers = adventurers;
        }
    }
}
