using System;
using System.Collections.Generic;
using Blacksmith.Core;
using Fusion;
using UnityEngine;

namespace Blacksmith.Network
{
    // 모든 영역이 공유하는 게임 상태를 둔다. 호스트만 값을 바꾸고, 클라이언트는 복제된 값을 읽기만 한다.
    // [Networked] 필드는 각 영역이 필요할 때 추가한다.
    // 낮 씬 오브젝트로 둔다(GameState 프리팹을 씬에 배치). 지금은 낮·밤 단계, 날짜, 골드를 맡는다.
    public class GameState : NetworkBehaviour
    {
        [SerializeField] private DayCycleData _dayCycle;

        // 진행 중인 작업(미니게임, 채집)이 있는지 알려 주는 곳들. 대장간과 채집이 등록한다. 하나라도 true면 진행 중이다.
        private readonly List<Func<bool>> _workInProgressSources = new List<Func<bool>>();
        private ChangeDetector _changes;

        [Networked] public int Day { get; private set; }
        [Networked] public DayPhase Phase { get; private set; }
        // 낮에는 낮 시간, 밤 전환 연출(Dusk) 중에는 연출 시간을 잰다.
        [Networked] private TickTimer PhaseTimer { get; set; }

        // 모두가 같이 쓰는 골드. 판매 수익과 전투 보상이 늘리고, 대장간 업그레이드 등이 쓴다.
        [Networked] public int Gold { get; private set; }

        // 스폰 직후와 날짜·단계가 바뀔 때 모든 참가자에게 불린다.
        public event Action PhaseChanged;

        // 스폰 직후와 골드가 바뀔 때 모든 참가자에게 불린다.
        public event Action<int> GoldChanged;

        public bool IsReady => Object != null && Object.IsValid;

        // 표시 전용. 낮이 아니면 0이다.
        public float RemainingDaySeconds => IsReady && Phase == DayPhase.Day
            ? PhaseTimer.RemainingTime(Runner) ?? 0f
            : 0f;

        // 표시 전용. 밤 전환 연출의 남은 시간. 연출 중이 아니면 0이다.
        public float RemainingDuskSeconds => IsReady && Phase == DayPhase.Dusk
            ? PhaseTimer.RemainingTime(Runner) ?? 0f
            : 0f;

        // 표시 전용. 문 여는 게임 속 시각(자정부터 센 분).
        public int OpenClockMinutes => _dayCycle.OpenHour * 60;

        // 표시 전용. 낮의 게임 속 시각(자정부터 센 분). 낮이 끝난 뒤에는 문 닫는 시각이다.
        public int ClockMinutes => DayCycleRules.GetClockMinutes(
            IsReady && Phase == DayPhase.Day ? _dayCycle.DaySeconds - RemainingDaySeconds : _dayCycle.DaySeconds,
            _dayCycle.DaySeconds, _dayCycle.OpenHour, _dayCycle.CloseHour);

        public void AddWorkInProgressSource(Func<bool> isWorkInProgress)
        {
            if (!_workInProgressSources.Contains(isWorkInProgress))
            {
                _workInProgressSources.Add(isWorkInProgress);
            }
        }

        public void RemoveWorkInProgressSource(Func<bool> isWorkInProgress)
        {
            _workInProgressSources.Remove(isWorkInProgress);
        }

        public override void Spawned()
        {
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
            if (HasStateAuthority && Day < DayCycleRules.FirstDay)
            {
                BeginDay(DayCycleRules.FirstDay);
            }

            PhaseChanged?.Invoke();
            GoldChanged?.Invoke(Gold);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
            {
                return;
            }

            DayPhase next = DayCycleRules.Next(Phase, PhaseTimer.ExpiredOrNotRunning(Runner), IsAnyWorkInProgress());
            if (next == Phase)
            {
                return;
            }

            Phase = next;
            if (next == DayPhase.Dusk)
            {
                PhaseTimer = TickTimer.CreateFromSeconds(Runner, _dayCycle.DuskSeconds);
            }
        }

        public override void Render()
        {
            foreach (string change in _changes.DetectChanges(this))
            {
                if (change == nameof(Phase) || change == nameof(Day))
                {
                    PhaseChanged?.Invoke();
                }
                else if (change == nameof(Gold))
                {
                    GoldChanged?.Invoke(Gold);
                }
            }
        }

        // 호스트 전용. 골드는 0 아래로 내려가지 않는다. 모자라서 못 쓰면 false다.
        public bool TryAddGold(int amount)
        {
            if (!HasStateAuthority || Gold + amount < 0)
            {
                return false;
            }

            Gold += amount;
            return true;
        }

        // 호스트 전용. 밤에 파견 결과를 반영했거나 파견을 건너뛰었을 때 부른다.
        // [미정] 다음 날이 시작되기 직전의 저장 (docs/GAME_CONCEPT.md 10.1)
        public bool StartNextDay()
        {
            if (!HasStateAuthority || !DayCycleRules.CanStartNextDay(Phase))
            {
                return false;
            }

            BeginDay(Day + 1);
            return true;
        }

        // 호스트 전용. 테스트에서 낮 시간을 바로 끝낸다. 진행 중인 작업이 있으면 끝날 때까지 마감 상태로 기다린다.
        public void EndDayNow()
        {
            if (HasStateAuthority && Phase == DayPhase.Day)
            {
                PhaseTimer = TickTimer.CreateFromSeconds(Runner, 0f);
            }
        }

        private bool IsAnyWorkInProgress()
        {
            foreach (Func<bool> source in _workInProgressSources)
            {
                if (source())
                {
                    return true;
                }
            }

            return false;
        }

        private void BeginDay(int day)
        {
            Day = day;
            Phase = DayPhase.Day;
            PhaseTimer = TickTimer.CreateFromSeconds(Runner, _dayCycle.DaySeconds);
        }
    }
}
