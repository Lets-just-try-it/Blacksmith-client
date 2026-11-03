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
        [SerializeField, Min(0.1f)] private float _heavyHold = 0.7f;
        [SerializeField, Min(0f)] private float _endDelay = 0.9f;
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
        [SerializeField, Min(0.1f)] private float _needScale = 7f;
        [SerializeField, Min(1)] private int _minNeed = 2;
        [SerializeField, Min(1)] private int _maxNeed = 5;
        [SerializeField, Min(0.01f)] private float _matchTolerance = 0.25f;

        [Tooltip("난이도 등급 0, 1, 2 순서. 재료 단계가 올라갈수록 어렵다.")]
        [SerializeField] private ForgingDifficulty[] _difficulties =
        {
            new ForgingDifficulty { PerfectWindow = 0.05f, GoodWindow = 0.10f, WeakWindow = 0.20f, BeatSpacing = 0.55f, HeavyChance = 0.5f },
            new ForgingDifficulty { PerfectWindow = 0.045f, GoodWindow = 0.09f, WeakWindow = 0.17f, BeatSpacing = 0.5f, HeavyChance = 0.55f },
            new ForgingDifficulty { PerfectWindow = 0.04f, GoodWindow = 0.08f, WeakWindow = 0.15f, BeatSpacing = 0.45f, HeavyChance = 0.6f },
        };

        public ForgingRules ToRules()
        {
            return new ForgingRules
            {
                SegmentCount = _segmentCount,
                LeadIn = _leadIn,
                TravelTime = _travelTime,
                HeavyHold = _heavyHold,
                EndDelay = _endDelay,
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
                NeedScale = _needScale,
                MinNeed = _minNeed,
                MaxNeed = _maxNeed,
                MatchTolerance = _matchTolerance,
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

            if (_maxNeed < _minNeed)
            {
                Debug.LogWarning("박자 수 최댓값은 최솟값 이상이어야 한다.", this);
            }
        }
    }
}
