using System;

namespace Blacksmith.Core.Characters
{
    // 같은 Random 상태와 규칙이면 같은 외형이 나온다. 뽑는 순서를 바꾸면 기존 시드의 결과가 달라진다.
    public static class HumanoidAppearanceGenerator
    {
        public static HumanoidAppearance Generate(Random random, HumanoidAppearanceRules rules)
        {
            Validate(rules);
            return new HumanoidAppearance
            {
                HairStyle = random.Next(rules.HairStyleCount),
                EyeStyle = random.Next(rules.EyeStyleCount),
                MouthStyle = random.Next(rules.MouthStyleCount),
                HairColor = random.Next(rules.HairColorCount),
                SkinColor = random.Next(rules.SkinColorCount),
                Outfit = PickOutfit(random, rules),
            };
        }

        public static void Validate(HumanoidAppearanceRules rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            if (rules.HairStyleCount < 1 || rules.EyeStyleCount < 1 || rules.MouthStyleCount < 1
                || rules.HairColorCount < 1 || rules.SkinColorCount < 1 || rules.OutfitCount < 1)
            {
                throw new ArgumentException("외형 프리셋과 색은 하나 이상 있어야 한다.", nameof(rules));
            }

            foreach (int outfit in rules.OutfitChoices)
            {
                if (outfit < 0 || outfit >= rules.OutfitCount)
                {
                    throw new ArgumentException($"의상 목록에 없는 의상 번호다: {outfit}", nameof(rules));
                }
            }
        }

        private static int PickOutfit(Random random, HumanoidAppearanceRules rules)
        {
            return rules.OutfitChoices.Count > 0
                ? rules.OutfitChoices[random.Next(rules.OutfitChoices.Count)]
                : random.Next(rules.OutfitCount);
        }
    }
}
