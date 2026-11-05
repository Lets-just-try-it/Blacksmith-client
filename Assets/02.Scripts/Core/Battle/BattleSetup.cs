using System;
using System.Collections.Generic;

namespace Blacksmith.Core.Battle
{
    // 밸런스 수치는 ScriptableObject에서 읽어 넘긴다. 위치와 거리는 정수 단위로 계산해 기기마다 결과가 같게 한다.
    public sealed class BattleRules
    {
        public int TicksPerSecond { get; set; }
        // 전투가 끝나지 않는 데이터 오류를 막는 안전장치다. 넘기면 패배로 처리한다.
        public int MaxTicks { get; set; }
        public int MarchSpeed { get; set; }
        // 앞줄과 뒷줄 사이 거리다.
        public int FormationSpacing { get; set; }
        // 사거리가 이 값 이하면 근접으로 보고 앞줄에 세운다.
        public int MeleeRangeMax { get; set; }
        // 한 줄에 서는 최대 인원이다. 넘으면 같은 종류끼리 다음 줄을 만든다.
        public int MaxRowSize { get; set; }
        public int StageSpawnDistance { get; set; }
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
        public int SpawnOffset { get; set; }
    }

    public sealed class StageSetup
    {
        public bool IsBoss { get; set; }
        public List<MonsterSetup> Monsters { get; } = new List<MonsterSetup>();
    }

    // 네트워크로 보낼 수 있도록 ID와 수치만 담는다.
    public sealed class BattleSetup
    {
        public int Seed { get; set; }
        public BattleRules Rules { get; set; }
        public List<AdventurerSetup> Adventurers { get; } = new List<AdventurerSetup>();
        public List<StageSetup> Stages { get; } = new List<StageSetup>();
    }
}
