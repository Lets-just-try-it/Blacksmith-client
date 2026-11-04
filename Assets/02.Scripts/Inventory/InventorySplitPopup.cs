using Blacksmith.Core.Inventory;
using Blacksmith.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Inventory
{
    // 스택 나누기 개수 입력 창이다. InventoryChannel이 나누기를 요청하면 열려, 입력칸이나 슬라이더로 옮길 개수를 고른다.
    // 기본값은 쌓인 개수의 절반이다. 숫자를 입력하는 동안 캐릭터가 움직이거나 숫자키가 손에 든 슬롯을 바꾸지 않도록 입력을 잠근다.
    public class InventorySplitPopup : MonoBehaviour
    {
        [SerializeField] private InventoryChannel _channel;
        [SerializeField] private PlayerNetworkInputLock _inputLock;
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_InputField _countInput;
        [SerializeField] private Slider _countSlider;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        private int _maxCount;

        private void Awake()
        {
            _panel.SetActive(false);
            _countInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            _countSlider.wholeNumbers = true;
        }

        private void OnEnable()
        {
            _channel.SplitRequested += Open;
            _channel.SplitClosed += Close;
            _countInput.onEndEdit.AddListener(OnInputEdited);
            _countInput.onSubmit.AddListener(OnSubmit);
            _countSlider.onValueChanged.AddListener(OnSliderChanged);
            _confirmButton.onClick.AddListener(Confirm);
            _cancelButton.onClick.AddListener(_channel.CloseSplit);
        }

        private void OnDisable()
        {
            _channel.SplitRequested -= Open;
            _channel.SplitClosed -= Close;
            _countInput.onEndEdit.RemoveListener(OnInputEdited);
            _countInput.onSubmit.RemoveListener(OnSubmit);
            _countSlider.onValueChanged.RemoveListener(OnSliderChanged);
            _confirmButton.onClick.RemoveListener(Confirm);
            _cancelButton.onClick.RemoveListener(_channel.CloseSplit);
            _channel.CloseSplit();
            Close();
        }

        private void Open()
        {
            ItemStack stack = _channel.SplitSource.Get(_channel.SplitSlot);
            _maxCount = stack.Count;
            _countSlider.minValue = 1;
            _countSlider.maxValue = _maxCount;
            SetCount(Mathf.Max(1, _maxCount / 2));
            _panel.SetActive(true);
            _inputLock.Lock(this);
            _countInput.Select();
            _countInput.ActivateInputField();
        }

        private void Close()
        {
            // 씬을 내릴 때는 창이 먼저 파괴되었을 수 있다.
            if (_panel != null)
            {
                _panel.SetActive(false);
            }

            _inputLock.Release(this);
        }

        private void Confirm()
        {
            OnInputEdited(_countInput.text);
            _channel.ConfirmSplit((int)_countSlider.value);
        }

        private void OnSubmit(string text)
        {
            Confirm();
        }

        private void OnInputEdited(string text)
        {
            SetCount(int.TryParse(text, out int count) ? count : (int)_countSlider.value);
        }

        private void OnSliderChanged(float value)
        {
            _countInput.SetTextWithoutNotify(((int)value).ToString());
        }

        private void SetCount(int count)
        {
            int clamped = Mathf.Clamp(count, 1, _maxCount);
            _countSlider.SetValueWithoutNotify(clamped);
            _countInput.SetTextWithoutNotify(clamped.ToString());
        }
    }
}
