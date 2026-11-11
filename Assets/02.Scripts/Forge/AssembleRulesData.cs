using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 조립 수치. 모두 프로토타입 값이며 플레이 테스트 후 조정한다.
    [CreateAssetMenu(fileName = "AssembleRules", menuName = "Blacksmith/Balance/Assemble Rules")]
    public class AssembleRulesData : ScriptableObject
    {
        [Header("진행")]
        [SerializeField, Min(0f)] private float _introTime = 1f;
        [SerializeField, Min(0f)] private float _seamEndTime = 1.2f;
        [SerializeField, Min(0f)] private float _endDelay = 1f;
        [Tooltip("이음새 제한시간 = 기본 + 구멍당 시간 × 구멍 수")]
        [SerializeField, Min(1f)] private float _seamBaseTime = 38f;
        [SerializeField, Min(0f)] private float _seamTimePerHole = 8f;

        [Header("판 맞추기")]
        [SerializeField, Min(0f)] private float _stripPad = 0.9f;
        [SerializeField, Min(0f)] private float _grabPad = 0.5f;
        [Tooltip("판 맞춤 점수 = 1 - 이 값 × 오차 / 허용 오차")]
        [SerializeField, Range(0f, 1f)] private float _alignLoss = 0.5f;

        [Header("리벳 박기")]
        [SerializeField, Min(0.1f)] private float _seatRadius = 0.95f;
        [SerializeField, Min(0f)] private float _seatDelay = 0.15f;
        [SerializeField, Min(0f)] private float _ringRest = 0.25f;
        [SerializeField, Min(0f)] private float _ringOvertime = 0.1f;
        [SerializeField, Min(1)] private int _hitsNeeded = 2;
        [SerializeField, Range(0f, 1f)] private float _perfectScore = 1f;
        [SerializeField, Range(0f, 1f)] private float _goodScore = 0.75f;
        [SerializeField, Range(0f, 0.5f)] private float _missPenalty = 0.08f;
        [SerializeField, Range(0f, 0.5f)] private float _holeQualityOffset;

        [Header("점수")]
        [Tooltip("이음새 점수 = 구멍 품질 평균 × (기본 + 가중 × 판 맞춤)")]
        [SerializeField, Range(0f, 1f)] private float _alignBase = 0.75f;
        [SerializeField, Range(0f, 1f)] private float _alignWeight = 0.25f;
        [Tooltip("최종 정확도 = 이음새 평균 × (기본 + 가중 × 판 성형 점수)")]
        [SerializeField, Range(0f, 1f)] private float _plateBase = 0.92f;
        [SerializeField, Range(0f, 1f)] private float _plateWeight = 0.08f;
        [SerializeField, Range(0f, 1f)] private float _successScore = 0.6f;

        [Tooltip("난이도 0, 1, 2 순서. 어려울수록 허용 오차가 좁고 링이 빠르고 이음새와 구멍이 많다.")]
        [SerializeField] private AssembleDifficulty[] _difficulties =
        {
            new AssembleDifficulty { AlignTolerance = 0.34f, StartOffset = 1.3f, RingDuration = 0.85f, PerfectWindow = 0.07f, GoodWindow = 0.18f, HoleBonus = 0, SideHoles = 2, OptionalSeams = 0 },
            new AssembleDifficulty { AlignTolerance = 0.26f, StartOffset = 1.7f, RingDuration = 0.7f, PerfectWindow = 0.06f, GoodWindow = 0.15f, HoleBonus = 1, SideHoles = 3, OptionalSeams = 1 },
            new AssembleDifficulty { AlignTolerance = 0.18f, StartOffset = 2.1f, RingDuration = 0.6f, PerfectWindow = 0.05f, GoodWindow = 0.12f, HoleBonus = 2, SideHoles = 3, OptionalSeams = 2 },
        };

        public AssembleRules ToRules()
        {
            return new AssembleRules
            {
                IntroTime = _introTime,
                SeamEndTime = _seamEndTime,
                EndDelay = _endDelay,
                SeamBaseTime = _seamBaseTime,
                SeamTimePerHole = _seamTimePerHole,
                StripPad = _stripPad,
                GrabPad = _grabPad,
                SeatRadius = _seatRadius,
                AlignLoss = _alignLoss,
                SeatDelay = _seatDelay,
                RingRest = _ringRest,
                RingOvertime = _ringOvertime,
                HitsNeeded = _hitsNeeded,
                PerfectScore = _perfectScore,
                GoodScore = _goodScore,
                MissPenalty = _missPenalty,
                HoleQualityOffset = _holeQualityOffset,
                AlignBase = _alignBase,
                AlignWeight = _alignWeight,
                PlateBase = _plateBase,
                PlateWeight = _plateWeight,
                SuccessScore = _successScore,
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
                AssembleDifficulty difficulty = _difficulties[i];
                if (difficulty.AlignTolerance <= 0f || difficulty.RingDuration <= 0f)
                {
                    Debug.LogWarning($"난이도 {i}: 허용 오차와 링 시간이 0보다 커야 한다.", this);
                }

                if (difficulty.GoodWindow < difficulty.PerfectWindow)
                {
                    Debug.LogWarning($"난이도 {i}: 판정 범위는 완벽 <= 좋음 순서여야 한다.", this);
                }

                if (difficulty.OptionalSeams < 0 || difficulty.OptionalSeams > 2 || difficulty.SideHoles < 1)
                {
                    Debug.LogWarning($"난이도 {i}: 선택 이음새는 0~2개, 한쪽 구멍은 1개 이상이다.", this);
                }
            }
        }
    }
}
