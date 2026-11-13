using System.Collections.Generic;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Battle;
using Blacksmith.Core.Inventory;
using Blacksmith.Network;
using Blacksmith.Plaza;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 밤이 지나며 바뀌는 모험가 몸 상태를 관리 목록에 반영한다. 낮 씬에 둔다. 호스트에서만 동작한다. (docs/GAME_CONCEPT.md 6.5)
    // 파견한 모험가는 전투 결과의 부상수치를 넣고, 사망했으면 사망 기록으로 옮긴 뒤 관리 목록에서 뺀다.
    // 루비 목걸이가 사망을 막았으면 부서진 목걸이를 벗긴다.
    // 파견하지 않은 모험가는 그날 밤 쉬었으므로 하루치만큼 회복한다. 파견을 건너뛴 밤에는 모두 회복한다.
    public class AdventurerInjuryApplier : MonoBehaviour
    {
        [SerializeField] private BattleChannel _channel;
        [SerializeField] private DispatchState _dispatch;
        [SerializeField] private AdventurerRosterState _roster;
        [SerializeField] private FallenAdventurerState _fallen;
        [SerializeField] private GameState _gameState;
        [SerializeField] private AdventurerInjuryData _injury;

        // 결과를 반영할 때는 다음 날로 넘어가는 처리와 순서가 정해져 있지 않아, 전투가 열릴 때 날짜를 기억한다.
        private int _battleDay;

        private void OnEnable()
        {
            _channel.BattleOpened += RememberBattleDay;
            _channel.ResultConfirmed += ApplyBattle;
            _dispatch.Skipped += RestAll;
        }

        private void OnDisable()
        {
            _channel.BattleOpened -= RememberBattleDay;
            _channel.ResultConfirmed -= ApplyBattle;
            _dispatch.Skipped -= RestAll;
        }

        private void RememberBattleDay()
        {
            _battleDay = _gameState.Day;
        }

        private void ApplyBattle(BattleOutcome outcome)
        {
            if (!_roster.HasStateAuthority)
            {
                return;
            }

            var party = new HashSet<int>();
            foreach (AdventurerBattleResult result in outcome.Result.Adventurers)
            {
                party.Add(result.AdventurerId);
            }

            Rest(party);
            foreach (AdventurerBattleResult result in outcome.Result.Adventurers)
            {
                if (result.IsDead)
                {
                    RecordDeath(result, outcome);
                    continue;
                }

                _roster.TrySetInjury(result.AdventurerId, result.Injury);
                if (result.IsAccessoryBroken)
                {
                    _roster.TrySetEquipment(result.AdventurerId, EquipmentSlot.Accessory, ItemStack.Empty);
                }
            }
        }

        private void RecordDeath(AdventurerBattleResult result, BattleOutcome outcome)
        {
            AdventurerData adventurer = _roster.FindById(result.AdventurerId);
            if (adventurer == null)
            {
                return;
            }

            adventurer.Injury = result.Injury;
            _fallen.TryAdd(FallenAdventurer.Create(adventurer, _battleDay, outcome.DungeonId, outcome.Stage));
            _roster.TryRemove(result.AdventurerId);
        }

        private void RestAll()
        {
            if (_roster.HasStateAuthority)
            {
                Rest(null);
            }
        }

        private void Rest(HashSet<int> dispatched)
        {
            int recovery = _injury.ToBalance().DailyRecovery;
            for (int i = 0; i < _roster.Count; i++)
            {
                AdventurerData adventurer = _roster.Get(i);
                if (adventurer.Injury > 0 && (dispatched == null || !dispatched.Contains(adventurer.AdventurerId)))
                {
                    _roster.TrySetInjury(adventurer.AdventurerId, InjuryRules.Recover(adventurer.Injury, recovery));
                }
            }
        }
    }
}
