using Blacksmith.Network;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Blacksmith.Inventory
{
    // 공용 창고를 여는 상자다. 내 캐릭터가 범위 안에서 상호작용 키를 누르면 내 화면에만 창고 창을 연다.
    // 여러 명이 동시에 열 수 있으므로 점유는 두지 않는다. 연 동안에는 캐릭터 조작 입력을 잠근다.
    [RequireComponent(typeof(BoxCollider))]
    public class SharedInventoryBox : MonoBehaviour
    {
        [SerializeField] private SharedInventoryState _inventory;
        [SerializeField] private InventoryView _view;
        [Tooltip("연 동안 빠른 이동으로 내 슬롯의 물건을 이 창고로 보낼 수 있게 알린다")]
        [SerializeField] private InventoryChannel _channel;
        [SerializeField] private InputActionReference _interact;
        [SerializeField] private InputActionReference _cancel;
        [SerializeField] private PlayerNetworkInputLock _inputLock;
        [Tooltip("내 캐릭터가 범위 안에 있고 창이 닫혀 있을 때 띄우는 상호작용 안내")]
        [SerializeField] private GameObject _prompt;

        private BoxCollider _range;
        private NetworkPlayer _localPlayer;
        private bool _isOpen;

        private void Awake()
        {
            _range = GetComponent<BoxCollider>();
            _prompt.SetActive(false);
        }

        private void OnEnable()
        {
            _interact.action.Enable();
            _cancel.action.Enable();
        }

        private void OnDisable()
        {
            if (_isOpen)
            {
                Close();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            NetworkPlayer player = other.GetComponentInParent<NetworkPlayer>();
            if (player != null && player.HasInputAuthority)
            {
                _localPlayer = player;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            NetworkPlayer player = other.GetComponentInParent<NetworkPlayer>();
            if (player != null && player == _localPlayer)
            {
                _localPlayer = null;
            }
        }

        private void Update()
        {
            if (_isOpen)
            {
                if (_interact.action.WasPressedThisFrame() || _cancel.action.WasPressedThisFrame())
                {
                    Close();
                }

                return;
            }

            // OnTriggerExit가 오지 않는 경우(순간이동, 디스폰 등)에도 범위를 벗어나면 열지 못하도록 직접 확인한다.
            if (_localPlayer != null && !IsInRange(_localPlayer.transform.position))
            {
                _localPlayer = null;
            }

            // 대화·미니게임이 입력을 잠근 동안에는 열지 않는다.
            bool canOpen = _localPlayer != null && _inventory.IsReady && !_inputLock.IsLocked;
            _prompt.SetActive(canOpen);
            if (canOpen && _interact.action.WasPressedThisFrame())
            {
                Open();
            }
        }

        private void Open()
        {
            _isOpen = true;
            _prompt.SetActive(false);
            _inputLock.Lock(this);
            _view.Show(_inventory);
            _channel.SetOpenStorage(_inventory);
        }

        private void Close()
        {
            _isOpen = false;
            _channel.ClearOpenStorage(_inventory);
            // 씬을 내릴 때는 창이 먼저 파괴되었을 수 있다.
            if (_view != null)
            {
                _view.Hide();
            }

            _inputLock.Release(this);
        }

        // 높이는 보지 않고 바닥 기준 범위만 확인한다.
        private bool IsInRange(Vector3 position)
        {
            Bounds bounds = _range.bounds;
            return bounds.Contains(new Vector3(position.x, bounds.center.y, position.z));
        }
    }
}
