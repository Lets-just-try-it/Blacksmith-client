using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 활 몸체 휘기 수치. 모두 프로토타입 값이며 플레이 테스트 후 조정한다.
    [CreateAssetMenu(fileName = "BowBendRules", menuName = "Blacksmith/Balance/Bow Bend Rules")]
    public class BowBendRulesData : ScriptableObject
    {
        [Header("진행")]
        [SerializeField, Min(0f)] private float _introTime = 1f;
        [SerializeField, Min(0f)] private float _endDelay = 1f;

        [Header("휘기")]
        [Tooltip("마우스를 아래로 한 칸 끌면 휘는 정도(쇠가 가장 뜨거울 때)")]
        [SerializeField, Min(0.05f)] private float _bendPerCell = 0.3f;
        [Tooltip("다 식었을 때 끄는 거리에 곱하는 값")]
        [SerializeField, Range(0.05f, 1f)] private float _coldFactor = 0.35f;
        [Tooltip("불에 달굴 때 초당 열 회복")]
        [SerializeField, Min(0.1f)] private float _heatRate = 0.9f;
        [Tooltip("초당 열 감소(마디마다 0.7~1.3배)")]
        [SerializeField, Min(0f)] private float _coolRate = 0.05f;
        [Tooltip("놓으면 휘었던 정도가 이 비율만큼 되돌아온다")]
        [SerializeField, Range(0f, 0.5f)] private float _springBack = 0.12f;
        [Tooltip("다 식은 쇠는 되돌아오는 비율이 (1 + 이 값)배가 된다")]
        [SerializeField, Min(0f)] private float _springCold = 0.8f;
        [SerializeField, Min(0.2f)] private float _maxBend = 1.2f;
        [Tooltip("허용 폭을 넘은 오차가 이만큼이면 그 마디 점수가 0이다")]
        [SerializeField, Min(0.05f)] private float _errorScale = 0.35f;

        [Header("점수")]
        [SerializeField, Range(0f, 1f)] private float _successScore = 0.6f;

        [Tooltip("광석 등급 0, 1, 2 순서. 전체 제한시간, 맞았다고 보는 허용 폭, 목표 휘기가 흔들리는 비율")]
        [SerializeField] private BowBendDifficulty[] _difficulties =
        {
            new BowBendDifficulty { TimeLimit = 55f, Tolerance = 0.07f, Jitter = 0.1f },
            new BowBendDifficulty { TimeLimit = 50f, Tolerance = 0.055f, Jitter = 0.15f },
            new BowBendDifficulty { TimeLimit = 45f, Tolerance = 0.04f, Jitter = 0.2f },
        };

        public BowBendRules ToRules()
        {
            return new BowBendRules
            {
                IntroTime = _introTime,
                EndDelay = _endDelay,
                BendPerCell = _bendPerCell,
                ColdFactor = _coldFactor,
                HeatRate = _heatRate,
                CoolRate = _coolRate,
                SpringBack = _springBack,
                SpringCold = _springCold,
                MaxBend = _maxBend,
                ErrorScale = _errorScale,
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
                BowBendDifficulty difficulty = _difficulties[i];
                if (difficulty.TimeLimit <= 0f || difficulty.Tolerance <= 0f || difficulty.Jitter < 0f)
                {
                    Debug.LogWarning($"난이도 {i}: 시간과 허용 폭은 0보다 크고 흔들림은 0 이상이어야 한다.", this);
                }
            }
        }
    }
}
