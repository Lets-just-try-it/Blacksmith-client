using UnityEngine;

namespace Blacksmith.Plaza
{
    // 여관 안에서 모험가가 걸어갈 수 있는 지점이다. 이웃으로 이은 지점 사이만 곧게 걷는다.
    // 벽을 뚫지 않도록, 서로 이은 두 지점(반경 포함)은 벽 없이 볼록한 한 공간(복도, 방) 안에 둔다. 방과 복도는 문 지점으로 잇는다.
    // 높이는 모험가 루트 높이(바닥 + 1m)에 맞춘다.
    public class InnWaypoint : MonoBehaviour
    {
        [SerializeField] private InnWaypoint[] _neighbors = new InnWaypoint[0];
        [Tooltip("도착할 자리를 이 반경 안에서 고른다(m). 문 지점은 0으로 둔다")]
        [SerializeField, Min(0f)] private float _radius;
        [Tooltip("도착해서 멈춰 있는 시간 배율. 0이면 멈추지 않고 바로 다음 지점으로 간다(문 지점)")]
        [SerializeField, Min(0f)] private float _idleScale = 1f;
        [Tooltip("침대 옆 자리. 이 침대를 쓰는 모험가만 들어온다")]
        [SerializeField] private bool _isBed;

        public InnWaypoint[] Neighbors => _neighbors;
        public float IdleScale => _idleScale;
        public bool IsBed => _isBed;

        public Vector3 PickPoint(System.Random random)
        {
            if (_radius <= 0f)
            {
                return transform.position;
            }

            double angle = random.NextDouble() * Mathf.PI * 2f;
            // 반경 안에 고르게 흩어지도록 거리에 제곱근을 쓴다.
            float distance = _radius * Mathf.Sqrt((float)random.NextDouble());
            return transform.position + new Vector3(Mathf.Cos((float)angle), 0f, Mathf.Sin((float)angle)) * distance;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _isBed ? Color.magenta : _idleScale <= 0f ? Color.yellow : Color.cyan;
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(_radius, 0.15f));
            Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
            foreach (InnWaypoint neighbor in _neighbors)
            {
                if (neighbor != null)
                {
                    Gizmos.DrawLine(transform.position, neighbor.transform.position);
                }
            }
        }
    }
}
