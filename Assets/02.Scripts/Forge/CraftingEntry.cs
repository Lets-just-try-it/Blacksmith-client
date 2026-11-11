using System.Collections.Generic;
using System.Text;
using Blacksmith.Core.Forge;
using Blacksmith.Inventory;

namespace Blacksmith.Forge
{
    // 제작 목록의 한 줄: 어떤 레시피의 몇 번째 단계를 이 작업대에서 하는가.
    // 작업대가 같은 데이터에서 같은 순서로 만들기 때문에, 번호만 주고받아도 호스트와 클라이언트가 같은 항목을 가리킨다.
    public sealed class CraftingEntry
    {
        public CraftingEntry(CraftingRecipeData recipe, int stepIndex)
        {
            Recipe = recipe;
            StepIndex = stepIndex;
        }

        public CraftingRecipeData Recipe { get; }
        public int StepIndex { get; }

        public CraftingRecipeData.Step Step => Recipe.GetStep(StepIndex);
        public ItemData Output => Step.Output;
        public bool IsFinal => Recipe.IsFinalStep(StepIndex);
        public bool IsSmelting => Step.Station == CraftingStationKind.Smelting;

        // 이 단계 전에 점수를 낸 단계 수. 정제는 주괴 수만 정하고 점수에 들어가지 않는다.
        public int ScoredStepsDone
        {
            get
            {
                int count = 0;
                for (int i = 0; i < StepIndex; i++)
                {
                    if (Recipe.GetStep(i).Station != CraftingStationKind.Smelting)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        // 미니게임이 모양을 고르는 데 쓰는 번호(판 성형의 투구/갑옷 등)
        public int Variant => Step.Variant;

        // 재료 티어에서 정해지는 미니게임 난이도의 바탕
        public int Tier => Output.Tier;

        public void GetIngredients(List<CraftingIngredient> results)
        {
            CraftingRecipeData.GetIngredients(Step, results);
        }

        // 같은 재료로 같은 결과를 내는 단계는 레시피가 달라도 한 줄로 합치려고 쓴다.
        public string Signature()
        {
            CraftingRecipeData.Step step = Step;
            var builder = new StringBuilder();
            builder.Append(StepIndex).Append(':').Append(step.Output.ItemId).Append(':').Append(step.Variant);
            var ingredients = new List<CraftingIngredient>();
            CraftingRecipeData.GetIngredients(step, ingredients);
            foreach (CraftingIngredient ingredient in ingredients)
            {
                builder.Append('|').Append(ingredient.ItemId).Append('x').Append(ingredient.Count);
            }

            return builder.ToString();
        }
    }
}
