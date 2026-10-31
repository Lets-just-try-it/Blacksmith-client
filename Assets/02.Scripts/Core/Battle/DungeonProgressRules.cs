namespace Blacksmith.Core.Battle
{
    // 던전 단계별 진행 상황(클리어한 스테이지 수)으로 파견할 스테이지 범위를 정한다. (docs/GAME_CONCEPT.md 7.3)
    // 스테이지 번호는 0부터 센다. 진행 상황 자체는 호스트의 게임 상태에 둔다.
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

        public static int FirstStageToFight(int clearedStages)
        {
            return clearedStages;
        }

        // [미정] 보스까지 클리어한 단계를 다시 고를 수 있는지. 지금은 고를 수 없다.
        public static bool CanSelectStage(int clearedStages, int stageCount, int selectedStage)
        {
            return selectedStage >= FirstStageToFight(clearedStages) && selectedStage < stageCount;
        }

        // 패배해도 그 전투에서 클리어한 스테이지는 진행 상황에 남긴다.
        public static int ApplyResult(int clearedStages, BattleResult result)
        {
            return clearedStages + result.StagesCleared;
        }
    }
}
