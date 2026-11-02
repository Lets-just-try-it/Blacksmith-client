using System;

namespace Blacksmith.Core.Adventurers
{
    public enum AdventurerCondition
    {
        Healthy,
        Injured,
        Dead,
    }

    // 장비·레벨 보정을 적용하기 전의 체력·공격력·방어력이다. 기본값과 레벨당 성장값에 같이 쓴다.
    [Serializable]
    public struct AdventurerStats
    {
        public int MaxHp;
        public int Attack;
        public int Defense;
    }

    // 각 값은 0부터 센 프리셋·색 번호다. 실제 모델과 색은 표시 쪽에서 번호로 찾는다.
    [Serializable]
    public struct AdventurerAppearance
    {
        public int HairPreset;
        public int EyePreset;
        public int MouthPreset;
        public int HairColor;
        public int SkinColor;
    }

    // 모험가 한 명의 원본 정보다. 광장, 파견, 전투가 모두 이 형식을 기준으로 삼는다.
    public sealed class AdventurerData
    {
        public const int NoAdventurerId = 0;
        public const int NoEquipment = 0;
        public const int NoSkill = 0;

        // 관리 목록에 들어갈 때 호스트가 발급한다. 영입 전 후보는 NoAdventurerId다.
        public int AdventurerId { get; set; }
        public string Name { get; set; }
        public int JobId { get; set; }
        public int Level { get; set; }
        // 현재 레벨에서 쌓은 경험치다. 레벨이 오르면 필요 경험치만큼 빠지고 나머지가 남는다.
        public int Experience { get; set; }
        public AdventurerAppearance Appearance { get; set; }
        // 1레벨 기준 값이다. 현재 레벨의 값은 AdventurerStatRules로 구한다.
        public AdventurerStats BaseStats { get; set; }
        public AdventurerStats GrowthStats { get; set; }
        // 스킬 수치는 저장하지 않고 번호로 찾는다. 수치를 조정하면 이미 영입한 모험가에도 반영된다.
        public int SkillId { get; set; }
        public int WeaponId { get; set; }
        public int HelmetId { get; set; }
        public int ArmorId { get; set; }
        public AdventurerCondition Condition { get; set; }
    }
}
