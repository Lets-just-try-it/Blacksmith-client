using Blacksmith.Core;
using Blacksmith.Core.Inventory;
using Blacksmith.Inventory;
using Blacksmith.Network;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Blacksmith.Forge
{
    // 진열대 앞의 상호작용이다. 내 캐릭터가 범위 안에서 상호작용 키(E)를 누르면, 손에 장비를 들고 있고 진열대가 비어 있으면
    // 올리고, 올려 둔 장비가 있으면 내린다. 진열대 위에는 올려 둔 장비의 이름·등급·정가를 글자로 보여 준다.
    [RequireComponent(typeof(BoxCollider))]
    public class DisplayShelfStation : MonoBehaviour
    {
        [SerializeField] private DisplayShelfState _shelf;
        [SerializeField] private GradeThresholdData _grades;
        [SerializeField] private InventoryChannel _inventory;
        [SerializeField] private InputActionReference _interact;
        [SerializeField] private PlayerNetworkInputLock _inputLock;
        [SerializeField] private ForgeNotice _notice;
        [Tooltip("진열대 위에 띄우는 글자(월드 공간)")]
        [SerializeField] private TMP_Text _label;

        private BoxCollider _range;
        private NetworkPlayer _localPlayer;
        private Camera _camera;

        private void Awake()
        {
            _range = GetComponent<BoxCollider>();
        }

        private void OnEnable()
        {
            _interact.action.Enable();
            _shelf.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            _shelf.Changed -= Refresh;
        }

        private void OnTriggerEnter(Collider other)
        {
            NetworkPlayer player = other.GetComponentInParent<NetworkPlayer>();
            if (player != null && player.HasInputAuthority)
            {
                _localPlayer = player;
                Refresh();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            NetworkPlayer player = other.GetComponentInParent<NetworkPlayer>();
            if (player != null && player == _localPlayer)
            {
                _localPlayer = null;
                Refresh();
            }
        }

        private void Update()
        {
            // OnTriggerExit가 오지 않는 경우(순간이동 등)에도 범위를 벗어나면 쓰지 못하도록 직접 확인한다.
            if (_localPlayer != null && !IsInRange(_localPlayer.transform.position))
            {
                _localPlayer = null;
                Refresh();
            }

            if (_localPlayer != null && !_inputLock.IsLocked && _interact.action.WasPressedThisFrame())
            {
                Interact();
            }
        }

        // 글자는 쿼터뷰 카메라를 향하게 한다.
        private void LateUpdate()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera != null)
            {
                _label.transform.rotation = _camera.transform.rotation;
            }
        }

        private void Interact()
        {
            SalesPlayerActions actions = _localPlayer.GetComponent<SalesPlayerActions>();
            if (!_shelf.IsEmpty)
            {
                actions.RequestTake(_shelf);
                return;
            }

            if (!TryGetHeldEquipment(out int slot))
            {
                _notice.Show("진열할 장비를 손에 들어야 한다 (숫자키 1~8)");
                return;
            }

            actions.RequestPlace(_shelf, slot);
        }

        private bool TryGetHeldEquipment(out int slot)
        {
            slot = _inventory.HeldSlot;
            PlayerInventoryState inventory = _inventory.LocalPlayerInventory;
            if (slot < 0 || inventory == null)
            {
                return false;
            }

            ItemStack stack = inventory.Get(slot);
            ItemData item = stack.IsEmpty ? null : inventory.Catalog.Find(stack.ItemId);
            return item != null && item.Kind == ItemKind.Equipment;
        }

        private void Refresh()
        {
            ItemStack stack = _shelf.Item;
            string content;
            if (stack.IsEmpty)
            {
                content = "비어 있음";
            }
            else
            {
                ItemData item = _shelf.Catalog.Find(stack.ItemId);
                string name = item != null ? item.DisplayName : $"#{stack.ItemId}";
                bool hasGrade = stack.Grade >= GradeRule.MinGrade && stack.Grade < _grades.GradeCount + GradeRule.MinGrade;
                string grade = hasGrade ? $" ({_grades.GetGradeName(stack.Grade)})" : string.Empty;
                content = $"{name}{grade}\n<color=#FFD966>{_shelf.PriceOf(stack):N0} G</color>";
            }

            string hint = _localPlayer == null ? string.Empty : (stack.IsEmpty ? "\n<size=70%><color=#9FE0A8>[E] 진열하기</color></size>" : "\n<size=70%><color=#9FE0A8>[E] 내리기</color></size>");
            _label.text = content + hint;
        }

        // 높이는 보지 않고 바닥 기준 범위만 확인한다.
        private bool IsInRange(Vector3 position)
        {
            Bounds bounds = _range.bounds;
            return bounds.Contains(new Vector3(position.x, bounds.center.y, position.z));
        }
    }
}
