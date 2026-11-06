using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 판 성형 수치. 모두 프로토타입 값이며 플레이 테스트 후 조정한다.
    [CreateAssetMenu(fileName = "PlateRules", menuName = "Blacksmith/Balance/Plate Rules")]
    public class PlateRulesData : ScriptableObject
    {
        [Header("진행")]
        [SerializeField, Min(0f)] private float _partPause = 0.9f;
        [SerializeField, Min(0f)] private float _endDelay = 1.2f;

        [Header("오리기")]
        [Tooltip("한 번에 따라잡을 수 있는 앞쪽 범위(외곽선 표본 수)")]
        [SerializeField, Min(1)] private int _ahead = 30;
        [Tooltip("불순물 구간에서 띠가 좁아지는 배율")]
        [SerializeField, Range(0.1f, 1f)] private float _narrowMul = 0.7f;
        [SerializeField, Min(1)] private int _narrowLen = 18;
        [Tooltip("녹색 점을 눌러 시작할 때 허용 거리 = 띠 폭 × 이 값 + 아래 여유")]
        [SerializeField, Min(1f)] private float _startReachScale = 1.8f;
        [SerializeField, Min(0f)] private float _startReachPad = 0.3f;
        [SerializeField, Range(0f, 1f)] private float _startErr = 0.3f;

        [Header("점수")]
        [SerializeField, Range(0f, 0.2f)] private float _slipPenalty = 0.02f;
        [SerializeField, Range(0f, 0.5f)] private float _maxSlipPenalty = 0.15f;
        [SerializeField, Range(0f, 0.2f)] private float _timeBonus = 0.05f;
        [Tooltip("선에서 가장 멀 때 깎이는 비율")]
        [SerializeField, Range(0f, 1f)] private float _fitLoss = 0.6f;
        [SerializeField, Range(0f, 1f)] private float _successScore = 0.6f;
        [Tooltip("띠 폭 = 기본 폭 × (아래 최소 + 폭 × 앞 단계 평균 점수)")]
        [SerializeField, Range(0.1f, 1.5f)] private float _priorMin = 0.9f;
        [SerializeField, Range(0f, 1f)] private float _priorSpan = 0.2f;

        [Tooltip("난이도 0, 1, 2 순서. 어려울수록 띠가 좁고 불순물 구간과 조각이 많고 제한시간이 빠듯하다.")]
        [SerializeField] private PlateDifficulty[] _difficulties =
        {
            new PlateDifficulty { BandWidth = 0.55f, NarrowCount = 0, ExtraParts = 0, PerimeterPerSecond = 2.2f, BaseTime = 20f },
            new PlateDifficulty { BandWidth = 0.42f, NarrowCount = 1, ExtraParts = 1, PerimeterPerSecond = 2.0f, BaseTime = 20f },
            new PlateDifficulty { BandWidth = 0.32f, NarrowCount = 2, ExtraParts = 2, PerimeterPerSecond = 1.8f, BaseTime = 20f },
        };

        public PlateRules ToRules()
        {
            return new PlateRules
            {
                PartPause = _partPause,
                EndDelay = _endDelay,
                Ahead = _ahead,
                NarrowMul = _narrowMul,
                NarrowLen = _narrowLen,
                StartReachScale = _startReachScale,
                StartReachPad = _startReachPad,
                StartErr = _startErr,
                SlipPenalty = _slipPenalty,
                MaxSlipPenalty = _maxSlipPenalty,
                TimeBonus = _timeBonus,
                FitLoss = _fitLoss,
                SuccessScore = _successScore,
                PriorMin = _priorMin,
                PriorSpan = _priorSpan,
                Difficulties = _difficulties,
            };
        }

        private void OnValidate()
        {
            if (_difficulties.Length == 0)
            {
                Debug.LogWarning("난이도가 하나 이상 있어야 한다.", this);
                return;
            }

            for (int i = 0; i < _difficulties.Length; i++)
            {
                PlateDifficulty difficulty = _difficulties[i];
                if (difficulty.BandWidth <= 0f || difficulty.PerimeterPerSecond <= 0f || difficulty.BaseTime <= 0f)
                {
                    Debug.LogWarning($"난이도 {i}: 띠 폭, 둘레 속도, 기본 시간이 0보다 커야 한다.", this);
                }

                if (difficulty.ExtraParts < 0 || difficulty.ExtraParts > 2)
                {
                    Debug.LogWarning($"난이도 {i}: 보강 조각 수는 0~2이다.", this);
                }
            }
        }
    }
}
