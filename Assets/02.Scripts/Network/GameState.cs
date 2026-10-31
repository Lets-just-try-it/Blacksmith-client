using Fusion;

namespace Blacksmith.Network
{
    // 모든 영역이 공유하는 게임 상태를 둔다. 호스트만 값을 바꾸고, 클라이언트는 복제된 값을 읽기만 한다.
    // [Networked] 필드는 각 영역이 필요할 때 추가한다.
    public class GameState : NetworkBehaviour
    {
    }
}
