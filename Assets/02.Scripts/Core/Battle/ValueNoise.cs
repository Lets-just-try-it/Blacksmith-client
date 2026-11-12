using System;

namespace Blacksmith.Core.Battle
{
    // 시드로 정해지는 2D 값 노이즈. 격자점마다 정수 해시로 값을 두고 부드럽게 보간해 -1 ~ 1을 준다.
    // UnityEngine에 기대지 않고 정수 연산으로 격자 값을 정해 기기마다 같은 값이 나온다.
    internal sealed class ValueNoise
    {
        private readonly uint _seed;

        public ValueNoise(int seed)
        {
            _seed = unchecked((uint)seed);
        }

        public float Sample(float x, float z)
        {
            int x0 = (int)Math.Floor(x);
            int z0 = (int)Math.Floor(z);
            float tx = Smooth(x - x0);
            float tz = Smooth(z - z0);
            float bottom = Lerp(Lattice(x0, z0), Lattice(x0 + 1, z0), tx);
            float top = Lerp(Lattice(x0, z0 + 1), Lattice(x0 + 1, z0 + 1), tx);
            return Lerp(bottom, top, tz);
        }

        // 굵은 굴곡 위에 절반 크기의 잔굴곡을 더한다.
        public float Fractal(float x, float z)
        {
            return (Sample(x, z) + Sample(x * 2f + 17.3f, z * 2f - 9.1f) * 0.5f) / 1.5f;
        }

        private float Lattice(int x, int z)
        {
            uint hash = unchecked(_seed ^ (uint)x * 0x27D4EB2Du ^ (uint)z * 0x165667B1u);
            hash = unchecked((hash ^ (hash >> 15)) * 0x85EBCA6Bu);
            hash = unchecked((hash ^ (hash >> 13)) * 0xC2B2AE35u);
            hash ^= hash >> 16;
            return hash / (float)uint.MaxValue * 2f - 1f;
        }

        private static float Smooth(float t)
        {
            return t * t * (3f - 2f * t);
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
    }
}
