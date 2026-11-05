using UnityEngine;

namespace Blacksmith.Network
{
    // 내 플레이어가 트리거에 닿으면 도착 지점으로 옮긴다. 대장간과 광장처럼 한 씬 안의 구역을 잇는다.
    // 닿았는지는 각 클라이언트가 자기 플레이어만 확인하고, 실제 이동은 PlayerAreaTravel이 호스트에 요청한다.
    [RequireComponent(typeof(Collider))]
    public class AreaPortal : MonoBehaviour
    {
        [Tooltip("도착 위치와 바라볼 방향. 반대편 포털의 트리거 바깥에 둔다.")]
        [SerializeField] private Transform _destination;

        private void OnTriggerEnter(Collider other)
        {
            PlayerAreaTravel traveler = other.GetComponentInParent<PlayerAreaTravel>();
            if (traveler != null && traveler.HasInputAuthority)
            {
                traveler.Travel(_destination.position, _destination.rotation);
            }
        }
    }
}
