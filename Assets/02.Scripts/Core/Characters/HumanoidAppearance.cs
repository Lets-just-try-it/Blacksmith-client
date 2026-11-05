using System;

namespace Blacksmith.Core.Characters
{
    // 인간형 캐릭터(모험가, 플레이어, 손님)의 외형이다. 각 값은 0부터 센 번호이고,
    // 실제 모델 파츠와 색은 표시 쪽의 외형 목록(HumanoidLook)에서 번호로 찾는다.
    // 번호는 목록 위치이므로 목록에는 끝에만 추가한다. 중간을 지우면 이미 만든 캐릭터의 외형이 바뀐다.
    [Serializable]
    public struct HumanoidAppearance
    {
        public int HairStyle;
        public int EyeStyle;
        public int MouthStyle;
        public int HairColor;
        public int SkinColor;
        public int Outfit;
    }
}
