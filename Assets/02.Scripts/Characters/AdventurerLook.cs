using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Inventory;
using Blacksmith.Inventory;
using UnityEngine;

namespace Blacksmith.Characters
{
    // 모험가의 외형과 입은 장비를 인간형 모델에 보여 준다. 영입 후보, 장비 착용 창의 미리보기, 전투가 같은 함수를 써서 모습이 같다.
    // 투구와 무기는 아이템의 모델 파츠(WornPart)를 켠다. 갑옷·장신구는 모델이 없어 아이템 색을 칠한 상자를 켠다.
    public class AdventurerLook : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private HumanoidAppearanceView _view;
        [Tooltip("갑옷 모델이 생기기 전까지 쓰는 상자. 비우면 갑옷을 보여 주지 않는다")]
        [SerializeField] private Renderer _armorBox;
        [Tooltip("장신구 모델이 생기기 전까지 쓰는 상자. 비우면 장신구를 보여 주지 않는다")]
        [SerializeField] private Renderer _accessoryBox;

        private MaterialPropertyBlock _properties;

        // adventurer가 null이면 아무것도 바꾸지 않고 상자만 숨긴다.
        public void Show(AdventurerData adventurer, ItemCatalog catalog)
        {
            SetBox(_armorBox, adventurer, catalog, EquipmentSlot.Armor);
            SetBox(_accessoryBox, adventurer, catalog, EquipmentSlot.Accessory);
            if (adventurer == null)
            {
                return;
            }

            _view.Apply(adventurer.Appearance,
                FindEquipped(adventurer, catalog, EquipmentSlot.Helmet)?.WornPart,
                FindEquipped(adventurer, catalog, EquipmentSlot.Weapon)?.WornPart);
        }

        private static ItemData FindEquipped(AdventurerData adventurer, ItemCatalog catalog, EquipmentSlot slot)
        {
            ItemStack equipped = EquipmentRules.GetEquipped(adventurer, slot);
            return equipped.IsEmpty ? null : catalog.Find(equipped.ItemId);
        }

        private void SetBox(Renderer box, AdventurerData adventurer, ItemCatalog catalog, EquipmentSlot slot)
        {
            if (box == null)
            {
                return;
            }

            ItemData item = adventurer != null ? FindEquipped(adventurer, catalog, slot) : null;
            box.gameObject.SetActive(item != null);
            if (item == null)
            {
                return;
            }

            _properties ??= new MaterialPropertyBlock();
            box.GetPropertyBlock(_properties);
            _properties.SetColor(BaseColorId, item.Color);
            box.SetPropertyBlock(_properties);
        }
    }
}
