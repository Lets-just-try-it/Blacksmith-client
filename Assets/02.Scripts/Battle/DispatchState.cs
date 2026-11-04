using System;
using System.Collections.Generic;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Battle;
using Blacksmith.Plaza;
using Fusion;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 밤 파견 창에서 호스트가 고른 내용(창 열림, 파티, 던전 단계, 스테이지)이다. 낮 씬 오브젝트이며 호스트가 소유한다.
    // 고르기는 호스트만 하고, 다른 참가자는 같은 창을 보기만 한다 (docs/GAME_CONCEPT.md 8절).
    // 호스트가 직접 값을 바꾸므로 RPC를 두지 않는다.
    public class DispatchState : NetworkBehaviour
    {
        [SerializeField] private BattleChannel _channel;
        [SerializeField] private AdventurerRosterState _roster;
        [SerializeField] private DungeonProgressState _progress;
        [SerializeField] private BattleSceneLoader _loader;

        private ChangeDetector _changes;

        [Networked] public NetworkBool IsOpen { get; private set; }
        [Networked] public int TierIndex { get; private set; }
        // 0부터 센 번호. 고를 스테이지가 없으면 -1이다.
        [Networked] public int LastStage { get; private set; }
        [Networked] public int PartyCount { get; private set; }
        [Networked, Capacity(BattleState.MaxPartySize)] private NetworkArray<int> PartyIds => default;

        // 스폰 직후와 고른 내용이 바뀔 때 모든 참가자에게 불린다.
        public event Action Changed;

        public bool IsReady => Object != null && Object.IsValid;
        public bool CanEdit => IsReady && HasStateAuthority;
        public DungeonData SelectedDungeon => _progress.GetDungeon(TierIndex);

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
                // 처음에는 단계의 마지막(보스) 스테이지까지 싸우도록 고른다.
                LastStage = int.MaxValue;
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

        // 호스트 전용. 실제 흐름에서는 밤이 되면 낮·밤 사이클이 부른다.
        public void Open()
        {
            if (CanEdit)
            {
                IsOpen = true;
                ClampSelection();
            }
        }

        public void Close()
        {
            if (CanEdit)
            {
                IsOpen = false;
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

        // 호스트 전용. 고를 수 있는 단계 중 offset만큼 떨어진 단계로 옮긴다.
        public void ChangeTier(int offset)
        {
            if (!CanEdit)
            {
                return;
            }

            for (int tier = TierIndex + offset; tier >= 0 && tier < _progress.TierCount; tier += offset)
            {
                if (_progress.IsTierSelectable(tier))
                {
                    TierIndex = tier;
                    LastStage = int.MaxValue;
                    ClampSelection();
                    return;
                }
            }
        }

        public void ChangeStage(int offset)
        {
            if (CanEdit && LastStage >= 0)
            {
                LastStage = DispatchRules.ClampStage(GetClearedStages(), SelectedDungeon.StageCount, LastStage + offset);
            }
        }

        public bool CanDispatch()
        {
            return CanEdit && _loader.CanDispatch && LastStage >= 0
                && DispatchRules.IsValidPartySize(PartyCount, BattleState.MaxPartySize);
        }

        // 호스트 전용. 고른 파티로 전투 씬을 불러오고 창을 닫는다.
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
                LastStage = LastStage,
                PartyIds = partyIds,
            });

            if (isDispatched)
            {
                IsOpen = false;
            }

            return isDispatched;
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

        // 진행 상황이 바뀌어 고른 단계나 스테이지를 더는 고를 수 없으면 가까운 곳으로 옮긴다.
        private void ClampSelection()
        {
            if (!CanEdit || _progress.TierCount == 0)
            {
                return;
            }

            if (!_progress.IsTierSelectable(TierIndex))
            {
                TierIndex = FindSelectableTier();
            }

            LastStage = DispatchRules.ClampStage(GetClearedStages(), SelectedDungeon.StageCount, LastStage);
        }

        // 고를 수 있는 가장 앞 단계. 모두 클리어했으면 마지막 단계다.
        private int FindSelectableTier()
        {
            for (int tier = 0; tier < _progress.TierCount; tier++)
            {
                if (_progress.IsTierSelectable(tier))
                {
                    return tier;
                }
            }

            return _progress.TierCount - 1;
        }
    }
}
