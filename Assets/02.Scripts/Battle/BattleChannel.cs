using System;
using System.Collections.Generic;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Battle;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 호스트가 파견을 확정할 때 전투 씬에 넘기는 파견 정보다. 스테이지 번호는 0부터 센다.
    public sealed class DispatchRequest
    {
        public int Seed { get; set; }
        public int DungeonId { get; set; }
        // 파견을 확정할 때의 진행 상황. 전투는 이 값 다음 스테이지부터 싸운다.
        public int ClearedStages { get; set; }
        public int LastStage { get; set; }
        public IReadOnlyList<int> PartyIds { get; set; } = Array.Empty<int>();
    }

    public sealed class BattleOutcome
    {
        public int DungeonId { get; set; }
        public BattleResult Result { get; set; }
        public BattleRewards Rewards { get; set; }
    }

    // 낮 씬과 Additive로 불러오는 전투 씬을 잇는 에셋이다. 두 씬은 서로를 인스펙터로 참조할 수 없어 이 에셋을 거친다.
    // 낮 씬: 관리 목록 조회를 등록하고, 파견 정보를 맡기고, 결과를 받는다.
    // 전투 씬: 파견 정보로 전투를 시작하고, 열림·닫힘과 결과를 알린다.
    [CreateAssetMenu(fileName = "BattleChannel", menuName = "Blacksmith/Runtime/Battle Channel")]
    public class BattleChannel : ScriptableObject
    {
        private Func<int, AdventurerData> _findAdventurer;
        private readonly HashSet<object> _openScenes = new HashSet<object>();

        // 호스트에서만 값이 있다. 전투 씬이 시작할 때 꺼내 쓴다.
        public DispatchRequest PendingDispatch { get; private set; }
        public bool IsBattleOpen => _openScenes.Count > 0;

        // 모든 참가자에게 불린다. 낮 씬은 이때 화면을 끄고 켠다.
        public event Action BattleOpened;
        public event Action BattleClosed;
        // 호스트에서만 불린다. 전투 씬을 내리기 직전이다. 진행 상황, 골드·재료, 관리 목록 반영을 여기에 연결한다.
        public event Action<BattleOutcome> ResultConfirmed;

        // 모든 참가자가 낮 씬에서 등록한다. 전투 씬은 모험가 ID로 모험가 정보(입은 장비 포함)를 찾는다.
        public void SetAdventurerSource(Func<int, AdventurerData> findAdventurer)
        {
            _findAdventurer = findAdventurer;
        }

        public AdventurerData FindAdventurer(int adventurerId)
        {
            return _findAdventurer?.Invoke(adventurerId);
        }

        public void SetPendingDispatch(DispatchRequest request)
        {
            PendingDispatch = request;
        }

        public DispatchRequest TakePendingDispatch()
        {
            DispatchRequest request = PendingDispatch;
            PendingDispatch = null;
            return request;
        }

        public void NotifyBattleOpened(object scene)
        {
            if (_openScenes.Add(scene))
            {
                BattleOpened?.Invoke();
            }
        }

        public void NotifyBattleClosed(object scene)
        {
            if (_openScenes.Remove(scene))
            {
                BattleClosed?.Invoke();
            }
        }

        public void ConfirmResult(BattleOutcome outcome)
        {
            ResultConfirmed?.Invoke(outcome);
        }

        // 에디터에서 도메인 리로드 없이 플레이를 다시 시작하면 이전 세션의 값이 남으므로 세션 시작 때 비운다.
        // 관리 목록 조회는 낮 씬이 다시 등록하며 덮어쓰므로 비우지 않는다.
        public void Clear()
        {
            _openScenes.Clear();
            PendingDispatch = null;
        }
    }
}
