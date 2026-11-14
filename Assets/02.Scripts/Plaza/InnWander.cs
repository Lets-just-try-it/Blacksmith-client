using System.Collections.Generic;
using Blacksmith.Dialogue;
using Fusion;
using UnityEngine;

namespace Blacksmith.Plaza
{
    // 여관 모험가가 InnWaypoint로 이은 지점을 따라 돌아다닌다. 지점에 도착하면 잠시 멈췄다가 이웃 지점 하나로 걷는다.
    // 다른 모험가의 침대 자리에는 가지 않는다. 호스트만 움직이고 NetworkTransform이 위치·방향을 복제한다.
    // 무작위 지점은 게임 상태에 영향이 없는 연출이라 시드를 저장하지 않는다(CandidateWander와 같다).
    // 누군가 말을 걸고 있으면 그 자리에 멈춰 말을 건 플레이어를 바라본다.
    public class InnWander : NetworkBehaviour
    {
        [SerializeField] private DialogueSeat _seat;
        [Tooltip("걷는 속도(m/s)")]
        [SerializeField, Min(0.1f)] private float _walkSpeed = 1f;
        [Tooltip("걷는 방향으로 도는 속도(도/s)")]
        [SerializeField, Min(1f)] private float _turnSpeed = 300f;
        [Tooltip("지점에 도착해 멈춰 있는 시간 범위(초). 지점의 IdleScale을 곱한다")]
        [SerializeField] private Vector2 _idleSeconds = new Vector2(2f, 6f);

        private readonly List<InnWaypoint> _choices = new List<InnWaypoint>();

        // 호스트에서만 쓴다.
        private System.Random _random;
        private InnWaypoint _bed;
        private InnWaypoint _current;
        private InnWaypoint _previous;
        private Vector3 _target;
        private bool _isWalking;
        private TickTimer _idleTimer;

        // 호스트 전용. 스폰 직전(onBeforeSpawned)에 불러 자기 침대 자리에서 시작하게 한다.
        public void Initialize(InnWaypoint bed)
        {
            _bed = bed;
            _current = bed;
        }

        public override void Spawned()
        {
            if (!HasStateAuthority)
            {
                return;
            }

            _random = new System.Random(unchecked((int)Object.Id.Raw * 31 + Runner.Tick.Raw));
            BeginIdle(1f);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || _current == null)
            {
                return;
            }

            // 걷던 길을 벗어나면 벽을 지나칠 수 있으므로, 대화가 끝나면 하던 걸음을 그대로 이어 간다.
            if (_seat.IsBusy)
            {
                FaceTalker(Runner.DeltaTime);
                return;
            }

            if (_isWalking)
            {
                Walk(Runner.DeltaTime);
            }
            else if (_idleTimer.ExpiredOrNotRunning(Runner))
            {
                PickNext();
            }
        }

        private void Walk(float deltaTime)
        {
            TurnTowards(_target, deltaTime);
            transform.position = Vector3.MoveTowards(transform.position, _target, _walkSpeed * deltaTime);
            if ((transform.position - _target).sqrMagnitude > 0.0001f)
            {
                return;
            }

            if (_current.IdleScale <= 0f)
            {
                // 문처럼 지나가는 지점은 멈추지 않는다.
                PickNext();
            }
            else
            {
                BeginIdle(_current.IdleScale);
            }
        }

        private void PickNext()
        {
            _choices.Clear();
            foreach (InnWaypoint neighbor in _current.Neighbors)
            {
                if (neighbor != null && (!neighbor.IsBed || neighbor == _bed))
                {
                    _choices.Add(neighbor);
                }
            }

            // 지나가는 지점에서 되돌아가면 문 앞에서 서성이는 것처럼 보이므로, 다른 길이 있으면 온 곳을 뺀다.
            if (_current.IdleScale <= 0f && _choices.Count > 1)
            {
                _choices.Remove(_previous);
            }

            if (_choices.Count == 0)
            {
                BeginIdle(1f);
                return;
            }

            _previous = _current;
            _current = _choices[_random.Next(_choices.Count)];
            _target = _current.PickPoint(_random);
            _isWalking = true;
        }

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

        private void BeginIdle(float scale)
        {
            _isWalking = false;
            float min = Mathf.Min(_idleSeconds.x, _idleSeconds.y);
            float max = Mathf.Max(_idleSeconds.x, _idleSeconds.y);
            float seconds = (min + (float)_random.NextDouble() * (max - min)) * scale;
            _idleTimer = TickTimer.CreateFromSeconds(Runner, seconds);
        }
    }
}
