using System.Collections.Generic;
using Blacksmith.Core.Inventory;

namespace Blacksmith.Core.Forge
{
    // 제작 단계를 맡는 작업대의 종류다. 장비 종류마다 쓰는 작업대와 순서는 레시피 데이터가 정한다.
    public enum CraftingStationKind
    {
        Smelting,
        Forging,
        Quench,
        Polish,
        // 방어구: 판 성형, 조립
        PlateForming,
        Assembly,
        // 장신구: 체인 잇기, 보석 깎기, 장착, 광내기를 한 작업대에서 한다.
        Jewelry,
        // 활: 활 몸체를 휘고 시위를 건다. 지팡이: 마석을 지팡이 머리에 박는다. 정제한 주괴로 바로 장비를 만든다.
        BowShooting,
        StaffSetting,
        // 활 제작의 앞 두 단계: 참나무 활 몸체 깎기, 보강쇠 달기
        BowCarving,
        BowFitting,
        // 활 제작의 첫 단계: 달군 쇠 몸체를 형틀에 맞춰 휘기
        BowBending,
        // 활 제작의 셋째 단계: 힘줄 시위를 감고 활에 걸기
        BowStringing,
    }

    // 미니게임 하나가 끝났을 때 재료가 다음 형태로 바뀌는 규칙이다. 네트워크와 Unity에 의존하지 않는다.
    // 중간 형태(주괴, 단조품 등)는 ItemStack의 Accuracy 칸에 지금까지의 정확도 평균을 0~100 퍼센트로 담는다.
    // 마지막 단계에서 그 평균을 등급(1~5)으로 바꿔 장비의 Grade로 쓰고, 완성 장비의 Accuracy는 0으로 둔다.
    public static class CraftingRules
    {
        public const int MaxPercent = 100;

        // stagesDone: 이번 단계 전에 점수를 낸 단계 수(정제는 세지 않는다). 0이면 재료에는 점수가 없으므로 이전 값을 무시한다.
        public static int AccumulatePercent(int previousPercent, int stagesDone, float accuracy)
        {
            float current = Clamp01(accuracy) * MaxPercent;
            if (stagesDone <= 0)
            {
                return Round(current);
            }

            int previous = previousPercent < 0 ? 0 : (previousPercent > MaxPercent ? MaxPercent : previousPercent);
            return Round((previous * stagesDone + current) / (stagesDone + 1));
        }

        // 한 단계를 끝낸 결과물이다. 마지막 단계면 평균 정확도를 등급으로 바꾼 장비이고, 아니면 평균 퍼센트를 담은 중간 형태다.
        public static ItemStack MakeOutput(int outputItemId, int stagesDone, bool isFinalStage, int previousPercent, float accuracy, IReadOnlyList<float> gradeLowerBounds)
        {
            int percent = AccumulatePercent(previousPercent, stagesDone, accuracy);
            if (isFinalStage)
            {
                return new ItemStack(outputItemId, GradeRule.FromAccuracy(percent / (float)MaxPercent, gradeLowerBounds), 1);
            }

            return new ItemStack(outputItemId, 0, 1, percent);
        }

        // 정제를 끝낸 결과물이다. 주괴는 등급과 정확도 없이 쌓이고, 정확도는 주괴 수만 정한다.
        public static ItemStack MakeIngots(int ingotItemId, int count)
        {
            return new ItemStack(ingotItemId, 0, count);
        }

        private static float Clamp01(float value)
        {
            return value < 0f ? 0f : (value > 1f ? 1f : value);
        }

        private static int Round(float value)
        {
            return (int)(value + 0.5f);
        }
    }
}
