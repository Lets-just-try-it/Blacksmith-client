using Blacksmith.Forge;
using Blacksmith.Network;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 광장에서 채집할 던전으로 들어가는 문이다 (docs/GAME_CONCEPT.md 7.4). 갈 수 있는 곳은 전날 밤 모험가가 클리어한 스테이지 하나라 고르지 않는다.
    // 내 플레이어가 트리거에 닿으면, 채집할 곳이 있으면 PlayerAreaTravel로 맵 시작 지점에 보내고 없으면 안내를 띄운다.
    // 맵은 화면이 다 어두워진 뒤에 만든다. 호스트는 다른 플레이어를 옮기고 물리로 움직이므로, 그 플레이어가 닿으면 이동 요청이 오기 전에 바로 만든다.
    // 이동 규칙은 docs/NETWORK_DESIGN.md 3.2를 따른다.
    [RequireComponent(typeof(Collider))]
    public class DungeonGate : MonoBehaviour
    {
        [SerializeField] private GatheringSiteState _site;
        [SerializeField] private GatheringSiteView _siteView;
        [Tooltip("채집 맵의 시작 지점. 돌아오는 포털의 트리거 바깥에 둔다")]
        [SerializeField] private Transform _destination;
        [SerializeField] private ForgeNotice _notice;
        [SerializeField] private string _closedMessage = "던전에 몬스터가 잔뜩 있습니다. 위험합니다.";

        private void OnTriggerEnter(Collider other)
        {
            PlayerAreaTravel traveler = other.GetComponentInParent<PlayerAreaTravel>();
            if (traveler == null)
            {
                return;
            }

            bool isOpen = _site.IsReady && _site.HasSite;
            if (traveler.HasInputAuthority)
            {
                if (isOpen)
                {
                    traveler.Travel(_destination.position, _destination.rotation, _siteView.EnsureBuilt);
                }
                else
                {
                    _notice.Show(_closedMessage);
                }
            }
            else if (isOpen && _site.HasStateAuthority)
            {
                _siteView.EnsureBuilt();
            }
        }
    }
}
