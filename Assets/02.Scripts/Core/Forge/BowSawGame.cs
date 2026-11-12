using System;

namespace Blacksmith.Core.Forge
{
    // 활 깎기(톱질): 제한시간 안에 통나무를 톱으로 최대한 많이 켠다. 마우스 버튼을 누른 채 위아래로 왔다갔다 움직여 톱질하고, 움직이는 빠르기가 곧 톱질의 힘이다.
    // 알맞게 힘차게 움직이면 잘 켜지고, 너무 느리면 조금밖에 안 켜지며, 너무 세게 휘두르면 톱이 낀다.
    // 정확도는 켠 통나무 수(켜는 중인 통나무 포함)를 만점 기준 수로 나눈 값이다.
    public sealed class BowSawGame : MiniGame
    {
        public enum PhaseKind { Intro, Play, Done }

        private readonly BowSawRules _rules;
        private readonly BowSawDifficulty _difficulty;
        private float _endTimer;
        private float _anchor;
        private float _lastPointerY;
        private bool _hadPointerY;
        private bool _tracking;
        private float _startY;
        private float _extremeY;
        private float _startClock;
        private float _extremeClock;
        private int _direction;

        public PhaseKind Phase { get; private set; } = PhaseKind.Intro;
        public int Difficulty { get; }
        public float Clock { get; private set; }
        public float PhaseClock { get; private set; }
        public float PointerX { get; private set; } = -100f;
        public float PointerY { get; private set; }
        public bool Holding { get; private set; }

        // 지금 켜는 통나무를 켠 비율(0~1), 다 켠 통나무 수, 만점에 필요한 통나무 수, 톱질 횟수
        public int LogsCut { get; private set; }
        public int LogsTarget => _difficulty.Logs;
        public float Depth { get; private set; }
        public int StrokesNeeded => _difficulty.Strokes;
        public int Strokes { get; private set; }
        public int Binds { get; private set; }
        public int Streak { get; private set; }
        public float TimeLeft { get; private set; }
        public float TimeLimit => _difficulty.TimeLimit;
        public float BindLeft { get; private set; }
        public bool TimedOut { get; private set; }

        // 마지막 톱질의 점수(0~1), 힘(빠르기, 칸/초), 그 시각
        public float LastQuality { get; private set; }
        public float LastSpeed { get; private set; }
        public float LastStrokeTime { get; private set; } = -10f;
        // 지금 마우스를 움직이는 빠르기(칸/초, 부드럽게 따라간 값). 화면이 힘을 보여 주는 데 쓴다.
        public float Speed { get; private set; }
        // 마우스 높이는 어디서 시작해도 된다. 누르는 순간의 높이를 기준(앵커)으로 잡고, 천천히 따라 옮겨 가며 그 기준에서 벗어난 만큼을 톱질의 위치로 본다.
        public float RelativeY => PointerY - _anchor;
        // 알맞은 힘 구간
        public float ZoneLo => _difficulty.ZoneLo;
        public float ZoneHi => _difficulty.ZoneHi;
        public float SuccessScore => _rules.SuccessScore;

        // 지금까지의 점수(0~1)
        public float Progress => Clamp01((LogsCut + Depth) / Math.Max(1, _difficulty.Logs));

        public BowSawGame(BowSawRules rules, int difficulty)
        {
            _rules = rules;
            Difficulty = Math.Max(0, Math.Min(rules.Difficulties.Length - 1, difficulty));
            _difficulty = rules.Difficulties[Difficulty];
            TimeLeft = _difficulty.TimeLimit;
        }

        public override void Tick(float deltaTime, MiniInput input)
        {
            if (Finished)
            {
                return;
            }

            Clock += deltaTime;
            PhaseClock += deltaTime;
            bool hasPointer = input.PointerX > -50f;
            PointerX = hasPointer ? input.PointerX : -100f;
            PointerY = hasPointer ? input.PointerY : 0f;

            switch (Phase)
            {
                case PhaseKind.Intro:
                    if (PhaseClock >= _rules.IntroTime)
                    {
                        Phase = PhaseKind.Play;
                        PhaseClock = 0f;
                        Raise(MiniEventKind.PhaseChanged, 0f, 0);
                    }
                    break;
                case PhaseKind.Play:
                    TickPlay(deltaTime, input, hasPointer);
                    break;
                case PhaseKind.Done:
                    _endTimer += deltaTime;
                    if (_endTimer >= _rules.EndDelay)
                    {
                        Finished = true;
                        Raise(MiniEventKind.Finish, Accuracy);
                    }
                    break;
            }
        }

        private void TickPlay(float deltaTime, MiniInput input, bool hasPointer)
        {
            TimeLeft -= deltaTime;
            Holding = hasPointer && input.PointerHeld;
            UpdateSpeed(deltaTime);
            _anchor = Holding ? _anchor + (PointerY - _anchor) * Math.Min(1f, deltaTime * 1.2f) : PointerY;

            if (BindLeft > 0f)
            {
                BindLeft = Math.Max(0f, BindLeft - deltaTime);
                _tracking = false;
            }
            else if (Holding)
            {
                Track();
            }
            else
            {
                _tracking = false;
            }

            if (LogsCut >= _difficulty.Logs)
            {
                Finish();
            }
            else if (TimeLeft <= 0f)
            {
                TimeLeft = 0f;
                TimedOut = true;
                Finish();
            }
        }

        private void UpdateSpeed(float deltaTime)
        {
            float instant = Holding && _hadPointerY ? Math.Abs(PointerY - _lastPointerY) / Math.Max(0.001f, deltaTime) : 0f;
            Speed += (instant - Speed) * Math.Min(1f, deltaTime * 20f);
            _lastPointerY = PointerY;
            _hadPointerY = Holding;
        }

        // 마우스가 한쪽(위나 아래)으로 가다가 되돌아오는 순간까지를 톱질 한 번으로 센다.
        private void Track()
        {
            if (!_tracking)
            {
                _tracking = true;
                _startY = PointerY;
                _extremeY = PointerY;
                _startClock = Clock;
                _extremeClock = Clock;
                _direction = 0;
            }

            if (_direction == 0)
            {
                if (Math.Abs(PointerY - _startY) >= _rules.Hysteresis)
                {
                    _direction = PointerY > _startY ? 1 : -1;
                    _extremeY = PointerY;
                    _extremeClock = Clock;
                }

                return;
            }

            if (_direction * (PointerY - _extremeY) > 0f)
            {
                _extremeY = PointerY;
                _extremeClock = Clock;
            }
            else if (_direction * (_extremeY - PointerY) >= _rules.Hysteresis)
            {
                float distance = Math.Abs(_extremeY - _startY);
                if (distance >= _rules.StrokeMin)
                {
                    CompleteStroke(distance, _extremeClock - _startClock);
                }

                _startY = _extremeY;
                _startClock = _extremeClock;
                _direction = -_direction;
                _extremeY = PointerY;
                _extremeClock = Clock;
            }
        }

        private void CompleteStroke(float distance, float duration)
        {
            float speed = distance / Math.Max(0.01f, duration);
            float lo = ZoneLo;
            float hi = ZoneHi;
            float quality;
            float extra;
            MiniEventKind kind;
            if (speed > hi * _rules.BindFactor)
            {
                quality = 0f;
                extra = 1f;
                kind = MiniEventKind.Miss;
                Binds++;
                BindLeft = _rules.BindCooldown;
                Streak = 0;
            }
            else if (speed >= lo && speed <= hi)
            {
                quality = 1f;
                extra = 0f;
                Streak++;
                kind = _rules.PerfectStreak > 0 && Streak % _rules.PerfectStreak == 0 ? MiniEventKind.Perfect : MiniEventKind.Hit;
            }
            else if (speed < lo * _rules.SlowFactor)
            {
                quality = _rules.SlowQuality;
                extra = -1f;
                kind = MiniEventKind.Miss;
                Streak = 0;
            }
            else
            {
                quality = _rules.EdgeQuality;
                extra = speed < lo ? -0.5f : 0.5f;
                kind = MiniEventKind.Hit;
                Streak = 0;
            }

            LastQuality = quality;
            LastSpeed = speed;
            LastStrokeTime = PhaseClock;
            Strokes++;
            if (quality > 0f)
            {
                Depth += (0.3f + 0.7f * quality) / _difficulty.Strokes;
            }

            Raise(kind, quality, Strokes, extra);
            if (Depth >= 1f)
            {
                LogsCut++;
                Depth = 0f;
                Raise(MiniEventKind.Good, LogsCut, LogsCut);
            }
        }

        private void Finish()
        {
            Accuracy = Progress;
            Phase = PhaseKind.Done;
            PhaseClock = 0f;
            _endTimer = 0f;
            Raise(MiniEventKind.Complete, Accuracy);
        }
    }
}
