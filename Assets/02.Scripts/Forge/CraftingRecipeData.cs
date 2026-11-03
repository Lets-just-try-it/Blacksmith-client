using System;
using Blacksmith.Core.Forge;
using Blacksmith.Inventory;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 장비 하나를 만드는 단계 순서다. 단계마다 어느 작업대에서 어떤 물건을 받아 어떤 물건으로 바꾸는지 적는다.
    // 마지막 단계의 결과물이 장비이고, 앞 단계의 결과물은 쌓이지 않는 중간 형태다.
    [CreateAssetMenu(fileName = "Recipe", menuName = "Blacksmith/Equipment/Crafting Recipe")]
    public class CraftingRecipeData : ScriptableObject
    {
        [Serializable]
        public struct Step
        {
            public CraftingStationKind Station;
            public ItemData Input;
            public ItemData Output;
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

        // 이 작업대가 받는 재료의 이름. 안내 문구에 쓴다. 이 작업대를 쓰는 단계가 없으면 null이다.
        public string GetInputName(CraftingStationKind station)
        {
            foreach (Step step in _steps)
            {
                if (step.Station == station && step.Input != null)
                {
                    return step.Input.DisplayName;
                }
            }

            return null;
        }

        // 이 작업대가 이 물건을 받는 단계가 있으면 그 번호를 돌려준다.
        public bool TryFindStep(CraftingStationKind station, int inputItemId, out int stepIndex)
        {
            for (int i = 0; i < _steps.Length; i++)
            {
                Step step = _steps[i];
                if (step.Station == station && step.Input != null && step.Input.ItemId == inputItemId)
                {
                    stepIndex = i;
                    return true;
                }
            }

            stepIndex = -1;
            return false;
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
