using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Battle;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 전투 유닛이 어느 모험가인지 인스펙터에서 확인하기 위한 표시용 정보다. 전투 계산에는 쓰지 않는다.
    public class AdventurerUnitInfo : MonoBehaviour
    {
        [SerializeField] private int _adventurerId;
        [SerializeField] private string _adventurerName;
        [SerializeField] private string _jobName;
        [SerializeField] private int _level;
        [SerializeField] private int _experience;
        [SerializeField] private AdventurerCondition _condition;
        [Tooltip("1레벨 기준 보정 전 값")]
        [SerializeField] private AdventurerStats _baseStats;
        [Tooltip("레벨당 성장값")]
        [SerializeField] private AdventurerStats _growthStats;
        [SerializeField] private int _skillId;
        [Tooltip("전투에 실제로 들어간 능력치")]
        [SerializeField] private UnitStats _battleStats;
        [SerializeField] private SkillData _battleSkill;

        public void Bind(AdventurerData adventurer, string jobName, BattleUnit unit)
        {
            _adventurerId = adventurer.AdventurerId;
            _adventurerName = adventurer.Name;
            _jobName = jobName;
            _level = adventurer.Level;
            _experience = adventurer.Experience;
            _condition = adventurer.Condition;
            _baseStats = adventurer.BaseStats;
            _growthStats = adventurer.GrowthStats;
            _skillId = adventurer.SkillId;
            _battleStats = unit.Stats;
            _battleSkill = unit.Skill;
            name = $"{adventurer.Name} Lv{adventurer.Level} (#{adventurer.AdventurerId})";
        }
    }
}
