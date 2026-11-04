using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Inventory;
using Blacksmith.Inventory;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 모험가 전투 모델에 입은 장비를 보여 준다. 장비 착용 창의 미리보기와 전투가 같은 모델과 같은 함수를 써서 모습이 같다.
    // 지금은 아이템 색을 칠한 상자를 머리·몸·손·목 위치에 켠다. 상자는 몸 아래에 두어 쓰러질 때 함께 눕는다.
    // 장비 모델이 생기면 상자를 바꾼다.
    public class AdventurerEquipmentLook : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer _helmet;
        [SerializeField] private Renderer _armor;
        [SerializeField] private Renderer _weapon;
        [SerializeField] private Renderer _accessory;

        private MaterialPropertyBlock _properties;

        // adventurer가 null이면 장비를 모두 숨긴다.
        public void Show(AdventurerData adventurer, ItemCatalog catalog)
        {
            SetEquipment(_helmet, adventurer, catalog, EquipmentSlot.Helmet);
            SetEquipment(_armor, adventurer, catalog, EquipmentSlot.Armor);
            SetEquipment(_weapon, adventurer, catalog, EquipmentSlot.Weapon);
            SetEquipment(_accessory, adventurer, catalog, EquipmentSlot.Accessory);
        }

        private void SetEquipment(Renderer target, AdventurerData adventurer, ItemCatalog catalog, EquipmentSlot slot)
        {
            ItemStack equipped = adventurer != null ? EquipmentRules.GetEquipped(adventurer, slot) : ItemStack.Empty;
            ItemData item = equipped.IsEmpty ? null : catalog.Find(equipped.ItemId);
            target.gameObject.SetActive(item != null);
            if (item == null)
            {
                return;
            }

            _properties ??= new MaterialPropertyBlock();
            target.GetPropertyBlock(_properties);
            _properties.SetColor(BaseColorId, item.Color);
            target.SetPropertyBlock(_properties);
        }
    }
}
