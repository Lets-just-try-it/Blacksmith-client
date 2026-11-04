using System;
using Blacksmith.Core.Forge;
using Blacksmith.Core.Inventory;
using Blacksmith.Inventory;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 작업대 화면(미니게임)과 호스트의 사용권 상태를 잇는다. 작업대 오브젝트에 ForgeStationState와 함께 둔다.
    // 작업대는 E를 눌렀을 때 RequestStart를 부르고, 호스트가 승인하면 Approved를 받아 미니게임을 시작한다.
    // 끝나면 SubmitResult로 승인받은 세션의 정확도를 호스트에 보낸다.
    public class ForgeStationLink : MonoBehaviour
    {
        // 승인이 이만큼 안 오면 요청을 포기한다.
        private const float ResponseTimeout = 6f;

        [SerializeField] private ForgeStationState _state;
        [SerializeField] private ForgeChannel _forge;
        [SerializeField] private InventoryChannel _inventory;
        [SerializeField] private ForgeNotice _notice;
        [SerializeField] private TierRulesData _tiers;

        private bool _waiting;
        private float _waitUntil;
        private int _sessionId;

        // 호스트가 사용권을 준 직후에 불린다.
        public event Action Approved;

        public bool Requesting => _waiting;

        // 요청한 재료의 티어가 정한 미니게임 난이도 번호. 승인을 받은 뒤 미니게임을 시작할 때 쓴다.
        public int Difficulty { get; private set; }

        // 요청한 재료가 중간 형태면 지금까지의 정확도 평균(0~1), 아니면 1이다. 연마가 처음 날 상태를 정하는 데 쓴다.
        public float PriorAccuracy { get; private set; } = 1f;

        private void OnEnable()
        {
            _forge.StartApproved += OnApproved;
            _forge.StartRejected += OnRejected;
        }

        private void OnDisable()
        {
            _forge.StartApproved -= OnApproved;
            _forge.StartRejected -= OnRejected;
            _waiting = false;
        }

        private void Update()
        {
            if (_waiting && Time.unscaledTime > _waitUntil)
            {
                _waiting = false;
                _notice.Show("작업대가 응답하지 않는다");
            }
        }

        // 손에 든 슬롯의 물건으로 사용을 요청한다. 손에 든 물건이 없으면 요청하지 않고 안내한다.
        public void RequestStart()
        {
            if (_waiting)
            {
                return;
            }

            int slot = _inventory.HeldSlot;
            if (slot < 0)
            {
                _notice.Show(GetNeedMessage());
                return;
            }

            ReadHeldItem(slot);

            // 호스트 자신의 요청은 RPC가 바로 실행되어 승인이 이 호출 안에서 돌아오므로, 보내기 전에 기다림 상태로 만든다.
            _waiting = true;
            _waitUntil = Time.unscaledTime + ResponseTimeout;
            if (!_forge.RequestStart(_state, slot))
            {
                _waiting = false;
                _notice.Show("아직 플레이어가 준비되지 않았다");
            }
        }

        public void SubmitResult(float accuracy)
        {
            if (_sessionId == 0)
            {
                return;
            }

            _forge.SubmitResult(_state, _sessionId, accuracy);
            _sessionId = 0;
        }

        private void ReadHeldItem(int slot)
        {
            PlayerInventoryState inventory = _inventory.LocalPlayerInventory;
            ItemStack held = inventory != null ? inventory.Get(slot) : ItemStack.Empty;
            ItemData item = held.IsEmpty ? null : inventory.Catalog.Find(held.ItemId);
            Difficulty = _tiers.DifficultyIndex(item != null ? item.Tier : 1);
            PriorAccuracy = item != null && item.Kind == ItemKind.Intermediate ? Mathf.Clamp01(held.Accuracy / (float)CraftingRules.MaxPercent) : 1f;
        }

        private void OnApproved(ForgeStationState station, int sessionId)
        {
            if (station != _state || !_waiting)
            {
                return;
            }

            _waiting = false;
            _sessionId = sessionId;
            Approved?.Invoke();
        }

        private void OnRejected(ForgeStationState station, ForgeRejectReason reason)
        {
            if (station != _state || !_waiting)
            {
                return;
            }

            _waiting = false;
            _notice.Show(reason == ForgeRejectReason.Busy ? "다른 사람이 쓰고 있다" : GetNeedMessage());
        }

        private string GetNeedMessage()
        {
            string name = _state.Recipe != null ? _state.Recipe.GetInputName(_state.Kind) : null;
            return string.IsNullOrEmpty(name)
                ? "손에 재료를 들어야 한다 (숫자키 1~8)"
                : $"{name}을(를) 손에 들어야 한다 (숫자키 1~8)";
        }
    }
}
