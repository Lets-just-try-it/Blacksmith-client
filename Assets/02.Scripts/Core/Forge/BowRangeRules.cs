using System;

namespace Blacksmith.Core.Forge
{
    // 밸런스 수치는 ScriptableObject에서 읽어 넘긴다.
    public sealed class BowRangeRules
    {
        // 시작 안내, 한 발을 쏜 뒤 결과를 보여 주는 시간(화살이 날아가는 시간 포함), 모두 끝난 뒤 결과를 보여 주는 시간(초)
        public float IntroTime { get; set; }
        public float ResultTime { get; set; }
        public float EndDelay { get; set; }

        // 화살이 날아가는 시간(초). 결과를 보여 주는 시간 안에 포함된다.
        public float FlightTime { get; set; }
        // 숨을 참을 수 있는 시간(초), 숨을 참는 동안 손 떨림에 곱하는 값, 숨이 다했을 때 손 떨림에 곱하는 값, 숨이 차오르는 데 걸리는 시간(초). 0이면 숨 참기를 쓰지 않는다.
        public float BreathTime { get; set; }
        public float BreathSteady { get; set; }
        public float GaspSway { get; set; }
        public float BreathRecover { get; set; }
        // 시위를 끝까지 당기는 데 걸리는 시간(초). 덜 당기고 쏘면 화살이 아래로 처진다.
        public float DrawTime { get; set; }
        // 다 당긴 뒤 이 시간(초)이 지나면 팔이 떨려 조준점이 더 크게 흔들린다. 흔들림 크기는 그 뒤 초당 SwayGrowth배씩 커진다.
        public float FatigueStart { get; set; }
        public float SwayGrowth { get; set; }
        public float SwayFrequency { get; set; }
        // 시위를 하나도 안 당겼을 때 더 처지는 거리(칸)
        public float DropScale { get; set; }
        // 기준 거리(m). 과녁 크기, 바람이 미는 거리, 중력으로 처지는 거리는 모두 이 거리를 기준으로 정한다.
        public float RefDistance { get; set; }
        // 기준 거리에서 바람 1단위가 화살을 옆으로 미는 거리(칸). 거리에 비례해 커진다.
        public float WindScale { get; set; }
        // 기준 거리에서 중력으로 처지는 거리(칸). 거리의 제곱에 비례해 커진다. 처지는 만큼 위를 겨냥해야 한다.
        public float GravityDrop { get; set; }
        // 허수아비 가슴 가운데가 놓이는 세로 위치(칸)
        public float CenterY { get; set; }
        // 허수아비가 놓일 수 있는 범위의 절반 폭(칸, 가로·세로)
        public float RangeX { get; set; }
        public float RangeY { get; set; }
        // 이 점수 이상이면 화면이 성공으로 알린다.
        public float SuccessScore { get; set; }

        public BowRangeDifficulty[] Difficulties { get; set; }
    }

    // 재료 티어에서 정해진 난이도 하나의 수치. 어려울수록 목표 명중 수가 많고 허수아비가 작고 바람이 세고 허수아비가 움직이고 손이 많이 떨린다.
    [Serializable]
    public struct BowRangeDifficulty
    {
        // 정확도 만점에 필요한 명중 수, 제한시간(초)
        public int GoalHits;
        public float TimeLimit;
        // 기준 거리에서 허수아비 가슴의 맞는 구역 반지름(칸), 바람의 최대 세기, 허수아비가 좌우로 움직이는 폭(칸)과 빠르기, 처음 손 떨림 크기(칸)
        public float TargetRadius;
        public float Wind;
        public float TargetMove;
        public float TargetSpeed;
        public float Sway;
        // 허수아비 거리(m)가 나올 수 있는 범위. 1m 단위로 고른다.
        public float DistanceMin;
        public float DistanceMax;
    }
}
