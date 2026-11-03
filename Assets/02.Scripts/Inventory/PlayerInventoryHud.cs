using UnityEngine;

namespace Blacksmith.Inventory
{
    // 화면 하단에 항상 떠 있는 내 슬롯 8칸이다. 내 플레이어가 스폰되면 그 슬롯을 보여 주고, 디스폰되면 숨긴다.
    public class PlayerInventoryHud : MonoBehaviour
    {
        [SerializeField] private InventoryChannel _channel;
        [SerializeField] private InventoryView _view;

        private void OnEnable()
        {
            _channel.LocalPlayerInventoryChanged += ShowLocalInventory;
            ShowLocalInventory();
        }

        private void OnDisable()
        {
            _channel.LocalPlayerInventoryChanged -= ShowLocalInventory;
        }

        private void ShowLocalInventory()
        {
            if (_channel.LocalPlayerInventory != null)
            {
                _view.Show(_channel.LocalPlayerInventory);
            }
            else
            {
                _view.Hide();
            }
        }
    }
}
