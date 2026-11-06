using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 장신구 제작(체인 잇기, 보석 깎기, 장착, 광내기) 수치. 모두 프로토타입 값이며 플레이 테스트 후 조정한다.
    [CreateAssetMenu(fileName = "NecklaceRules", menuName = "Blacksmith/Balance/Necklace Rules")]
    public class NecklaceRulesData : ScriptableObject
    {
        [Header("진행")]
        [SerializeField, Min(0f)] private float _introTime = 0.6f;
        [SerializeField, Min(0f)] private float _stepDoneTime = 0.9f;
        [SerializeField, Min(0f)] private float _endTime = 1.2f;

        [Header("보석 깎기·장착")]
        [Tooltip("보석 반지름이 실제로 몇 cm인지. 두께 표시와 허용 오차를 cm로 말하기 위한 눈금이다.")]
        [SerializeField, Min(1f)] private float _gemCm = 12f;
        [Tooltip("거친 숫돌이 면을 가는 속도(보석 반지름 1 기준, 초당)")]
        [SerializeField, Min(0.01f)] private float _coarseRate = 0.24f;
        [Tooltip("고운 숫돌이 면을 가는 속도. 어려워지는 것은 속도가 아니라 멈춰야 하는 허용 오차다.")]
        [SerializeField, Min(0.01f)] private float _fineRate = 0.08f;
        [Tooltip("고운 숫돌로 갈 때 면의 거칠기가 줄어드는 속도(초당)")]
        [SerializeField, Min(0f)] private float _polishSmoothRate = 0.7f;
        [SerializeField, Min(0.1f)] private float _seatTime = 0.7f;
        [Tooltip("깎기 제한시간 = (기본 + 면 수 × 면당) × 난이도 배율")]
        [SerializeField, Min(1f)] private float _cutBaseTime = 20f;
        [SerializeField, Min(0f)] private float _cutTimePerFacet = 6f;

        [Header("점수")]
        [Tooltip("체인·깎기·장착·광내기 점수의 비중. 합이 1이 되게 둔다.")]
        [SerializeField, Range(0f, 1f)] private float _chainWeight = 0.3f;
        [SerializeField, Range(0f, 1f)] private float _cutWeight = 0.3f;
        [SerializeField, Range(0f, 1f)] private float _setWeight = 0.15f;
        [SerializeField, Range(0f, 1f)] private float _polishWeight = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _successScore = 0.6f;

        [Tooltip("난이도 0, 1, 2 순서. 어려울수록 조각이 많고 면이 많으며 허용 오차가 좁고 제한시간이 빠듯하다.")]
        [SerializeField] private NecklaceDifficulty[] _difficulties =
        {
            new NecklaceDifficulty { ChainBaseTime = 10f, ChainTimePerPiece = 4.5f, CutTolCm = 0.08f, CutMatchSpan = 0.14f, CutTimeMul = 1f, FacetCount = 4, FacetSpread = 0.03f, SetPosTol = 0.26f, SetLimit = 30f, PolishBaseTime = 10f, PolishTimePerFacet = 3f, PolishNeedMul = 1f },
            new NecklaceDifficulty { ChainBaseTime = 9f, ChainTimePerPiece = 4f, CutTolCm = 0.05f, CutMatchSpan = 0.10f, CutTimeMul = 0.95f, FacetCount = 6, FacetSpread = 0.05f, SetPosTol = 0.19f, SetLimit = 30f, PolishBaseTime = 9f, PolishTimePerFacet = 2.8f, PolishNeedMul = 1.25f },
            new NecklaceDifficulty { ChainBaseTime = 8f, ChainTimePerPiece = 3.5f, CutTolCm = 0.03f, CutMatchSpan = 0.07f, CutTimeMul = 0.9f, FacetCount = 8, FacetSpread = 0.07f, SetPosTol = 0.15f, SetLimit = 28f, PolishBaseTime = 8f, PolishTimePerFacet = 2.6f, PolishNeedMul = 1.5f },
        };

        public NecklaceRules ToRules()
        {
            return new NecklaceRules
            {
                IntroTime = _introTime,
                StepDoneTime = _stepDoneTime,
                EndTime = _endTime,
                GemCm = _gemCm,
                CoarseRate = _coarseRate,
                FineRate = _fineRate,
                PolishSmoothRate = _polishSmoothRate,
                SeatTime = _seatTime,
                CutBaseTime = _cutBaseTime,
                CutTimePerFacet = _cutTimePerFacet,
                ChainWeight = _chainWeight,
                CutWeight = _cutWeight,
                SetWeight = _setWeight,
                PolishWeight = _polishWeight,
                SuccessScore = _successScore,
                Difficulties = _difficulties,
            };
        }

        private void OnValidate()
        {
            if (_difficulties.Length == 0)
            {
                Debug.LogWarning("난이도가 하나 이상 있어야 한다.", this);
            }

            float sum = _chainWeight + _cutWeight + _setWeight + _polishWeight;
            if (Mathf.Abs(sum - 1f) > 0.001f)
            {
                Debug.LogWarning($"단계별 점수 비중의 합이 1이 아니다({sum}).", this);
            }

            for (int i = 0; i < _difficulties.Length; i++)
            {
                NecklaceDifficulty d = _difficulties[i];
                if (d.FacetCount < 3 || d.FacetCount > 12 || 360 % d.FacetCount != 0)
                {
                    Debug.LogWarning($"난이도 {i}: 면 수는 3~12이고 360의 약수여야 한다(4, 6, 8 등).", this);
                }
            }
        }
    }
}
