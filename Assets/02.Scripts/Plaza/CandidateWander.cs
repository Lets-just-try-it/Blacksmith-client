using Blacksmith.Dialogue;
using Fusion;
using UnityEngine;

namespace Blacksmith.Plaza
{
    // 영입 후보가 스폰된 자리 주변을 천천히 걷다 멈추기를 반복한다. 누군가 말을 걸고 있으면 그 자리에 멈춰 말을 건 플레이어를 바라본다.
    // 호스트만 움직이고 NetworkTransform이 위치·방향을 복제한다. 걷기 동작은 모델의 HumanoidMotion이 위치 변화를 보고 맞춘다.
    // 무작위 지점은 게임 상태에 영향이 없는 연출이라 시드를 저장하지 않는다.
    public class CandidateWander : NetworkBehaviour
    {
        [SerializeField] private DialogueSeat _seat;
        [Tooltip("스폰된 자리에서 이 거리 안에서만 걷는다(m)")]
        [SerializeField, Min(0f)] private float _radius = 1.5f;
        [Tooltip("걷는 속도(m/s)")]
        [SerializeField, Min(0.1f)] private float _walkSpeed = 0.7f;
        [Tooltip("걷는 방향으로 도는 속도(도/s)")]
        [SerializeField, Min(1f)] private float _turnSpeed = 240f;
        [Tooltip("다음 지점으로 걷기 전에 멈춰 있는 시간 범위(초)")]
        [SerializeField] private Vector2 _idleSeconds = new Vector2(2f, 5f);
        [Tooltip("이보다 짧은 거리는 걷지 않고 다시 멈춘다(m)")]
        [SerializeField, Min(0.01f)] private float _minStep = 0.4f;

        // 호스트에서만 쓴다.
        private System.Random _random;
        private Vector3 _home;
        private Vector3 _target;
        private bool _isWalking;
        private bool _wasBusy;
        private TickTimer _idleTimer;

        public override void Spawned()
        {
            if (!HasStateAuthority)
            {
                return;
            }

            _random = new System.Random(unchecked((int)Object.Id.Raw * 31 + Runner.Tick.Raw));
            _home = transform.position;
            BeginIdle();
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
            {
                return;
            }

            if (_seat.IsBusy)
            {
                _isWalking = false;
                _wasBusy = true;
                FaceTalker(Runner.DeltaTime);
                return;
            }

            if (_wasBusy)
            {
                // 대화가 끝나면 바로 걷지 않고 잠시 서 있다가 움직인다.
                _wasBusy = false;
                BeginIdle();
                return;
            }

            if (_isWalking)
            {
                Walk(Runner.DeltaTime);
            }
            else if (_idleTimer.ExpiredOrNotRunning(Runner))
            {
                PickTarget();
            }
        }

        private void Walk(float deltaTime)
        {
            TurnTowards(_target, deltaTime);
            transform.position = Vector3.MoveTowards(transform.position, _target, _walkSpeed * deltaTime);
            if ((transform.position - _target).sqrMagnitude < 0.0001f)
            {
                BeginIdle();
            }
        }

        // 말을 건 플레이어를 바라본다. 대화가 끝나도 되돌리지 않고, 다음에 걷기 시작할 때 걷는 방향으로 돈다.
        private void FaceTalker(float deltaTime)
        {
            if (Runner.TryGetPlayerObject(_seat.Talker, out NetworkObject talker))
            {
                TurnTowards(talker.transform.position, deltaTime);
            }
        }

        private void TurnTowards(Vector3 position, float deltaTime)
        {
            Vector3 direction = position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion facing = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, _turnSpeed * deltaTime);
        }

        private void PickTarget()
        {
            double angle = _random.NextDouble() * Mathf.PI * 2f;
            // 반경 안에 고르게 흩어지도록 거리에 제곱근을 쓴다.
            float distance = _radius * Mathf.Sqrt((float)_random.NextDouble());
            Vector3 target = _home + new Vector3(Mathf.Cos((float)angle), 0f, Mathf.Sin((float)angle)) * distance;
            target.y = transform.position.y;

            if ((target - transform.position).magnitude < _minStep)
            {
                BeginIdle();
                return;
            }

            _target = target;
            _isWalking = true;
        }

        private void BeginIdle()
        {
            _isWalking = false;
            float min = Mathf.Min(_idleSeconds.x, _idleSeconds.y);
            float max = Mathf.Max(_idleSeconds.x, _idleSeconds.y);
            float seconds = min + (float)_random.NextDouble() * (max - min);
            _idleTimer = TickTimer.CreateFromSeconds(Runner, seconds);
        }
    }
}
