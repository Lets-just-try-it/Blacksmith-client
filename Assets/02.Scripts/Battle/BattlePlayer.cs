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
        [Tooltip("유닛 체력바가 바라볼 카메라")]
        [SerializeField] private Camera _camera;
        [SerializeField, Min(0.1f)] private float _playbackSpeed = 1f;

        private readonly List<BattleUnitView> _views = new List<BattleUnitView>();
        private readonly List<Vector2Int> _previousPositions = new List<Vector2Int>();

        private BattleSimulation _simulation;
        private Func<BattleUnit, BattleUnitView> _getPrefab;
        private Func<float, float, Vector3> _toWorld;
        private float _tickDuration;
        private float _elapsed;
        private float _holdRemaining;
        private int _monsterCount;
        private int _defeatedCount;

        public event Action<BattleUnitView> UnitSpawned;
        public event Action<BattleUnitView> UnitEnraged;
        public event Action<BattleUnitView> UnitFell;
        public event Action TickAdvanced;
        // 모험가를 발견해 다가오기 시작한 무리 번호(BattleSetup.Groups 순서)
        public event Action<int> GroupEngaged;
        // 토벌한 몬스터 수와 이 스테이지의 전체 몬스터 수
        public event Action<int, int> ProgressChanged;
        public event Action<BattleResult> Finished;

        public IReadOnlyList<BattleUnitView> Views => _views;
        public bool IsPlaying => _simulation != null && !_simulation.IsFinished;
        public bool IsHolding => _holdRemaining > 0f;

        public float PlaybackSpeed
        {
            get => _playbackSpeed;
            set => _playbackSpeed = Mathf.Max(0.1f, value);
        }

        // toWorld는 전투 좌표(BattleSpace 단위)를 월드 좌표로 바꾼다.
        public void Play(BattleSetup setup, int ticksPerSecond, Func<BattleUnit, BattleUnitView> getPrefab, Func<float, float, Vector3> toWorld)
        {
            Clear();
            _simulation = new BattleSimulation(setup);
            _getPrefab = getPrefab;
            _toWorld = toWorld;
            _tickDuration = 1f / ticksPerSecond;
            _monsterCount = 0;
            foreach (MonsterGroupSetup group in setup.Groups)
            {
                _monsterCount += group.Monsters.Count;
            }

            _defeatedCount = 0;
            SpawnNewViews();
            ProgressChanged?.Invoke(_defeatedCount, _monsterCount);
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
                _previousPositions[i] = new Vector2Int(_views[i].Unit.X, _views[i].Unit.Z);
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
                BattleUnitView view = Instantiate(_getPrefab(unit), _toWorld(unit.X, unit.Z), Quaternion.identity, _unitRoot);
                view.Bind(unit, _camera.transform.rotation);
                _views.Add(view);
                _previousPositions.Add(new Vector2Int(unit.X, unit.Z));
                UnitSpawned?.Invoke(view);
            }
        }

        private void HandleEvent(BattleEvent battleEvent)
        {
            switch (battleEvent.Type)
            {
                case BattleEventType.BasicAttack:
                    Face(_views[battleEvent.SourceUnitId], _views[battleEvent.TargetUnitId].transform.position);
                    _views[battleEvent.SourceUnitId].PlayAttack();
                    break;
                case BattleEventType.SkillCast:
                    Face(_views[battleEvent.SourceUnitId], _views[battleEvent.TargetUnitId].transform.position);
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
                    if (_views[battleEvent.TargetUnitId].Unit.Team == BattleTeam.Monster)
                    {
                        _defeatedCount++;
                        ProgressChanged?.Invoke(_defeatedCount, _monsterCount);
                    }

                    break;
                case BattleEventType.Enraged:
                    _views[battleEvent.SourceUnitId].PlayEnrage();
                    UnitEnraged?.Invoke(_views[battleEvent.SourceUnitId]);
                    break;
                case BattleEventType.GroupEngaged:
                    HideFallenUnits();
                    GroupEngaged?.Invoke(battleEvent.Amount);
                    break;
            }
        }

        // 쓰러진 모험가는 다음 무리와 싸우기 전까지 쓰러진 채로 보인다. 몬스터는 BattleCorpses가 시체로 바꾼다.
        private void HideFallenUnits()
        {
            foreach (BattleUnitView view in _views)
            {
                if (view.Unit.IsFallen && view.Unit.Team == BattleTeam.Adventurer)
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
                    BattleUnit unit = _views[i].Unit;
                    Vector2Int previous = _previousPositions[i];
                    float moved = Vector3.Distance(_toWorld(unit.X, unit.Z), _toWorld(previous.x, previous.y));
                    float worldSpeed = moved / _tickDuration * _playbackSpeed;
                    _views[i].SetMoving(!IsHolding && moved > 0f, worldSpeed);
                }
            }
        }

        private void UpdateViewPositions(float alpha)
        {
            for (int i = 0; i < _views.Count; i++)
            {
                BattleUnit unit = _views[i].Unit;
                Vector2 position = Vector2.Lerp(_previousPositions[i], new Vector2(unit.X, unit.Z), alpha);
                Vector3 world = _toWorld(position.x, position.y);
                if (unit.IsAlive)
                {
                    Face(_views[i], world + (world - _views[i].transform.position));
                }

                _views[i].transform.position = world;
            }
        }

        // 인간형 모델의 정면은 +Z라 모험가를 바라볼 곳으로 돌린다. 몬스터 프리팹은 지금 방향 그대로 둔다.
        private static void Face(BattleUnitView view, Vector3 point)
        {
            Vector3 forward = Vector3.ProjectOnPlane(point - view.transform.position, Vector3.up);
            if (view.Unit.Team == BattleTeam.Adventurer && forward.sqrMagnitude > 0.0001f)
            {
                view.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            }
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
