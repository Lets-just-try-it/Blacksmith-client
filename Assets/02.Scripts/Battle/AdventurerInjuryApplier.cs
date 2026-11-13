using System.Collections.Generic;
using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Battle;
using Blacksmith.Plaza;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 밤이 지나며 바뀌는 모험가 몸 상태를 관리 목록에 반영한다. 낮 씬에 둔다. 호스트에서만 동작한다. (docs/GAME_CONCEPT.md 6.5)
    // 파견한 모험가는 전투 결과의 부상수치를 넣고, 사망했으면 관리 목록에서 뺀다.
    // 파견하지 않은 모험가는 그날 밤 쉬었으므로 하루치만큼 회복한다. 파견을 건너뛴 밤에는 모두 회복한다.
    public class AdventurerInjuryApplier : MonoBehaviour
    {
        [SerializeField] private BattleChannel _channel;
        [SerializeField] private DispatchState _dispatch;
        [SerializeField] private AdventurerRosterState _roster;
        [SerializeField] private AdventurerInjuryData _injury;

        private void OnEnable()
        {
            _channel.ResultConfirmed += ApplyBattle;
            _dispatch.Skipped += RestAll;
        }

        private void OnDisable()
        {
            _channel.ResultConfirmed -= ApplyBattle;
            _dispatch.Skipped -= RestAll;
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
                    _roster.TryRemove(result.AdventurerId);
                }
                else
                {
                    _roster.TrySetInjury(result.AdventurerId, result.Injury);
                }
            }
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
