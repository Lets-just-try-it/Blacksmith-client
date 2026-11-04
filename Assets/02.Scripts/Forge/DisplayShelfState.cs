using System;
using Blacksmith.Core.Inventory;
using Blacksmith.Inventory;
using Fusion;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 판매 구역의 진열대 하나의 상태다. 장비 하나를 올려 둘 수 있고, 올리고 내리는 일과 손님이 사 가는 일은 호스트만 한다.
    // 진열대 오브젝트에 붙는 씬 네트워크 오브젝트이며, 올리고 내리는 요청 RPC는 Player 프리팹의 SalesPlayerActions가 맡는다.
    public class DisplayShelfState : NetworkBehaviour
    {
        [SerializeField] private ItemCatalog _catalog;
        [SerializeField] private TierRulesData _tiers;
        [Tooltip("손님이 이 진열대를 보러 와서 서는 자리")]
        [SerializeField] private Transform _customerPoint;

        [Networked] private NetworkItemStack Slot { get; set; }

        private ChangeDetector _changes;

        // 스폰 직후와 올린 물건이 바뀔 때 모든 참가자에게 불린다.
        public event Action Changed;

        public bool IsReady => Object != null && Object.IsValid;
        public ItemCatalog Catalog => _catalog;
        public ItemStack Item => IsReady ? Slot.ToStack() : ItemStack.Empty;
        public bool IsEmpty => Item.IsEmpty;
        public Vector3 CustomerPoint => _customerPoint.position;

        public override void Spawned()
        {
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
            Changed?.Invoke();
        }

        public override void Render()
        {
            foreach (string _ in _changes.DetectChanges(this))
            {
                Changed?.Invoke();
            }
        }

        // 이 물건의 정가. 재료 티어와 등급으로 정해진다.
        public int PriceOf(ItemStack stack)
        {
            ItemData item = stack.IsEmpty ? null : _catalog.Find(stack.ItemId);
            return _tiers.SalePrice(item != null ? item.Tier : 1, stack.Grade);
        }

        // 호스트 전용. 비어 있고 장비일 때만 올린다.
        public bool TryPlace(ItemStack item)
        {
            ItemData data = item.IsEmpty ? null : _catalog.Find(item.ItemId);
            if (!HasStateAuthority || !IsEmpty || data == null || data.Kind != ItemKind.Equipment)
            {
                return false;
            }

            Slot = NetworkItemStack.FromStack(item.WithCount(1));
            return true;
        }

        // 호스트 전용. 올려 둔 물건을 내려 돌려주고 진열대를 비운다.
        public ItemStack Take()
        {
            if (!HasStateAuthority)
            {
                return ItemStack.Empty;
            }

            ItemStack item = Item;
            Slot = default;
            return item;
        }

        // 호스트 전용. 손님이 사 간다. 올려 둔 물건이 있으면 비우고 정가를 돌려준다.
        public bool TryBuy(out int price)
        {
            price = 0;
            if (!HasStateAuthority || IsEmpty)
            {
                return false;
            }

            price = PriceOf(Item);
            Slot = default;
            return true;
        }
    }
}
