using Blacksmith.Core.Battle;
using Blacksmith.Core.Inventory;
using Blacksmith.Inventory;
using Blacksmith.Network;
using Blacksmith.Plaza;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 전투 보상(골드, 재료, 모험가 경험치·레벨)을 낮 씬의 게임 상태에 반영한다. 낮 씬에 둔다. (docs/GAME_CONCEPT.md 7.1)
    // 결과창에 보여 준 값과 같은, 호스트가 계산한 BattleRewards를 그대로 넣는다. 호스트에서만 동작한다.
    public class BattleRewardApplier : MonoBehaviour
    {
        [SerializeField] private BattleChannel _channel;
        [SerializeField] private GameState _gameState;
        [Tooltip("재료를 넣는 공용 창고")]
        [SerializeField] private SharedInventoryState _storage;
        [SerializeField] private AdventurerRosterState _roster;

        private void OnEnable()
        {
            _channel.ResultConfirmed += ApplyRewards;
        }

        private void OnDisable()
        {
            _channel.ResultConfirmed -= ApplyRewards;
        }

        private void ApplyRewards(BattleOutcome outcome)
        {
            if (!_gameState.HasStateAuthority)
            {
                return;
            }

            BattleRewards rewards = outcome.Rewards;
            _gameState.TryAddGold(rewards.Gold);
            AddMaterials(rewards);
            ApplyExperience(rewards);
        }

        // 재료는 등급이 없어 0등급으로 넣는다. 창고가 최대 크기까지 차서 다 들어가지 않는 재료는 버려진다.
        private void AddMaterials(BattleRewards rewards)
        {
            foreach (MaterialReward material in rewards.Materials)
            {
                if (!_storage.TryAdd(new ItemStack(material.MaterialId, 0, material.Count)))
                {
                    Debug.LogWarning($"공용 창고에 자리가 없어 전투 재료를 넣지 못했다: #{material.MaterialId} ×{material.Count}", this);
                }
            }
        }

        // 사망한 모험가도 경험치 0으로 들어오므로 레벨이 그대로 남는다. 관리 목록에서 빼는 것은 사망 처리가 맡는다.
        private void ApplyExperience(BattleRewards rewards)
        {
            foreach (AdventurerReward reward in rewards.Adventurers)
            {
                _roster.TrySetLevel(reward.AdventurerId, reward.Progress.Level, reward.Progress.Experience);
            }
        }
    }
}
