using Blacksmith.Core.Battle;
using UnityEngine;

namespace Blacksmith.Battle
{
    [CreateAssetMenu(fileName = "BattleRules", menuName = "Blacksmith/Battle/Battle Rules")]
    public class BattleRulesData : ScriptableObject
    {
        [SerializeField, Min(1)] private int _ticksPerSecond = 20;
        [SerializeField, Min(1)] private int _maxTicks = 20000;
        [SerializeField, Min(0)] private int _marchSpeed = 10;
        [SerializeField, Min(0)] private int _formationSpacing = 100;
        [SerializeField, Min(0)] private int _stageSpawnDistance = 1000;
        [SerializeField, Range(0, 100)] private int _damageVariancePercent = 10;
        [SerializeField, Range(0, 100)] private int _deathChancePercent = 10;

        public int TicksPerSecond => _ticksPerSecond;

        public BattleRules ToRules()
        {
            return new BattleRules
            {
                TicksPerSecond = _ticksPerSecond,
                MaxTicks = _maxTicks,
                MarchSpeed = _marchSpeed,
                FormationSpacing = _formationSpacing,
                StageSpawnDistance = _stageSpawnDistance,
                DamageVariancePercent = _damageVariancePercent,
                DeathChancePercent = _deathChancePercent,
            };
        }
    }
}
