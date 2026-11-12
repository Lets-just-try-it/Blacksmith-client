using Blacksmith.Core.Battle;
using UnityEngine;

namespace Blacksmith.Battle
{
    [CreateAssetMenu(fileName = "BattleRules", menuName = "Blacksmith/Battle/Battle Rules")]
    public class BattleRulesData : ScriptableObject
    {
        [SerializeField, Min(1)] private int _ticksPerSecond = 20;
        [SerializeField, Min(1)] private int _maxTicks = 20000;
        [Tooltip("[추정] 싸울 무리가 없을 때 모험가가 맵을 걷는 속도(틱당, 1칸 = 100)")]
        [SerializeField, Min(0)] private int _marchSpeed = 40;
        [Tooltip("[추정] 걷는 동안 원거리가 근접보다 앞서 있으면 원거리는 이 비율(%)만큼 느리게, 근접은 빠르게 걸어 근접이 앞에 서게 한다")]
        [SerializeField, Range(0, 90)] private int _lineUpSpeedPercent = 25;
        [Tooltip("전투 시작 대열의 앞줄과 뒷줄 사이 거리")]
        [SerializeField, Min(0)] private int _formationSpacing = 100;
        [Tooltip("[추정] 대열에서 옆자리 사이 거리. 걸을 때도 모험가끼리 이보다 가까우면 비켜 선다")]
        [SerializeField, Min(0)] private int _laneSpacing = 160;
        [Tooltip("[추정] 사거리가 이 값 이하인 모험가는 근접으로 보고 앞줄에 세운다")]
        [SerializeField, Min(0)] private int _meleeRangeMax = 200;
        [Tooltip("[추정] 대열 한 줄의 최대 인원. 넘으면 같은 종류끼리 다음 줄을 만든다")]
        [SerializeField, Min(1)] private int _maxRowSize = 3;
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
                LineUpSpeedPercent = _lineUpSpeedPercent,
                FormationSpacing = _formationSpacing,
                LaneSpacing = _laneSpacing,
                MeleeRangeMax = _meleeRangeMax,
                MaxRowSize = _maxRowSize,
                DamageVariancePercent = _damageVariancePercent,
                DeathChancePercent = _deathChancePercent,
            };
        }
    }
}
