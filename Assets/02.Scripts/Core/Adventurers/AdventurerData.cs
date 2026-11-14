using System;
using Blacksmith.Core.Characters;

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
        public HumanoidAppearance Appearance { get; set; }
        // 1레벨 기준 값이다. 현재 레벨의 값은 AdventurerStatRules로 구한다.
        public AdventurerStats BaseStats { get; set; }
        public AdventurerStats GrowthStats { get; set; }
        // 스킬 수치는 저장하지 않고 번호로 찾는다. 수치를 조정하면 이미 영입한 모험가에도 반영된다.
        public int SkillId { get; set; }
        // 착용 장비의 아이템 번호와 등급. 장비가 없으면 번호가 NoEquipment다.
        // Wear는 그 장비가 쓴 내구도다(ItemStack.Wear). 장신구에는 내구도가 없다.
        public int WeaponId { get; set; }
        public int WeaponGrade { get; set; }
        public int WeaponWear { get; set; }
        public int HelmetId { get; set; }
        public int HelmetGrade { get; set; }
        public int HelmetWear { get; set; }
        public int ArmorId { get; set; }
        public int ArmorGrade { get; set; }
        public int ArmorWear { get; set; }
        public int AccessoryId { get; set; }
        public int AccessoryGrade { get; set; }
        // 0이면 건강하다. 범위와 파견 제한은 InjuryRules를 따른다. 사망한 모험가는 관리 목록에서 빠지므로 저장하지 않는다.
        public int Injury { get; set; }
        public AdventurerCondition Condition => InjuryRules.GetCondition(Injury);
        // 플레이어와 쌓은 친밀도. 범위와 영입 시 값은 AffinityBalance를 따른다. (docs/GAME_CONCEPT.md 6.6)
        public int Affinity { get; set; }
        // 하루 제한을 지키기 위한 기록이다. 0이면 아직 그런 날이 없다.
        public int LastTalkDay { get; set; }
        public int AffinityGainDay { get; set; }
        // AffinityGainDay에 얻은 친밀도 증가량의 합이다.
        public int AffinityGainedToday { get; set; }
    }
}
