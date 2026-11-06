using System;
using System.Collections.Generic;
using Blacksmith.Core.Forge;
using Blacksmith.Inventory;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 장비 하나를 만드는 단계 순서다. 단계마다 어느 작업대에서 어떤 재료를 얼마나 받아 어떤 물건으로 바꾸는지 적는다.
    // 마지막 단계의 결과물이 장비이고, 앞 단계의 결과물은 쌓이지 않는 중간 형태다.
    [CreateAssetMenu(fileName = "Recipe", menuName = "Blacksmith/Equipment/Crafting Recipe")]
    public class CraftingRecipeData : ScriptableObject
    {
        [Serializable]
        public struct Ingredient
        {
            public ItemData Item;
            [Min(1)] public int Count;
        }

        [Serializable]
        public struct Step
        {
            public CraftingStationKind Station;
            [Tooltip("이 단계가 받는 주 재료. 앞 단계의 결과물(중간 형태)이면 그 정확도 평균을 이어받는다")]
            public ItemData Input;
            [Tooltip("주 재료의 개수. 0은 1로 본다")]
            [Min(0)] public int InputCount;
            [Tooltip("주 재료 말고 더 필요한 재료(부재료 등)")]
            public Ingredient[] ExtraInputs;
            public ItemData Output;
            [Tooltip("미니게임이 모양을 고르는 데 쓰는 번호. 판 성형은 0이 투구, 1이 갑옷이다. 쓰지 않는 단계는 0")]
            [Min(0)] public int Variant;
        }

        [SerializeField] private string _displayName = "";
        [SerializeField] private Step[] _steps = Array.Empty<Step>();

        public string DisplayName => _displayName;
        public int StepCount => _steps.Length;

        public Step GetStep(int index)
        {
            return _steps[index];
        }

        public bool IsFinalStep(int index)
        {
            return index == _steps.Length - 1;
        }

        // 장비를 만드는 마지막 단계의 결과물. 단계가 없으면 null이다.
        public ItemData FinalItem => _steps.Length == 0 ? null : _steps[_steps.Length - 1].Output;

        // 한 단계가 받는 재료 전체(주 재료와 더 필요한 재료)를 results에 채운다.
        public static void GetIngredients(Step step, List<CraftingIngredient> results)
        {
            results.Clear();
            if (step.Input != null)
            {
                results.Add(new CraftingIngredient(step.Input.ItemId, Mathf.Max(1, step.InputCount)));
            }

            if (step.ExtraInputs == null)
            {
                return;
            }

            foreach (Ingredient extra in step.ExtraInputs)
            {
                if (extra.Item != null)
                {
                    results.Add(new CraftingIngredient(extra.Item.ItemId, extra.Count));
                }
            }
        }

        private void OnValidate()
        {
            for (int i = 0; i < _steps.Length; i++)
            {
                Step step = _steps[i];
                if (step.Input == null || step.Output == null)
                {
                    Debug.LogWarning($"단계 {i}: 받는 물건과 결과물을 모두 정해야 한다.", this);
                    continue;
                }

                bool isFinal = i == _steps.Length - 1;
                if (isFinal != (step.Output.Kind == ItemKind.Equipment))
                {
                    Debug.LogWarning($"단계 {i}: 마지막 단계의 결과물만 장비여야 한다.", this);
                }

                if (i > 0 && _steps[i - 1].Output != step.Input)
                {
                    Debug.LogWarning($"단계 {i}: 받는 물건이 앞 단계의 결과물과 달라 이어지지 않는다.", this);
                }
            }
        }
    }
}
