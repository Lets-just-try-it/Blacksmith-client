using System.Collections.Generic;

namespace Blacksmith.Core.Battle
{
    public readonly struct AdventurerBattleResult
    {
        public int AdventurerId { get; }
        public bool IsInjured { get; }
        public bool IsDead { get; }
        public int DamageDealt { get; }
        // 쓰러지기 전까지 클리어한 웨이브 수다. 이 수만큼의 웨이브 경험치를 받는다.
        public int WavesCleared { get; }
        // 루비 목걸이가 사망 판정을 막고 부서졌다.
        public bool IsAccessoryBroken { get; }

        public AdventurerBattleResult(int adventurerId, bool isInjured, bool isDead, int damageDealt, int wavesCleared,
            bool isAccessoryBroken = false)
        {
            IsAccessoryBroken = isAccessoryBroken;
            AdventurerId = adventurerId;
            IsInjured = isInjured;
            IsDead = isDead;
            DamageDealt = damageDealt;
            WavesCleared = wavesCleared;
        }
    }

    public sealed class BattleResult
    {
        public bool IsVictory { get; }
        public int WavesCleared { get; }
        public int EndTick { get; }
        public IReadOnlyList<AdventurerBattleResult> Adventurers { get; }

        public BattleResult(bool isVictory, int wavesCleared, int endTick, IReadOnlyList<AdventurerBattleResult> adventurers)
        {
            IsVictory = isVictory;
            WavesCleared = wavesCleared;
            EndTick = endTick;
            Adventurers = adventurers;
        }
    }
}
