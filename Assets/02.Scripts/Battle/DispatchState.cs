using System;
using System.Collections.Generic;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Battle;
using Blacksmith.Plaza;
using Fusion;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 파견할 때 보이는 창. 던전 지도에서 단계와 스테이지를 고른 뒤 파견 창에서 파티를 꾸린다.
    public enum DispatchScreen
    {
        Map,
        Party,
    }

    // 밤 파견에서 호스트가 고른 내용(창 열림, 보이는 창, 던전 단계, 스테이지, 파티)이다. 낮 씬 오브젝트이며 호스트가 소유한다.
    // 고르기는 호스트만 하고, 다른 참가자는 같은 창을 보기만 한다 (docs/GAME_CONCEPT.md 8절).
    // 호스트가 직접 값을 바꾸므로 RPC를 두지 않는다.
    public class DispatchState : NetworkBehaviour
    {
        [SerializeField] private BattleChannel _channel;
        [SerializeField] private AdventurerRosterState _roster;
        [SerializeField] private DungeonProgressState _progress;
        [SerializeField] private BattleSceneLoader _loader;

        private ChangeDetector _changes;
        // 이 참가자의 씬에 연결된 던전 지도 창이 있는지. 없으면 지도를 건너뛰고 파견 창부터 연다.
        private bool _hasMap;

        [Networked] public NetworkBool IsOpen { get; private set; }
        // 밤에 열린 창은 닫을 수 없고, 파견하거나 파견하지 않고 다음 날로 넘겨야 한다.
        [Networked] public NetworkBool IsNight { get; private set; }
        [Networked] public DispatchScreen Screen { get; private set; }
        [Networked] public int TierIndex { get; private set; }
        // 한 번 파견하면 싸우는 스테이지 하나. 0부터 센 번호이고, 고를 스테이지가 없으면 -1이다.
        [Networked] public int Stage { get; private set; }
        [Networked] public int PartyCount { get; private set; }
        [Networked, Capacity(BattleState.MaxPartySize)] private NetworkArray<int> PartyIds => default;

        // 스폰 직후와 고른 내용이 바뀔 때 모든 참가자에게 불린다.
        public event Action Changed;
        // 호스트에서만 불린다. 밤에 파견하지 않고 다음 날로 넘기기로 했을 때다.
        public event Action Skipped;

        public bool IsReady => Object != null && Object.IsValid;
        public bool CanEdit => IsReady && HasStateAuthority;
        public DungeonData SelectedDungeon => _progress.GetDungeon(TierIndex);
        public bool HasMap => _hasMap;

        // 모든 참가자의 전투 씬이 관리 목록에서 파티를 찾는다. 입은 장비가 함께 담겨 전투 유닛에 장비 보정이 들어간다.
        private void Awake()
        {
            _channel.SetAdventurerSource(_roster.FindById);
        }

        private void OnEnable()
        {
            _roster.Changed += RemoveUndispatchable;
            _progress.Changed += ClampSelection;
        }

        private void OnDisable()
        {
            _roster.Changed -= RemoveUndispatchable;
            _progress.Changed -= ClampSelection;
        }

        public override void Spawned()
        {
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
            if (HasStateAuthority)
            {
                // 처음에는 열린 가장 깊은 단계의 다음 스테이지를 골라 둔다.
                TierIndex = FindDeepestUnlockedTier();
                Stage = -1;
            }

            ClampSelection();
            Changed?.Invoke();
        }

        public override void Render()
        {
            bool hasChanged = false;
            foreach (string _ in _changes.DetectChanges(this))
            {
                hasChanged = true;
            }

            if (hasChanged)
            {
                Changed?.Invoke();
            }
        }

        public bool IsInParty(int adventurerId)
        {
            return IndexOfParty(adventurerId) >= 0;
        }

        public int GetClearedStages()
        {
            return _progress.GetClearedStages(SelectedDungeon.DungeonId);
        }

        // 고른 스테이지를 이미 클리어했는지. 다시 싸워도 진행 상황은 늘지 않는다.
        public bool IsReplay()
        {
            return Stage >= 0 && DungeonProgressRules.IsStageCleared(GetClearedStages(), Stage);
        }

        // 호스트 전용. 테스트 씬의 버튼이 낮에 연다. 낮에 연 창은 닫을 수 있다.
        public void Open()
        {
            Open(false);
        }

        // 호스트 전용. 밤이 되면 NightDispatchLink가 연다.
        public void OpenForNight()
        {
            Open(true);
        }

        public void Close()
        {
            if (CanEdit && !IsNight)
            {
                IsOpen = false;
            }
        }

        // 호스트 전용. 밤에 파견하지 않고 다음 날로 넘긴다.
        public void Skip()
        {
            if (CanEdit && IsOpen && IsNight)
            {
                IsOpen = false;
                IsNight = false;
                Skipped?.Invoke();
            }
        }

        // 호스트 전용. 파티에 없으면 넣고 있으면 뺀다.
        public void ToggleMember(int adventurerId)
        {
            if (!CanEdit)
            {
                return;
            }

            int index = IndexOfParty(adventurerId);
            if (index >= 0)
            {
                RemoveAt(index);
                return;
            }

            AdventurerData adventurer = _roster.FindById(adventurerId);
            if (adventurer != null && DispatchRules.CanDispatch(adventurer.Condition)
                && DispatchRules.CanAddToParty(PartyCount, BattleState.MaxPartySize))
            {
                PartyIds.Set(PartyCount, adventurerId);
                PartyCount++;
            }
        }

        // 던전 지도 창이 씬에 연결되어 있으면 켜질 때 알린다. 지도를 아직 붙이지 않은 씬은 예전처럼 파견 창부터 연다.
        public void RegisterMap()
        {
            _hasMap = true;
        }

        public void UnregisterMap()
        {
            _hasMap = false;
        }

        // 호스트 전용. 지도의 거점을 눌러 던전 단계와 스테이지를 함께 고른다.
        // 열린 단계에서 클리어한 스테이지나 다음 스테이지만 고를 수 있다.
        public void Select(int tierIndex, int stage)
        {
            if (!CanEdit || !_progress.IsTierSelectable(tierIndex))
            {
                return;
            }

            DungeonData dungeon = _progress.GetDungeon(tierIndex);
            if (DungeonProgressRules.CanSelectStage(_progress.GetClearedStages(dungeon.DungeonId), dungeon.StageCount, stage))
            {
                TierIndex = tierIndex;
                Stage = stage;
            }
        }

        // 호스트 전용. 지도에서 고른 스테이지로 파티를 꾸리러 간다.
        public void ShowParty()
        {
            if (CanEdit && IsOpen && Stage >= 0)
            {
                Screen = DispatchScreen.Party;
            }
        }

        // 호스트 전용. 파견 창에서 지도로 돌아가 스테이지를 다시 고른다. 고른 파티는 그대로 둔다.
        public void ShowMap()
        {
            if (CanEdit && IsOpen && _hasMap)
            {
                Screen = DispatchScreen.Map;
            }
        }

        public bool CanDispatch()
        {
            return CanEdit && _loader.CanDispatch && Screen == DispatchScreen.Party && Stage >= 0
                && DispatchRules.IsValidPartySize(PartyCount, BattleState.MaxPartySize);
        }

        // 호스트 전용. 고른 파티로 고른 스테이지의 전투 씬을 불러오고 창을 닫는다.
        public bool Dispatch()
        {
            if (!CanDispatch())
            {
                return false;
            }

            var partyIds = new List<int>(PartyCount);
            for (int i = 0; i < PartyCount; i++)
            {
                partyIds.Add(PartyIds[i]);
            }

            bool isDispatched = _loader.Dispatch(new DispatchRequest
            {
                Seed = UnityEngine.Random.Range(0, int.MaxValue),
                DungeonId = SelectedDungeon.DungeonId,
                ClearedStages = GetClearedStages(),
                Stage = Stage,
                PartyIds = partyIds,
            });

            if (isDispatched)
            {
                IsOpen = false;
                IsNight = false;
            }

            return isDispatched;
        }

        private void Open(bool isNight)
        {
            if (CanEdit)
            {
                // 밤에 열린 창을 낮용으로 다시 열어도 밤 창(닫을 수 없음)으로 남긴다.
                IsNight = isNight || (IsOpen && IsNight);
                if (!IsOpen)
                {
                    // 창을 새로 열 때마다 열린 가장 깊은 단계의 다음 스테이지부터 보여 준다.
                    TierIndex = FindDeepestUnlockedTier();
                    Stage = -1;
                    Screen = _hasMap ? DispatchScreen.Map : DispatchScreen.Party;
                }

                IsOpen = true;
                ClampSelection();
            }
        }

        private int IndexOfParty(int adventurerId)
        {
            if (!IsReady)
            {
                return -1;
            }

            for (int i = 0; i < PartyCount; i++)
            {
                if (PartyIds[i] == adventurerId)
                {
                    return i;
                }
            }

            return -1;
        }

        private void RemoveAt(int index)
        {
            for (int i = index; i < PartyCount - 1; i++)
            {
                PartyIds.Set(i, PartyIds[i + 1]);
            }

            PartyCount--;
            PartyIds.Set(PartyCount, 0);
        }

        // 관리 목록에서 빠졌거나 부상당한 모험가는 파티에서 뺀다.
        private void RemoveUndispatchable()
        {
            if (!CanEdit)
            {
                return;
            }

            for (int i = PartyCount - 1; i >= 0; i--)
            {
                AdventurerData adventurer = _roster.FindById(PartyIds[i]);
                if (adventurer == null || !DispatchRules.CanDispatch(adventurer.Condition))
                {
                    RemoveAt(i);
                }
            }
        }

        // 진행 상황이 바뀌어 고른 단계나 스테이지를 더는 고를 수 없으면 기본값으로 옮긴다.
        private void ClampSelection()
        {
            if (!CanEdit || _progress.TierCount == 0)
            {
                return;
            }

            if (!_progress.IsTierSelectable(TierIndex))
            {
                TierIndex = FindDeepestUnlockedTier();
            }

            Stage = DispatchRules.ClampStage(GetClearedStages(), SelectedDungeon.StageCount, Stage);
        }

        // 열린 단계 중 가장 깊은 단계. 아직 진행 중인 곳이 기본으로 골라진다.
        private int FindDeepestUnlockedTier()
        {
            for (int tier = _progress.TierCount - 1; tier > 0; tier--)
            {
                if (_progress.IsTierSelectable(tier))
                {
                    return tier;
                }
            }

            return 0;
        }
    }
}
