using System.Collections.Generic;

namespace Blacksmith.Core.Battle
{
    public readonly struct AdventurerBattleResult
    {
        public int AdventurerId { get; }
        public bool IsInjured { get; }
        public bool IsDead { get; }
        public int DamageDealt { get; }
        // 쓰러지기 전까지 토벌한 무리 수다. BattleResult.ClearedGroups의 앞에서부터 이 수만큼의 경험치를 받는다.
        public int GroupsCleared { get; }
        // 루비 목걸이가 사망 판정을 막고 부서졌다.
        public bool IsAccessoryBroken { get; }

        public AdventurerBattleResult(int adventurerId, bool isInjured, bool isDead, int damageDealt, int groupsCleared,
            bool isAccessoryBroken = false)
        {
            IsAccessoryBroken = isAccessoryBroken;
            AdventurerId = adventurerId;
            IsInjured = isInjured;
            IsDead = isDead;
            DamageDealt = damageDealt;
            GroupsCleared = groupsCleared;
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

        public BattleResult(bool isVictory, IReadOnlyList<int> clearedGroups, int endTick, IReadOnlyList<AdventurerBattleResult> adventurers)
        {
            IsVictory = isVictory;
            ClearedGroups = clearedGroups;
            EndTick = endTick;
            Adventurers = adventurers;
        }
    }
}
