using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    [CreateAssetMenu(fileName = "SmeltingRules", menuName = "Blacksmith/Balance/Smelting Rules")]
    public class SmeltingRulesData : ScriptableObject
    {
        [Header("① 쪼개기")]
        [SerializeField, Min(0f)] private float _crackLead = 1.5f;
        [SerializeField, Min(0.1f)] private float _approach = 1f;
        [SerializeField, Min(0f)] private float _perfectWindow = 0.065f;
        [SerializeField, Min(0f)] private float _goodWindow = 0.13f;
        [SerializeField, Min(0f)] private float _weakWindow = 0.22f;
        [SerializeField, Min(0f)] private float _decoyWindow = 0.12f;
        [SerializeField, Range(0f, 1f)] private float _strayPenalty = 0.015f;
        [SerializeField, Range(0f, 1f)] private float _decoyPenalty = 0.12f;
        [SerializeField, Min(0)] private int _decoyStartIndex = 3;
        [SerializeField, Min(0f)] private float _splitDelay = 0.9f;
        [SerializeField, Min(0f)] private float _crackFirstDistance = 0.2f;
        [SerializeField, Min(0f)] private float _crackMinDistance = 0.45f;
        [SerializeField, Min(0f)] private float _crackMaxDistance = 0.95f;
        [SerializeField, Range(0.1f, 1f)] private float _crackRadiusLimit = 0.8f;
        [SerializeField, Min(0f)] private float _crackMinSeparation = 0.5f;

        [Header("② 풀무질")]
        [SerializeField, Min(1f)] private float _bellowsMax = 24f;
        [SerializeField, Range(0.01f, 0.5f)] private float _cursorHalf = 0.09f;
        [SerializeField, Min(0f)] private float _riseAccel = 1.9f;
        [SerializeField, Min(0f)] private float _fallAccel = 2.1f;
        [SerializeField, Min(0f)] private float _maxRise = 0.65f;
        [SerializeField, Min(0f)] private float _maxFall = 0.8f;
        [SerializeField, Range(0f, 1f)] private float _heatStart = 0.12f;
        [SerializeField, Min(0f)] private float _zoneHoldMin = 1f;
        [SerializeField, Min(0f)] private float _zoneHoldRange = 1.4f;
        [SerializeField, Min(0f)] private float _moltenHold = 1f;
        [SerializeField, Range(0f, 1f)] private float _timeoutFluidityBase = 0.45f;
        [SerializeField, Range(0f, 1f)] private float _timeoutFluidityHeat = 0.3f;
        [SerializeField, Min(0f)] private float _fluidityLoss = 0.055f;
        [SerializeField, Min(0f)] private float _fluidityGrace = 4.5f;
        [SerializeField, Range(0f, 1f)] private float _fluidityMin = 0.6f;

        [Header("③ 붓기")]
        [SerializeField, Min(1f)] private float _pourMax = 40f;
        [SerializeField, Min(0.01f)] private float _flowMax = 0.34f;
        [SerializeField, Range(0f, 0.9f)] private float _tiltDead = 0.12f;
        [SerializeField, Range(0f, 0.8f)] private float _tiltEmptyBonus = 0.3f;
        [SerializeField, Min(0.01f)] private float _moldEdge = 0.3f;
        [SerializeField, Min(0f)] private float _spoutLimit = 2.6f;
        [SerializeField] private float _spoutStart = 1f;
        [SerializeField] private float _moldMin = -2f;
        [SerializeField] private float _moldMax = 1f;
        [SerializeField, Min(0f)] private float _reach = 2.6f;
        [SerializeField, Min(1f)] private float _overflowEnd = 1.25f;
        [SerializeField, Range(0f, 1f)] private float _coldLimit = 0.2f;
        [SerializeField, Min(0f)] private float _heatLoss = 0.032f;
        [SerializeField, Min(0.1f)] private float _reserveBase = 1.15f;
        [SerializeField, Min(0f)] private float _reserveCrackBonus = 0.3f;
        [SerializeField, Min(0f)] private float _drossBase = 0.05f;
        [SerializeField, Min(0f)] private float _drossCrackPenalty = 0.13f;
        [SerializeField, Min(0.01f)] private float _spillZero = 0.7f;
        [SerializeField, Min(0.01f)] private float _slagZero = 0.12f;
        [SerializeField, Min(0f)] private float _endDelay = 0.9f;

        [Header("정확도 비중 (합 1)")]
        [SerializeField, Range(0f, 1f)] private float _crackWeight = 0.28f;
        [SerializeField, Range(0f, 1f)] private float _fluidityWeight = 0.12f;
        [SerializeField, Range(0f, 1f)] private float _pourWeight = 0.6f;

        [Tooltip("광석 등급 0, 1, 2 순서. 귀한 광석일수록 판정이 좁고 쇳물이 빨리 식는다.")]
        [SerializeField] private SmeltingDifficulty[] _difficulties =
        {
            new SmeltingDifficulty
            {
                WindowScale = 1.25f, HitRadius = 0.21f, GapScale = 1.15f, CrackCount = 15, DecoyChance = 0.2f, DecoyGap = 3,
                FlameHalfWidth = 0.15f, ZoneSpeed = 0.22f, ZoneHoldDivisor = 1f, HeatGain = 0.21f, HeatDrain = 0.07f,
                MoldHalfWidth = 0.5f, FillLow = 0.94f, FillHigh = 1.04f, FillTolerance = 0.4f, FlowScale = 1f, HeatLossScale = 1f,
            },
            new SmeltingDifficulty
            {
                WindowScale = 0.95f, HitRadius = 0.18f, GapScale = 0.88f, CrackCount = 18, DecoyChance = 0.28f, DecoyGap = 2,
                FlameHalfWidth = 0.12f, ZoneSpeed = 0.3f, ZoneHoldDivisor = 1.3f, HeatGain = 0.18f, HeatDrain = 0.09f,
                MoldHalfWidth = 0.38f, FillLow = 0.945f, FillHigh = 1.035f, FillTolerance = 0.34f, FlowScale = 1.1f, HeatLossScale = 1.15f,
            },
            new SmeltingDifficulty
            {
                WindowScale = 0.65f, HitRadius = 0.15f, GapScale = 0.68f, CrackCount = 21, DecoyChance = 0.36f, DecoyGap = 1,
                FlameHalfWidth = 0.095f, ZoneSpeed = 0.4f, ZoneHoldDivisor = 1.6f, HeatGain = 0.16f, HeatDrain = 0.11f,
                MoldHalfWidth = 0.28f, FillLow = 0.95f, FillHigh = 1.03f, FillTolerance = 0.28f, FlowScale = 1.2f, HeatLossScale = 1.35f,
            },
        };

        public SmeltingRules ToRules()
        {
            return new SmeltingRules
            {
                CrackLead = _crackLead,
                Approach = _approach,
                PerfectWindow = _perfectWindow,
                GoodWindow = _goodWindow,
                WeakWindow = _weakWindow,
                DecoyWindow = _decoyWindow,
                StrayPenalty = _strayPenalty,
                DecoyPenalty = _decoyPenalty,
                DecoyStartIndex = _decoyStartIndex,
                SplitDelay = _splitDelay,
                CrackFirstDistance = _crackFirstDistance,
                CrackMinDistance = _crackMinDistance,
                CrackMaxDistance = _crackMaxDistance,
                CrackRadiusLimit = _crackRadiusLimit,
                CrackMinSeparation = _crackMinSeparation,
                BellowsMax = _bellowsMax,
                CursorHalf = _cursorHalf,
                RiseAccel = _riseAccel,
                FallAccel = _fallAccel,
                MaxRise = _maxRise,
                MaxFall = _maxFall,
                HeatStart = _heatStart,
                ZoneHoldMin = _zoneHoldMin,
                ZoneHoldRange = _zoneHoldRange,
                MoltenHold = _moltenHold,
                TimeoutFluidityBase = _timeoutFluidityBase,
                TimeoutFluidityHeat = _timeoutFluidityHeat,
                FluidityLoss = _fluidityLoss,
                FluidityGrace = _fluidityGrace,
                FluidityMin = _fluidityMin,
                PourMax = _pourMax,
                FlowMax = _flowMax,
                TiltDead = _tiltDead,
                TiltEmptyBonus = _tiltEmptyBonus,
                MoldEdge = _moldEdge,
                SpoutLimit = _spoutLimit,
                SpoutStart = _spoutStart,
                MoldMin = _moldMin,
                MoldMax = _moldMax,
                Reach = _reach,
                OverflowEnd = _overflowEnd,
                ColdLimit = _coldLimit,
                HeatLoss = _heatLoss,
                ReserveBase = _reserveBase,
                ReserveCrackBonus = _reserveCrackBonus,
                DrossBase = _drossBase,
                DrossCrackPenalty = _drossCrackPenalty,
                SpillZero = _spillZero,
                SlagZero = _slagZero,
                EndDelay = _endDelay,
                CrackWeight = _crackWeight,
                FluidityWeight = _fluidityWeight,
                PourWeight = _pourWeight,
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

            if (!Mathf.Approximately(_crackWeight + _fluidityWeight + _pourWeight, 1f))
            {
                Debug.LogWarning("정확도 비중의 합은 1이어야 한다.", this);
            }

            if (_moldMin >= _moldMax)
            {
                Debug.LogWarning("틀 위치 범위의 최솟값은 최댓값보다 작아야 한다.", this);
            }

            for (int i = 0; i < _difficulties.Length; i++)
            {
                SmeltingDifficulty difficulty = _difficulties[i];
                if (difficulty.CrackCount <= 0 || difficulty.ZoneHoldDivisor <= 0f || difficulty.FillTolerance <= 0f
                    || difficulty.FillLow >= difficulty.FillHigh || 2f * difficulty.FlameHalfWidth >= 1f)
                {
                    Debug.LogWarning($"난이도 {i}: 균열 수, 머무는 시간 나눔값, 눈금 허용 거리가 0보다 크고 눈금 범위와 불꽃 띠 폭이 올바라야 한다.", this);
                }
            }
        }
    }
}
