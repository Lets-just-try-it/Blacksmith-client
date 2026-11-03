using UnityEngine;

namespace Blacksmith.Inventory
{
    public enum ItemKind
    {
        Material,
        // 제작 단계 사이의 형태(예: 주괴, 단조품)
        Intermediate,
        Equipment,
    }

    public enum EquipmentSlot
    {
        None,
        Weapon,
        Helmet,
        Armor,
    }

    // 아이템 한 종류의 정의다. 인벤토리 칸에는 이 정의의 번호(ItemId)와 등급, 개수만 담는다.
    [CreateAssetMenu(fileName = "Item", menuName = "Blacksmith/Items/Item")]
    public class ItemData : ScriptableObject
    {
        [Tooltip("ItemCatalog 안에서 겹치지 않는 1 이상의 번호. 저장 데이터가 이 번호를 쓰므로 정한 뒤에는 바꾸지 않는다")]
        [SerializeField, Min(1)] private int _itemId = 1;
        [SerializeField] private string _displayName = "";
        [SerializeField] private Sprite _icon;
        [Tooltip("아이콘이 없을 때 칸에 칠하는 임시 색")]
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private ItemKind _kind;
        [Tooltip("장비일 때만 쓴다")]
        [SerializeField] private EquipmentSlot _equipmentSlot;

        public int ItemId => _itemId;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public Color Color => _color;
        public ItemKind Kind => _kind;
        public EquipmentSlot EquipmentSlot => _kind == ItemKind.Equipment ? _equipmentSlot : EquipmentSlot.None;
        // 재료만 한 칸에 쌓는다. 장비와 중간 형태는 한 칸에 하나다.
        public bool IsStackable => _kind == ItemKind.Material;
    }
}
