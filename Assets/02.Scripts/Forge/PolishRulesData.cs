using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    [CreateAssetMenu(fileName = "PolishRules", menuName = "Blacksmith/Balance/Polish Rules")]
    public class PolishRulesData : ScriptableObject
    {
        [SerializeField, Min(0f)] private float _introTime = 0.9f;
        [SerializeField, Min(0f)] private float _endDelay = 0.8f;

        [Header("갈기")]
        [Tooltip("한 칸의 날이 이만큼 서면 다 갈린 것이다.")]
        [SerializeField, Range(0.1f, 1f)] private float _segmentGoal = 0.9f;
        [Tooltip("각도가 알맞을 때 날이 초당 서는 양")]
        [SerializeField, Min(0.01f)] private float _grindRate = 0.5f;
        [SerializeField, Range(0.1f, 1f)] private float _hardRate = 0.55f;
        [Tooltip("숫돌에 닿는 폭의 절반(날 전체가 -1~1)")]
        [SerializeField, Min(0.01f)] private float _contactRadius = 0.07f;

        [Header("알맞은 각도")]
        [SerializeField, Min(0f)] private float _angleAmplitude = 0.12f;
        [SerializeField, Min(0.01f)] private float _angleSpeedMin = 0.6f;
        [SerializeField, Min(0.01f)] private float _angleSpeedMax = 1.1f;

        [Header("열")]
        [SerializeField, Min(0.01f)] private float _heatUp = 0.3f;
        [Tooltip("각도가 어긋날수록 열이 더 오르는 배율")]
        [SerializeField, Min(0f)] private float _offAngleHeat = 1.2f;
        [SerializeField, Min(0f)] private float _heatCool = 0.12f;
        [SerializeField, Min(0.01f)] private float _dipCool = 0.7f;
        [SerializeField, Min(0.1f)] private float _burnHeat = 1f;
        [SerializeField, Min(0f)] private float _resumeHeat = 0.5f;
        [SerializeField, Min(0f)] private float _warnHeat = 0.7f;
        [SerializeField, Range(0f, 1f)] private float _burnPenalty = 0.07f;
        [SerializeField, Range(0f, 1f)] private float _burnSharpLoss = 0.25f;

        [Header("점수")]
        [Tooltip("다 간 곳을 계속 갈 때 초당 깎이는 점수")]
        [SerializeField, Min(0f)] private float _thinLossPerSecond = 0.04f;
        [SerializeField, Range(0f, 1f)] private float _maxThin = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _initialMin = 0.05f;
        [SerializeField, Range(0f, 1f)] private float _initialMax = 0.45f;
        [SerializeField, Range(0f, 1f)] private float _priorThreshold = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _priorDrop = 0.15f;
        [SerializeField, Min(0f)] private float _comboBonusPer = 0.008f;
        [SerializeField, Min(0f)] private float _comboBonusMax = 0.06f;
        [SerializeField, Range(0f, 1f)] private float _successScore = 0.6f;

        [Tooltip("광석 등급 0, 1, 2 순서. 귀한 광석일수록 알맞은 각도 폭과 제한시간이 빠듯하고 열이 빨리 오르며 단단한 칸이 많다.")]
        [SerializeField] private PolishDifficulty[] _difficulties =
        {
            new PolishDifficulty { AngleTolerance = 0.07f, TimeLimit = 40f, HeatScale = 1f, HardChance = 0.1f },
            new PolishDifficulty { AngleTolerance = 0.06f, TimeLimit = 34f, HeatScale = 1.15f, HardChance = 0.25f },
            new PolishDifficulty { AngleTolerance = 0.05f, TimeLimit = 30f, HeatScale = 1.3f, HardChance = 0.4f },
        };

        public PolishRules ToRules()
        {
            return new PolishRules
            {
                IntroTime = _introTime,
                EndDelay = _endDelay,
                SegmentGoal = _segmentGoal,
                GrindRate = _grindRate,
                HardRate = _hardRate,
                ContactRadius = _contactRadius,
                AngleAmplitude = _angleAmplitude,
                AngleSpeedMin = _angleSpeedMin,
                AngleSpeedMax = _angleSpeedMax,
                HeatUp = _heatUp,
                OffAngleHeat = _offAngleHeat,
                HeatCool = _heatCool,
                DipCool = _dipCool,
                BurnHeat = _burnHeat,
                ResumeHeat = _resumeHeat,
                WarnHeat = _warnHeat,
                BurnPenalty = _burnPenalty,
                BurnSharpLoss = _burnSharpLoss,
                ThinLossPerSecond = _thinLossPerSecond,
                MaxThin = _maxThin,
                InitialMin = _initialMin,
                InitialMax = _initialMax,
                PriorThreshold = _priorThreshold,
                PriorDrop = _priorDrop,
                ComboBonusPer = _comboBonusPer,
                ComboBonusMax = _comboBonusMax,
                SuccessScore = _successScore,
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

            if (_angleSpeedMin > _angleSpeedMax || _resumeHeat >= _burnHeat || _warnHeat >= _burnHeat)
            {
                Debug.LogWarning("각도 흔들림 속도의 아래가 위보다 작아야 하고, 재개 열과 경고 열은 타는 열보다 낮아야 한다.", this);
            }

            for (int i = 0; i < _difficulties.Length; i++)
            {
                PolishDifficulty difficulty = _difficulties[i];
                if (difficulty.AngleTolerance <= 0f || difficulty.TimeLimit <= 0f || difficulty.HeatScale <= 0f)
                {
                    Debug.LogWarning($"난이도 {i}: 각도 허용 폭, 제한시간, 열 배율이 0보다 커야 한다.", this);
                }
            }
        }
    }
}
