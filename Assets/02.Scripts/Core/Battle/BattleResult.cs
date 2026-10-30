using System.Collections.Generic;

namespace Blacksmith.Core.Battle
{
    public readonly struct AdventurerBattleResult
    {
        public int AdventurerId { get; }
        public bool IsInjured { get; }
        public bool IsDead { get; }
        public int DamageDealt { get; }

        public AdventurerBattleResult(int adventurerId, bool isInjured, bool isDead, int damageDealt)
        {
            AdventurerId = adventurerId;
            IsInjured = isInjured;
            IsDead = isDead;
            DamageDealt = damageDealt;
        }
    }

    public sealed class BattleResult
    {
        public bool IsVictory { get; }
        public int StagesCleared { get; }
        public int EndTick { get; }
        public IReadOnlyList<AdventurerBattleResult> Adventurers { get; }

        public BattleResult(bool isVictory, int stagesCleared, int endTick, IReadOnlyList<AdventurerBattleResult> adventurers)
        {
            IsVictory = isVictory;
            StagesCleared = stagesCleared;
            EndTick = endTick;
            Adventurers = adventurers;
        }
    }
}
