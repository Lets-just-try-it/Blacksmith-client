using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Battle;
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
        // 쓰면 사라지는 물건(예: 포션). 재료처럼 한 칸에 쌓는다.
        Consumable,
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
        [Tooltip("투구·무기일 때만 쓴다. 입었을 때 켤 인간형 모델 파츠(Hero.fbx 안의 오브젝트 이름). 투구는 외형 목록(HumanoidLook)의 모자에도 있어야 한다. 비우면 모습에 나오지 않는다")]
        [SerializeField] private string _wornPart = "";
        [Tooltip("장신구(목걸이)일 때만 쓴다. 처음 넣은 보석이 정하는 효과 종류. 수치는 전투의 AccessoryEffects 데이터에 있다 (docs/GAME_CONCEPT.md 4.4)")]
        [SerializeField] private AccessoryEffectKind _accessoryEffect;
        [Tooltip("약초·포션처럼 모험가에게 건네면 줄어드는 부상수치. 0이면 치료 아이템이 아니다 (docs/GAME_CONCEPT.md 5.1)")]
        [SerializeField, Min(0)] private int _injuryRecovery;
        [Tooltip("무기·투구·갑옷일 때만 쓴다. 최대 내구도. 0이면 닳지 않는다(기본무기). 등급별 소모량은 EquipmentStats 데이터에 있다 (docs/GAME_CONCEPT.md 4.6)")]
        [SerializeField, Min(0)] private int _maxDurability;

        public int ItemId => _itemId;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public Color Color => _color;
        public ItemKind Kind => _kind;
        public int Tier => _tier;
        public EquipmentSlot EquipmentSlot => _kind == ItemKind.Equipment ? _equipmentSlot : EquipmentSlot.None;
        public WeaponType WeaponType => EquipmentSlot == EquipmentSlot.Weapon ? _weaponType : WeaponType.None;
        public AdventurerStats StatBonus => _kind == ItemKind.Equipment ? _statBonus : default;
        public string WornPart => _kind == ItemKind.Equipment ? _wornPart : "";
        public AccessoryEffectKind AccessoryEffect => EquipmentSlot == EquipmentSlot.Accessory ? _accessoryEffect : AccessoryEffectKind.None;
        public int InjuryRecovery => _injuryRecovery;
        public int MaxDurability => EquipmentSlot == EquipmentSlot.None || EquipmentSlot == EquipmentSlot.Accessory ? 0 : _maxDurability;
        // 재료와 사용 아이템만 한 칸에 쌓는다. 장비와 중간 형태는 한 칸에 하나다.
        public bool IsStackable => _kind == ItemKind.Material || _kind == ItemKind.Consumable;
    }
}
