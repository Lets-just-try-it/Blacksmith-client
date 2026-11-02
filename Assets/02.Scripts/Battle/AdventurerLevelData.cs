using Blacksmith.Core.Adventurers;
using UnityEngine;

namespace Blacksmith.Battle
{
    // [추정] 레벨업 필요 경험치 = 기본값 + 증가분 × (현재 레벨 - 1) (docs/GAME_CONCEPT.md 6.4)
    [CreateAssetMenu(fileName = "AdventurerLevel", menuName = "Blacksmith/Balance/Adventurer Level")]
    public class AdventurerLevelData : ScriptableObject
    {
        [Tooltip("1레벨에서 2레벨로 오르는 데 필요한 경험치")]
        [SerializeField, Min(1)] private int _baseRequiredExperience = 100;
        [Tooltip("레벨이 하나 오를 때마다 필요 경험치가 늘어나는 양")]
        [SerializeField, Min(0)] private int _requiredExperiencePerLevel = 50;

        public AdventurerLevelRules ToRules()
        {
            return new AdventurerLevelRules
            {
                BaseRequiredExperience = _baseRequiredExperience,
                RequiredExperiencePerLevel = _requiredExperiencePerLevel,
            };
        }
    }
}
