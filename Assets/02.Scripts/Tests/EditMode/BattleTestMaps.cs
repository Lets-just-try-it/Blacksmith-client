using Blacksmith.Core.Battle;

namespace Blacksmith.Tests
{
    // 전투 테스트용 열린 직사각형 맵과 무리. 시작 지점은 왼쪽 가운데이고, 무리는 시작 지점과 같은 줄에 둔다.
    internal static class BattleTestMaps
    {
        public const int Width = 80;
        public const int Height = 24;
        public const float CenterZ = Height / 2 + 0.5f;

        public static StageMap Open()
        {
            var floor = new bool[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Height; z++)
                {
                    floor[x, z] = true;
                }
            }

            return new StageMap(floor, new MapPoint(2.5f, CenterZ));
        }

        // x칸 위치에 스폰하는 무리
        public static MonsterGroupSetup Group(bool isBoss, float x, params MonsterSetup[] monsters)
        {
            var group = new MonsterGroupSetup { IsBoss = isBoss, Spawn = new MapPoint(x, CenterZ) };
            group.Monsters.AddRange(monsters);
            return group;
        }
    }
}
