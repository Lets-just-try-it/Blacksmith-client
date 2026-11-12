using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 시험 사격 수치. 모두 프로토타입 값이며 플레이 테스트 후 조정한다.
    [CreateAssetMenu(fileName = "BowRangeRules", menuName = "Blacksmith/Balance/Bow Range Rules")]
    public class BowRangeRulesData : ScriptableObject
    {
        [Header("진행")]
        [SerializeField, Min(0f)] private float _introTime = 1f;
        [Tooltip("한 발을 쏜 뒤 결과를 보여 주는 시간(화살이 날아가는 시간 포함)")]
        [SerializeField, Min(0f)] private float _resultTime = 1.0f;
        [SerializeField, Min(0f)] private float _endDelay = 1f;

        [Header("활쏘기")]
        [Tooltip("화살이 날아가는 시간(초). 결과 시간 안에 포함된다")]
        [SerializeField, Min(0.1f)] private float _flightTime = 0.6f;
        [Tooltip("숨을 참을 수 있는 시간(초). 0이면 숨 참기를 쓰지 않는다")]
        [SerializeField, Min(0f)] private float _breathTime = 2.2f;
        [Tooltip("숨을 참는 동안 손 떨림에 곱하는 값")]
        [SerializeField, Range(0f, 1f)] private float _breathSteady = 0.25f;
        [Tooltip("숨이 다해 헐떡일 때 손 떨림에 곱하는 값")]
        [SerializeField, Min(1f)] private float _gaspSway = 1.8f;
        [Tooltip("숨이 가득 차오르는 데 걸리는 시간(초)")]
        [SerializeField, Min(0.1f)] private float _breathRecover = 3f;
        [Tooltip("시위를 끝까지 당기는 데 걸리는 시간(초). 덜 당기고 쏘면 화살이 처진다.")]
        [SerializeField, Min(0.1f)] private float _drawTime = 0.8f;
        [Tooltip("다 당긴 뒤 이 시간(초)이 지나면 팔이 떨린다")]
        [SerializeField, Min(0f)] private float _fatigueStart = 0.6f;
        [SerializeField, Min(0f)] private float _swayGrowth = 0.6f;
        [SerializeField, Min(0.1f)] private float _swayFrequency = 2.2f;
        [Tooltip("시위를 하나도 안 당겼을 때 화살이 더 처지는 거리(칸)")]
        [SerializeField, Min(0f)] private float _dropScale = 3f;

        [Header("거리와 바람")]
        [Tooltip("기준 거리(m). 허수아비 크기, 바람, 중력 영향은 이 거리를 기준으로 정한다")]
        [SerializeField, Min(5f)] private float _refDistance = 15f;
        [Tooltip("기준 거리에서 바람 1단위가 화살을 옆으로 미는 거리(칸). 거리에 비례한다")]
        [SerializeField, Min(0f)] private float _windScale = 0.8f;
        [Tooltip("기준 거리에서 중력으로 처지는 거리(칸). 거리의 제곱에 비례한다")]
        [SerializeField, Min(0f)] private float _gravityDrop = 0.4f;
        [Tooltip("허수아비 가슴이 놓일 수 있는 범위의 절반 폭(칸)")]
        [SerializeField, Min(0f)] private float _rangeX = 4f;
        [SerializeField, Min(0f)] private float _rangeY = 0.8f;
        [Tooltip("허수아비 가슴 가운데의 기본 세로 위치(칸, 아래가 +)")]
        [SerializeField] private float _centerY = 1.2f;

        [Header("점수")]
        [SerializeField, Range(0f, 1f)] private float _successScore = 0.6f;

        [Tooltip("광석 등급 0, 1, 2 순서. 귀한 광석일수록 목표 명중 수가 많고 허수아비가 작고 바람이 세고 허수아비가 움직이고 손이 많이 떨린다.")]
        [SerializeField] private BowRangeDifficulty[] _difficulties =
        {
            new BowRangeDifficulty { GoalHits = 12, TimeLimit = 45f, TargetRadius = 0.8f, Wind = 0.5f, TargetMove = 0f, TargetSpeed = 0f, Sway = 0.2f, DistanceMin = 8f, DistanceMax = 14f },
            new BowRangeDifficulty { GoalHits = 16, TimeLimit = 45f, TargetRadius = 0.7f, Wind = 1f, TargetMove = 0.8f, TargetSpeed = 0.6f, Sway = 0.28f, DistanceMin = 10f, DistanceMax = 18f },
            new BowRangeDifficulty { GoalHits = 20, TimeLimit = 45f, TargetRadius = 0.6f, Wind = 1.5f, TargetMove = 1.4f, TargetSpeed = 0.9f, Sway = 0.4f, DistanceMin = 12f, DistanceMax = 22f },
        };

        public BowRangeRules ToRules()
        {
            return new BowRangeRules
            {
                IntroTime = _introTime,
                ResultTime = _resultTime,
                EndDelay = _endDelay,
                FlightTime = _flightTime,
                BreathTime = _breathTime,
                BreathSteady = _breathSteady,
                GaspSway = _gaspSway,
                BreathRecover = _breathRecover,
                DrawTime = _drawTime,
                FatigueStart = _fatigueStart,
                SwayGrowth = _swayGrowth,
                SwayFrequency = _swayFrequency,
                DropScale = _dropScale,
                RefDistance = _refDistance,
                WindScale = _windScale,
                GravityDrop = _gravityDrop,
                CenterY = _centerY,
                RangeX = _rangeX,
                RangeY = _rangeY,
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

            for (int i = 0; i < _difficulties.Length; i++)
            {
                BowRangeDifficulty difficulty = _difficulties[i];
                if (difficulty.GoalHits <= 0 || difficulty.TimeLimit <= 0f || difficulty.TargetRadius <= 0f || difficulty.DistanceMin <= 0f || difficulty.DistanceMin > difficulty.DistanceMax)
                {
                    Debug.LogWarning($"난이도 {i}: 목표 명중 수, 시간, 허수아비 반지름, 거리는 0보다 크고 최소 거리는 최대 거리보다 짧아야 한다.", this);
                }
            }
        }
    }
}
