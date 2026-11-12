using Blacksmith.Forge;
using Blacksmith.Network;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 광장에서 채집할 던전으로 들어가는 문이다 (docs/GAME_CONCEPT.md 7.4). 갈 수 있는 곳은 전날 밤 모험가가 클리어한 스테이지 하나라 고르지 않는다.
    // 내 플레이어가 트리거에 닿으면, 채집할 곳이 있으면 PlayerAreaTravel로 맵 시작 지점에 보내고 없으면 안내를 띄운다.
    // 맵은 화면이 다 어두워진 뒤에 만든다. 호스트는 다른 플레이어를 옮기고 물리로 움직이므로, 그 플레이어가 닿으면 이동 요청이 오기 전에 바로 만든다.
    // 이동 규칙은 docs/NETWORK_DESIGN.md 3.2를 따른다.
    // 문에 붙어 서 있으면 트리거에 다시 닿은 것으로 여러 번 알려질 수 있다. 그래서 내 플레이어가 문에서 떨어질 때까지는 한 번만 반응한다.
    // 그러지 않으면 안내가 계속 다시 뜨고, 문 앞에 선 채로 밤을 넘기면 채집할 곳이 생기자마자 들어가 버린다.
    [RequireComponent(typeof(Collider))]
    public class DungeonGate : MonoBehaviour
    {
        [SerializeField] private GatheringSiteState _site;
        [SerializeField] private GatheringSiteView _siteView;
        [Tooltip("채집 맵의 시작 지점. 돌아오는 포털의 트리거 바깥에 둔다")]
        [SerializeField] private Transform _destination;
        [SerializeField] private ForgeNotice _notice;
        [Tooltip("파견 창·전투·대화 등으로 조작이 잠긴 동안에는 문에 닿아도 반응하지 않는다")]
        [SerializeField] private PlayerNetworkInputLock _inputLock;
        [SerializeField] private string _closedMessage = "던전에 몬스터가 잔뜩 있습니다. 위험합니다.";
        [Tooltip("내 플레이어가 트리거에서 이만큼(m) 떨어지면 문을 떠난 것으로 보고 다음에 닿을 때 다시 반응한다")]
        [SerializeField, Min(0f)] private float _leaveMargin = 1f;

        private Collider _trigger;
        // 문에 닿은 뒤 아직 떠나지 않은 내 플레이어
        private PlayerAreaTravel _visitor;

        private void Awake()
        {
            _trigger = GetComponent<Collider>();
            enabled = false;
        }

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
                HandleLocalPlayer(traveler, isOpen);
            }
            else if (isOpen && _site.HasStateAuthority)
            {
                _siteView.EnsureBuilt();
            }
        }

        private void HandleLocalPlayer(PlayerAreaTravel traveler, bool isOpen)
        {
            if (_visitor == traveler)
            {
                return;
            }

            // 조작이 잠긴 동안 닿은 것도 방문으로 남겨, 잠금이 풀린 뒤 문 앞에 선 채로 들어가지 않게 한다.
            _visitor = traveler;
            enabled = true;
            if (_inputLock.IsLocked)
            {
                return;
            }

            if (isOpen)
            {
                traveler.Travel(_destination.position, _destination.rotation, _siteView.EnsureBuilt);
            }
            else
            {
                _notice.Show(_closedMessage);
            }
        }

        // 방문 중일 때만 돈다. 순간이동으로 OnTriggerExit가 오지 않아도 떨어졌는지 직접 확인한다.
        private void Update()
        {
            if (_visitor == null || !IsNear(_visitor.transform.position))
            {
                _visitor = null;
                enabled = false;
            }
        }

        // 높이는 보지 않고 바닥 기준으로 본다.
        private bool IsNear(Vector3 position)
        {
            Bounds bounds = _trigger.bounds;
            bounds.Expand(new Vector3(_leaveMargin * 2f, 0f, _leaveMargin * 2f));
            return bounds.Contains(new Vector3(position.x, bounds.center.y, position.z));
        }
    }
}
