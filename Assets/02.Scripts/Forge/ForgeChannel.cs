using System;
using UnityEngine;

namespace Blacksmith.Forge
{
    public enum ForgeRejectReason
    {
        // 작업대를 다른 사람이 쓰고 있다.
        Busy,
        // 작업대를 찾지 못했거나 제작 항목이 없다.
        Unavailable,
        // 낮 영업이 끝나 새 미니게임을 시작할 수 없다.
        Closed,
        // 작업대 슬롯의 재료가 모자라다.
        MissingMaterials,
    }

    // 씬의 작업대와 Player 프리팹의 ForgePlayerActions를 잇는 에셋이다.
    // 작업대는 내 플레이어 오브젝트를 몰라도 이 에셋으로 열기·제작·결과를 호스트에 보낸다.
    [CreateAssetMenu(fileName = "ForgeChannel", menuName = "Blacksmith/Runtime/Forge Channel")]
    public class ForgeChannel : ScriptableObject
    {
        private ForgePlayerActions _localActions;

        // 호스트가 작업대 사용권을 준 직후(제작 목록을 열어도 된다)
        public event Action<ForgeStationState> Opened;
        // 호스트가 제작을 승인했다. 두 번째 값은 미니게임 번호, 세 번째 값은 제작 항목 번호다.
        public event Action<ForgeStationState, int, int> CraftApproved;
        public event Action<ForgeStationState, ForgeRejectReason> Rejected;
        // 영업 중인지가 바뀌었다(true면 열림). 열려 있던 제작 목록은 영업이 끝나면 닫는다.
        public event Action<bool> OpenChanged;

        public bool HasLocalPlayer => _localActions != null;

        // 새 미니게임을 시작할 수 있는지. 낮·밤 단계에 맞춰 ForgeDayCycleLink가 정한다. 연결이 없으면 늘 열려 있다.
        // 호스트는 열기와 제작 요청을 받을 때 이 값을 확인한다.
        public bool IsOpen { get; private set; } = true;

        public void SetOpen(bool isOpen)
        {
            if (IsOpen == isOpen)
            {
                return;
            }

            IsOpen = isOpen;
            OpenChanged?.Invoke(isOpen);
        }

        public void Register(ForgePlayerActions actions)
        {
            _localActions = actions;
        }

        public void Unregister(ForgePlayerActions actions)
        {
            if (_localActions == actions)
            {
                _localActions = null;
            }
        }

        // 요청이 호스트에 닿았는지는 열림·거절 사건으로 알려 준다.
        public bool RequestOpen(ForgeStationState station)
        {
            if (_localActions == null)
            {
                return false;
            }

            _localActions.RequestOpen(station);
            return true;
        }

        public void RequestClose(ForgeStationState station)
        {
            if (_localActions != null)
            {
                _localActions.RequestClose(station);
            }
        }

        // reserved: 쓸 재료의 예약(CraftingReservation.Pack). 호스트가 개인 슬롯에 그만큼 있는지 확인한다.
        public bool RequestCraft(ForgeStationState station, int entryIndex, long reserved)
        {
            if (_localActions == null)
            {
                return false;
            }

            _localActions.RequestCraft(station, entryIndex, reserved);
            return true;
        }

        public void SubmitResult(ForgeStationState station, int sessionId, float accuracy)
        {
            if (_localActions != null)
            {
                _localActions.SubmitResult(station, sessionId, accuracy);
            }
        }

        public void RaiseOpened(ForgeStationState station)
        {
            Opened?.Invoke(station);
        }

        public void RaiseCraftApproved(ForgeStationState station, int sessionId, int entryIndex)
        {
            CraftApproved?.Invoke(station, sessionId, entryIndex);
        }

        public void RaiseRejected(ForgeStationState station, ForgeRejectReason reason)
        {
            Rejected?.Invoke(station, reason);
        }
    }
}
