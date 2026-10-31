using Fusion;
using UnityEngine;

namespace Blacksmith.Network
{
    // 플레이어 오브젝트다. 클라이언트의 행동 요청 RPC(InputAuthority → StateAuthority)는 이 클래스에 둔다.
    public class NetworkPlayer : NetworkBehaviour
    {
        [Tooltip("내 캐릭터를 구분하는 임시 표시. 캐릭터 외형이 생기면 대체한다.")]
        [SerializeField] private GameObject _localMarker;

        public override void Spawned()
        {
            name = $"Player {Object.InputAuthority}";
            _localMarker.SetActive(HasInputAuthority);
        }
    }
}
