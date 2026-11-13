using System;
using System.Collections.Generic;

namespace Blacksmith.Core.Inventory
{
    // 입은 장비 하나의 내구도 상태다. 전투 설정이 전투 전 값을, 전투 결과가 전투 뒤 값을 담는다.
    // MaxDurability가 0이면 닳지 않는 장비다(기본무기, 장신구, 빈 칸).
    public readonly struct EquipmentDurability
    {
        public int Wear { get; }
        public int MaxDurability { get; }
        // 무기는 공격 한 번, 방어구는 공격받기 한 번에 줄어드는 내구도(0.01 단위). 등급이 정한다.
        public int WearPerUse { get; }

        public EquipmentDurability(int wear, int maxDurability, int wearPerUse)
        {
            Wear = wear;
            MaxDurability = maxDurability;
            WearPerUse = wearPerUse;
        }

        public bool HasDurability => MaxDurability > 0;
        public bool IsBroken => DurabilityRules.IsBroken(Wear, MaxDurability);

        public EquipmentDurability AddUses(int uses)
        {
            return HasDurability ? new EquipmentDurability(DurabilityRules.AddWear(Wear, uses, WearPerUse), MaxDurability, WearPerUse) : this;
        }
    }

    // 무기·방어구 내구도 규칙이다 (docs/GAME_CONCEPT.md 4.6). 장비는 쓴 내구도(Wear)를 저장하고, 새 장비는 0이다.
    // 최대 내구도는 정수(예: 100)지만 한 번에 1보다 적게 닳을 수 있어서, 쓴 내구도와 소모량은 0.01 단위 정수로 센다(Wear 144 = 내구도 1.44).
    // 전투 중에는 쓴 횟수만 세고, 전투가 끝난 뒤 한꺼번에 줄인다. 그래서 전투 중에는 내구도가 0이 되어도 부서지지 않는다.
    public static class DurabilityRules
    {
        public const int WearUnitsPerDurability = 100;

        // wearPerUseByGrade: 등급별 소모량(0.01 단위). 첫 칸이 최하 등급이다. 범위를 벗어난 등급은 가까운 끝 칸을 쓴다.
        // typePercent: 장비 종류별 배율(퍼센트). 자주 공격하는 무기(활·지팡이)는 한 번에 덜 닳게 낮춘다. 반올림하고, 닳는 장비는 최소 0.01씩 닳는다.
        public static int GetWearPerUse(int grade, IReadOnlyList<int> wearPerUseByGrade, int typePercent = 100)
        {
            if (wearPerUseByGrade == null || wearPerUseByGrade.Count == 0 || typePercent <= 0)
            {
                return 0;
            }

            int index = Math.Max(0, Math.Min(grade - GradeRule.MinGrade, wearPerUseByGrade.Count - 1));
            int baseWear = Math.Max(0, wearPerUseByGrade[index]);
            if (baseWear == 0)
            {
                return 0;
            }

            return Math.Max(1, (int)(((long)baseWear * typePercent + 50) / 100));
        }

        public static int AddWear(int wear, int uses, int wearPerUse)
        {
            if (uses <= 0 || wearPerUse <= 0)
            {
                return wear;
            }

            return (int)Math.Min(int.MaxValue, wear + (long)uses * wearPerUse);
        }

        // 전투가 끝난 뒤 이 값이 참이면 장비가 파괴된다.
        public static bool IsBroken(int wear, int maxDurability)
        {
            return maxDurability > 0 && wear >= GetMaxWear(maxDurability);
        }

        // 0.01 단위 정수로 바꾼 최대 내구도.
        public static long GetMaxWear(int maxDurability)
        {
            return (long)Math.Max(0, maxDurability) * WearUnitsPerDurability;
        }

        // 남은 내구도(0.01 단위).
        public static long GetRemaining(int wear, int maxDurability)
        {
            return maxDurability > 0 ? Math.Max(0, GetMaxWear(maxDurability) - wear) : 0;
        }

        // 남은 내구도 비율(0~100, 버림).
        public static int GetRemainingPercent(int wear, int maxDurability)
        {
            return maxDurability > 0 ? (int)(GetRemaining(wear, maxDurability) * 100 / GetMaxWear(maxDurability)) : 100;
        }

        // 장비를 녹여 돌려받는 주괴 수 = 들어간 주괴 수 × 남은 내구도 비율(버림). 닳지 않는 장비는 모두 돌려받는다.
        public static int GetMeltIngots(int ingotCount, int wear, int maxDurability)
        {
            if (ingotCount <= 0)
            {
                return 0;
            }

            return maxDurability > 0 ? (int)(ingotCount * GetRemaining(wear, maxDurability) / GetMaxWear(maxDurability)) : ingotCount;
        }
    }
}
