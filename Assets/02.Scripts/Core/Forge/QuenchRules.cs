using System;

namespace Blacksmith.Core.Forge
{
    // 밸런스 수치는 ScriptableObject에서 읽어 넘긴다.
    public sealed class QuenchRules
    {
        // 화로에서 통으로 옮기는 시간이다. 담근 시간(초)에 넣지 않는다.
        public float CarryTime { get; set; }
        // 담근 뒤 이 시간까지만 초를 보여 준다.
        public float ShowSeconds { get; set; }
        // 준비 직후에 눌린 입력이 바로 담그기로 넘어가지 않도록 막는 시간이다.
        public float ReadyLockTime { get; set; }
        // 이 시간이 지나면 입력이 없어도 자동으로 담근다.
        public float ReadyAutoTime { get; set; }
        // 담근 직후에 눌린 입력이 바로 꺼내기로 넘어가지 않도록 막는 시간이다.
        public float MinPullTime { get; set; }
        public float EndDelay { get; set; }
        // 제한시간 = 기준 시간 * LimitTargetMultiplier + LimitExtraSeconds
        public float LimitTargetMultiplier { get; set; }
        public float LimitExtraSeconds { get; set; }
        // 제한시간을 넘겨서 꺼냈을 때의 정확도다.
        public float TimeoutAccuracy { get; set; }
        // 이 정확도 이상이면 화면이 쓸 수 있도록 Perfect 사건을 낸다.
        public float PerfectEventAccuracy { get; set; }
        public QuenchDifficulty[] Difficulties { get; set; }
    }

    // 난이도 등급 하나의 수치. System.Serializable이라 ScriptableObject 필드로도 쓸 수 있다.
    [Serializable]
    public struct QuenchDifficulty
    {
        public float MinTarget;
        // 기준 시간 = MinTarget + (0~TargetRandomRange 사이 값)
        public float TargetRandomRange;
        // 기준 시간 대비 이 비율 안으로 꺼내면 만점이다.
        public float PerfectFraction;
        // 기준 시간 대비 이 비율 이상 벗어나면 0점이다.
        public float ZeroFraction;
    }
}
