using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Battle;
using Blacksmith.Core.Inventory;
using Blacksmith.Plaza;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 전투에서 닳은 장비 내구도를 관리 목록에 반영한다. 낮 씬에 둔다. 호스트에서만 동작한다. (docs/GAME_CONCEPT.md 4.6)
    // 전투 중에는 부서지지 않고, 전투 뒤 내구도를 다 쓴 장비만 여기서 파괴한다. 무기가 파괴되면 직업의 기본무기로 돌아간다.
    // 사망한 모험가는 입은 장비와 함께 관리 목록에서 빠지므로 건너뛴다.
    public class EquipmentWearApplier : MonoBehaviour
    {
        [SerializeField] private BattleChannel _channel;
        [SerializeField] private AdventurerRosterState _roster;
        [Tooltip("무기가 파괴되었을 때 돌아갈 직업의 기본무기를 찾는 데 쓴다")]
        [SerializeField] private AdventurerGenerationData _jobs;

        private void OnEnable()
        {
            _channel.ResultConfirmed += ApplyBattle;
        }

        private void OnDisable()
        {
            _channel.ResultConfirmed -= ApplyBattle;
        }

        private void ApplyBattle(BattleOutcome outcome)
        {
            if (!_roster.HasStateAuthority)
            {
                return;
            }

            foreach (AdventurerBattleResult result in outcome.Result.Adventurers)
            {
                AdventurerData adventurer = result.IsDead ? null : _roster.FindById(result.AdventurerId);
                if (adventurer == null)
                {
                    continue;
                }

                Apply(adventurer, EquipmentSlot.Weapon, result.Weapon);
                Apply(adventurer, EquipmentSlot.Helmet, result.Helmet);
                Apply(adventurer, EquipmentSlot.Armor, result.Armor);
            }
        }

        private void Apply(AdventurerData adventurer, EquipmentSlot slot, EquipmentDurability durability)
        {
            ItemStack equipped = EquipmentRules.GetEquipped(adventurer, slot);
            if (!durability.HasDurability || equipped.IsEmpty)
            {
                return;
            }

            ItemStack after = durability.IsBroken
                ? GetBrokenReplacement(adventurer, slot)
                : new ItemStack(equipped.ItemId, equipped.Grade, 1, 0, durability.Wear);
            _roster.TrySetEquipment(adventurer.AdventurerId, slot, after);
        }

        private ItemStack GetBrokenReplacement(AdventurerData adventurer, EquipmentSlot slot)
        {
            return slot == EquipmentSlot.Weapon ? _jobs.GetDefaultWeapon(adventurer.JobId) : ItemStack.Empty;
        }
    }
}
