using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 시위 걸기 수치. 모두 프로토타입 값이며 플레이 테스트 후 조정한다.
    [CreateAssetMenu(fileName = "BowStringRules", menuName = "Blacksmith/Balance/Bow String Rules")]
    public class BowStringRulesData : ScriptableObject
    {
        [Header("진행")]
        [SerializeField, Min(0f)] private float _introTime = 1f;
        [SerializeField, Min(0f)] private float _endDelay = 1f;

        [Header("감기")]
        [Tooltip("원을 그린 것으로 치는 최소 반지름(칸)")]
        [SerializeField, Min(0.1f)] private float _minRadius = 0.8f;
        [Tooltip("원의 가운데가 마우스를 따라 옮겨 가는 빠르기(초당 비율)")]
        [SerializeField, Min(0f)] private float _centerFollow = 1.2f;

        [Header("걸기")]
        [Tooltip("마우스를 아래로 한 칸 끌면 활이 휘는 정도")]
        [SerializeField, Min(0.05f)] private float _flexPerCell = 0.3f;
        [Tooltip("휜 활에서 고리를 옆으로 한 칸 밀면 고리가 활 끝으로 나아가는 정도(1이 활 끝의 홈)")]
        [SerializeField, Min(0.01f)] private float _slidePerCell = 0.11f;
        [Tooltip("너무 휘었을 때 고리가 초당 뒤로 밀리는 정도")]
        [SerializeField, Min(0f)] private float _slipRate = 0.5f;

        [Header("점수")]
        [Tooltip("정확도에서 감기 점수가 차지하는 비율(나머지는 걸기)")]
        [SerializeField, Range(0f, 1f)] private float _wrapWeight = 0.6f;
        [SerializeField, Range(0f, 1f)] private float _successScore = 0.6f;

        [Tooltip("광석 등급 0, 1, 2 순서. 감는 횟수, 좋은 감기의 빠르기 구간(rad/초), 미끄러지는 빠르기, 제한시간, 걸기 구간의 폭·오르내림 폭·기준 시간")]
        [SerializeField] private BowStringDifficulty[] _difficulties =
        {
            new BowStringDifficulty { Wraps = 3, SpeedLo = 3.5f, SpeedHi = 9f, SlipSpeed = 14f, TimeLimit = 50f, WindowWidth = 0.22f, WindowDrift = 0.06f, ParTime = 6f },
            new BowStringDifficulty { Wraps = 4, SpeedLo = 4f, SpeedHi = 8f, SlipSpeed = 13f, TimeLimit = 50f, WindowWidth = 0.17f, WindowDrift = 0.1f, ParTime = 5f },
            new BowStringDifficulty { Wraps = 5, SpeedLo = 4.5f, SpeedHi = 7.5f, SlipSpeed = 12f, TimeLimit = 50f, WindowWidth = 0.13f, WindowDrift = 0.14f, ParTime = 4.5f },
        };

        public BowStringRules ToRules()
        {
            return new BowStringRules
            {
                IntroTime = _introTime,
                EndDelay = _endDelay,
                MinRadius = _minRadius,
                CenterFollow = _centerFollow,
                FlexPerCell = _flexPerCell,
                SlidePerCell = _slidePerCell,
                SlipRate = _slipRate,
                WrapWeight = _wrapWeight,
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
                BowStringDifficulty difficulty = _difficulties[i];
                if (difficulty.Wraps <= 0 || difficulty.SpeedLo <= 0f || difficulty.SpeedHi <= difficulty.SpeedLo || difficulty.SlipSpeed <= difficulty.SpeedHi || difficulty.WindowWidth <= 0f || difficulty.TimeLimit <= 0f || difficulty.ParTime <= 0f)
                {
                    Debug.LogWarning($"난이도 {i}: 감는 횟수, 시간, 구간 폭은 0보다 크고 빠르기는 아래쪽 < 위쪽 < 미끄러짐 순서여야 한다.", this);
                }
            }
        }
    }
}
