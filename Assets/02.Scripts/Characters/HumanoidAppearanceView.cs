using System.Collections.Generic;
using Blacksmith.Core.Characters;
using UnityEngine;

namespace Blacksmith.Characters
{
    // 인간형 모델 하나의 모습을 외형 번호와 착용 파츠에 맞춘다. 모델 루트(Hero)에 붙인다.
    // 모든 파츠가 한 모델 안에 같은 리그로 들어 있어서, 파츠를 켜고 끄고 피부·머리 머티리얼만 칠한다.
    [DisallowMultipleComponent]
    public class HumanoidAppearanceView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private HumanoidLookData _look;

        private readonly Dictionary<string, GameObject> _parts = new Dictionary<string, GameObject>();
        private readonly List<MaterialSlot> _skinSlots = new List<MaterialSlot>();
        private readonly List<MaterialSlot> _hairSlots = new List<MaterialSlot>();
        private readonly HashSet<string> _warnedParts = new HashSet<string>();
        private MaterialPropertyBlock _block;
        private bool _isCached;

        private readonly struct MaterialSlot
        {
            public readonly Renderer Renderer;
            public readonly int Index;

            public MaterialSlot(Renderer renderer, int index)
            {
                Renderer = renderer;
                Index = index;
            }
        }

        private void Awake()
        {
            EnsureCache();
        }

        // hatPart: 투구 칸 장비의 모델 파츠. heldPart: 손에 든 파츠. 비우면 쓰지 않거나 들지 않는다.
        public void Apply(HumanoidAppearance appearance, string hatPart = null, string heldPart = null)
        {
            EnsureCache();
            foreach (string part in _look.GetSwappableParts())
            {
                SetPart(part, false);
            }

            HumanoidLookData.Hat hat = FindHat(hatPart);
            HumanoidLookData.HairVariant variant = hat != null ? hat.HairVariant : HumanoidLookData.HairVariant.Plain;
            SetPart(_look.GetHairStyle(appearance.HairStyle)?.Get(variant), true);
            SetPart(_look.GetEyePart(appearance.EyeStyle), true);
            SetPart(_look.GetMouthPart(appearance.MouthStyle), true);
            HumanoidLookData.Outfit outfit = _look.GetOutfit(appearance.Outfit);
            if (outfit != null)
            {
                foreach (string part in outfit.Parts)
                {
                    SetPart(part, true);
                }
            }

            SetPart(hat?.Part, true);
            SetPart(heldPart, true);
            bool hideEars = hat != null && hat.HideEars;
            foreach (string ear in _look.EarParts)
            {
                SetPart(ear, !hideEars);
            }

            Paint(_skinSlots, _look.GetSkinColor(appearance.SkinColor));
            Paint(_hairSlots, _look.GetHairColor(appearance.HairColor));
        }

        private HumanoidLookData.Hat FindHat(string hatPart)
        {
            if (string.IsNullOrEmpty(hatPart))
            {
                return null;
            }

            HumanoidLookData.Hat hat = _look.FindHat(hatPart);
            if (hat == null && _warnedParts.Add(hatPart))
            {
                Debug.LogWarning($"외형 목록의 모자에 없는 파츠다: {hatPart}", this);
            }

            return hat;
        }

        private void EnsureCache()
        {
            if (_isCached)
            {
                return;
            }

            _isCached = true;
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                _parts.TryAdd(child.name, child.gameObject);
            }

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                    {
                        continue;
                    }

                    if (materials[i].name == _look.SkinMaterial)
                    {
                        _skinSlots.Add(new MaterialSlot(renderer, i));
                    }
                    else if (materials[i].name == _look.HairMaterial)
                    {
                        _hairSlots.Add(new MaterialSlot(renderer, i));
                    }
                }
            }
        }

        private void SetPart(string part, bool isActive)
        {
            if (string.IsNullOrEmpty(part))
            {
                return;
            }

            if (_parts.TryGetValue(part, out GameObject target))
            {
                target.SetActive(isActive);
            }
            else if (_warnedParts.Add(part))
            {
                Debug.LogWarning($"모델에 없는 파츠다: {part}", this);
            }
        }

        private void Paint(List<MaterialSlot> slots, Color color)
        {
            _block ??= new MaterialPropertyBlock();
            foreach (MaterialSlot slot in slots)
            {
                slot.Renderer.GetPropertyBlock(_block, slot.Index);
                _block.SetColor(BaseColorId, color);
                slot.Renderer.SetPropertyBlock(_block, slot.Index);
            }
        }
    }
}
