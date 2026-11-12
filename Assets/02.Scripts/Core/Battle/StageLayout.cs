using System.Collections.Generic;

namespace Blacksmith.Core.Battle
{
    // 스폰 지점 배치 수치. 밸런스 데이터(StageMapData)에서 읽어 넘긴다. 길이는 모두 칸 단위다.
    public sealed class StageLayoutSettings
    {
        // 몬스터 무리는 시작 지점에서 이 직선 거리 이상 떨어진다. 파견 직후 화면에 무리가 보이지 않게 카메라 범위보다 넉넉히 둔다.
        public int MinStartDistance { get; set; }
        // 몬스터 무리(보스 포함)끼리 이 직선 거리 이상 떨어진다.
        public int MinGroupSpacing { get; set; }
        // 스폰 칸은 가장 가까운 바닥 밖 칸까지 이 칸 수 이상 걸어야 닿는 곳이다. 무리가 벽에 붙어 나오지 않게 한다.
        public int WallMargin { get; set; }
    }

    // 스테이지 맵 위의 스폰 지점 (docs/GAME_CONCEPT.md 7.1 "몬스터 무리", "보스 스테이지"). 좌표는 StageMap과 같은 칸 좌표다.
    public sealed class StageLayout
    {
        public StageLayout(IReadOnlyList<MapPoint> mobGroups, bool hasBoss, MapPoint boss)
        {
            MobGroups = mobGroups;
            HasBoss = hasBoss;
            Boss = boss;
        }

        // 잡몹 무리마다 하나. 순서는 던전 데이터의 무리 순서와 같다.
        public IReadOnlyList<MapPoint> MobGroups { get; }
        public bool HasBoss { get; }
        // 시작 지점에서 걸어서 가장 먼 곳. 잡몹 무리를 모두 토벌하면 여기 나타난다.
        public MapPoint Boss { get; }
    }
}
