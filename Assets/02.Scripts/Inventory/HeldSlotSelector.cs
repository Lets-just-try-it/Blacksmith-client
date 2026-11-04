using UnityEngine;
using UnityEngine.InputSystem;

namespace Blacksmith.Inventory
{
    // 숫자키 1~8로 손에 들 개인 슬롯을 고른다. 빈 슬롯을 고르면 아무것도 들지 않은 상태가 된다.
    // 고른 슬롯은 내 PC의 InventoryChannel에만 두고, 제작을 요청할 때 슬롯 번호를 실어 호스트에 보낸다.
    public class HeldSlotSelector : MonoBehaviour
    {
        [SerializeField] private InventoryChannel _channel;

        private InputAction[] _keys;

        private void Awake()
        {
            _keys = new InputAction[PlayerInventoryState.MaxSlotCount];
            for (int i = 0; i < _keys.Length; i++)
            {
                // 입력 에셋 대신 코드로 정의한다. 숫자 윗줄 키와 숫자 패드를 모두 받는다.
                var action = new InputAction($"HeldSlot{i + 1}", InputActionType.Button);
                action.AddBinding($"<Keyboard>/{i + 1}");
                action.AddBinding($"<Keyboard>/numpad{i + 1}");
                _keys[i] = action;
            }
        }

        private void OnEnable()
        {
            foreach (InputAction key in _keys)
            {
                key.Enable();
            }
        }

        private void OnDisable()
        {
            foreach (InputAction key in _keys)
            {
                key.Disable();
            }
        }

        private void OnDestroy()
        {
            foreach (InputAction key in _keys)
            {
                key.Dispose();
            }
        }

        private void Update()
        {
            if (_channel.LocalPlayerInventory == null)
            {
                return;
            }

            for (int i = 0; i < _keys.Length; i++)
            {
                if (_keys[i].WasPressedThisFrame())
                {
                    _channel.SetHeldSlot(i);
                }
            }
        }
    }
}
