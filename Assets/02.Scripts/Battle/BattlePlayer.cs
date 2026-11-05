using System;
using System.Collections.Generic;
using Blacksmith.Core.Battle;
using UnityEngine;

namespace Blacksmith.Battle
{
    // BattleSimulation을 틱 속도에 맞춰 진행하며 연출로 재생한다.
    // 결과에는 영향을 주지 않는 표시 전용 처리라 Update()에서 진행한다.
    public class BattlePlayer : MonoBehaviour
    {
        [SerializeField] private Transform _unitRoot;
        [SerializeField] private Transform _pathOrigin;
        [Tooltip("유닛 체력바가 바라볼 카메라")]
        [SerializeField] private Camera _camera;
        [Tooltip("시뮬레이션 위치 1이 이동하는 월드 거리와 방향 (쿼터뷰 길 방향)")]
        [SerializeField] private Vector3 _pathStep = new Vector3(0.01f, 0f, 0.005f);
        [Tooltip("옆자리 한 칸당 벌어지는 월드 거리 (BattleUnit.Lane은 반 칸 단위)")]
        [SerializeField] private Vector3 _laneStep = new Vector3(-0.3f, 0f, 0.6f);
        [SerializeField, Min(0.1f)] private float _playbackSpeed = 1f;

        private readonly List<BattleUnitView> _views = new List<BattleUnitView>();
        private readonly List<int> _previousPositions = new List<int>();

        private BattleSimulation _simulation;
        private Func<BattleUnit, BattleUnitView> _getPrefab;
        private float _tickDuration;
        private float _elapsed;
        private float _holdRemaining;

        public event Action<BattleUnitView> UnitSpawned;
        public event Action<BattleUnitView> UnitEnraged;
        public event Action<BattleUnitView> UnitFell;
        public event Action TickAdvanced;
        public event Action<BattleResult> Finished;

        public IReadOnlyList<BattleUnitView> Views => _views;
        public bool IsPlaying => _simulation != null && !_simulation.IsFinished;
        public bool IsHolding => _holdRemaining > 0f;

        public float PlaybackSpeed
        {
            get => _playbackSpeed;
            set => _playbackSpeed = Mathf.Max(0.1f, value);
        }

        public void Play(BattleSetup setup, int ticksPerSecond, Func<BattleUnit, BattleUnitView> getPrefab)
        {
            Clear();
            _simulation = new BattleSimulation(setup);
            _getPrefab = getPrefab;
            _tickDuration = 1f / ticksPerSecond;
            SpawnNewViews();
        }

        // 보스 등장 연출처럼 화면을 보여 주는 동안 재생을 멈춘다. 시뮬레이션 결과는 바뀌지 않는다.
        public void Hold(float seconds)
        {
            _holdRemaining = Mathf.Max(_holdRemaining, seconds);
            foreach (BattleUnitView view in _views)
            {
                view.SetMoving(false);
            }
        }

        private void Update()
        {
            if (!IsPlaying)
            {
                return;
            }

            if (IsHolding)
            {
                _holdRemaining -= Time.deltaTime;
                return;
            }

            _elapsed += Time.deltaTime * _playbackSpeed;
            while (_elapsed >= _tickDuration && IsPlaying && !IsHolding)
            {
                _elapsed -= _tickDuration;
                StepOnce();
            }

            UpdateViewPositions(Mathf.Clamp01(_elapsed / _tickDuration));
        }

        private void StepOnce()
        {
            for (int i = 0; i < _views.Count; i++)
            {
                _previousPositions[i] = _views[i].Unit.Position;
            }

            IReadOnlyList<BattleEvent> events = _simulation.Step();
            SpawnNewViews();
            for (int i = 0; i < events.Count; i++)
            {
                HandleEvent(events[i]);
            }

            UpdateMovingState();
            TickAdvanced?.Invoke();

            if (_simulation.IsFinished)
            {
                UpdateViewPositions(1f);
                Finished?.Invoke(_simulation.Result);
            }
        }

        private void SpawnNewViews()
        {
            IReadOnlyList<BattleUnit> units = _simulation.Units;
            for (int i = _views.Count; i < units.Count; i++)
            {
                BattleUnit unit = units[i];
                BattleUnitView view = Instantiate(_getPrefab(unit), ToWorld(unit.Position, unit.Lane), GetSpawnRotation(unit), _unitRoot);
                view.Bind(unit, _camera.transform.rotation);
                _views.Add(view);
                _previousPositions.Add(unit.Position);
                UnitSpawned?.Invoke(view);
            }
        }

        private void HandleEvent(BattleEvent battleEvent)
        {
            switch (battleEvent.Type)
            {
                case BattleEventType.BasicAttack:
                    _views[battleEvent.SourceUnitId].PlayAttack();
                    break;
                case BattleEventType.SkillCast:
                    _views[battleEvent.SourceUnitId].PlaySkill();
                    break;
                case BattleEventType.Damaged:
                    _views[battleEvent.TargetUnitId].PlayHit();
                    _views[battleEvent.TargetUnitId].RefreshHp();
                    break;
                case BattleEventType.Healed:
                    _views[battleEvent.TargetUnitId].PlayHeal();
                    _views[battleEvent.TargetUnitId].RefreshHp();
                    break;
                case BattleEventType.Fell:
                    _views[battleEvent.TargetUnitId].PlayFall();
                    _views[battleEvent.TargetUnitId].RefreshHp();
                    UnitFell?.Invoke(_views[battleEvent.TargetUnitId]);
                    break;
                case BattleEventType.Enraged:
                    _views[battleEvent.SourceUnitId].PlayEnrage();
                    UnitEnraged?.Invoke(_views[battleEvent.SourceUnitId]);
                    break;
                case BattleEventType.StageStarted:
                    HideFallenUnits();
                    break;
            }
        }

        // 쓰러진 유닛은 그 스테이지가 끝날 때까지 쓰러진 채로 보이고, 다음 스테이지에는 나오지 않는다.
        private void HideFallenUnits()
        {
            foreach (BattleUnitView view in _views)
            {
                if (view.Unit.IsFallen)
                {
                    view.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateMovingState()
        {
            for (int i = 0; i < _views.Count; i++)
            {
                if (_views[i].Unit.IsAlive)
                {
                    _views[i].SetMoving(!IsHolding && _views[i].Unit.Position != _previousPositions[i]);
                }
            }
        }

        private void UpdateViewPositions(float alpha)
        {
            for (int i = 0; i < _views.Count; i++)
            {
                BattleUnit unit = _views[i].Unit;
                float position = Mathf.Lerp(_previousPositions[i], unit.Position, alpha);
                _views[i].transform.position = ToWorld(position, unit.Lane);
            }
        }

        // 인간형 모델의 정면은 +Z라, 모험가는 진행 방향(위치가 늘어나는 _pathStep 쪽)을 보게 돌려 생성한다.
        // 몬스터 프리팹은 지금 방향 그대로 둔다.
        private Quaternion GetSpawnRotation(BattleUnit unit)
        {
            Vector3 forward = Vector3.ProjectOnPlane(_pathStep, Vector3.up);
            if (unit.Team != BattleTeam.Adventurer || forward.sqrMagnitude < Mathf.Epsilon)
            {
                return Quaternion.identity;
            }

            return Quaternion.LookRotation(forward, Vector3.up);
        }

        // BattleUnit.Lane은 반 칸 단위라 절반을 곱한다.
        private Vector3 ToWorld(float position, int lane)
        {
            return _pathOrigin.position + _pathStep * position + _laneStep * (lane * 0.5f);
        }

        private void Clear()
        {
            foreach (BattleUnitView view in _views)
            {
                Destroy(view.gameObject);
            }

            _views.Clear();
            _previousPositions.Clear();
            _elapsed = 0f;
            _holdRemaining = 0f;
        }
    }
}
