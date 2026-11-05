using System;
using System.Collections.Generic;

namespace Blacksmith.Core.Characters
{
    // 외형을 무작위로 정할 때 고를 수 있는 범위다. 개수는 외형 목록 데이터의 길이에서 나온다.
    public sealed class HumanoidAppearanceRules
    {
        public int HairStyleCount { get; set; }
        public int EyeStyleCount { get; set; }
        public int MouthStyleCount { get; set; }
        public int HairColorCount { get; set; }
        public int SkinColorCount { get; set; }
        public int OutfitCount { get; set; }
        // 이 중에서만 의상을 고른다. 비어 있으면 모든 의상에서 고른다.
        public IReadOnlyList<int> OutfitChoices { get; set; } = Array.Empty<int>();
    }
}
