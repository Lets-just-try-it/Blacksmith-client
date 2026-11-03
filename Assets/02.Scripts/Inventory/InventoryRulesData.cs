using Blacksmith.Core.Inventory;
using UnityEngine;

namespace Blacksmith.Inventory
{
    // 인벤토리 하나의 크기와 쌓기 수치다. 공용 창고와 플레이어 슬롯이 각자 에셋을 하나씩 쓴다.
    [CreateAssetMenu(fileName = "InventoryRules", menuName = "Blacksmith/Balance/Inventory Rules")]
    public class InventoryRulesData : ScriptableObject
    {
        [SerializeField, Min(1)] private int _columns = 5;
        [SerializeField, Min(1)] private int _initialRows = 10;
        [Tooltip("넣을 빈 칸이 없을 때 한 번에 늘리는 줄 수. 0이면 늘어나지 않는다")]
        [SerializeField, Min(0)] private int _growRows = 5;
        [Tooltip("늘어날 수 있는 최대 줄 수. 네트워크 상태의 최대 칸 수를 넘으면 그 안으로 줄인다")]
        [SerializeField, Min(1)] private int _maxRows = 40;
        [Tooltip("재료를 한 칸에 쌓을 수 있는 최대 개수")]
        [SerializeField, Min(1)] private int _materialMaxStack = 99;

        public int MaterialMaxStack => _materialMaxStack;

        public InventoryRules ToRules(int maxSlotCount)
        {
            int maxRows = Mathf.Min(Mathf.Max(_maxRows, _initialRows), maxSlotCount / _columns);
            return new InventoryRules
            {
                Columns = _columns,
                InitialRows = Mathf.Min(_initialRows, maxRows),
                GrowRows = _growRows,
                MaxRows = maxRows,
            };
        }

        private void OnValidate()
        {
            _maxRows = Mathf.Max(_maxRows, _initialRows);
        }
    }
}
