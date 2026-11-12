namespace Blacksmith.Core.Battle
{
    public enum BattleEventType
    {
        // 무리가 모험가를 발견해 다가오기 시작했다.
        GroupEngaged,
        BasicAttack,
        SkillCast,
        Damaged,
        Healed,
        Fell,
        GroupCleared,
        BattleEnded,
        Enraged,
    }

    // Amount는 종류에 따라 피해량, 회복량, 무리 번호, 승리 여부(1/0)를 담는다.
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
