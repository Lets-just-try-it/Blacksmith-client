namespace Blacksmith.Core.Battle
{
    // 목걸이 효과 종류다. 목걸이에 처음 넣은 보석이 정한다 (docs/GAME_CONCEPT.md 4.4 "장신구 효과").
    // 아이템 데이터에 번호로 저장되므로 순서를 바꾸지 않는다.
    public enum AccessoryEffectKind
    {
        None,
        // 사파이어. 입은 모험가 본인의 경험치 증가
        ExperienceBonus,
        // 토파즈. 파티 합산 골드 증가
        GoldBonus,
        // 에메랄드. 파티 합산 광석·부재료 획득 증가 (보석은 늘리지 않는다)
        MaterialBonus,
        // 루비. 입은 모험가 본인의 사망 판정 1회 면제. 막으면 목걸이가 부서진다
        DeathImmunity,
    }

    // 입은 목걸이 하나의 효과다. Percent는 증가율 또는 면제 확률(퍼센트)이다.
    public readonly struct AccessoryEffect
    {
        public AccessoryEffectKind Kind { get; }
        public int Percent { get; }

        public AccessoryEffect(AccessoryEffectKind kind, int percent)
        {
            Kind = kind;
            Percent = percent;
        }

        public int PercentOf(AccessoryEffectKind kind)
        {
            return Kind == kind ? Percent : 0;
        }
    }
}
