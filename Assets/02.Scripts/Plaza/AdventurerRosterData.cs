using Blacksmith.Core.Adventurers;
using UnityEngine;

namespace Blacksmith.Plaza
{
    // 슬롯 증가(1 → 2 → 4 → 6 → 8)는 대장간 업그레이드가 AdventurerRosterState.SetSlotCount로 바꾼다.
    [CreateAssetMenu(fileName = "AdventurerRoster", menuName = "Blacksmith/Balance/Adventurer Roster")]
    public class AdventurerRosterData : ScriptableObject
    {
        [SerializeField, Range(1, AdventurerRosterState.MaxCapacity)] private int _initialSlotCount = 1;
        [Tooltip("[추정] 친밀도 수치 (docs/GAME_CONCEPT.md 6.6)")]
        [SerializeField] private AffinityBalance _affinity = new AffinityBalance
        {
            InitialAffinity = 10,
            MaxAffinity = 100,
            FriendlyAffinity = 20,
            TrustedAffinity = 50,
            SwornAffinity = 80,
            TalkGain = 5,
            InnDispatchGain = 3,
            EquipGain = 3,
            HighGradeEquipGain = 5,
            HerbGain = 3,
            PotionGain = 6,
            SurvivedBattleGain = 2,
            SevereInjuryLoss = 5,
            DailyGainLimit = 15,
        };

        public int InitialSlotCount => _initialSlotCount;
        public AffinityBalance Affinity => _affinity;
    }
}
