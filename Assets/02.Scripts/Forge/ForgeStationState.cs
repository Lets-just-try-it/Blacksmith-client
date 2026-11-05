using Blacksmith.Core.Forge;
using Fusion;
using UnityEngine;

namespace Blacksmith.Forge
{
    public enum ForgeStationStatus
    {
        Available,
        InProgress,
    }

    // 작업대 하나의 사용권 상태다. 한 번에 한 명만 쓸 수 있고, 사용권 부여와 해제는 호스트만 한다.
    // 작업대 오브젝트에 붙는 씬 네트워크 오브젝트이며, 요청과 결과를 주고받는 RPC는 Player 프리팹의 ForgePlayerActions가 맡는다.
    // 규칙은 docs/NETWORK_DESIGN.md의 "작업대 사용권 관리"를 따른다.
    public class ForgeStationState : NetworkBehaviour, IPlayerLeft
    {
        [SerializeField] private CraftingStationKind _kind;
        [SerializeField] private CraftingRecipeData _recipe;
        [Tooltip("Player 프리팹이 사용 중인 작업대를 찾을 수 있게 스폰되면 등록한다")]
        [SerializeField] private ForgeStationRegistry _registry;

        [Networked] private ForgeStationStatus Status { get; set; }
        [Networked] private PlayerRef CurrentUser { get; set; }
        // 사용권을 줄 때마다 1씩 늘려 발급하는 번호. 0은 없음이다.
        [Networked] private int SessionId { get; set; }
        [Networked] private int SessionCounter { get; set; }
        [Networked] private int LastProcessedSession { get; set; }
        // 사용자가 재료를 꺼내 쓰는 개인 슬롯 번호
        [Networked] private int ReservedSlot { get; set; }

        public CraftingStationKind Kind => _kind;
        public CraftingRecipeData Recipe => _recipe;
        public bool IsReady => Object != null && Object.IsValid;
        public bool InUse => IsReady && Status == ForgeStationStatus.InProgress;
        public PlayerRef User => CurrentUser;
        public int ActiveSessionId => SessionId;
        public int Slot => ReservedSlot;

        public override void Spawned()
        {
            if (_registry != null)
            {
                _registry.Register(this);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (_registry != null)
            {
                _registry.Unregister(this);
            }
        }

        // 호스트 전용. 사용 가능할 때만 사용권을 주고 새 세션 번호를 발급한다.
        public bool TryAcquire(PlayerRef user, int slot, out int sessionId)
        {
            sessionId = 0;
            if (!HasStateAuthority || Status != ForgeStationStatus.Available)
            {
                return false;
            }

            SessionCounter++;
            SessionId = SessionCounter;
            CurrentUser = user;
            ReservedSlot = slot;
            Status = ForgeStationStatus.InProgress;
            sessionId = SessionId;
            return true;
        }

        // 호스트 전용. 결과를 보낸 사람이 현재 사용자이고 현재 세션의 결과이며 아직 처리하지 않은 것일 때만 true다.
        // true면 사용권은 풀리고 이 세션은 처리한 것으로 기록된다.
        public bool TryFinish(PlayerRef user, int sessionId, out int slot)
        {
            slot = -1;
            if (!HasStateAuthority || Status != ForgeStationStatus.InProgress || CurrentUser != user)
            {
                return false;
            }

            if (sessionId != SessionId || sessionId == LastProcessedSession)
            {
                return false;
            }

            slot = ReservedSlot;
            LastProcessedSession = sessionId;
            Release();
            return true;
        }

        // 호스트 전용. 사용권을 풀고 작업대를 비운다.
        public void Release()
        {
            if (!HasStateAuthority)
            {
                return;
            }

            Status = ForgeStationStatus.Available;
            CurrentUser = default;
            ReservedSlot = -1;
        }

        // 미니게임 도중 연결이 끊어지면 사용권을 풀어 작업대가 계속 사용 중으로 남지 않게 한다. 결과는 반영하지 않는다.
        public void PlayerLeft(PlayerRef player)
        {
            if (HasStateAuthority && Status == ForgeStationStatus.InProgress && CurrentUser == player)
            {
                Release();
            }
        }
    }
}
