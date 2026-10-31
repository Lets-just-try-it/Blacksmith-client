using Fusion;
using UnityEngine;

namespace Blacksmith.Network
{
    // 플레이어 오브젝트 본체다. 영역별 행동 요청 RPC는 이 클래스가 아니라 Player 프리팹에 붙이는
    // 영역별 행동 컴포넌트(예: ForgePlayerActions)에 둔다. 이 클래스에는 플레이어 자체에 관한 요청만 둔다.
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
