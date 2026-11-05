using System;
using Blacksmith.Core.Characters;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class HumanoidAppearanceGeneratorTests
    {
        private static HumanoidAppearanceRules CreateRules()
        {
            return new HumanoidAppearanceRules
            {
                HairStyleCount = 4,
                EyeStyleCount = 3,
                MouthStyleCount = 3,
                HairColorCount = 6,
                SkinColorCount = 5,
                OutfitCount = 2,
            };
        }

        [Test]
        public void Generate_SameSeed_ReturnsSameAppearance()
        {
            HumanoidAppearanceRules rules = CreateRules();
            for (int seed = 0; seed < 50; seed++)
            {
                Assert.AreEqual(
                    HumanoidAppearanceGenerator.Generate(new Random(seed), rules),
                    HumanoidAppearanceGenerator.Generate(new Random(seed), rules));
            }
        }

        [Test]
        public void Generate_ValuesStayInRangesAndCoverEveryOutfit()
        {
            HumanoidAppearanceRules rules = CreateRules();
            var outfits = new bool[rules.OutfitCount];
            var random = new Random(1);
            for (int i = 0; i < 300; i++)
            {
                HumanoidAppearance appearance = HumanoidAppearanceGenerator.Generate(random, rules);
                Assert.That(appearance.HairStyle, Is.InRange(0, rules.HairStyleCount - 1));
                Assert.That(appearance.EyeStyle, Is.InRange(0, rules.EyeStyleCount - 1));
                Assert.That(appearance.MouthStyle, Is.InRange(0, rules.MouthStyleCount - 1));
                Assert.That(appearance.HairColor, Is.InRange(0, rules.HairColorCount - 1));
                Assert.That(appearance.SkinColor, Is.InRange(0, rules.SkinColorCount - 1));
                outfits[appearance.Outfit] = true;
            }

            Assert.That(outfits, Is.All.True);
        }

        [Test]
        public void Generate_WithOutfitChoices_PicksOnlyThoseOutfits()
        {
            HumanoidAppearanceRules rules = CreateRules();
            rules.OutfitChoices = new[] { 1 };
            var random = new Random(5);
            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(1, HumanoidAppearanceGenerator.Generate(random, rules).Outfit);
            }
        }

        [Test]
        public void Generate_EmptyList_Throws()
        {
            HumanoidAppearanceRules rules = CreateRules();
            rules.EyeStyleCount = 0;

            Assert.Throws<ArgumentException>(() => HumanoidAppearanceGenerator.Generate(new Random(1), rules));
        }

        [Test]
        public void Generate_OutfitChoiceOutOfRange_Throws()
        {
            HumanoidAppearanceRules rules = CreateRules();
            rules.OutfitChoices = new[] { 2 };

            Assert.Throws<ArgumentException>(() => HumanoidAppearanceGenerator.Generate(new Random(1), rules));
        }
    }
}
