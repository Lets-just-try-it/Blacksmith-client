using System;
using System.Collections.Generic;
using Blacksmith.Core.Characters;
using UnityEngine;

namespace Blacksmith.Characters
{
    // 인간형 모델(Hero.fbx)의 외형 목록이다. 외형 번호(HumanoidAppearance)가 이 목록의 위치를 가리킨다.
    // 파츠는 모델 안의 오브젝트 이름으로 적는다. 헤어·눈·입·색·의상을 늘릴 때는 코드를 고치지 않고 목록 끝에 추가한다.
    // 중간 항목을 지우거나 순서를 바꾸면 이미 만든 캐릭터의 외형이 바뀐다.
    [CreateAssetMenu(fileName = "HumanoidLook", menuName = "Blacksmith/Characters/Humanoid Look")]
    public class HumanoidLookData : ScriptableObject
    {
        // 모자를 쓰면 머리가 모자에 눌린 버전으로 바뀐다.
        public enum HairVariant
        {
            Plain,
            Cap,
            Helmet,
        }

        [Serializable]
        public class HairStyle
        {
            public string DisplayName;
            public string Plain;
            public string Cap;
            public string Helmet;

            public string Get(HairVariant variant)
            {
                switch (variant)
                {
                    case HairVariant.Cap:
                        return Cap;
                    case HairVariant.Helmet:
                        return Helmet;
                    default:
                        return Plain;
                }
            }
        }

        [Serializable]
        public class Outfit
        {
            public string DisplayName;
            public string[] Parts = new string[0];
        }

        // 팔레트(Hero_Palette)의 행 하나. 행은 색 램프(그림자·기본·하이라이트)다.
        [Serializable]
        public class PaletteColor
        {
            public string DisplayName;
            public int Row;
        }

        // 한 부위의 색 변형. 모델은 BaseRow를 쓰고, 셰이더가 렌더러마다 BaseRow를 고른 색의 행으로 바꿔 읽는다.
        [Serializable]
        public class ColorVariants
        {
            [Tooltip("모델이 기본으로 쓰는 팔레트 행")]
            public int BaseRow;
            public PaletteColor[] Colors = new PaletteColor[0];
        }

        // 투구 칸 장비의 모델 파츠(아이템의 WornPart)가 어떤 헤어 버전을 쓰는지 정한다.
        [Serializable]
        public class Hat
        {
            public string Part;
            public HairVariant HairVariant;
            public bool HideEars;
        }

        [SerializeField] private HairStyle[] _hairStyles = new HairStyle[0];
        [SerializeField] private string[] _eyeStyles = new string[0];
        [SerializeField] private string[] _mouthStyles = new string[0];
        [SerializeField] private ColorVariants _hairColors = new ColorVariants();
        [SerializeField] private ColorVariants _skinColors = new ColorVariants();
        [Tooltip("피부색을 바꿀 파츠. 헤어 파츠는 헤어 목록에서 정해진다")]
        [SerializeField] private string[] _skinParts = new string[0];
        [SerializeField] private Outfit[] _outfits = new Outfit[0];
        [SerializeField] private Hat[] _hats = new Hat[0];
        [Tooltip("손에 드는 모든 파츠. 들고 있지 않은 파츠는 숨긴다")]
        [SerializeField] private string[] _heldParts = new string[0];
        [Tooltip("투구에 가려 숨길 귀 파츠")]
        [SerializeField] private string[] _earParts = new string[0];
        public IReadOnlyList<string> EarParts => _earParts;
        public IReadOnlyList<string> HeldParts => _heldParts;
        public IReadOnlyList<string> SkinParts => _skinParts;
        public int SkinBaseRow => _skinColors.BaseRow;
        public int HairBaseRow => _hairColors.BaseRow;

        // outfitChoices: 고를 수 있는 의상 번호. 비우면 모든 의상에서 고른다.
        public HumanoidAppearanceRules ToRules(IReadOnlyList<int> outfitChoices)
        {
            return new HumanoidAppearanceRules
            {
                HairStyleCount = _hairStyles.Length,
                EyeStyleCount = _eyeStyles.Length,
                MouthStyleCount = _mouthStyles.Length,
                HairColorCount = _hairColors.Colors.Length,
                SkinColorCount = _skinColors.Colors.Length,
                OutfitCount = _outfits.Length,
                OutfitChoices = outfitChoices ?? Array.Empty<int>(),
            };
        }

        public HairStyle GetHairStyle(int index) => Pick(_hairStyles, index);
        public string GetEyePart(int index) => Pick(_eyeStyles, index);
        public string GetMouthPart(int index) => Pick(_mouthStyles, index);
        public int GetHairRow(int index) => Pick(_hairColors.Colors, index)?.Row ?? _hairColors.BaseRow;
        public int GetSkinRow(int index) => Pick(_skinColors.Colors, index)?.Row ?? _skinColors.BaseRow;
        public Outfit GetOutfit(int index) => Pick(_outfits, index);

        // 목록에 없는 파츠면 null. 모자가 아닌 파츠를 투구 칸에 적은 경우다.
        public Hat FindHat(string part)
        {
            foreach (Hat hat in _hats)
            {
                if (hat.Part == part)
                {
                    return hat;
                }
            }

            return null;
        }

        // 모든 헤어 파츠(모자에 따른 버전 포함). 머리색을 바꿀 파츠다.
        public IEnumerable<string> GetHairParts()
        {
            foreach (HairStyle hair in _hairStyles)
            {
                yield return hair.Plain;
                yield return hair.Cap;
                yield return hair.Helmet;
            }
        }

        // 외형이 켜고 끄는 모든 파츠. 몸처럼 항상 보이는 파츠는 들어 있지 않다.
        public IEnumerable<string> GetSwappableParts()
        {
            foreach (string hair in GetHairParts())
            {
                yield return hair;
            }

            foreach (string eye in _eyeStyles)
            {
                yield return eye;
            }

            foreach (string mouth in _mouthStyles)
            {
                yield return mouth;
            }

            foreach (Outfit outfit in _outfits)
            {
                foreach (string part in outfit.Parts)
                {
                    yield return part;
                }
            }

            foreach (Hat hat in _hats)
            {
                yield return hat.Part;
            }

            foreach (string held in _heldParts)
            {
                yield return held;
            }
        }

        // 목록이 줄어 번호가 범위를 벗어나면 마지막 항목을 쓴다.
        private T Pick<T>(T[] list, int index)
        {
            if (list.Length == 0)
            {
                return default;
            }

            if (index < 0 || index >= list.Length)
            {
                Debug.LogWarning($"외형 목록에 없는 번호다: {index} (목록 {list.Length}개)", this);
                return list[Mathf.Clamp(index, 0, list.Length - 1)];
            }

            return list[index];
        }

        private void OnValidate()
        {
            if (_hairStyles.Length == 0 || _eyeStyles.Length == 0 || _mouthStyles.Length == 0
                || _hairColors.Colors.Length == 0 || _skinColors.Colors.Length == 0 || _outfits.Length == 0)
            {
                Debug.LogWarning("헤어·눈·입·머리색·피부색·의상은 하나 이상 있어야 한다.", this);
            }
        }
    }
}
