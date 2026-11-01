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
        public int HairPreset;
        public int EyePreset;
        public int MouthPreset;
        public int HairColor;
        public int SkinColor;
        public int BaseMaxHp;
        public int BaseAttack;
        public int BaseDefense;
        public int GrowthMaxHp;
        public int GrowthAttack;
        public int GrowthDefense;
        public int SkillId;
        public int WeaponId;
        public int HelmetId;
        public int ArmorId;
        public AdventurerCondition Condition;

        public static NetworkAdventurer FromData(AdventurerData data)
        {
            return new NetworkAdventurer
            {
                AdventurerId = data.AdventurerId,
                Name = data.Name,
                JobId = data.JobId,
                Level = data.Level,
                Experience = data.Experience,
                HairPreset = data.Appearance.HairPreset,
                EyePreset = data.Appearance.EyePreset,
                MouthPreset = data.Appearance.MouthPreset,
                HairColor = data.Appearance.HairColor,
                SkinColor = data.Appearance.SkinColor,
                BaseMaxHp = data.BaseStats.MaxHp,
                BaseAttack = data.BaseStats.Attack,
                BaseDefense = data.BaseStats.Defense,
                GrowthMaxHp = data.GrowthStats.MaxHp,
                GrowthAttack = data.GrowthStats.Attack,
                GrowthDefense = data.GrowthStats.Defense,
                SkillId = data.SkillId,
                WeaponId = data.WeaponId,
                HelmetId = data.HelmetId,
                ArmorId = data.ArmorId,
                Condition = data.Condition,
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
                Appearance = new AdventurerAppearance
                {
                    HairPreset = HairPreset,
                    EyePreset = EyePreset,
                    MouthPreset = MouthPreset,
                    HairColor = HairColor,
                    SkinColor = SkinColor,
                },
                BaseStats = new AdventurerStats { MaxHp = BaseMaxHp, Attack = BaseAttack, Defense = BaseDefense },
                GrowthStats = new AdventurerStats { MaxHp = GrowthMaxHp, Attack = GrowthAttack, Defense = GrowthDefense },
                SkillId = SkillId,
                WeaponId = WeaponId,
                HelmetId = HelmetId,
                ArmorId = ArmorId,
                Condition = Condition,
            };
        }
    }
}
