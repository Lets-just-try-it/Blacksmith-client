using UnityEngine;

namespace Blacksmith.Plaza
{
    // 슬롯 증가(1 → 2 → 4 → 6 → 8)는 대장간 업그레이드가 AdventurerRosterState.SetSlotCount로 바꾼다.
    [CreateAssetMenu(fileName = "AdventurerRoster", menuName = "Blacksmith/Balance/Adventurer Roster")]
    public class AdventurerRosterData : ScriptableObject
    {
        [SerializeField, Range(1, AdventurerRosterState.MaxCapacity)] private int _initialSlotCount = 1;

        public int InitialSlotCount => _initialSlotCount;
    }
}
