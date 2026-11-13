using Blacksmith.Core;
using Blacksmith.Core.Inventory;
using Blacksmith.Forge;

namespace Blacksmith.Inventory
{
    // 칸에 보이는 아이템 이름과 등급·내구도 문구다. 인벤토리 창과 장비 착용 창이 같은 문구를 쓴다.
    public static class ItemLabels
    {
        // grades: 등급 이름을 찾는 데 쓴다. 비워 두면 등급을 표시하지 않는다.
        public static string Get(ItemStack stack, ItemData item, GradeThresholdData grades)
        {
            if (stack.IsEmpty)
            {
                return "";
            }

            string name = item != null ? item.DisplayName : $"#{stack.ItemId}";
            // 등급이 없는 물건(예: 재료)은 등급을 0으로 둔다.
            bool hasGrade = grades != null && stack.Grade >= GradeRule.MinGrade && stack.Grade < grades.GradeCount + GradeRule.MinGrade;
            string grade = hasGrade ? grades.GetGradeName(stack.Grade) : "";
            string durability = GetDurability(stack, item);
            string detail = grade.Length > 0 && durability.Length > 0 ? $"{grade} · {durability}" : grade + durability;
            return detail.Length > 0 ? $"{name}\n{detail}" : name;
        }

        // 닳기 시작한 장비만 남은 내구도를 적는다. 예: "내구 75%"
        private static string GetDurability(ItemStack stack, ItemData item)
        {
            if (item == null || item.MaxDurability <= 0 || stack.Wear <= 0)
            {
                return "";
            }

            return $"내구 {DurabilityRules.GetRemainingPercent(stack.Wear, item.MaxDurability)}%";
        }
    }
}
