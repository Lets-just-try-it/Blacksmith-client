using Blacksmith.Core.Battle;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 맵을 걷는 모험가 파티의 중심을 따라간다. 전투가 시작되면 파티 위치로 바로 옮긴다. 분노는 보스만 하므로 분노할 때 화면을 흔든다.
    public class BattleCameraFollow : MonoBehaviour
    {
        [SerializeField] private BattlePlayer _player;
        [SerializeField] private Vector3 _offset = new Vector3(-6f, 9f, -9f);
        [SerializeField, Min(0f)] private float _smoothTime = 0.4f;
        [SerializeField, Min(0f)] private float _enrageShakeStrength = 0.3f;
        [SerializeField, Min(0.01f)] private float _enrageShakeDuration = 0.6f;

        private Vector3 _velocity;
        private Vector3 _followPosition;
        private float _shakeRemaining;
        private bool _hasTarget;

        private void Awake()
        {
            _followPosition = transform.position;
        }

        private void OnEnable()
        {
            _player.UnitEnraged += HandleUnitEnraged;
        }

        private void OnDisable()
        {
            _player.UnitEnraged -= HandleUnitEnraged;
        }

        private void LateUpdate()
        {
            bool hasTarget = TryGetPartyCenter(out Vector3 center);
            if (hasTarget && !_hasTarget)
            {
                _followPosition = center + _offset;
                _velocity = Vector3.zero;
            }

            _hasTarget = hasTarget;
            if (hasTarget)
            {
                _followPosition = Vector3.SmoothDamp(_followPosition, center + _offset, ref _velocity, _smoothTime);
            }

            transform.position = _followPosition + GetShakeOffset();
        }

        private void HandleUnitEnraged(BattleUnitView view)
        {
            _shakeRemaining = _enrageShakeDuration;
        }

        private Vector3 GetShakeOffset()
        {
            if (_shakeRemaining <= 0f)
            {
                return Vector3.zero;
            }

            _shakeRemaining -= Time.deltaTime;
            float strength = _enrageShakeStrength * Mathf.Clamp01(_shakeRemaining / _enrageShakeDuration);
            return Random.insideUnitSphere * strength;
        }

        private bool TryGetPartyCenter(out Vector3 center)
        {
            center = Vector3.zero;
            int count = 0;
            foreach (BattleUnitView view in _player.Views)
            {
                if (view.Unit.Team == BattleTeam.Adventurer && view.Unit.IsAlive)
                {
                    center += view.transform.position;
                    count++;
                }
            }

            if (count == 0)
            {
                return false;
            }

            center /= count;
            return true;
        }
    }
}
