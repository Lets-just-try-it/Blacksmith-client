using System;
using System.Collections.Generic;

namespace Blacksmith.Core.Battle
{
    public readonly struct StageMapReport
    {
        public readonly int FloorCells;
        // 시작 지점에서 상하좌우로 이어 걸어 닿는 바닥 칸 수
        public readonly int ReachableCells;
        // 바닥에 둘러싸여 맵 바깥과 이어지지 않은 빈 칸 수
        public readonly int HoleCells;
        // 2×2 안에서 대각선으로만 닿은 곳의 수
        public readonly int DiagonalContacts;
        // MinWidth 크기의 바닥 정사각형 어디에도 들지 않는 좁은 칸 수
        public readonly int NarrowCells;

        public StageMapReport(int floorCells, int reachableCells, int holeCells, int diagonalContacts, int narrowCells)
        {
            FloorCells = floorCells;
            ReachableCells = reachableCells;
            HoleCells = holeCells;
            DiagonalContacts = diagonalContacts;
            NarrowCells = narrowCells;
        }

        public bool IsValid => FloorCells > 0 && ReachableCells == FloorCells && HoleCells == 0 && DiagonalContacts == 0 && NarrowCells == 0;
    }

    // 스테이지 맵이 한 덩어리이고, 구멍이 없고, 좁은 목이 없는지 확인한다. 생성기도 같은 판정 함수를 쓴다.
    public static class StageMapValidator
    {
        public static StageMapReport Check(StageMap map, int minWidth)
        {
            bool[,] floor = map.Floor;
            bool[,] outside = FindOutside(floor);
            bool[,] covered = FindCovered(floor, minWidth);
            int floorCells = 0;
            int holeCells = 0;
            int diagonalContacts = 0;
            int narrowCells = 0;
            for (int x = 0; x < map.Width; x++)
            {
                for (int z = 0; z < map.Height; z++)
                {
                    floorCells += floor[x, z] ? 1 : 0;
                    holeCells += !floor[x, z] && !outside[x, z] ? 1 : 0;
                    narrowCells += floor[x, z] && !covered[x, z] ? 1 : 0;
                    diagonalContacts += x < map.Width - 1 && z < map.Height - 1 && IsDiagonalContact(floor, x, z) ? 1 : 0;
                }
            }

            int reachable = CountReachable(floor, (int)Math.Floor(map.Start.X), (int)Math.Floor(map.Start.Z));
            return new StageMapReport(floorCells, reachable, holeCells, diagonalContacts, narrowCells);
        }

        // (x, z)를 왼쪽 아래로 하는 2×2에서 바닥 두 칸이 대각선으로만 닿아 있는지
        internal static bool IsDiagonalContact(bool[,] floor, int x, int z)
        {
            bool a = floor[x, z];
            bool b = floor[x + 1, z];
            bool c = floor[x, z + 1];
            bool d = floor[x + 1, z + 1];
            return (a && d && !b && !c) || (b && c && !a && !d);
        }

        // 바닥으로만 이뤄진 size×size 정사각형들의 합집합. 여기 들지 않는 바닥 칸은 size보다 좁은 곳이다.
        internal static bool[,] FindCovered(bool[,] floor, int size)
        {
            int width = floor.GetLength(0);
            int height = floor.GetLength(1);
            size = Math.Max(1, size);
            int[,] floorSum = PrefixSum(width, height, (x, z) => floor[x, z]);
            var fits = new bool[width, height];
            for (int x = 0; x + size <= width; x++)
            {
                for (int z = 0; z + size <= height; z++)
                {
                    fits[x, z] = RectSum(floorSum, x, z, x + size, z + size) == size * size;
                }
            }

            // 칸 (x, z)는 왼쪽 아래가 [x - size + 1, x] × [z - size + 1, z] 안에 있는 정사각형에 든다.
            int[,] fitSum = PrefixSum(width, height, (x, z) => fits[x, z]);
            var covered = new bool[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    covered[x, z] = floor[x, z] && RectSum(fitSum, Math.Max(0, x - size + 1), Math.Max(0, z - size + 1), x + 1, z + 1) > 0;
                }
            }

            return covered;
        }

        // 상하좌우로 이어진 바닥 덩어리마다 번호를 붙인다. 바닥이 아닌 칸은 -1이다.
        internal static int[,] LabelRegions(bool[,] floor, out List<int> sizes)
        {
            int width = floor.GetLength(0);
            int height = floor.GetLength(1);
            var labels = new int[width, height];
            sizes = new List<int>();
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    labels[x, z] = -1;
                }
            }

            var queue = new Queue<int>();
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    if (!floor[x, z] || labels[x, z] >= 0)
                    {
                        continue;
                    }

                    int label = sizes.Count;
                    int count = 0;
                    labels[x, z] = label;
                    queue.Enqueue(x * height + z);
                    while (queue.Count > 0)
                    {
                        int index = queue.Dequeue();
                        int cx = index / height;
                        int cz = index % height;
                        count++;
                        Visit(cx + 1, cz);
                        Visit(cx - 1, cz);
                        Visit(cx, cz + 1);
                        Visit(cx, cz - 1);
                    }

                    sizes.Add(count);

                    void Visit(int vx, int vz)
                    {
                        if (vx >= 0 && vz >= 0 && vx < width && vz < height && floor[vx, vz] && labels[vx, vz] < 0)
                        {
                            labels[vx, vz] = label;
                            queue.Enqueue(vx * height + vz);
                        }
                    }
                }
            }

            return labels;
        }

        // 맵 가장자리의 빈 칸에서 빈 칸끼리 상하좌우로 이어 닿는 칸. 여기 닿지 않는 빈 칸이 구멍이다.
        internal static bool[,] FindOutside(bool[,] floor)
        {
            int width = floor.GetLength(0);
            int height = floor.GetLength(1);
            var outside = new bool[width, height];
            var queue = new Queue<int>();
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    bool isEdge = x == 0 || z == 0 || x == width - 1 || z == height - 1;
                    if (isEdge && !floor[x, z])
                    {
                        outside[x, z] = true;
                        queue.Enqueue(x * height + z);
                    }
                }
            }

            Flood(queue, outside, (x, z) => !floor[x, z]);
            return outside;
        }

        private static int CountReachable(bool[,] floor, int startX, int startZ)
        {
            int width = floor.GetLength(0);
            int height = floor.GetLength(1);
            if (startX < 0 || startZ < 0 || startX >= width || startZ >= height || !floor[startX, startZ])
            {
                return 0;
            }

            var reached = new bool[width, height];
            var queue = new Queue<int>();
            reached[startX, startZ] = true;
            queue.Enqueue(startX * height + startZ);
            Flood(queue, reached, (x, z) => floor[x, z]);
            int count = 0;
            foreach (bool cell in reached)
            {
                count += cell ? 1 : 0;
            }

            return count;
        }

        private static void Flood(Queue<int> queue, bool[,] marked, Func<int, int, bool> canEnter)
        {
            int width = marked.GetLength(0);
            int height = marked.GetLength(1);
            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int x = index / height;
                int z = index % height;
                TryEnter(x + 1, z);
                TryEnter(x - 1, z);
                TryEnter(x, z + 1);
                TryEnter(x, z - 1);
            }

            void TryEnter(int x, int z)
            {
                if (x >= 0 && z >= 0 && x < width && z < height && !marked[x, z] && canEnter(x, z))
                {
                    marked[x, z] = true;
                    queue.Enqueue(x * height + z);
                }
            }
        }

        // sum[x, z]는 [0, x) × [0, z) 안의 참 칸 수다.
        private static int[,] PrefixSum(int width, int height, Func<int, int, bool> isSet)
        {
            var sum = new int[width + 1, height + 1];
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    sum[x + 1, z + 1] = sum[x, z + 1] + sum[x + 1, z] - sum[x, z] + (isSet(x, z) ? 1 : 0);
                }
            }

            return sum;
        }

        private static int RectSum(int[,] sum, int x0, int z0, int x1, int z1)
        {
            return sum[x1, z1] - sum[x0, z1] - sum[x1, z0] + sum[x0, z0];
        }
    }
}
