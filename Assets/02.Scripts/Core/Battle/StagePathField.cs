using System.Collections.Generic;

namespace Blacksmith.Core.Battle
{
    // 스테이지 맵의 목표 칸까지 8방향으로 걷는 거리장. 상하좌우 한 걸음은 10, 대각선은 14다.
    // 대각선은 양옆 두 칸이 모두 바닥일 때만 가서 벽 모서리를 가로지르지 않는다. 정수로만 계산해 기기마다 같다.
    public sealed class StagePathField
    {
        public const int Unreachable = int.MaxValue;
        // 상하좌우 한 칸을 걷는 비용
        public const int StraightCost = 10;
        private const int DiagonalCost = 14;
        private static readonly int[] StepX = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] StepZ = { 0, 0, 1, -1, 1, -1, 1, -1 };

        private readonly StageMap _map;
        private readonly int[,] _costs;

        public StagePathField(StageMap map, int targetX, int targetZ)
        {
            _map = map;
            _costs = new int[map.Width, map.Height];
            for (int x = 0; x < map.Width; x++)
            {
                for (int z = 0; z < map.Height; z++)
                {
                    _costs[x, z] = Unreachable;
                }
            }

            if (map.IsFloor(targetX, targetZ))
            {
                Fill(targetX, targetZ);
            }
        }

        // 이 칸에서 목표 칸까지의 걷는 비용. 바닥이 아니거나 닿지 않으면 Unreachable이다.
        public int CostAt(int x, int z)
        {
            return _map.IsFloor(x, z) ? _costs[x, z] : Unreachable;
        }

        // 목표 쪽으로 한 걸음 가는 이웃 칸. 이미 목표 칸이거나 닿지 않는 칸이면 false다.
        public bool TryNextCell(int x, int z, out int nextX, out int nextZ)
        {
            nextX = x;
            nextZ = z;
            int best = CostAt(x, z);
            if (best == 0 || best == Unreachable)
            {
                return false;
            }

            for (int i = 0; i < StepX.Length; i++)
            {
                int nx = x + StepX[i];
                int nz = z + StepZ[i];
                if (CanStep(x, z, StepX[i], StepZ[i]) && _costs[nx, nz] < best)
                {
                    best = _costs[nx, nz];
                    nextX = nx;
                    nextZ = nz;
                }
            }

            return nextX != x || nextZ != z;
        }

        private bool CanStep(int x, int z, int dx, int dz)
        {
            if (!_map.IsFloor(x + dx, z + dz))
            {
                return false;
            }

            return dx == 0 || dz == 0 || (_map.IsFloor(x + dx, z) && _map.IsFloor(x, z + dz));
        }

        // 비용이 작은 칸부터 넓히는 다익스트라. 같은 비용이면 칸 번호가 작은 쪽을 먼저 꺼내 순서가 늘 같다.
        private void Fill(int targetX, int targetZ)
        {
            long cellCount = (long)_map.Width * _map.Height;
            var heap = new List<long>();
            _costs[targetX, targetZ] = 0;
            Push(heap, targetX * _map.Height + targetZ);
            while (heap.Count > 0)
            {
                long key = Pop(heap);
                int index = (int)(key % cellCount);
                int cost = (int)(key / cellCount);
                int x = index / _map.Height;
                int z = index % _map.Height;
                if (cost > _costs[x, z])
                {
                    continue;
                }

                for (int i = 0; i < StepX.Length; i++)
                {
                    if (!CanStep(x, z, StepX[i], StepZ[i]))
                    {
                        continue;
                    }

                    int nx = x + StepX[i];
                    int nz = z + StepZ[i];
                    int next = cost + (i < 4 ? StraightCost : DiagonalCost);
                    if (next < _costs[nx, nz])
                    {
                        _costs[nx, nz] = next;
                        Push(heap, (long)next * cellCount + nx * _map.Height + nz);
                    }
                }
            }
        }

        private static void Push(List<long> items, long value)
        {
            items.Add(value);
            int child = items.Count - 1;
            while (child > 0)
            {
                int parent = (child - 1) / 2;
                if (items[parent] <= items[child])
                {
                    break;
                }

                (items[parent], items[child]) = (items[child], items[parent]);
                child = parent;
            }
        }

        private static long Pop(List<long> items)
        {
            long top = items[0];
            int last = items.Count - 1;
            items[0] = items[last];
            items.RemoveAt(last);
            int parent = 0;
            while (true)
            {
                int left = parent * 2 + 1;
                int right = left + 1;
                int smallest = parent;
                if (left < items.Count && items[left] < items[smallest])
                {
                    smallest = left;
                }

                if (right < items.Count && items[right] < items[smallest])
                {
                    smallest = right;
                }

                if (smallest == parent)
                {
                    return top;
                }

                (items[parent], items[smallest]) = (items[smallest], items[parent]);
                parent = smallest;
            }
        }
    }
}
