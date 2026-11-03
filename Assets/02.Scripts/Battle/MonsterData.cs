using System.Collections.Generic;
using Blacksmith.Core.Battle;
using UnityEngine;

namespace Blacksmith.Battle
{
    [CreateAssetMenu(fileName = "Monster", menuName = "Blacksmith/Battle/Monster")]
    public class MonsterData : ScriptableObject
    {
        [SerializeField] private int _monsterId;
        [SerializeField] private string _displayName;
        [Tooltip("보스는 머리 위 체력바 대신 화면 상단 체력바로 보여 준다.")]
        [SerializeField] private bool _isBoss;
        [SerializeField] private UnitStats _stats;
        [Tooltip("마나가 찰 때마다 위에서부터 차례로 쓴다. 끝까지 쓰면 처음으로 돌아간다. 비우면 기본 공격만 한다.")]
        [SerializeField] private List<SkillData> _skills = new List<SkillData>();
        [Tooltip("체력이 HpPercent% 이하로 처음 떨어지면 한 번 강해진다. HpPercent가 0이면 분노하지 않는다.")]
        [SerializeField] private EnrageData _enrage;
        [SerializeField] private BattleUnitView _viewPrefab;

        public int MonsterId => _monsterId;
        public string DisplayName => _displayName;
        public bool IsBoss => _isBoss;
        public BattleUnitView ViewPrefab => _viewPrefab;

        public MonsterSetup ToSetup(int spawnOffset)
        {
            return new MonsterSetup
            {
                MonsterId = _monsterId,
                Stats = _stats,
                Skills = _skills.ToArray(),
                Enrage = _enrage,
                SpawnOffset = spawnOffset,
            };
        }
    }
}
