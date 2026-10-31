using System;

namespace Blacksmith.Core.Forge
{
    // 담금질: 시스템이 알려 준 기준 시간이 되었다 싶을 때 달군 칼날을 물에서 꺼낸다.
    // 담근 뒤 처음 ShowSeconds까지만 초를 보여 주고 그 뒤는 가린다.
    // 기준 시간과 꺼낸 시간의 차이가 정확도가 된다.
    public sealed class QuenchGame : MiniGame
    {
        public enum PhaseKind { Ready, Dunk, Done }

        private readonly QuenchRules _rules;
        private readonly QuenchDifficulty _difficulty;
        private int _lastTick = -1;
        private float _endTimer;

        public PhaseKind Phase { get; private set; } = PhaseKind.Ready;
        public float PhaseClock { get; private set; }
        // 기준 시간(초)
        public float Target { get; }
        // 담근 뒤 지난 시간(초)
        public float Elapsed { get; private set; }
        public float Pulled { get; private set; }
        // 꺼낸 시간 - 기준 시간. 음수면 일찍 꺼낸 것이다.
        public float Diff { get; private set; }
        public bool TimedOut { get; private set; }

        public float LimitTime => Target * _rules.LimitTargetMultiplier + _rules.LimitExtraSeconds;
        public bool Submerged => Phase == PhaseKind.Dunk && PhaseClock >= _rules.CarryTime;
        public bool IsCountVisible => Elapsed < _rules.ShowSeconds;

        public QuenchGame(QuenchRules rules, int seed, int difficulty)
        {
            _rules = rules;
            int index = Math.Max(0, Math.Min(rules.Difficulties.Length - 1, difficulty));
            _difficulty = rules.Difficulties[index];

            var random = new Random(seed);
            float target = _difficulty.MinTarget + _difficulty.TargetRandomRange * (float)random.NextDouble();
            Target = (float)Math.Round(target * 10f) / 10f;
        }

        public override void Tick(float deltaTime, MiniInput input)
        {
            if (Finished)
            {
                return;
            }

            PhaseClock += deltaTime;
            switch (Phase)
            {
                case PhaseKind.Ready:
                    TickReady(input);
                    break;
                case PhaseKind.Dunk:
                    TickDunk(input);
                    break;
                case PhaseKind.Done:
                    TickDone(deltaTime);
                    break;
            }
        }

        private void TickReady(MiniInput input)
        {
            bool started = input.ActionPressed && PhaseClock >= _rules.ReadyLockTime;
            if (started || PhaseClock >= _rules.ReadyAutoTime)
            {
                EnterPhase(PhaseKind.Dunk);
            }
        }

        private void TickDunk(MiniInput input)
        {
            if (PhaseClock < _rules.CarryTime)
            {
                return;
            }

            Elapsed = PhaseClock - _rules.CarryTime;
            RaiseSecondTick();

            if (input.ActionPressed && Elapsed > _rules.MinPullTime)
            {
                Pull(false);
            }
            else if (Elapsed >= LimitTime)
            {
                Pull(true);
            }
        }

        private void TickDone(float deltaTime)
        {
            _endTimer += deltaTime;
            if (_endTimer >= _rules.EndDelay)
            {
                Finished = true;
                Raise(MiniEventKind.Finish, Accuracy);
            }
        }

        private void RaiseSecondTick()
        {
            int second = (int)Elapsed;
            bool inShowRange = second >= 1 && second <= (int)_rules.ShowSeconds;
            if (inShowRange && second != _lastTick)
            {
                _lastTick = second;
                Raise(MiniEventKind.SecondTick, second, second);
            }
        }

        private void EnterPhase(PhaseKind phase)
        {
            Phase = phase;
            PhaseClock = 0f;
            Raise(MiniEventKind.PhaseChanged, (int)phase, (int)phase);
        }

        private void Pull(bool timedOut)
        {
            TimedOut = timedOut;
            Pulled = Elapsed;
            Diff = Pulled - Target;
            Accuracy = timedOut ? _rules.TimeoutAccuracy : CalculateAccuracy();

            Raise(MiniEventKind.Pull, Accuracy, 0, Diff);
            if (!timedOut && Accuracy >= _rules.PerfectEventAccuracy)
            {
                Raise(MiniEventKind.Perfect, Accuracy);
            }
            EnterPhase(PhaseKind.Done);
        }

        private float CalculateAccuracy()
        {
            float error = Math.Abs(Diff) / Math.Max(0.1f, Target);
            float range = Math.Max(0.0001f, _difficulty.ZeroFraction - _difficulty.PerfectFraction);
            return Clamp01(1f - (error - _difficulty.PerfectFraction) / range);
        }
    }
}
