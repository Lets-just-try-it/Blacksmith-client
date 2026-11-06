using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 작업대 화면(제작 목록과 미니게임)과 호스트의 사용권 상태를 잇는다. 작업대 오브젝트에 ForgeStationState와 함께 둔다.
    // 작업대는 E를 눌렀을 때 RequestOpen을 부르고, 호스트가 승인하면 Opened를 받아 제작 목록을 연다.
    // 목록에서 제작을 누르면 RequestCraft로 호스트에 항목을 보내고, 승인되면 Approved를 받아 미니게임을 시작한다.
    // 미니게임이 끝나면 SubmitResult로 승인받은 세션의 정확도를 호스트에 보내고 제작 목록을 닫는다.
    public class ForgeStationLink : MonoBehaviour
    {
        // 응답이 이만큼 안 오면 요청을 포기한다.
        private const float ResponseTimeout = 6f;

        [SerializeField] private ForgeStationState _state;
        [SerializeField] private ForgeChannel _forge;
        [SerializeField] private ForgeNotice _notice;
        [SerializeField] private TierRulesData _tiers;
        [Tooltip("모든 작업대가 함께 쓰는 제작 목록 창")]
        [SerializeField] private CraftingMenuView _menu;

        private bool _waiting;
        private float _waitUntil;
        private int _sessionId;

        // 호스트가 사용권을 준 직후에 불린다. 제작 목록을 열 때다.
        public event Action Opened;
        // 호스트가 제작을 승인한 직후에 불린다. 미니게임을 시작할 때다.
        public event Action Approved;
        // 미니게임 결과를 보낸 직후에 불린다. 제작 목록이 작업대를 닫을 때다.
        public event Action CraftFinished;
        // 제작 목록을 닫을 때 불린다.
        public event Action Closed;
        // 열기나 제작이 거절됐을 때 불린다.
        public event Action<ForgeRejectReason> Rejected;

        public ForgeStationState Station => _state;
        public ForgeChannel Channel => _forge;
        public bool Requesting => _waiting;

        // 내가 이 작업대를 열어 둔 동안(제작 목록을 보거나 미니게임을 하는 동안) true다. 이때는 작업대가 새 열기 요청을 보내지 않는다.
        public bool IsOpen { get; private set; }

        // 승인받은 제작 항목. 미니게임을 시작할 때 쓴다.
        public CraftingEntry Entry { get; private set; }

        // 재료 티어가 정한 미니게임 난이도 번호.
        public int Difficulty { get; private set; }

        // 쓴 재료가 중간 형태면 지금까지의 정확도 평균(0~1), 아니면 1이다. 연마와 판 성형이 처음 상태를 정하는 데 쓴다.
        public float PriorAccuracy { get; private set; } = 1f;

        // 제작 항목이 정한 미니게임 모양 번호(판 성형의 투구/갑옷 등)
        public int Variant { get; private set; }

        private void OnEnable()
        {
            _forge.Opened += OnOpened;
            _forge.CraftApproved += OnCraftApproved;
            _forge.Rejected += OnRejected;
        }

        private void OnDisable()
        {
            _forge.Opened -= OnOpened;
            _forge.CraftApproved -= OnCraftApproved;
            _forge.Rejected -= OnRejected;
            _waiting = false;
            IsOpen = false;
        }

        private void Update()
        {
            if (_waiting && Time.unscaledTime > _waitUntil)
            {
                _waiting = false;
                _notice.Show("작업대가 응답하지 않는다");
                Rejected?.Invoke(ForgeRejectReason.Unavailable);
            }
        }

        // 작업대를 열어 달라고 요청한다.
        public void RequestOpen()
        {
            if (_waiting)
            {
                return;
            }

            // 호스트 자신의 요청은 RPC가 바로 실행되어 승인이 이 호출 안에서 돌아오므로, 보내기 전에 기다림 상태로 만든다.
            Wait();
            if (!_forge.RequestOpen(_state))
            {
                _waiting = false;
                _notice.Show("아직 플레이어가 준비되지 않았다");
            }
        }

        public void RequestClose()
        {
            _waiting = false;
            IsOpen = false;
            _forge.RequestClose(_state);
            Closed?.Invoke();
        }

        // 예약한 재료로 이 항목을 만들어 달라고 요청한다. 재료가 모자란지는 목록이 먼저 확인한다.
        // priorAccuracy: 예약한 재료 중 중간 형태의 정확도 평균(0~1). 중간 형태를 쓰지 않으면 1이다. 미니게임 시작 상태에 쓴다.
        public void RequestCraft(CraftingEntry entry, long reserved, float priorAccuracy)
        {
            if (_waiting)
            {
                return;
            }

            int index = IndexOf(entry);
            if (index < 0)
            {
                return;
            }

            Entry = entry;
            Difficulty = _tiers.DifficultyIndex(entry.Tier);
            Variant = entry.Variant;
            PriorAccuracy = Mathf.Clamp01(priorAccuracy);

            Wait();
            if (!_forge.RequestCraft(_state, index, reserved))
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
            CraftFinished?.Invoke();
        }

        private void Wait()
        {
            _waiting = true;
            _waitUntil = Time.unscaledTime + ResponseTimeout;
        }

        private int IndexOf(CraftingEntry entry)
        {
            IReadOnlyList<CraftingEntry> entries = _state.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] == entry)
                {
                    return i;
                }
            }

            return -1;
        }

        private void OnOpened(ForgeStationState station)
        {
            if (station != _state || !_waiting)
            {
                return;
            }

            _waiting = false;
            IsOpen = true;
            _menu.Open(this);
            Opened?.Invoke();
        }

        private void OnCraftApproved(ForgeStationState station, int sessionId, int entryIndex)
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
            string message = GetRejectMessage(reason);
            if (!string.IsNullOrEmpty(message))
            {
                _notice.Show(message);
            }

            Rejected?.Invoke(reason);
        }

        private static string GetRejectMessage(ForgeRejectReason reason)
        {
            switch (reason)
            {
                case ForgeRejectReason.Busy:
                    return "다른 사람이 쓰고 있다";
                case ForgeRejectReason.Closed:
                    return "오늘 영업이 끝났다";
                case ForgeRejectReason.MissingMaterials:
                    return "재료가 모자라다";
                default:
                    return "작업대를 쓸 수 없다";
            }
        }
    }
}
