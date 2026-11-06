using System;

namespace Blacksmith.Core.Forge
{
    // 목걸이 모양. 모양마다 체인 줄(가닥)의 곡선과 보석 자리가 정해져 있다.
    // 좌표는 작업판 위의 칸(1칸 = 50px)이고, 판의 왼쪽 위가 (0,0)이다.
    //   난이도 0 = 단순한 목걸이: 걸쇠 두 개를 잇는 U자 목걸이, 가운데 펜던트
    //   난이도 1 = 복잡한 목걸이: U자 목걸이 + 펜던트 아래로 늘어지는 줄(조각 하나)
    //   난이도 2 = 매우 복잡한 목걸이: 이중 U자(바깥/안쪽 줄)와 두 펜던트, 아래로 늘어지는 줄
    // 가닥은 시작에서 끝으로 향하고, 조각의 왼쪽 끝이 시작 쪽, 오른쪽 끝이 끝 쪽이다.
    public static class NecklaceShape
    {
        public const float BoardW = 13.2f;
        public const float BoardH = 7.2f;

        // 2차 곡선의 세 점(시작, 조절, 끝)
        public struct Strand
        {
            public float X0, Y0, X1, Y1, X2, Y2;
        }

        public static readonly string[] Names = { "단순한 목걸이", "복잡한 목걸이", "매우 복잡한 목걸이" };

        // 모양마다 가닥별 조각 수
        public static readonly int[][] Lens = { new[] { 3, 3 }, new[] { 4, 4, 1 }, new[] { 4, 4, 3, 3, 1 } };

        // 보석(펜던트)이 놓이는 자리: x, y, 반지름(칸). 첫 번째가 주 보석이다.
        public static readonly float[][][] Gems =
        {
            new[] { new[] { 6.6f, 6.2f, 0.5f } },
            new[] { new[] { 6.6f, 4.6f, 0.5f } },
            new[] { new[] { 6.6f, 4.7f, 0.5f }, new[] { 6.6f, 3.3f, 0.38f } },
        };

        // 곡선의 양 끝이 거의 수직으로 서면 조각의 어느 끝이 어느 쪽인지 알아보기 어렵다.
        // 그래서 목걸이 줄은 완만하게 휘고(끝 각도 약 60도 이하), 걸쇠를 낮추고 폭을 넓혔다. 아래로 늘어지는 줄만 수직이다.
        private static readonly Strand[][] All =
        {
            new[]
            {
                S(50, 90, 150, 240, 288, 310),
                S(372, 310, 510, 240, 610, 90),
            },
            new[]
            {
                S(35, 40, 134, 171, 288, 230),
                S(372, 230, 526, 171, 625, 40),
                S(330, 262, 330, 310, 330, 352),
            },
            new[]
            {
                S(30, 35, 128, 175, 288, 235),
                S(372, 235, 532, 175, 630, 35),
                S(80, 20, 161, 125, 288, 165),
                S(372, 165, 499, 125, 580, 20),
                S(330, 267, 330, 310, 330, 353),
            },
        };

        private static Strand S(float x0, float y0, float x1, float y1, float x2, float y2)
        {
            return new Strand { X0 = x0 / 50f, Y0 = y0 / 50f, X1 = x1 / 50f, Y1 = y1 / 50f, X2 = x2 / 50f, Y2 = y2 / 50f };
        }

        public static Strand[] Of(int difficulty)
        {
            return All[Math.Max(0, Math.Min(All.Length - 1, difficulty))];
        }

        public static void Point(Strand strand, float t, out float x, out float y)
        {
            float u = 1f - t;
            x = u * u * strand.X0 + 2f * u * t * strand.X1 + t * t * strand.X2;
            y = u * u * strand.Y0 + 2f * u * t * strand.Y1 + t * t * strand.Y2;
        }

        public static float Length(Strand strand)
        {
            float length = 0f;
            Point(strand, 0f, out float px, out float py);
            for (int i = 1; i <= 48; i++)
            {
                Point(strand, i / 48f, out float x, out float y);
                length += (float)Math.Sqrt((x - px) * (x - px) + (y - py) * (y - py));
                px = x;
                py = y;
            }

            return length;
        }

        // 시작에서 distance(칸)만큼 간 곳의 위치와 방향(도)
        public static void At(Strand strand, float distance, out float x, out float y, out float angleDeg)
        {
            float accumulated = 0f;
            Point(strand, 0f, out float px, out float py);
            float found = 1f;
            for (int i = 1; i <= 64; i++)
            {
                float t = i / 64f;
                Point(strand, t, out float cx, out float cy);
                float segment = (float)Math.Sqrt((cx - px) * (cx - px) + (cy - py) * (cy - py));
                if (accumulated + segment >= distance)
                {
                    float fraction = segment < 0.0001f ? 0f : (distance - accumulated) / segment;
                    found = (i - 1 + fraction) / 64f;
                    break;
                }

                accumulated += segment;
                px = cx;
                py = cy;
            }

            Point(strand, found, out x, out y);
            float u = 1f - found;
            float dx = 2f * u * (strand.X1 - strand.X0) + 2f * found * (strand.X2 - strand.X1);
            float dy = 2f * u * (strand.Y1 - strand.Y0) + 2f * found * (strand.Y2 - strand.Y1);
            angleDeg = (float)(Math.Atan2(dy, dx) * 180.0 / Math.PI);
        }

        // 가닥 strand의 index번째 칸(조각이 length개일 때)의 가운데 위치와 방향
        public static void Slot(int difficulty, int strand, int index, int length, out float x, out float y, out float angleDeg)
        {
            Strand s = Of(difficulty)[strand];
            float total = Length(s);
            float margin = Math.Min(0.8f, total * 0.28f);
            float distance = margin + (total - 2f * margin) * (index + 0.5f) / length;
            At(s, distance, out x, out y, out angleDeg);
        }
    }
}
