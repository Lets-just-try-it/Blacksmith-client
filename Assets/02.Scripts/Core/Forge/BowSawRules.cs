using System;

namespace Blacksmith.Core.Forge
{
    // 밸런스 수치는 ScriptableObject에서 읽어 넘긴다.
    public sealed class BowSawRules
    {
        // 시작 안내, 모두 끝난 뒤 결과를 보여 주는 시간(초)
        public float IntroTime { get; set; }
        public float EndDelay { get; set; }

        // 톱질 한 번(위로 올렸다 아래로 내리기 한 번)으로 치는 최소 거리(칸), 방향을 바꾼 것으로 보는 되돌림 거리(칸)
        public float StrokeMin { get; set; }
        public float Hysteresis { get; set; }
        // 톱이 끼었을 때 톱질을 못 하는 시간(초)
        public float BindCooldown { get; set; }
        // 힘(빠르기)이 구간 위쪽 한계의 이 배수보다 세면 톱이 낀다. 구간 아래쪽 한계의 이 비율보다 약하면 거의 안 켜진다.
        public float BindFactor { get; set; }
        public float SlowFactor { get; set; }
        // 구간을 조금 벗어난 톱질의 점수, 거의 안 켜진 톱질의 점수
        public float EdgeQuality { get; set; }
        public float SlowQuality { get; set; }
        // 연속으로 구간 안에서 톱질한 횟수가 이만큼 쌓일 때마다 완벽으로 알린다.
        public int PerfectStreak { get; set; }
        // 이 점수 이상이면 화면이 성공으로 알린다.
        public float SuccessScore { get; set; }

        public BowSawDifficulty[] Difficulties { get; set; }
    }

    // 재료 티어에서 정해진 난이도 하나의 수치. 활 깎기는 참나무만 쓰므로 보통 첫 항목을 쓴다.
    [Serializable]
    public struct BowSawDifficulty
    {
        // 통나무 하나를 다 켜는 데 필요한 톱질 횟수, 좋은 톱질의 힘(빠르기) 구간(칸/초), 만점에 필요한 통나무 수, 제한시간(초)
        public int Strokes;
        public float ZoneLo;
        public float ZoneHi;
        public int Logs;
        public float TimeLimit;
    }
}
