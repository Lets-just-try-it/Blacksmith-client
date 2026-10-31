using Fusion;
using UnityEngine;

namespace Blacksmith.Network
{
    // 입력 권한을 가진 클라이언트는 예측으로, 호스트는 받은 입력으로 같은 이동을 계산한다.
    public class PlayerMovement : NetworkBehaviour
    {
        [SerializeField] private NetworkCharacterController _controller;

        public override void FixedUpdateNetwork()
        {
            // 다른 플레이어의 캐릭터는 입력이 없고, 호스트가 동기화한 위치를 그대로 받는다.
            if (!GetInput(out PlayerNetworkInput input))
            {
                return;
            }

            _controller.Move(new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y));
        }
    }
}
