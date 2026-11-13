using Blacksmith.Core.Adventurers;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 모험가 부상·사망 수치다. 파견 창, 전투, 하루 휴식이 같은 값을 쓴다. (docs/GAME_CONCEPT.md 6.5)
    [CreateAssetMenu(fileName = "AdventurerInjury", menuName = "Blacksmith/Battle/Adventurer Injury")]
    public class AdventurerInjuryData : ScriptableObject
    {
        [SerializeField, Min(1)] private int _maxInjury = 100;
        [Tooltip("[추정] 부상수치가 이 값 이상이면 파견할 수 없다")]
        [SerializeField, Min(1)] private int _dispatchLimit = 50;
        [Tooltip("[추정] 전투가 끝났을 때 부상수치가 이 값 이상이면 사망 판정을 한다")]
        [SerializeField, Min(1)] private int _deathCheckInjury = 90;
        [Tooltip("[추정] 사망 판정 기준에서의 사망 확률(%)")]
        [SerializeField, Range(0, 100)] private int _baseDeathChancePercent = 10;
        [Tooltip("[추정] 사망 판정 기준을 부상 1 넘을 때마다 더하는 사망 확률(%p)")]
        [SerializeField, Min(0)] private int _deathChancePerInjury = 1;
        [Tooltip("[추정] 남은 체력이 이 비율(%) 이상이면 부상을 입지 않는다")]
        [SerializeField, Range(1, 100)] private int _uninjuredHpPercent = 70;
        [Tooltip("[추정] 쓰러지지 않았을 때 전투 한 번에 입는 최대 부상. 남은 체력이 줄수록 이 값에 가까워진다")]
        [SerializeField, Min(0)] private int _maxSurvivedInjury = 60;
        [Tooltip("[추정] 쓰러졌을 때 전투 한 번에 입는 부상")]
        [SerializeField, Min(0)] private int _fallenInjury = 90;
        [Tooltip("[추정] 파견되지 않고 하루를 보내면 줄어드는 부상")]
        [SerializeField, Min(0)] private int _dailyRecovery = 15;

        public InjuryBalance ToBalance()
        {
            return new InjuryBalance
            {
                MaxInjury = _maxInjury,
                DispatchLimit = _dispatchLimit,
                DeathCheckInjury = _deathCheckInjury,
                BaseDeathChancePercent = _baseDeathChancePercent,
                DeathChancePerInjury = _deathChancePerInjury,
                UninjuredHpPercent = _uninjuredHpPercent,
                MaxSurvivedInjury = _maxSurvivedInjury,
                FallenInjury = _fallenInjury,
                DailyRecovery = _dailyRecovery,
            };
        }
    }
}
