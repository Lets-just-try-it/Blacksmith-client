using System;

namespace Blacksmith.Core.Forge
{
    // 밸런스 수치는 ScriptableObject에서 읽어 넘긴다.
    public sealed class AssembleRules
    {
        // 시작 안내, 이음새 하나를 마친 뒤 다음으로 넘어가기까지, 모두 끝난 뒤 결과를 보여 주는 시간(초)
        public float IntroTime { get; set; }
        public float SeamEndTime { get; set; }
        public float EndDelay { get; set; }

        // 이음새 제한시간 = SeamBaseTime + SeamTimePerHole * 구멍 수
        public float SeamBaseTime { get; set; }
        public float SeamTimePerHole { get; set; }

        // 구멍 둘레에 덮는 판이 더 나오는 여백, 판을 잡을 수 있는 여유(칸)
        public float StripPad { get; set; }
        public float GrabPad { get; set; }
        // 구멍을 눌러 리벳을 끼울 수 있는 거리(칸)
        public float SeatRadius { get; set; }

        // 판을 맞춘 정도(AlignScore) = 1 - AlignLoss * 오차 / 허용 오차
        public float AlignLoss { get; set; }

        // 리벳을 끼운 뒤 링이 줄어들기 시작하기까지의 대기, 망치질 뒤 링이 다시 시작하기까지의 쉬는 시간(초)
        public float SeatDelay { get; set; }
        public float RingRest { get; set; }
        // 박는 시간이 지난 뒤 이만큼(좋음 폭에 더해) 넘기면 놓친 것으로 본다.
        public float RingOvertime { get; set; }
        // 구멍 하나를 박는 데 필요한 성공한 망치질 수
        public int HitsNeeded { get; set; }
        // 망치질 점수와 놓칠 때마다 깎이는 점수, 구멍 품질에서 한 번에 빼는 보정
        public float PerfectScore { get; set; }
        public float GoodScore { get; set; }
        public float MissPenalty { get; set; }
        public float HoleQualityOffset { get; set; }

        // 이음새 점수 = 구멍 품질 평균 * (AlignBase + AlignWeight * 판 맞춤)
        public float AlignBase { get; set; }
        public float AlignWeight { get; set; }
        // 최종 정확도 = 이음새 평균 * (PlateBase + PlateWeight * 판 성형 점수)
        public float PlateBase { get; set; }
        public float PlateWeight { get; set; }
        // 이 점수 이상이면 화면이 성공으로 알린다.
        public float SuccessScore { get; set; }

        public AssembleDifficulty[] Difficulties { get; set; }
    }

    // 재료 티어에서 정해진 난이도 하나의 수치. 어려울수록 허용 오차가 좁고 링이 빠르고 이음새와 구멍이 많다.
    [Serializable]
    public struct AssembleDifficulty
    {
        // 이 안에서 놓으면 판이 핀으로 고정된다(칸)
        public float AlignTolerance;
        // 덮는 판이 구멍에서 어긋나 놓이는 거리(칸)
        public float StartOffset;
        // 링이 안쪽 원에 닿기까지 시간(초)
        public float RingDuration;
        // 닿는 순간과의 차이(초)가 이 안이면 완벽, 좋음이다.
        public float PerfectWindow;
        public float GoodWindow;
        // 이음새 한 줄에 더해지는 구멍 수
        public int HoleBonus;
        // 양쪽에 대칭으로 있는 이음새(볼가리개, 어깨판)의 한쪽 구멍 수
        public int SideHoles;
        // 선택 이음새(이마 보강판 등)가 들어가는 개수(0~2)
        public int OptionalSeams;
    }
}
