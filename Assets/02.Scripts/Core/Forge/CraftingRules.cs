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
    }

    // 미니게임 하나가 끝났을 때 재료가 다음 형태로 바뀌는 규칙이다. 네트워크와 Unity에 의존하지 않는다.
    // 중간 형태(주괴, 단조품 등)는 쌓이지 않으므로, ItemStack의 Grade 칸에 지금까지의 정확도 평균을 0~100 퍼센트로 담는다.
    // 마지막 단계에서 그 평균을 등급(1~5)으로 바꿔 장비의 Grade로 쓴다.
    public static class CraftingRules
    {
        public const int MaxPercent = 100;

        // stagesDone: 이번 단계 전에 끝낸 단계 수. 첫 단계(0)면 재료의 Grade는 점수가 아니므로 무시한다.
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
            int grade = isFinalStage ? GradeRule.FromAccuracy(percent / (float)MaxPercent, gradeLowerBounds) : percent;
            return new ItemStack(outputItemId, grade, 1);
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
