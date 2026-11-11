using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    [CreateAssetMenu(fileName = "ForgingRules", menuName = "Blacksmith/Balance/Forging Rules")]
    public class ForgingRulesData : ScriptableObject
    {
        [SerializeField, Min(1)] private int _segmentCount = 12;
        [SerializeField, Min(0f)] private float _leadIn = 2.2f;
        [SerializeField, Min(0.1f)] private float _travelTime = 1.7f;
        [SerializeField, Min(0f)] private float _endDelay = 0.9f;
        [Tooltip("입력·소리·화면이 도착하는 지연(초). 박자에 맞췄는데 '느림'으로 나오면 올리고, '빠름'으로 나오면 내린다. 판정 문구의 ms를 보고 맞춘다.")]
        [SerializeField, Range(0f, 0.3f)] private float _inputOffset = 0.06f;
        [SerializeField, Min(0f)] private float _segmentGap = 0.2f;

        [Header("판정 점수")]
        [SerializeField, Range(0f, 1f)] private float _perfectQuality = 1f;
        [SerializeField, Range(0f, 1f)] private float _goodQuality = 0.75f;
        [SerializeField, Range(0f, 1f)] private float _weakQuality = 0.4f;
        [SerializeField, Range(0f, 1f)] private float _unchargedHeavyQuality = 0.25f;

        [Header("도안")]
        [SerializeField, Range(0f, 1f)] private float _headThickness = 0.8f;
        [SerializeField, Min(0)] private int _headSegments = 2;
        [SerializeField, Range(0f, 1f)] private float _bodyThickness = 0.55f;
        [SerializeField, Range(0f, 0.5f)] private float _bodyJitter = 0.14f;
        [SerializeField, Range(0f, 1f)] private float _tipThickness = 0.3f;
        [SerializeField, Min(0)] private int _tipSegments = 3;
        [SerializeField, Min(1)] private int _minNeed = 2;

        [Tooltip("난이도 등급 0, 1, 2 순서. 재료 단계가 올라갈수록 어렵다.")]
        [SerializeField] private ForgingDifficulty[] _difficulties =
        {
            new ForgingDifficulty { PerfectWindow = 0.05f, GoodWindow = 0.10f, WeakWindow = 0.20f, BeatSpacing = 0.55f, HeavyChance = 0.45f, SpacingJitter = 0f, HeavyHoldMin = 0.45f, HeavyHoldMax = 1.5f, NeedScale = 7f, MaxNeed = 5 },
            new ForgingDifficulty { PerfectWindow = 0.035f, GoodWindow = 0.07f, WeakWindow = 0.14f, BeatSpacing = 0.40f, HeavyChance = 0.6f, SpacingJitter = 0.25f, HeavyHoldMin = 0.4f, HeavyHoldMax = 1.7f, NeedScale = 9f, MaxNeed = 6 },
            new ForgingDifficulty { PerfectWindow = 0.021f, GoodWindow = 0.042f, WeakWindow = 0.084f, BeatSpacing = 0.27f, HeavyChance = 0.75f, SpacingJitter = 0.45f, HeavyHoldMin = 0.4f, HeavyHoldMax = 1.9f, NeedScale = 12f, MaxNeed = 8 },
        };

        public ForgingRules ToRules()
        {
            return new ForgingRules
            {
                SegmentCount = _segmentCount,
                LeadIn = _leadIn,
                TravelTime = _travelTime,
                EndDelay = _endDelay,
                InputOffset = _inputOffset,
                SegmentGap = _segmentGap,
                PerfectQuality = _perfectQuality,
                GoodQuality = _goodQuality,
                WeakQuality = _weakQuality,
                UnchargedHeavyQuality = _unchargedHeavyQuality,
                HeadThickness = _headThickness,
                HeadSegments = _headSegments,
                BodyThickness = _bodyThickness,
                BodyJitter = _bodyJitter,
                TipThickness = _tipThickness,
                TipSegments = _tipSegments,
                MinNeed = _minNeed,
                Difficulties = _difficulties,
            };
        }

        private void OnValidate()
        {
            if (_difficulties.Length == 0)
            {
                Debug.LogWarning("난이도 등급이 하나 이상 있어야 한다.", this);
                return;
            }

            for (int i = 0; i < _difficulties.Length; i++)
            {
                ForgingDifficulty difficulty = _difficulties[i];
                if (difficulty.GoodWindow < difficulty.PerfectWindow || difficulty.WeakWindow < difficulty.GoodWindow)
                {
                    Debug.LogWarning($"난이도 {i}: 판정 범위는 완벽 <= 좋음 <= 약함 순서여야 한다.", this);
                }
            }

            foreach (ForgingDifficulty difficulty in _difficulties)
            {
                if (difficulty.MaxNeed < _minNeed || difficulty.HeavyHoldMax < difficulty.HeavyHoldMin)
                {
                    Debug.LogWarning("박자 수 최댓값은 최솟값 이상, 큰 박자 누르는 시간 최댓값은 최솟값 이상이어야 한다.", this);
                }
            }
        }
    }
}
