using System;
using System.Collections.Generic;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Battle;
using Blacksmith.Network;
using Blacksmith.Plaza;
using Fusion;
using UnityEngine;

namespace Blacksmith.Battle
{
    // BattleTest 씬을 확인하기 위한 진입점이다. 로비를 거치지 않고 참가자가 없는 호스트로 시작한다.
    // NetworkBootstrap의 Game 씬 경로를 BattleTest로 지정해 둔다.
    // 이 씬이 낮 씬 역할을 대신한다: 테스트 파티를 BattleChannel에 등록하고, 전투 씬(Battle)을 Additive로 불러와 파견한다.
    // 결과를 반영해 전투 씬이 내려가면 같은 파티로 다시 파견한다. 실제 흐름에서는 파견 창(DispatchView)이 파견한다.
    public class BattleTestStarter : MonoBehaviour
    {
        [SerializeField] private NetworkBootstrap _bootstrap;
        [SerializeField] private BattleChannel _channel;
        [SerializeField] private BattleSceneLoader _loader;
        [SerializeField] private DungeonProgressState _progress;
        [SerializeField] private DungeonData _dungeon;
        [Tooltip("파티 모험가를 광장과 같은 규칙으로 생성한다. 모든 참가자가 같은 값으로 생성하므로 파티가 같다.")]
        [SerializeField] private AdventurerGenerationData _adventurerGeneration;
        [Tooltip("파티 모험가의 생성 시드. 모험가마다 이 값에 순번을 더해 쓴다.")]
        [SerializeField] private int _partySeed;
        [Tooltip("파티 모험가별 레벨. 개수가 파티 인원이다.")]
        [SerializeField] private int[] _partyLevels = { 1, 1, 1, 1 };
        [Tooltip("어느 스테이지까지 싸울지 (1부터 센 번호)")]
        [SerializeField, Min(1)] private int _selectedStageNumber = 3;

        private readonly List<AdventurerData> _party = new List<AdventurerData>();

        private void Awake()
        {
            CreateParty();
            _channel.SetAdventurerSource(FindAdventurer);
        }

        private void OnEnable()
        {
            _loader.HostReady += Dispatch;
            _channel.BattleClosed += Dispatch;
        }

        private void OnDisable()
        {
            _loader.HostReady -= Dispatch;
            _channel.BattleClosed -= Dispatch;
        }

        private async void Start()
        {
            // 호스트가 시작되면 이 씬을 네트워크 씬으로 다시 불러온다. 그때는 이미 러너가 있으므로 다시 시작하지 않는다.
            if (NetworkRunner.Instances.Count > 0)
            {
                return;
            }

            // 같은 App ID를 쓰는 팀원과 세션이 겹치지 않게 이름을 매번 새로 만든다.
            await _bootstrap.StartHost($"BattleTest-{Guid.NewGuid():N}");
        }

        private void CreateParty()
        {
            AdventurerGenerationRules rules = _adventurerGeneration.ToRules();
            for (int i = 0; i < _partyLevels.Length; i++)
            {
                AdventurerData adventurer = AdventurerGenerator.Generate(_partySeed + i, rules);
                adventurer.AdventurerId = i + 1;
                adventurer.Level = Mathf.Max(AdventurerStatRules.MinLevel, _partyLevels[i]);
                _party.Add(adventurer);
            }
        }

        // 호스트에서만 파견된다.
        private void Dispatch()
        {
            if (!_loader.CanDispatch)
            {
                return;
            }

            int clearedStages = _progress.GetClearedStages(_dungeon.DungeonId);
            if (DungeonProgressRules.IsTierCleared(clearedStages, _dungeon.StageCount))
            {
                Debug.Log("테스트 던전을 모두 클리어해 진행 상황을 처음으로 되돌린다.");
                _progress.ResetProgress(_dungeon.DungeonId);
                clearedStages = 0;
            }

            _loader.Dispatch(new DispatchRequest
            {
                Seed = UnityEngine.Random.Range(0, int.MaxValue),
                DungeonId = _dungeon.DungeonId,
                ClearedStages = clearedStages,
                LastStage = DispatchRules.ClampStage(clearedStages, _dungeon.StageCount, _selectedStageNumber - 1),
                PartyIds = _party.ConvertAll(adventurer => adventurer.AdventurerId),
            });
        }

        private AdventurerData FindAdventurer(int adventurerId)
        {
            return _party.Find(adventurer => adventurer.AdventurerId == adventurerId);
        }
    }
}
