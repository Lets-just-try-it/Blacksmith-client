using UnityEngine.InputSystem;

namespace Blacksmith.Forge
{
    // 미니게임 안에서 상호작용 키(E)로 하는 동작은 스페이스바로도 할 수 있다.
    // 스페이스바는 걷는 동안 점프 키라서 입력 에셋의 상호작용 키에 묶지 않고, 미니게임이 입력을 읽을 때만 함께 본다.
    public static class MinigameActionKey
    {
        private static InputAction _space;

        private static InputAction Space
        {
            get
            {
                if (_space == null)
                {
                    _space = new InputAction("MinigameSpace", InputActionType.Button, "<Keyboard>/space");
                }

                if (!_space.enabled)
                {
                    _space.Enable();
                }

                return _space;
            }
        }

        public static bool WasPressed(InputAction interact)
        {
            return interact.WasPressedThisFrame() || Space.WasPressedThisFrame();
        }

        public static bool IsHeld(InputAction interact)
        {
            return interact.IsPressed() || Space.IsPressed();
        }

        public static bool WasReleased(InputAction interact)
        {
            return interact.WasReleasedThisFrame() || Space.WasReleasedThisFrame();
        }
    }
}
