using System;

namespace Blacksmith.Core.Forge
{
    // 밸런스 수치는 ScriptableObject에서 읽어 넘긴다. 모두 프로토타입 값이며 플레이 테스트 후 조정한다.
    public sealed class NecklaceRules
    {
        // 한 단계가 시작하기 전 잠깐 보여 주는 시간, 단계를 마치고 다음 단계로 넘어가기 전 시간, 끝난 뒤 결과를 보여 주는 시간
        public float IntroTime { get; set; }
        public float StepDoneTime { get; set; }
        public float EndTime { get; set; }

        // 보석 반지름이 실제로 몇 cm인지(확대해서 보는 크기). 두께 표시와 허용 오차를 cm로 말하기 위한 눈금이다.
        public float GemCm { get; set; }
        // 숫돌이 면을 가는 속도(보석 반지름 1 기준, 초당). 거친 숫돌은 빠르고 고운 숫돌은 느리다.
        public float CoarseRate { get; set; }
        public float FineRate { get; set; }
        // 고운 숫돌로 갈 때 면의 거칠기가 줄어드는 속도(초당)
        public float PolishSmoothRate { get; set; }
        // 보석을 틀에 박는 데 걸리는 시간
        public float SeatTime { get; set; }

        // 보석 깎기 제한시간 = (기본 + 면 수 × 면당) × 난이도 배율
        public float CutBaseTime { get; set; }
        public float CutTimePerFacet { get; set; }

        // 단계별 점수 비중(합이 1이 되게 둔다)과, 이 점수 이상이면 결과 사건을 성공으로 알리는 기준
        public float ChainWeight { get; set; }
        public float CutWeight { get; set; }
        public float SetWeight { get; set; }
        public float PolishWeight { get; set; }
        public float SuccessScore { get; set; }

        public NecklaceDifficulty[] Difficulties { get; set; }
    }

    // 재료 티어에서 정해진 난이도 하나의 수치. 어려울수록 가닥과 조각이 많고, 면이 많고 허용 오차가 좁으며, 제한시간이 빠듯하다.
    [Serializable]
    public struct NecklaceDifficulty
    {
        // 체인 잇기 제한시간 = 기본 + 조각당 × 조각 수
        public float ChainBaseTime;
        public float ChainTimePerPiece;
        // 목표 면의 허용 오차(cm), 목표와의 차이를 점수로 바꾸는 폭, 깎기 제한시간 배율
        public float CutTolCm;
        public float CutMatchSpan;
        public float CutTimeMul;
        // 보석 면의 수와, 면의 두께가 서로 다른 정도
        public int FacetCount;
        public float FacetSpread;
        // 장착에서 위치 허용 오차(칸)와 제한시간
        public float SetPosTol;
        public float SetLimit;
        // 광내기 제한시간 = 기본 + 면당 × 면 수, 면을 닦아야 하는 거리 배율
        public float PolishBaseTime;
        public float PolishTimePerFacet;
        public float PolishNeedMul;
    }
}
