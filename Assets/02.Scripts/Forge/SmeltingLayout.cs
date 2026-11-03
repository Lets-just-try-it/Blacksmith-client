using UnityEngine;

namespace Blacksmith.Forge
{
    // 정제 작업대 배치. 좌표는 SmeltingStage 안쪽 로컬 좌표이고, 작업대 윗면이 y 0이다.
    // 무대 전체는 SmeltingStage가 줄여서 작업대 위에 올린다.
    public static class SmeltingLayout
    {
        // 원석 위 좌표 1.0이 로컬로 이 길이다.
        public const float ArenaUnit = 2.2f;
        public static readonly Vector3 BoulderPos = new Vector3(1.6f, 1.55f, -0.2f);

        public const float HearthHeight = 1.0f;
        public static readonly Vector3 HearthPos = new Vector3(-2.2f, 0f, 1.0f);
        public static readonly Vector3 PitPos = new Vector3(-2.2f, HearthHeight + 0.5f, 1.0f);
        // 화덕 왼쪽 옆. 노즐이 +x 방향으로 불구멍에 닿는다.
        public static readonly Vector3 BellowsPos = new Vector3(-4.75f, 0f, 1.0f);

        public const float RailZ = -1.05f;
        public const float MoldY = 0.1f;
        public const float CraneY = 3.25f;
        public const float HangY = 1.95f;
        public const float HangZ = RailZ;
        // 눈금까지 쇳물 높이
        public const float FillHeight = 0.62f;
        // 도가니 안에서 주둥이 위치(왼쪽)
        public static readonly Vector3 LipLocal = new Vector3(-0.95f, 0.5f, 0f);

        // 카메라는 앞(-z)에서 이 각도로 내려다본다. 원석 위 좌표 평면은 이 카메라를 정면으로 향한다.
        public const float CameraPitch = 35f;

        public static Vector3 ArenaNormal => new Vector3(0f, Mathf.Sin(CameraPitch * Mathf.Deg2Rad), -Mathf.Cos(CameraPitch * Mathf.Deg2Rad));
        public static Vector3 ArenaRight => Vector3.right;
        public static Vector3 ArenaUp => new Vector3(0f, Mathf.Cos(CameraPitch * Mathf.Deg2Rad), Mathf.Sin(CameraPitch * Mathf.Deg2Rad));
        // 원석 앞면(광맥과 금이 놓이는 곳)과 균열 표시가 놓이는 좌표 평면이 원석 중심에서 카메라 쪽으로 떨어진 거리.
        // 좌표 평면은 튀어나온 돌 조각에 가리지 않도록 원석 앞면보다 앞에 둔다.
        public const float SurfaceDepth = 0.85f;
        public const float ArenaDepth = 2.1f;

        public static Vector3 ArenaOrigin => BoulderPos + ArenaNormal * ArenaDepth;

        // 원석 위 좌표(y는 아래쪽이 +)를 로컬 위치로 바꾼다.
        public static Vector3 ArenaToLocal(float x, float y)
        {
            return ArenaOrigin + ArenaRight * (x * ArenaUnit) + ArenaUp * (-y * ArenaUnit);
        }

        // 원석 앞면 위의 같은 좌표 위치(금이 생기는 곳)
        public static Vector3 SurfaceToLocal(float x, float y)
        {
            return ArenaToLocal(x, y) - ArenaNormal * (ArenaDepth - SurfaceDepth);
        }

        // 단계마다 카메라가 바라보는 곳과 시야 크기(원래 정사영 크기). 값이 클수록 멀리서 본다.
        public static readonly Vector3 CrackFocus = BoulderPos + new Vector3(-0.8f, 0f, 0.4f);
        public static readonly Vector3 BellowsFocus = new Vector3(-3.6f, 1.0f, 0.2f);
        public static readonly Vector3 PourFocus = new Vector3(0.2f, 1.7f, RailZ);
        public const float CrackSize = 4.4f;
        public const float BellowsSize = 3.9f;
        public const float PourSize = 4.4f;
    }
}
