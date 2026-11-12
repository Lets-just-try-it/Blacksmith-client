namespace Blacksmith.Core.Battle
{
    // 던전 단계별 진행 상황(클리어한 스테이지 수)으로 파견할 수 있는 스테이지를 정한다. (docs/GAME_CONCEPT.md 7.3)
    // 스테이지 번호는 0부터 센다. 진행 상황 자체는 호스트의 게임 상태에 둔다.
    // 한 번 파견하면 스테이지 하나를 싸운다. 스테이지를 클리어하면 다음 스테이지가 열리고,
    // 이미 클리어한 스테이지와 다음 스테이지 중에서 고를 수 있다.
    public static class DungeonProgressRules
    {
        public static bool IsTierUnlocked(int tierIndex, int previousTierClearedStages, int previousTierStageCount)
        {
            return tierIndex == 0 || IsTierCleared(previousTierClearedStages, previousTierStageCount);
        }

        public static bool IsTierCleared(int clearedStages, int stageCount)
        {
            return clearedStages >= stageCount;
        }

        // 아직 클리어하지 않은 첫 스테이지. 단계를 모두 클리어했으면 stageCount와 같다.
        public static int NextStage(int clearedStages)
        {
            return clearedStages;
        }

        public static bool IsStageCleared(int clearedStages, int stage)
        {
            return stage < clearedStages;
        }

        // 열린 단계 안에서 클리어한 스테이지와 다음 스테이지를 고를 수 있다.
        public static bool CanSelectStage(int clearedStages, int stageCount, int selectedStage)
        {
            return selectedStage >= 0 && selectedStage < stageCount && selectedStage <= NextStage(clearedStages);
        }

        // 지도에서 단계를 고르면 처음 골라 두는 스테이지. 다음 스테이지이고, 단계를 모두 클리어했으면 보스 스테이지다.
        public static int DefaultStage(int clearedStages, int stageCount)
        {
            int next = NextStage(clearedStages);
            return next < stageCount ? next : stageCount - 1;
        }

        // 스테이지의 몬스터 무리를 모두 토벌해야 그 스테이지를 클리어한다. 도중에 지면 진행 상황은 그대로다.
        // 클리어한 스테이지를 다시 싸우면 진행 상황은 그대로다.
        public static int ApplyResult(int clearedStages, int stage, BattleResult result)
        {
            int reached = result.IsVictory ? stage + 1 : stage;
            return reached > clearedStages ? reached : clearedStages;
        }
    }
}
