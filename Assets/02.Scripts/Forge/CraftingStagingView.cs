using System;
using System.Collections.Generic;
using Blacksmith.Core.Forge;
using Blacksmith.Core.Inventory;
using Blacksmith.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Blacksmith.Forge
{
    // 제작 목록 창 안의 작업대 슬롯이다. 개인 슬롯의 재료를 끌어다 놓거나 빠르게 옮기면 아이템을 옮기지 않고 예약만 하며,
    // 예약한 만큼이 이 칸에 놓인 것처럼 보인다(개인 슬롯 화면은 InventoryChannel.Reservation으로 그만큼 빼서 그린다).
    // 칸을 클릭하면 예약을 풀어 개인 슬롯으로 돌려보낸 것처럼 보인다. 고른 항목이 쓰지 않는 물건이나 필요한 개수를 넘는 것은 예약되지 않는다.
    public class CraftingStagingView : MonoBehaviour, IInventorySlotOwner
    {
        [SerializeField] private InventoryChannel _channel;
        [Tooltip("등급 이름을 찾는 데 쓴다")]
        [SerializeField] private GradeThresholdData _grades;
        [SerializeField] private GridLayoutGroup _slotRoot;
        [SerializeField] private InventorySlotView _slotPrefab;

        private readonly List<InventorySlotView> _cells = new List<InventorySlotView>();
        private readonly List<ItemStack> _actual = new List<ItemStack>();
        private readonly int[] _cellToSlot = new int[CraftingReservation.MaxSlots];
        private CraftingReservation _reservation;
        private Func<IReadOnlyList<CraftingIngredient>> _getNeeds;
        private Action _changed;

        private void Awake()
        {
            for (int i = 0; i < CraftingReservation.MaxSlots; i++)
            {
                InventorySlotView cell = Instantiate(_slotPrefab, _slotRoot.transform);
                cell.Bind(this, i);
                cell.Show(ItemStack.Empty, null, string.Empty);
                cell.SetHeld(false);
                _cells.Add(cell);
                _cellToSlot[i] = -1;
            }
        }

        // getNeeds: 지금 고른 항목의 재료. changed: 예약이 바뀌었을 때 창이 다시 그리도록 부른다.
        public void Bind(CraftingReservation reservation, Func<IReadOnlyList<CraftingIngredient>> getNeeds, Action changed)
        {
            _reservation = reservation;
            _getNeeds = getNeeds;
            _changed = changed;
        }

        // 예약한 칸을 앞에서부터 채워 그린다.
        public void Refresh(PlayerInventoryState inventory)
        {
            int cell = 0;
            if (inventory != null && _reservation != null)
            {
                for (int slot = 0; slot < CraftingReservation.MaxSlots && cell < _cells.Count; slot++)
                {
                    int reserved = _reservation.GetReserved(slot);
                    ItemStack stack = inventory.Get(slot);
                    if (reserved <= 0 || stack.IsEmpty)
                    {
                        continue;
                    }

                    ItemStack shown = stack.WithCount(Mathf.Min(reserved, stack.Count));
                    ItemData item = inventory.Catalog.Find(shown.ItemId);
                    _cells[cell].Show(shown, item, ItemLabels.Get(shown, item, _grades));
                    _cellToSlot[cell] = slot;
                    cell++;
                }
            }

            for (; cell < _cells.Count; cell++)
            {
                _cells[cell].Show(ItemStack.Empty, null, string.Empty);
                _cellToSlot[cell] = -1;
            }
        }

        // 개인 슬롯 slot의 물건을 count개까지 예약한다. 예약한 개수를 돌려준다.
        public int Reserve(PlayerInventoryState inventory, int slot, int count)
        {
            if (inventory == null || _reservation == null)
            {
                return 0;
            }

            _actual.Clear();
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                _actual.Add(inventory.Get(i));
            }

            int reserved = _reservation.TryReserve(slot, _actual, count, _getNeeds());
            if (reserved > 0)
            {
                _changed?.Invoke();
            }

            return reserved;
        }

        // 끌어다 놓는 쪽은 내 개인 슬롯이어야 한다. 남은 개수만큼 예약한다.
        public void DropOn(InventorySlotView slot)
        {
            PlayerInventoryState local = _channel.LocalPlayerInventory;
            if (local != null && _channel.DragSource == local)
            {
                Reserve(local, _channel.DragSlot, int.MaxValue);
            }

            _channel.EndDrag();
        }

        // 놓인 칸을 클릭하면 예약을 푼다.
        public void Click(InventorySlotView slot, int clickCount)
        {
            int playerSlot = _cellToSlot[slot.Slot];
            if (playerSlot >= 0 && _reservation.Release(playerSlot) > 0)
            {
                _changed?.Invoke();
            }
        }

        // 놓인 칸은 끌어서 옮기지 않는다. 클릭으로 돌려보낸다.
        public void BeginDrag(InventorySlotView slot, PointerEventData eventData)
        {
        }

        public void MoveDrag(PointerEventData eventData)
        {
        }

        public void EndDrag()
        {
        }
    }
}
