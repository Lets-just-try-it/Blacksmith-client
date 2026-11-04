using System;
using UnityEngine;

namespace Blacksmith.Forge
{
    public enum ForgeRejectReason
    {
        // 작업대를 다른 사람이 쓰고 있다.
        Busy,
        // 손에 든 물건이 이 작업대에서 받는 재료가 아니다.
        WrongItem,
        // 작업대를 찾지 못했다.
        Unavailable,
        // 낮 영업이 끝나 새 미니게임을 시작할 수 없다.
        Closed,
    }

    // 씬의 작업대와 Player 프리팹의 ForgePlayerActions를 잇는 에셋이다.
    // 작업대는 내 플레이어 오브젝트를 몰라도 이 에셋으로 사용을 요청하고 결과를 보낸다.
    [CreateAssetMenu(fileName = "ForgeChannel", menuName = "Blacksmith/Runtime/Forge Channel")]
    public class ForgeChannel : ScriptableObject
    {
        private ForgePlayerActions _localActions;

        public event Action<ForgeStationState, int> StartApproved;
        public event Action<ForgeStationState, ForgeRejectReason> StartRejected;

        public bool HasLocalPlayer => _localActions != null;

        // 새 미니게임을 시작할 수 있는지. 낮·밤 단계에 맞춰 ForgeDayCycleLink가 정한다. 연결이 없으면 늘 열려 있다.
        // 호스트는 시작 요청을 받을 때 이 값을 확인한다.
        public bool IsOpen { get; private set; } = true;

        public void SetOpen(bool isOpen)
        {
            IsOpen = isOpen;
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

        // slot: 손에 든 개인 슬롯. 요청이 호스트에 닿았는지는 승인·거절 사건으로 알려 준다.
        public bool RequestStart(ForgeStationState station, int slot)
        {
            if (_localActions == null)
            {
                return false;
            }

            _localActions.RequestStart(station, slot);
            return true;
        }

        public void SubmitResult(ForgeStationState station, int sessionId, float accuracy)
        {
            if (_localActions != null)
            {
                _localActions.SubmitResult(station, sessionId, accuracy);
            }
        }

        public void RaiseApproved(ForgeStationState station, int sessionId)
        {
            StartApproved?.Invoke(station, sessionId);
        }

        public void RaiseRejected(ForgeStationState station, ForgeRejectReason reason)
        {
            StartRejected?.Invoke(station, reason);
        }
    }
}
