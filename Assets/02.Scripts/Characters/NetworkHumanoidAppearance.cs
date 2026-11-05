using Blacksmith.Core.Characters;
using Fusion;

namespace Blacksmith.Characters
{
    // HumanoidAppearance를 [Networked]에 담기 위한 형식이다. Core는 Fusion을 모르므로 필드를 풀어서 담는다.
    // HumanoidAppearance에 필드를 추가하면 여기와 두 변환 함수에도 추가한다.
    public struct NetworkHumanoidAppearance : INetworkStruct
    {
        public int HairStyle;
        public int EyeStyle;
        public int MouthStyle;
        public int HairColor;
        public int SkinColor;
        public int Outfit;

        public static NetworkHumanoidAppearance FromAppearance(HumanoidAppearance appearance)
        {
            return new NetworkHumanoidAppearance
            {
                HairStyle = appearance.HairStyle,
                EyeStyle = appearance.EyeStyle,
                MouthStyle = appearance.MouthStyle,
                HairColor = appearance.HairColor,
                SkinColor = appearance.SkinColor,
                Outfit = appearance.Outfit,
            };
        }

        public HumanoidAppearance ToAppearance()
        {
            return new HumanoidAppearance
            {
                HairStyle = HairStyle,
                EyeStyle = EyeStyle,
                MouthStyle = MouthStyle,
                HairColor = HairColor,
                SkinColor = SkinColor,
                Outfit = Outfit,
            };
        }
    }
}
