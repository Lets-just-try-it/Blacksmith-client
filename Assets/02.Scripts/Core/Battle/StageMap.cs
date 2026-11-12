namespace Blacksmith.Core.Battle
{
    // 칸 단위 연속 좌표의 점. 칸 (x, z)는 x ~ x+1, z ~ z+1 범위를 차지하고 가운데가 (x + 0.5, z + 0.5)다.
    public readonly struct MapPoint
    {
        public readonly float X;
        public readonly float Z;

        public MapPoint(float x, float z)
        {
            X = x;
            Z = z;
        }
    }

    // 스테이지 맵 생성 수치. 밸런스 데이터(StageMapData)에서 읽어 넘긴다. 길이는 모두 칸 단위다.
    public sealed class StageMapSettings
    {
        // 맵의 긴 변 길이 범위. 덩어리를 이 크기에 닿을 때까지 붙이고, 넘게는 붙이지 않는다.
        public int MinSize { get; set; }
        public int MaxSize { get; set; }
        // 이어 붙이는 덩어리(타원) 수 범위
        public int MinLobes { get; set; }
        public int MaxLobes { get; set; }
        // 덩어리 하나의 반지름 범위
        public int MinLobeRadius { get; set; }
        public int MaxLobeRadius { get; set; }
        // 이웃 덩어리 중심 사이 거리 = 두 반지름의 합 × 이 비율(%). 작을수록 많이 겹쳐 이음목이 넓다.
        public int LobeSpacingPercent { get; set; }
        // 덩어리 전체를 크게 휘는 정도와 그 굽이의 크기
        public float WarpAmplitude { get; set; }
        public float WarpScale { get; set; }
        // 가장자리를 울퉁불퉁하게 하는 정도와 그 굴곡의 크기
        public float EdgeAmplitude { get; set; }
        public float EdgeScale { get; set; }
        // 이보다 좁은 목과 돌기는 지운다. 모든 바닥 칸은 이 크기의 바닥 정사각형 안에 들어간다.
        public int MinWidth { get; set; }
        // 변형과 정리 뒤 남은 바닥이 덩어리 넓이의 이 비율(%)보다 작으면 다시 만든다.
        public int MinKeptPercent { get; set; }
    }

    // 생성된 스테이지 맵. 바닥 칸과 모험가가 들어오는 시작 지점을 가진다.
    public sealed class StageMap
    {
        private readonly bool[,] _floor;

        public StageMap(bool[,] floor, MapPoint start)
        {
            _floor = floor;
            Start = start;
        }

        public int Width => _floor.GetLength(0);
        public int Height => _floor.GetLength(1);
        // 첫 덩어리의 가운데에 가장 가까운 바닥 칸의 가운데
        public MapPoint Start { get; }

        internal bool[,] Floor => _floor;

        public bool IsFloor(int x, int z)
        {
            return x >= 0 && z >= 0 && x < Width && z < Height && _floor[x, z];
        }

        public int CountFloor()
        {
            int count = 0;
            foreach (bool cell in _floor)
            {
                count += cell ? 1 : 0;
            }

            return count;
        }
    }
}
