using System;

namespace Blacksmith.Core.Forge
{
    // 밸런스 수치는 ScriptableObject에서 읽어 넘긴다.
    public sealed class BowStringRules
    {
        // 시작 안내, 모두 끝난 뒤 결과를 보여 주는 시간(초)
        public float IntroTime { get; set; }
        public float EndDelay { get; set; }

        // 힘줄을 감을 때 원을 그린 것으로 치는 최소 반지름(칸), 원의 가운데가 마우스를 따라 옮겨 가는 빠르기(초당 비율)
        public float MinRadius { get; set; }
        public float CenterFollow { get; set; }
        // 걸기: 마우스를 아래로 한 칸 끌면 활이 휘는 정도, 휜 활에서 고리를 옆으로 한 칸 밀면 고리가 활 끝으로 나아가는 정도(1이 활 끝의 홈)
        public float FlexPerCell { get; set; }
        public float SlidePerCell { get; set; }
        // 활을 너무 휘면(구간 위) 고리가 미끄러져 초당 이만큼 뒤로 밀린다.
        public float SlipRate { get; set; }
        // 감기 점수와 걸기 점수가 정확도에서 차지하는 비율
        public float WrapWeight { get; set; }
        // 이 점수 이상이면 화면이 성공으로 알린다.
        public float SuccessScore { get; set; }

        public BowStringDifficulty[] Difficulties { get; set; }
    }

    // 재료 티어에서 정해진 난이도 하나의 수치
    [Serializable]
    public struct BowStringDifficulty
    {
        // 감아야 하는 횟수, 좋은 감기의 빠르기 구간(rad/초), 이보다 빠르면 힘줄이 미끄러지는 빠르기, 제한시간(초)
        public int Wraps;
        public float SpeedLo;
        public float SpeedHi;
        public float SlipSpeed;
        public float TimeLimit;
        // 걸기: 고리가 밀리는 알맞은 휘기 구간의 폭, 그 구간이 시간에 따라 오르내리는 폭, 이 시간(초) 안에 걸면 만점인 기준 시간
        public float WindowWidth;
        public float WindowDrift;
        public float ParTime;
    }
}
