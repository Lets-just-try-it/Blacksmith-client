using System.Collections.Generic;
using Blacksmith.Core.Characters;
using UnityEngine;

namespace Blacksmith.Characters
{
    // 인간형 모델 하나의 모습을 외형 번호와 착용 파츠에 맞춘다. 모델 루트(Hero)에 붙인다.
    // 모든 파츠가 한 모델 안에 같은 리그로 들어 있어서, 파츠를 켜고 끄고 피부·머리 파츠의 팔레트 행만 바꾼다.
    [DisallowMultipleComponent]
    public class HumanoidAppearanceView : MonoBehaviour
    {
        // 팔레트 툰 셰이더(HumanoidPaletteToon)의 행 교체. 피부와 헤어는 렌더러가 겹치지 않아 첫 번째 쌍만 쓴다.
        private static readonly int SwapFromId = Shader.PropertyToID("_Swap0From");
        private static readonly int SwapToId = Shader.PropertyToID("_Swap0To");

        [SerializeField] private HumanoidLookData _look;

        private readonly Dictionary<string, GameObject> _parts = new Dictionary<string, GameObject>();
        private readonly List<Renderer> _skinRenderers = new List<Renderer>();
        private readonly List<Renderer> _hairRenderers = new List<Renderer>();
        private readonly HashSet<string> _warnedParts = new HashSet<string>();
        private MaterialPropertyBlock _block;
        private bool _isCached;

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

            SwapRow(_skinRenderers, _look.SkinBaseRow, _look.GetSkinRow(appearance.SkinColor));
            SwapRow(_hairRenderers, _look.HairBaseRow, _look.GetHairRow(appearance.HairColor));
        }

        // 외형은 그대로 두고 손에 든 파츠만 바꾼다. 비우면 아무것도 들지 않는다.
        public void SetHeldPart(string heldPart)
        {
            EnsureCache();
            foreach (string part in _look.HeldParts)
            {
                SetPart(part, false);
            }

            SetPart(heldPart, true);
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

            CollectRenderers(_look.SkinParts, _skinRenderers);
            CollectRenderers(_look.GetHairParts(), _hairRenderers);
        }

        private void CollectRenderers(IEnumerable<string> parts, List<Renderer> renderers)
        {
            foreach (string part in parts)
            {
                if (TryFindPart(part, out GameObject target))
                {
                    renderers.AddRange(target.GetComponents<Renderer>());
                }
            }
        }

        private void SetPart(string part, bool isActive)
        {
            if (TryFindPart(part, out GameObject target))
            {
                target.SetActive(isActive);
            }
        }

        // 비어 있는 파츠 이름은 "없음"이라 경고하지 않는다.
        private bool TryFindPart(string part, out GameObject target)
        {
            target = null;
            if (string.IsNullOrEmpty(part))
            {
                return false;
            }

            if (_parts.TryGetValue(part, out target))
            {
                return true;
            }

            if (_warnedParts.Add(part))
            {
                Debug.LogWarning($"모델에 없는 파츠다: {part}", this);
            }

            return false;
        }

        private void SwapRow(List<Renderer> renderers, int fromRow, int toRow)
        {
            _block ??= new MaterialPropertyBlock();
            foreach (Renderer renderer in renderers)
            {
                renderer.GetPropertyBlock(_block);
                _block.SetFloat(SwapFromId, fromRow);
                _block.SetFloat(SwapToId, toRow);
                renderer.SetPropertyBlock(_block);
            }
        }
    }
}
