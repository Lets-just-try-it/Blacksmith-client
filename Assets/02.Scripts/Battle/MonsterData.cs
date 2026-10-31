using Blacksmith.Core.Battle;
using UnityEngine;

namespace Blacksmith.Battle
{
    [CreateAssetMenu(fileName = "Monster", menuName = "Blacksmith/Battle/Monster")]
    public class MonsterData : ScriptableObject
    {
        [SerializeField] private int _monsterId;
        [SerializeField] private string _displayName;
        [SerializeField] private UnitStats _stats;
        [SerializeField] private SkillData _skill;
        [SerializeField] private BattleUnitView _viewPrefab;

        public int MonsterId => _monsterId;
        public string DisplayName => _displayName;
        public BattleUnitView ViewPrefab => _viewPrefab;

        public MonsterSetup ToSetup(int spawnOffset)
        {
            return new MonsterSetup
            {
                MonsterId = _monsterId,
                Stats = _stats,
                Skill = _skill,
                SpawnOffset = spawnOffset,
            };
        }
    }
}
