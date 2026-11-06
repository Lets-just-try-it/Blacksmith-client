using UnityEngine.InputSystem;

namespace Blacksmith.Forge
{
    // 미니게임 안에서만 쓰는 키: 좌우 방향(←/→, A/D), 숫자 1·2, 마무리(F), 오른쪽 마우스 버튼.
    // 입력 에셋에 묶지 않고 미니게임이 입력을 읽을 때만 본다(MinigameActionKey와 같은 방식).
    // 걷는 동안 이 키들은 이동과 손에 들 슬롯 선택이지만 미니게임 중에는 입력이 잠겨 있어 겹치지 않는다.
    public static class MinigameKeys
    {
        private static InputAction _left;
        private static InputAction _right;
        private static InputAction _one;
        private static InputAction _two;
        private static InputAction _finish;
        private static InputAction _secondary;

        // 이번 프레임에 누른 좌우 방향. -1 왼쪽, 1 오른쪽, 0 없음.
        public static int ArrowPressed()
        {
            EnsureActions();
            if (_left.WasPressedThisFrame())
            {
                return -1;
            }

            return _right.WasPressedThisFrame() ? 1 : 0;
        }

        // 이번 프레임에 누른 숫자 키(1 또는 2). 없으면 0.
        public static int ToolPressed()
        {
            EnsureActions();
            if (_one.WasPressedThisFrame())
            {
                return 1;
            }

            return _two.WasPressedThisFrame() ? 2 : 0;
        }

        public static bool FinishPressed()
        {
            EnsureActions();
            return _finish.WasPressedThisFrame();
        }

        public static bool SecondaryPressed()
        {
            EnsureActions();
            return _secondary.WasPressedThisFrame();
        }

        private static void EnsureActions()
        {
            if (_left == null)
            {
                _left = Create("MinigameLeft", "<Keyboard>/leftArrow", "<Keyboard>/a");
                _right = Create("MinigameRight", "<Keyboard>/rightArrow", "<Keyboard>/d");
                _one = Create("MinigameOne", "<Keyboard>/1");
                _two = Create("MinigameTwo", "<Keyboard>/2");
                _finish = Create("MinigameFinish", "<Keyboard>/f");
                _secondary = Create("MinigameSecondary", "<Mouse>/rightButton");
            }

            Enable(_left);
            Enable(_right);
            Enable(_one);
            Enable(_two);
            Enable(_finish);
            Enable(_secondary);
        }

        private static InputAction Create(string name, params string[] bindings)
        {
            var action = new InputAction(name, InputActionType.Button);
            foreach (string binding in bindings)
            {
                action.AddBinding(binding);
            }

            return action;
        }

        private static void Enable(InputAction action)
        {
            if (!action.enabled)
            {
                action.Enable();
            }
        }
    }
}
