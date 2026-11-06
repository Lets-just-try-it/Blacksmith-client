using System.Collections.Generic;
using Blacksmith.Core.Forge;
using Fusion;
using UnityEngine;

namespace Blacksmith.Forge
{
    public enum ForgeStationStatus
    {
        Available,
        // 누군가 제작 목록을 열어 두었다(미니게임 중이기도 하다). 다른 사람은 열 수 없다.
        InProgress,
    }

    // 작업대 하나의 사용권 상태다. 한 번에 한 명만 열 수 있고, 사용권 부여와 해제는 호스트만 한다.
    // 열려 있는 동안 그 사람이 제작 목록에서 항목을 고르고 재료를 예약해(작업대 슬롯에 놓은 것처럼 보인다) 미니게임을 시작한다.
    // 재료는 제작이 끝날 때까지 그 사람의 개인 슬롯에 그대로 있다.
    // 작업대 오브젝트에 붙는 씬 네트워크 오브젝트이며, 요청과 결과를 주고받는 RPC는 Player 프리팹의 ForgePlayerActions가 맡는다.
    // 규칙은 docs/NETWORK_DESIGN.md의 "작업대 사용권 관리"를 따른다.
    public class ForgeStationState : NetworkBehaviour, IPlayerLeft
    {
        [SerializeField] private CraftingStationKind _kind;
        [Tooltip("제작 목록 위쪽에 보이는 이름")]
        [SerializeField] private string _displayName = "";
        [Tooltip("이 작업대가 보여 줄 수 있는 레시피. 이 작업대가 하는 단계만 골라 제작 목록에 올린다")]
        [SerializeField] private CraftingRecipeData[] _recipes = System.Array.Empty<CraftingRecipeData>();
        [Tooltip("Player 프리팹이 사용 중인 작업대를 찾을 수 있게 스폰되면 등록한다")]
        [SerializeField] private ForgeStationRegistry _registry;

        [Networked] private ForgeStationStatus Status { get; set; }
        [Networked] private PlayerRef CurrentUser { get; set; }
        // 미니게임을 시작할 때마다 1씩 늘려 발급하는 번호. 0은 미니게임 중이 아님이다.
        [Networked] private int SessionId { get; set; }
        [Networked] private int SessionCounter { get; set; }
        [Networked] private int LastProcessedSession { get; set; }
        // 지금 미니게임이 만드는 제작 항목 번호
        [Networked] private int CraftEntry { get; set; }
        // 지금 미니게임이 쓰는 재료 예약(개인 슬롯 칸마다 1바이트로 담은 개수, CraftingReservation.Pack)
        [Networked] private long PendingReserved { get; set; }

        private List<CraftingEntry> _entries;

        public CraftingStationKind Kind => _kind;
        public string DisplayName => _displayName;
        public bool IsReady => Object != null && Object.IsValid;
        // 누가 열어 두었는가(제작 목록 또는 미니게임)
        public bool IsOccupied => IsReady && Status == ForgeStationStatus.InProgress;
        // 미니게임을 하는 중인가. 일하는 자세를 취하는 데 쓴다.
        public bool InUse => IsOccupied && SessionId != 0;
        public PlayerRef User => CurrentUser;
        public int ActiveSessionId => SessionId;

        // 이 작업대가 하는 단계들. 같은 재료로 같은 결과를 내는 단계는 하나로 합친다.
        public IReadOnlyList<CraftingEntry> Entries
        {
            get
            {
                if (_entries == null)
                {
                    BuildEntries();
                }

                return _entries;
            }
        }

        private void BuildEntries()
        {
            _entries = new List<CraftingEntry>();
            var seen = new HashSet<string>();
            foreach (CraftingRecipeData recipe in _recipes)
            {
                if (recipe == null)
                {
                    continue;
                }

                for (int i = 0; i < recipe.StepCount; i++)
                {
                    CraftingRecipeData.Step step = recipe.GetStep(i);
                    if (step.Station != _kind || step.Input == null || step.Output == null)
                    {
                        continue;
                    }

                    var entry = new CraftingEntry(recipe, i);
                    if (seen.Add(entry.Signature()))
                    {
                        _entries.Add(entry);
                    }
                }
            }
        }

        public bool TryGetEntry(int index, out CraftingEntry entry)
        {
            IReadOnlyList<CraftingEntry> entries = Entries;
            if (index >= 0 && index < entries.Count)
            {
                entry = entries[index];
                return true;
            }

            entry = null;
            return false;
        }

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

        public bool IsUsedBy(PlayerRef user)
        {
            return IsOccupied && CurrentUser == user;
        }

        // 호스트 전용. 비어 있을 때만 사용권을 준다.
        public bool TryOpen(PlayerRef user)
        {
            if (!HasStateAuthority || Status != ForgeStationStatus.Available)
            {
                return false;
            }

            CurrentUser = user;
            Status = ForgeStationStatus.InProgress;
            SessionId = 0;
            return true;
        }

        // 호스트 전용. 연 사람이 미니게임 중이 아닐 때 새 미니게임 번호를 발급한다.
        public bool TryBeginCraft(PlayerRef user, int entryIndex, long reserved, out int sessionId)
        {
            sessionId = 0;
            if (!HasStateAuthority || !IsUsedBy(user) || SessionId != 0)
            {
                return false;
            }

            SessionCounter++;
            SessionId = SessionCounter;
            CraftEntry = entryIndex;
            PendingReserved = reserved;
            sessionId = SessionId;
            return true;
        }

        // 호스트 전용. 결과를 보낸 사람이 연 사람이고 현재 세션의 결과이며 아직 처리하지 않은 것일 때만 true다.
        // true면 미니게임은 끝난 것으로 기록하고, 사용권은 그대로 둔다(제작 목록이 이어서 닫는다).
        public bool TryFinish(PlayerRef user, int sessionId, out int entryIndex, out long reserved)
        {
            entryIndex = -1;
            reserved = 0;
            if (!HasStateAuthority || !IsUsedBy(user) || SessionId == 0)
            {
                return false;
            }

            if (sessionId != SessionId || sessionId == LastProcessedSession)
            {
                return false;
            }

            entryIndex = CraftEntry;
            reserved = PendingReserved;
            LastProcessedSession = sessionId;
            SessionId = 0;
            return true;
        }

        // 호스트 전용. 연 사람이 미니게임 중이 아닐 때 닫는다.
        public bool TryClose(PlayerRef user)
        {
            if (!HasStateAuthority || !IsUsedBy(user) || SessionId != 0)
            {
                return false;
            }

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
            SessionId = 0;
        }

        // 연결이 끊기면 사용권을 풀어 작업대가 계속 사용 중으로 남지 않게 한다. 결과는 반영하지 않고 재료는 소모되지 않는다.
        public void PlayerLeft(PlayerRef player)
        {
            if (HasStateAuthority && Status == ForgeStationStatus.InProgress && CurrentUser == player)
            {
                Release();
            }
        }
    }
}
