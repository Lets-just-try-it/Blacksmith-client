using Blacksmith.Characters;
using Blacksmith.Core.Adventurers;
using Fusion;

namespace Blacksmith.Plaza
{
    // AdventurerData를 [Networked]에 담기 위한 형식이다. Core는 Fusion을 모르므로 필드를 풀어서 담는다.
    // AdventurerData에 필드를 추가하면 여기와 두 변환 함수에도 추가한다.
    public struct NetworkAdventurer : INetworkStruct
    {
        public int AdventurerId;
        // 16자를 넘는 이름은 잘린다.
        public NetworkString<_16> Name;
        public int JobId;
        public int Level;
        public int Experience;
        public NetworkHumanoidAppearance Appearance;
        public int BaseMaxHp;
        public int BaseAttack;
        public int BaseDefense;
        public int GrowthMaxHp;
        public int GrowthAttack;
        public int GrowthDefense;
        public int SkillId;
        public int WeaponId;
        public int WeaponGrade;
        public int WeaponWear;
        public int HelmetId;
        public int HelmetGrade;
        public int HelmetWear;
        public int ArmorId;
        public int ArmorGrade;
        public int ArmorWear;
        public int AccessoryId;
        public int AccessoryGrade;
        public int Injury;

        public static NetworkAdventurer FromData(AdventurerData data)
        {
            return new NetworkAdventurer
            {
                AdventurerId = data.AdventurerId,
                Name = data.Name,
                JobId = data.JobId,
                Level = data.Level,
                Experience = data.Experience,
                Appearance = NetworkHumanoidAppearance.FromAppearance(data.Appearance),
                BaseMaxHp = data.BaseStats.MaxHp,
                BaseAttack = data.BaseStats.Attack,
                BaseDefense = data.BaseStats.Defense,
                GrowthMaxHp = data.GrowthStats.MaxHp,
                GrowthAttack = data.GrowthStats.Attack,
                GrowthDefense = data.GrowthStats.Defense,
                SkillId = data.SkillId,
                WeaponId = data.WeaponId,
                WeaponGrade = data.WeaponGrade,
                WeaponWear = data.WeaponWear,
                HelmetId = data.HelmetId,
                HelmetGrade = data.HelmetGrade,
                HelmetWear = data.HelmetWear,
                ArmorId = data.ArmorId,
                ArmorGrade = data.ArmorGrade,
                ArmorWear = data.ArmorWear,
                AccessoryId = data.AccessoryId,
                AccessoryGrade = data.AccessoryGrade,
                Injury = data.Injury,
            };
        }

        public AdventurerData ToData()
        {
            return new AdventurerData
            {
                AdventurerId = AdventurerId,
                Name = Name.ToString(),
                JobId = JobId,
                Level = Level,
                Experience = Experience,
                Appearance = Appearance.ToAppearance(),
                BaseStats = new AdventurerStats { MaxHp = BaseMaxHp, Attack = BaseAttack, Defense = BaseDefense },
                GrowthStats = new AdventurerStats { MaxHp = GrowthMaxHp, Attack = GrowthAttack, Defense = GrowthDefense },
                SkillId = SkillId,
                WeaponId = WeaponId,
                WeaponGrade = WeaponGrade,
                WeaponWear = WeaponWear,
                HelmetId = HelmetId,
                HelmetGrade = HelmetGrade,
                HelmetWear = HelmetWear,
                ArmorId = ArmorId,
                ArmorGrade = ArmorGrade,
                ArmorWear = ArmorWear,
                AccessoryId = AccessoryId,
                AccessoryGrade = AccessoryGrade,
                Injury = Injury,
            };
        }
    }
}
