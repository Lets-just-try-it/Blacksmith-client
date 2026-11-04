using Blacksmith.Core.Inventory;
using Fusion;
using UnityEngine;

namespace Blacksmith.Inventory
{
    // Player 프리팹에 붙는다. 손에 든 슬롯의 물건을 캐릭터 손 위치에 보여 주고, 눈에 띄도록 강조 껍데기를 깜박인다.
    // 내 캐릭터는 지연 없이 InventoryChannel.HeldSlot을, 다른 참가자의 캐릭터는 호스트가 복제한 PlayerInventoryState.HeldSlot을 쓴다.
    // 아이콘이 있으면 카메라를 향한 그림으로, 없으면 아이템 색을 칠한 상자로 보여 준다.
    public class HeldItemView : NetworkBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private InventoryChannel _channel;
        [SerializeField] private PlayerInventoryState _inventory;
        [Tooltip("캐릭터 앞에 둔 자식. 캐릭터가 도는 대로 따라 돌고, 손에 든 것이 없으면 끈다")]
        [SerializeField] private GameObject _root;
        [SerializeField] private SpriteRenderer _icon;
        [Tooltip("아이콘이 없을 때 아이템 색으로 칠하는 상자")]
        [SerializeField] private Renderer _block;
        [Tooltip("물건을 감싸는 강조 껍데기. 반투명 머티리얼을 쓴다")]
        [SerializeField] private Renderer _highlight;
        [SerializeField] private Color _highlightColor = new Color(1f, 0.85f, 0.3f, 0.6f);
        [SerializeField, Min(0f)] private float _pulseSpeed = 3f;
        [Tooltip("깜박일 때 가장 흐린 투명도 비율")]
        [SerializeField, Range(0f, 1f)] private float _pulseMin = 0.3f;

        private MaterialPropertyBlock _properties;
        private ItemStack _shown;

        public override void Spawned()
        {
            _properties = new MaterialPropertyBlock();
            _shown = ItemStack.Empty;
            _root.SetActive(false);
        }

        public override void Render()
        {
            ItemStack held = GetHeldItem();
            if (!held.Equals(_shown))
            {
                Show(held);
            }

            if (_root.activeSelf)
            {
                Pulse();
                FaceCamera();
            }
        }

        private ItemStack GetHeldItem()
        {
            int slot = HasInputAuthority ? _channel.HeldSlot : _inventory.HeldSlot;
            return slot >= 0 ? _inventory.Get(slot) : ItemStack.Empty;
        }

        private void Show(ItemStack stack)
        {
            _shown = stack;
            ItemData item = stack.IsEmpty ? null : _inventory.Catalog.Find(stack.ItemId);
            _root.SetActive(item != null);
            if (item == null)
            {
                return;
            }

            bool hasIcon = item.Icon != null;
            _icon.gameObject.SetActive(hasIcon);
            _block.gameObject.SetActive(!hasIcon);
            if (hasIcon)
            {
                _icon.sprite = item.Icon;
            }
            else
            {
                SetColor(_block, item.Color);
            }
        }

        // 표시 전용이라 Time을 쓴다.
        private void Pulse()
        {
            float wave = (Mathf.Sin(Time.time * _pulseSpeed) + 1f) * 0.5f;
            Color color = _highlightColor;
            color.a *= Mathf.Lerp(_pulseMin, 1f, wave);
            SetColor(_highlight, color);
        }

        // 위치는 캐릭터를 따르고, 방향만 카메라를 향하게 해 아이콘이 옆으로 누워 보이지 않게 한다.
        private void FaceCamera()
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                _root.transform.rotation = camera.transform.rotation;
            }
        }

        private void SetColor(Renderer target, Color color)
        {
            target.GetPropertyBlock(_properties);
            _properties.SetColor(BaseColorId, color);
            target.SetPropertyBlock(_properties);
        }
    }
}
