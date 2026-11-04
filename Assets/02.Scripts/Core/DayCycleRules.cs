namespace Blacksmith.Core
{
    // 하루의 단계. (docs/GAME_CONCEPT.md 2.3)
    public enum DayPhase
    {
        // 영업 중. 제작·판매·영입을 한다.
        Day,
        // 낮 시간이 끝났지만 진행 중인 미니게임이 남아 있다. 새 미니게임은 시작할 수 없다.
        Closing,
        // 밤으로 넘어가는 연출. 시간이 멈추고 "밤이 되었습니다"를 띄우며 조명이 어두워진다. 정해진 시간이 지나면 밤이 된다.
        Dusk,
        // 파견·전투·결과. 결과를 반영하거나 파견을 건너뛰면 다음 날 낮이 된다.
        Night,
    }

    // 낮·밤 단계를 넘기는 규칙이다. 단계 타이머(낮 시간, 밤 전환 연출 시간)와 진행 중인 작업 여부를 받아 다음 단계를 정한다.
    public static class DayCycleRules
    {
        public const int FirstDay = 1;

        // 낮이 끝나는 순간 진행 중인 미니게임은 끝까지 진행해 결과를 반영한 뒤 밤으로 넘어간다 (docs/GAME_CONCEPT.md 4절).
        // 밤으로 넘어갈 때는 전환 연출(Dusk)을 거친다. isTimerOver는 낮에는 낮 시간, 전환 연출 중에는 연출 시간이 끝났는지다.
        // 밤은 시간으로 끝나지 않는다. 다음 날은 StartNextDay로만 시작한다.
        public static DayPhase Next(DayPhase phase, bool isTimerOver, bool isWorkInProgress)
        {
            switch (phase)
            {
                case DayPhase.Day:
                    if (!isTimerOver)
                    {
                        return DayPhase.Day;
                    }

                    return isWorkInProgress ? DayPhase.Closing : DayPhase.Dusk;
                case DayPhase.Closing:
                    return isWorkInProgress ? DayPhase.Closing : DayPhase.Dusk;
                case DayPhase.Dusk:
                    return isTimerOver ? DayPhase.Night : DayPhase.Dusk;
                default:
                    return phase;
            }
        }

        // 밤 조명과 밤 화면을 쓰는 단계인지. 전환 연출이 시작될 때부터 밤으로 본다.
        public static bool IsNightLook(DayPhase phase)
        {
            return phase == DayPhase.Dusk || phase == DayPhase.Night;
        }

        // 새 미니게임은 낮에만 시작할 수 있다.
        public static bool CanStartWork(DayPhase phase)
        {
            return phase == DayPhase.Day;
        }

        // 다음 날은 밤에서만 시작한다.
        public static bool CanStartNextDay(DayPhase phase)
        {
            return phase == DayPhase.Night;
        }

        // 낮 동안 흐른 시간을 게임 속 시각(자정부터 센 분)으로 바꾼다. 분 아래는 버린다.
        // 예: 낮 600초를 10시~20시로 두면 실제 1초가 게임 1분이다. 10초 뒤 10시 10분, 5분 뒤 오후 3시.
        public static int GetClockMinutes(float elapsedSeconds, float daySeconds, int startHour, int endHour)
        {
            int start = startHour * 60;
            int length = (endHour - startHour) * 60;
            if (daySeconds <= 0f || length <= 0)
            {
                return start;
            }

            float ratio = elapsedSeconds / daySeconds;
            ratio = ratio < 0f ? 0f : ratio > 1f ? 1f : ratio;
            return start + (int)(ratio * length);
        }
    }
}
