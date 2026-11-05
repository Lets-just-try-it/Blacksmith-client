namespace Blacksmith.Battle
{
    // 던전 지도와 파견 창이 함께 쓰는 표시 문구다.
    public static class DungeonMapLabels
    {
        public static string GetStageName(DungeonData dungeon, int stage)
        {
            return dungeon.IsBossStage(stage) ? "보스 스테이지" : $"스테이지 {stage + 1}";
        }
    }
}
