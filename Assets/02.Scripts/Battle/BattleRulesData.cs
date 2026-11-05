using Blacksmith.Core.Battle;
using UnityEngine;
using UnityEngine.Serialization;

namespace Blacksmith.Battle
{
    [CreateAssetMenu(fileName = "BattleRules", menuName = "Blacksmith/Battle/Battle Rules")]
    public class BattleRulesData : ScriptableObject
    {
        [SerializeField, Min(1)] private int _ticksPerSecond = 20;
        [SerializeField, Min(1)] private int _maxTicks = 20000;
        [SerializeField, Min(0)] private int _marchSpeed = 10;
        [Tooltip("전투 시작 대열의 앞줄과 뒷줄 사이 거리")]
        [SerializeField, Min(0)] private int _formationSpacing = 100;
        [Tooltip("[추정] 사거리가 이 값 이하인 모험가는 근접으로 보고 앞줄에 세운다")]
        [SerializeField, Min(0)] private int _meleeRangeMax = 200;
        [Tooltip("[추정] 대열 한 줄의 최대 인원. 넘으면 같은 종류끼리 다음 줄을 만든다")]
        [SerializeField, Min(1)] private int _maxRowSize = 3;
        [Tooltip("앞 웨이브를 이기고 다음 웨이브 몬스터가 나타나는 거리")]
        [FormerlySerializedAs("_stageSpawnDistance")]
        [SerializeField, Min(0)] private int _waveSpawnDistance = 1000;
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
                MeleeRangeMax = _meleeRangeMax,
                MaxRowSize = _maxRowSize,
                WaveSpawnDistance = _waveSpawnDistance,
                DamageVariancePercent = _damageVariancePercent,
                DeathChancePercent = _deathChancePercent,
            };
        }
    }
}
