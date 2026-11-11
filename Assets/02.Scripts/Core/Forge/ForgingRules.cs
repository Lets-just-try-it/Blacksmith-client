using System;

namespace Blacksmith.Core.Forge
{
    // 밸런스 수치는 ScriptableObject에서 읽어 넘긴다.
    public sealed class ForgingRules
    {
        // 칼날을 나눈 부위 수
        public int SegmentCount { get; set; }
        // 첫 박자까지 시간(초). 박자 표시가 나타나기 시작하는 시간보다 길어야 한다.
        public float LeadIn { get; set; }
        // 화면에서 박자 표시가 나타나 판정선에 닿기까지 시간(초). 규칙에는 영향이 없다.
        public float TravelTime { get; set; }
        // 입력·소리·화면이 실제로 도착하는 지연(초). 눌렀거나 뗀 순간에서 이만큼을 빼고 판정해, 박자에 맞췄다고 느끼는 순간이 정확으로 나오게 한다.
        public float InputOffset { get; set; }
        // 마지막 박자 뒤 결과를 보여 주는 시간(초)
        public float EndDelay { get; set; }
        // 한 부위를 마치고 다음 부위로 옮겨 가는 틈(초)
        public float SegmentGap { get; set; }

        // 판정별 타격 점수. 부위의 진행도는 박자 크기(1 또는 2) * 점수만큼 오른다.
        public float PerfectQuality { get; set; }
        public float GoodQuality { get; set; }
        public float WeakQuality { get; set; }
        // 큰 박자를 힘 모으기 없이 끝에서 톡 쳤을 때 인정하는 최대 점수
        public float UnchargedHeavyQuality { get; set; }

        // 도안(부위별 목표 두께, 1이 처음 두께)
        public float HeadThickness { get; set; }
        public int HeadSegments { get; set; }
        public float BodyThickness { get; set; }
        public float BodyJitter { get; set; }
        public float TipThickness { get; set; }
        public int TipSegments { get; set; }

        // 부위마다 필요한 박자 수 = clamp(round((1 - 목표 두께) * 난이도의 NeedScale), MinNeed, 난이도의 MaxNeed)
        public int MinNeed { get; set; }


        public ForgingDifficulty[] Difficulties { get; set; }
    }

    // 난이도 등급 하나의 수치. System.Serializable이라 ScriptableObject 필드로도 쓸 수 있다.
    [Serializable]
    public struct ForgingDifficulty
    {
        // 박자와 입력 시간 차이(초)가 이 안이면 각각 완벽, 좋음, 약함이고 넘으면 놓친 것이다.
        public float PerfectWindow;
        public float GoodWindow;
        public float WeakWindow;
        // 박자 사이 간격(초)
        public float BeatSpacing;
        // 두 박자를 한 번에 합친 큰 박자가 될 확률
        public float HeavyChance;
        // 박자 간격이 들쭉날쭉한 정도. 간격은 BeatSpacing * (1 + SpacingJitter * (-0.3~0.7))이다.
        public float SpacingJitter;
        // 큰 박자에서 꾹 누르는 시간(초)의 범위. 박자마다 이 안에서 정해진다.
        public float HeavyHoldMin;
        public float HeavyHoldMax;
        // 부위마다 필요한 박자 수를 정하는 배율과 최댓값
        public float NeedScale;
        public int MaxNeed;
    }
}
