using System;

namespace Blacksmith.Core.Sales
{
    // 손님 방문의 수치. 밸런스 수치는 ScriptableObject에서 읽어 넘긴다.
    public sealed class SalesRules
    {
        // 손님이 오는 간격(초), 한 곳에서 둘러보는 시간(초), 걷는 속도(m/초), 동시에 가게에 있는 손님 수 한도
        public float CustomerInterval { get; set; }
        public float BrowseTime { get; set; }
        public float WalkSpeed { get; set; }
        public int MaxCustomers { get; set; }
        // 장비가 올라간 진열대 앞에서 사는 확률(0~1), 나가기 전에 들르는 곳 수의 범위
        public float BuyChance { get; set; }
        public int MinStops { get; set; }
        public int MaxStops { get; set; }
    }

    // 손님이 가게를 돌아다니는 규칙이다. 네트워크와 Unity에 의존하지 않는다.
    public static class CustomerChoice
    {
        // 들를 곳(번호는 0 ~ pointCount-1)을 순서대로 무작위로 정한다. 같은 곳을 연달아 들르지 않는다.
        public static int[] PlanRoute(int pointCount, int minStops, int maxStops, Random random)
        {
            if (pointCount <= 0)
            {
                return new int[0];
            }

            int low = Math.Max(1, minStops);
            int stops = random.Next(low, Math.Max(low, maxStops) + 1);
            var route = new int[stops];
            int previous = -1;
            for (int i = 0; i < stops; i++)
            {
                int pick = random.Next(pointCount);
                if (pointCount > 1 && pick == previous)
                {
                    pick = (pick + 1 + random.Next(pointCount - 1)) % pointCount;
                }

                route[i] = pick;
                previous = pick;
            }

            return route;
        }

        // 올려 둔 장비를 볼 때 사기로 마음먹는지 정한다.
        public static bool WillBuy(float chance, Random random)
        {
            return random.NextDouble() < chance;
        }
    }
}
