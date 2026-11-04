using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Inventory;
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
        [Tooltip("이 물건이 속한 재료 티어(1부터, 높을수록 귀함). 재료와 그 재료로 만드는 중간 형태·장비는 같은 티어다")]
        [SerializeField, Min(1)] private int _tier = 1;
        [Tooltip("장비일 때만 쓴다")]
        [SerializeField] private EquipmentSlot _equipmentSlot;
        [Tooltip("무기일 때만 쓴다. 직업마다 쓸 수 있는 무기 종류가 정해져 있다")]
        [SerializeField] private WeaponType _weaponType;
        [Tooltip("장비일 때만 쓴다. 입은 모험가의 체력·공격력·방어력에 더하는 기본 보정. 등급 배수는 EquipmentStats 데이터에 있다")]
        [SerializeField] private AdventurerStats _statBonus;

        public int ItemId => _itemId;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public Color Color => _color;
        public ItemKind Kind => _kind;
        public int Tier => _tier;
        public EquipmentSlot EquipmentSlot => _kind == ItemKind.Equipment ? _equipmentSlot : EquipmentSlot.None;
        public WeaponType WeaponType => EquipmentSlot == EquipmentSlot.Weapon ? _weaponType : WeaponType.None;
        public AdventurerStats StatBonus => _kind == ItemKind.Equipment ? _statBonus : default;
        // 재료만 한 칸에 쌓는다. 장비와 중간 형태는 한 칸에 하나다.
        public bool IsStackable => _kind == ItemKind.Material;
    }
}
