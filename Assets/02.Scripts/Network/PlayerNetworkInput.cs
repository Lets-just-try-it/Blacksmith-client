using Fusion;
using UnityEngine;

namespace Blacksmith.Network
{
    public enum PlayerButton
    {
        Interact = 0,
    }

    // 틱마다 호스트로 보내는 입력이다. 입력 장치를 모르는 데이터만 담는다.
    public struct PlayerNetworkInput : INetworkInput
    {
        // 카메라 기준 입력을 월드 XZ 평면 방향으로 바꾼 값이다.
        public Vector2 MoveDirection;
        public NetworkButtons Buttons;
    }
}
