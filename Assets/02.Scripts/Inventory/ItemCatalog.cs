using System.Collections.Generic;
using UnityEngine;

namespace Blacksmith.Inventory
{
    // 아이템 번호로 정의를 찾는 표다. 인벤토리·제작·파견 창이 같은 표를 쓴다.
    [CreateAssetMenu(fileName = "ItemCatalog", menuName = "Blacksmith/Items/Item Catalog")]
    public class ItemCatalog : ScriptableObject
    {
        [SerializeField] private List<ItemData> _items = new List<ItemData>();

        private Dictionary<int, ItemData> _byId;

        public IReadOnlyList<ItemData> Items => _items;

        // 없는 번호면 null을 돌려준다.
        public ItemData Find(int itemId)
        {
            if (_byId == null)
            {
                BuildIndex();
            }

            return _byId.TryGetValue(itemId, out ItemData item) ? item : null;
        }

        private void BuildIndex()
        {
            _byId = new Dictionary<int, ItemData>();
            foreach (ItemData item in _items)
            {
                if (item != null)
                {
                    _byId[item.ItemId] = item;
                }
            }
        }

        private void OnValidate()
        {
            _byId = null;
            var seen = new HashSet<int>();
            foreach (ItemData item in _items)
            {
                if (item != null && !seen.Add(item.ItemId))
                {
                    Debug.LogWarning($"아이템 번호 {item.ItemId}가 겹친다: {item.name}", this);
                }
            }
        }
    }
}
