namespace Blacksmith.Core.Battle
{
    public enum BattleEventType
    {
        WaveStarted,
        BasicAttack,
        SkillCast,
        Damaged,
        Healed,
        Fell,
        WaveCleared,
        BattleEnded,
        Enraged,
    }

    // Amount는 종류에 따라 피해량, 회복량, 웨이브 번호, 승리 여부(1/0)를 담는다.
    public readonly struct BattleEvent
    {
        public const int NoUnit = -1;

        public BattleEventType Type { get; }
        public int SourceUnitId { get; }
        public int TargetUnitId { get; }
        public int Amount { get; }

        public BattleEvent(BattleEventType type, int sourceUnitId, int targetUnitId, int amount)
        {
            Type = type;
            SourceUnitId = sourceUnitId;
            TargetUnitId = targetUnitId;
            Amount = amount;
        }
    }
}
