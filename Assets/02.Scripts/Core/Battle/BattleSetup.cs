using System;
using System.Collections.Generic;

namespace Blacksmith.Core.Battle
{
    // 전투 좌표. 스테이지 맵 칸 좌표에 UnitsPerCell을 곱한 정수 단위다(1칸 = 1m = 100).
    // 위치와 거리는 정수로 계산해 기기마다 결과가 같게 한다.
    public static class BattleSpace
    {
        public const int UnitsPerCell = 100;

        public static int ToUnits(float cells)
        {
            return (int)Math.Round(cells * UnitsPerCell);
        }

        // 정수 제곱근(내림). Math.Sqrt는 IEEE 754에서 정확히 반올림되므로 기기마다 같고, 경계만 보정한다.
        public static int Sqrt(long value)
        {
            if (value <= 0)
            {
                return 0;
            }

            long root = (long)Math.Sqrt(value);
            while (root * root > value)
            {
                root--;
            }

            while ((root + 1) * (root + 1) <= value)
            {
                root++;
            }

            return (int)root;
        }
    }

    // 밸런스 수치는 ScriptableObject에서 읽어 넘긴다. 거리와 속도는 BattleSpace 단위다.
    public sealed class BattleRules
    {
        public int TicksPerSecond { get; set; }
        // 전투가 끝나지 않는 데이터 오류를 막는 안전장치다. 넘기면 패배로 처리한다.
        public int MaxTicks { get; set; }
        // 싸울 무리가 없을 때 모험가가 맵을 걷는 속도(틱당)
        public int MarchSpeed { get; set; }
        // 걷는 동안 원거리가 근접보다 앞서 있으면 원거리는 이 비율(%)만큼 느리게, 근접은 빠르게 걸어 근접이 앞에 서게 한다.
        public int LineUpSpeedPercent { get; set; }
        // 앞줄과 뒷줄 사이 거리다.
        public int FormationSpacing { get; set; }
        // 대열에서 옆자리 사이 거리다. 걸을 때도 모험가끼리 이보다 가까우면 비켜 선다.
        public int LaneSpacing { get; set; }
        // 사거리가 이 값 이하면 근접으로 보고 앞줄에 세운다.
        public int MeleeRangeMax { get; set; }
        // 한 줄에 서는 최대 인원이다. 넘으면 같은 종류끼리 다음 줄을 만든다.
        public int MaxRowSize { get; set; }
        public int DamageVariancePercent { get; set; }
        public int DeathChancePercent { get; set; }
    }

    // System.Serializable이라 ScriptableObject 필드로도 쓸 수 있다.
    [Serializable]
    public struct UnitStats
    {
        public int MaxHp;
        public int Attack;
        public int Defense;
        public int AttackRange;
        public int AttackIntervalTicks;
        public int MoveSpeed;
        public int MaxMana;
        public int ManaPerTick;
        public int ManaPerHit;
    }

    public enum SkillKind
    {
        None,
        SingleDamage,
        AreaDamage,
        HealLowestAlly,
        HealAllAllies,
    }

    [Serializable]
    public struct SkillData
    {
        public SkillKind Kind;
        public int PowerPercent;
        public int Radius;
    }

    // 체력이 HpPercent% 이하로 처음 떨어질 때 한 번 강해진다. HpPercent가 0이면 분노하지 않는다.
    [Serializable]
    public struct EnrageData
    {
        public int HpPercent;
        public int AttackBonusPercent;
        public int AttackIntervalReductionPercent;

        public bool IsEnabled => HpPercent > 0;
    }

    public sealed class AdventurerSetup
    {
        public int AdventurerId { get; set; }
        public UnitStats Stats { get; set; }
        public SkillData Skill { get; set; }
        // 입은 목걸이의 효과. 루비는 전투 시뮬레이션이, 나머지는 보상 계산이 쓴다.
        public AccessoryEffect Accessory { get; set; }
    }

    public sealed class MonsterSetup
    {
        public int MonsterId { get; set; }
        public UnitStats Stats { get; set; }
        // 마나가 찰 때마다 앞에서부터 차례로 쓰고, 끝까지 쓰면 처음으로 돌아간다.
        public IReadOnlyList<SkillData> Skills { get; set; } = Array.Empty<SkillData>();
        public EnrageData Enrage { get; set; }
        // 무리 스폰 지점에서 뒤로 물러서는 거리. 사거리가 짧은 몹이 앞에 선다.
        public int SpawnOffset { get; set; }
        // 모험가가 이 거리 안에 들어오면 이 몬스터의 무리 전체가 다가온다.
        public int SightRange { get; set; }
    }

    // 스테이지의 몬스터 무리 하나. 보스 무리는 잡몹 무리를 모두 토벌한 뒤 나타난다.
    public sealed class MonsterGroupSetup
    {
        public bool IsBoss { get; set; }
        // 스테이지 맵 칸 좌표
        public MapPoint Spawn { get; set; }
        public List<MonsterSetup> Monsters { get; } = new List<MonsterSetup>();
    }

    // 네트워크로 보낼 수 있도록 ID와 수치만 담는다.
    public sealed class BattleSetup
    {
        public int Seed { get; set; }
        public BattleRules Rules { get; set; }
        public List<AdventurerSetup> Adventurers { get; } = new List<AdventurerSetup>();
        // 전투 시드로 만든 스테이지 맵. 모험가는 맵의 시작 지점에서 출발한다.
        public StageMap Map { get; set; }
        public List<MonsterGroupSetup> Groups { get; } = new List<MonsterGroupSetup>();
    }
}
