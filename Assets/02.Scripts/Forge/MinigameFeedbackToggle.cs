using UnityEngine;
using UnityEngine.InputSystem;

namespace Blacksmith.Forge
{
    // J 키로 미니게임 연출(MinigameFeedbackSwitch)을 껐다 켠다. 고른 값은 이 컴퓨터에 기억해 다음에 켤 때도 이어 간다.
    // 미니게임 중에는 캐릭터 조작이 잠기지만 이 키는 로컬 입력 액션으로 읽으므로 그대로 동작한다.
    public class MinigameFeedbackToggle : MonoBehaviour
    {
        private const string PrefKey = "Forge.MinigameFeedback";

        [SerializeField] private ForgeNotice _notice;

        private readonly InputAction _toggle = new InputAction("MinigameFeedbackToggle", InputActionType.Button, "<Keyboard>/j");

        private void OnEnable()
        {
            _toggle.Enable();
            MinigameFeedbackSwitch.SetEnabled(PlayerPrefs.GetInt(PrefKey, 1) != 0);
        }

        private void OnDisable()
        {
            _toggle.Disable();
        }

        private void Update()
        {
            if (_toggle.WasPressedThisFrame())
            {
                Toggle();
            }
        }

        private void Toggle()
        {
            bool enabled = !MinigameFeedbackSwitch.Enabled;
            MinigameFeedbackSwitch.SetEnabled(enabled);
            PlayerPrefs.SetInt(PrefKey, enabled ? 1 : 0);
            _notice.Show(enabled ? "미니게임 연출 켬 (J)" : "미니게임 연출 끔 (J)");
        }
    }
}
