using System;

namespace Blacksmith.Core.Adventurers
{
    // 부상 밸런스 수치다. System.Serializable이라 ScriptableObject 필드로도 쓸 수 있다. (docs/GAME_CONCEPT.md 6.5)
    [Serializable]
    public struct InjuryBalance
    {
        public int MaxInjury;
        // 부상수치가 이 값 이상이면 파견할 수 없다.
        public int DispatchLimit;
        // 전투가 끝났을 때 부상수치가 이 값 이상이면 사망 판정을 한다.
        public int DeathCheckInjury;
        public int BaseDeathChancePercent;
        // DeathCheckInjury를 1 넘을 때마다 사망 확률에 더하는 값(%p)
        public int DeathChancePerInjury;
        // 남은 체력이 이 비율(%) 이상이면 부상을 입지 않는다.
        public int UninjuredHpPercent;
        // 쓰러지지 않았을 때 전투 한 번에 입는 최대 부상
        public int MaxSurvivedInjury;
        // 쓰러졌을 때 전투 한 번에 입는 부상
        public int FallenInjury;
        // 파견되지 않고 하루를 보내면 줄어드는 부상
        public int DailyRecovery;
    }

    // 부상수치의 증가, 회복, 파견 제한, 사망 확률을 정한다. 파견 창 표시와 호스트 판정, 전투 재생이 같은 함수를 쓴다.
    public static class InjuryRules
    {
        public static AdventurerCondition GetCondition(int injury)
        {
            return injury > 0 ? AdventurerCondition.Injured : AdventurerCondition.Healthy;
        }

        public static bool CanDispatch(int injury, InjuryBalance balance)
        {
            return injury < balance.DispatchLimit;
        }

        // 쓰러지면 FallenInjury, 아니면 남은 체력 비율이 UninjuredHpPercent에서 0%로 내려갈수록 0에서 MaxSurvivedInjury로 늘어난다(반올림).
        public static int GetBattleInjury(int remainingHp, int maxHp, bool isFallen, InjuryBalance balance)
        {
            if (isFallen || remainingHp <= 0)
            {
                return balance.FallenInjury;
            }

            long threshold = (long)maxHp * balance.UninjuredHpPercent;
            long remaining = (long)remainingHp * 100;
            if (maxHp <= 0 || remaining >= threshold)
            {
                return 0;
            }

            return (int)(((long)balance.MaxSurvivedInjury * (threshold - remaining) + threshold / 2) / threshold);
        }

        public static int AddInjury(int injury, int amount, InjuryBalance balance)
        {
            return Math.Clamp(injury + amount, 0, balance.MaxInjury);
        }

        // 약초·포션은 부상당한 모험가에게만 쓸 수 있다. 쓰면 Recover로 줄인다.
        public static bool CanTreat(int injury, int recovery)
        {
            return injury > 0 && recovery > 0;
        }

        // 하루 휴식, 약초, 포션이 모두 이 함수로 줄인다.
        public static int Recover(int injury, int amount)
        {
            return Math.Max(0, injury - amount);
        }

        public static int GetDeathChancePercent(int injury, InjuryBalance balance)
        {
            if (injury < balance.DeathCheckInjury)
            {
                return 0;
            }

            return Math.Clamp(balance.BaseDeathChancePercent + (injury - balance.DeathCheckInjury) * balance.DeathChancePerInjury, 0, 100);
        }
    }
}
