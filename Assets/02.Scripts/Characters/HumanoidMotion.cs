using UnityEngine;

namespace Blacksmith.Characters
{
    // 인간형 모델의 걷기·달리기 동작을 실제로 움직인 속도에 맞춘다. 표시 전용이라 프레임 시간으로 잰다.
    // 위치는 네트워크가 정하므로 입력이나 상태를 읽지 않고 화면에 보이는 위치 변화만 본다.
    [RequireComponent(typeof(Animator))]
    public class HumanoidMotion : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        [Tooltip("한 프레임에 이보다 멀리 움직이면 순간이동으로 보고 속도에 넣지 않는다")]
        [SerializeField] private float _teleportDistance = 1f;
        [Tooltip("속도 변화를 부드럽게 따라가는 시간(초)")]
        [SerializeField] private float _dampTime = 0.1f;

        private Animator _animator;
        private Vector3 _lastPosition;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            _lastPosition = transform.position;
        }

        private void LateUpdate()
        {
            Vector3 position = transform.position;
            Vector3 delta = position - _lastPosition;
            delta.y = 0f;
            _lastPosition = position;

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f || delta.magnitude > _teleportDistance)
            {
                return;
            }

            _animator.SetFloat(SpeedHash, delta.magnitude / deltaTime, _dampTime, deltaTime);
        }
    }
}
