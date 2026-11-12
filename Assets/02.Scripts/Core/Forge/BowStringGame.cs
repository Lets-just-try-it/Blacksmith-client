using System;

namespace Blacksmith.Core.Forge
{
    // 시위 걸기: 힘줄 시위의 고리를 활에 거는 두 단계다.
    // 1) 감기: 마우스 왼쪽 버튼을 누른 채 원을 그리며(시계 방향) 힘줄을 활 끝에 촘촘히 감는다. 한 바퀴가 감기 한 번이고, 일정한 빠르기로 돌려야 팽팽하게 감긴다.
    //    너무 느리면 느슨하고, 너무 빠르면 힘줄이 미끄러져 그 바퀴는 감기지 않는다.
    // 2) 걸기: 활 끝의 홈에 시위 고리를 건다. 한 손으로 활을 휘고 한 손으로 고리를 미는 두 손 작업이라, 마우스 버튼을 누른 채
    //    아래로 끌어 활을 휘고(끈 만큼 휨), 그 휨을 알맞은 구간(초록 띠, 시간에 따라 오르내린다)에 두면서 옆으로 끌어 고리를 활 끝으로 민다.
    //    덜 휘면 고리가 안 움직이고, 너무 휘면 고리가 미끄러져 뒤로 밀린다. 고리가 홈에 닿으면 걸린다. 왼쪽 끝, 오른쪽 끝 두 번 건다.
    // 정확도는 감기 점수와 걸기 점수의 가중 평균이다. 걸기 점수는 미끄러진 횟수와 걸린 시간으로 정한다.
    public sealed class BowStringGame : MiniGame
    {
        public enum PhaseKind { Intro, Wrap, Flex, Done }

        public const int FlexCount = 2;
        private const float TwoPi = (float)(Math.PI * 2.0);

        private readonly BowStringRules _rules;
        private readonly BowStringDifficulty _difficulty;
        private readonly float[] _windowBase = new float[FlexCount];
        private readonly float[] _windowPhase = new float[FlexCount];
        private readonly float[] _wrapQuality;
        private readonly float[] _flexQuality = new float[FlexCount];
        private float _endTimer;
        private bool _wasHeld;
        private bool _hasAngle;
        private float _lastAngle;
        private float _refX;
        private float _refY;
        private float _refClock;
        private const float SampleDistance = 0.15f;
        private float _accumulated;
        private float _wrapClock;
        private bool _needRelease;
        private float _pressY;
        private float _lastX;
        private float _hookClock;
        private int _hookSlips;
        private bool _wasOver;

        public PhaseKind Phase { get; private set; } = PhaseKind.Intro;
        public int Difficulty { get; }
        public float Clock { get; private set; }
        public float PhaseClock { get; private set; }
        public float TimeLeft { get; private set; }
        public float TimeLimit => _difficulty.TimeLimit;
        public bool TimedOut { get; private set; }
        public float SuccessScore => _rules.SuccessScore;

        // 감기: 필요한 횟수, 감은 횟수, 미끄러진 횟수, 지금 도는 바퀴의 진행(0~1), 지금 도는 각도(라디안, 화면 시계 방향이 +)
        public int Wraps => _difficulty.Wraps;
        public int WrapsDone { get; private set; }
        public int Slips { get; private set; }
        public float Turn => Math.Min(1f, _accumulated / TwoPi);
        public float Angle { get; private set; }
        // 도는 중심(칸)과 지금 도는 빠르기(rad/초, 부드럽게 따라간 값)
        public float CenterX { get; private set; }
        public float CenterY { get; private set; }
        public float Speed { get; private set; }
        public float SpeedLo => _difficulty.SpeedLo;
        public float SpeedHi => _difficulty.SpeedHi;
        public float SlipSpeed => _difficulty.SlipSpeed;
        public bool Holding { get; private set; }
        public float PointerX { get; private set; } = -100f;
        public float PointerY { get; private set; }

        // 걸기: 몇 번째인지, 지금 휜 정도(0~1), 이번 고리가 걸리는 구간
        public int FlexIndex { get; private set; }
        // 지금 활이 휜 정도(0~1)와, 고리가 활 끝으로 나아간 정도(0 활 가운데쯤 ~ 1 홈)
        public float Flex { get; private set; }
        public float Slide { get; private set; }
        // 이번 끝에서 알맞은 휘기 구간: 가운데가 시간에 따라 오르내린다.
        public float WindowCenter => _windowBase[Math.Min(FlexIndex, FlexCount - 1)] + _difficulty.WindowDrift * (float)Math.Sin(_hookClock * 0.9 + _windowPhase[Math.Min(FlexIndex, FlexCount - 1)]);
        public float WindowLo => WindowCenter - _difficulty.WindowWidth * 0.5f;
        public float WindowHi => WindowCenter + _difficulty.WindowWidth * 0.5f;
        public bool InWindow => Flex >= WindowLo && Flex <= WindowHi;
        public bool OverFlexed => Flex > WindowHi;
        // 고리를 미는 방향(왼쪽 끝은 왼쪽, 오른쪽 끝은 오른쪽)
        public int SlideDirection => FlexIndex == 0 ? -1 : 1;
        public int Hooked { get; private set; }
        // 걸기에서 미끄러진 횟수(전체)
        public int HookSlips { get; private set; }
        public float ParTime => _difficulty.ParTime;

        public float WrapScore
        {
            get
            {
                float sum = 0f;
                foreach (float q in _wrapQuality)
                {
                    sum += q;
                }

                return sum / Math.Max(1, _difficulty.Wraps);
            }
        }

        public float FlexScore
        {
            get
            {
                float sum = 0f;
                foreach (float q in _flexQuality)
                {
                    sum += q;
                }

                return sum / FlexCount;
            }
        }

        // 지금까지의 점수(0~1)
        public float Progress => Clamp01(_rules.WrapWeight * WrapScore + (1f - _rules.WrapWeight) * FlexScore);

        public BowStringGame(BowStringRules rules, int seed, int difficulty)
        {
            _rules = rules;
            Difficulty = Math.Max(0, Math.Min(rules.Difficulties.Length - 1, difficulty));
            _difficulty = rules.Difficulties[Difficulty];
            TimeLeft = _difficulty.TimeLimit;
            _wrapQuality = new float[_difficulty.Wraps];
            var random = new Random(seed);
            for (int i = 0; i < FlexCount; i++)
            {
                _windowBase[i] = 0.45f + 0.2f * (float)random.NextDouble();
                _windowPhase[i] = (float)(random.NextDouble() * Math.PI * 2.0);
            }
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
            Holding = hasPointer && input.PointerHeld;

            switch (Phase)
            {
                case PhaseKind.Intro:
                    if (PhaseClock >= _rules.IntroTime)
                    {
                        Phase = PhaseKind.Wrap;
                        PhaseClock = 0f;
                        Raise(MiniEventKind.PhaseChanged, 0f, 0);
                    }
                    break;
                case PhaseKind.Wrap:
                    TickTime(deltaTime);
                    TickWrap(deltaTime);
                    break;
                case PhaseKind.Flex:
                    TickTime(deltaTime);
                    TickFlex(deltaTime);
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

            _wasHeld = Holding;
        }

        private void TickTime(float deltaTime)
        {
            TimeLeft -= deltaTime;
            if (TimeLeft <= 0f && Phase != PhaseKind.Done)
            {
                TimeLeft = 0f;
                TimedOut = true;
                Finish();
            }
        }

        // 마우스가 그리는 길이 시계 방향으로 얼마나 꺾이는지(진행 방향이 도는 각도)를 더한다. 원의 가운데를 몰라도 한 바퀴가 2π다.
        // 반지름이 너무 작은 떨림은 세지 않는다. 가운데(CenterX, CenterY)는 화면 안내용으로 마우스 위치의 평균을 따라간다.
        private void TickWrap(float deltaTime)
        {
            if (Phase != PhaseKind.Wrap)
            {
                return;
            }

            if (!Holding)
            {
                _hasAngle = false;
                Speed += (0f - Speed) * Math.Min(1f, deltaTime * 10f);
                return;
            }

            if (!_wasHeld)
            {
                CenterX = PointerX;
                CenterY = PointerY;
                _refX = PointerX;
                _refY = PointerY;
                _refClock = Clock;
                _hasAngle = false;
                _accumulated = 0f;
                _wrapClock = 0f;
                return;
            }

            CenterX += (PointerX - CenterX) * Math.Min(1f, deltaTime * _rules.CenterFollow);
            CenterY += (PointerY - CenterY) * Math.Min(1f, deltaTime * _rules.CenterFollow);
            _wrapClock += deltaTime;

            float dx = PointerX - _refX;
            float dy = PointerY - _refY;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);
            if (distance < SampleDistance)
            {
                Speed += (0f - Speed) * Math.Min(1f, deltaTime * 4f);
                return;
            }

            float heading = (float)Math.Atan2(dy, dx);
            if (_hasAngle)
            {
                float delta = heading - _lastAngle;
                while (delta > Math.PI)
                {
                    delta -= TwoPi;
                }

                while (delta < -Math.PI)
                {
                    delta += TwoPi;
                }

                float elapsed = Math.Max(0.0001f, Clock - _refClock);
                float radius = distance / Math.Max(0.0001f, Math.Abs(delta));
                if (delta > 0f && radius >= _rules.MinRadius)
                {
                    float omega = delta / elapsed;
                    Speed += (omega - Speed) * 0.35f;
                    _accumulated += delta;
                    Angle += delta;
                    if (_accumulated >= TwoPi)
                    {
                        CompleteWrap();
                    }
                }
            }

            _lastAngle = heading;
            _hasAngle = true;
            _refX = PointerX;
            _refY = PointerY;
            _refClock = Clock;
        }

        // 한 바퀴를 마쳤다. 걸린 시간으로 평균 빠르기를 구해 좋은 감기인지 정한다.
        private void CompleteWrap()
        {
            float average = TwoPi / Math.Max(0.05f, _wrapClock);
            float quality;
            float extra;
            MiniEventKind kind;
            if (average > _difficulty.SlipSpeed)
            {
                quality = 0f;
                extra = 1f;
                kind = MiniEventKind.Miss;
                Slips++;
            }
            else if (average >= _difficulty.SpeedLo && average <= _difficulty.SpeedHi)
            {
                quality = 1f;
                extra = 0f;
                kind = MiniEventKind.Perfect;
            }
            else if (average < _difficulty.SpeedLo)
            {
                quality = 0.6f;
                extra = -1f;
                kind = MiniEventKind.Hit;
            }
            else
            {
                quality = 0.7f;
                extra = 0.5f;
                kind = MiniEventKind.Hit;
            }

            _accumulated -= TwoPi;
            _wrapClock = 0f;
            if (quality > 0f)
            {
                _wrapQuality[WrapsDone] = quality;
                WrapsDone++;
            }

            Raise(kind, quality, quality > 0f ? WrapsDone : Slips, extra);
            if (WrapsDone >= _difficulty.Wraps)
            {
                Phase = PhaseKind.Flex;
                PhaseClock = 0f;
                _needRelease = true;
                _hookClock = 0f;
                _hasAngle = false;
                Raise(MiniEventKind.PhaseChanged, 1f, 1);
            }
        }

        // 누른 채 아래로 끌면 활이 휘고, 휜 구간이 알맞을 때 옆으로 끌면 고리가 활 끝으로 밀린다. 너무 휘면 고리가 미끄러진다.
        private void TickFlex(float deltaTime)
        {
            _hookClock += deltaTime;
            if (!Holding)
            {
                _needRelease = false;
                _wasOver = false;
                Flex = Math.Max(0f, Flex - 3f * deltaTime);
                return;
            }

            if (_needRelease)
            {
                return;
            }

            if (!_wasHeld)
            {
                _pressY = PointerY;
                _lastX = PointerX;
                Raise(MiniEventKind.Charge, 0f, FlexIndex);
            }

            Flex = Clamp01(Math.Max(0f, PointerY - _pressY) * _rules.FlexPerCell);
            float moved = (PointerX - _lastX) * SlideDirection;
            _lastX = PointerX;
            if (InWindow)
            {
                _wasOver = false;
                if (moved > 0f)
                {
                    Slide = Math.Min(1f, Slide + moved * _rules.SlidePerCell);
                }
            }
            else if (OverFlexed)
            {
                Slide = Math.Max(0f, Slide - _rules.SlipRate * deltaTime);
                if (!_wasOver)
                {
                    _wasOver = true;
                    _hookSlips++;
                    HookSlips++;
                    Raise(MiniEventKind.Miss, 0f, FlexIndex, 1f);
                }
            }
            else
            {
                _wasOver = false;
            }

            if (Slide >= 1f)
            {
                Hook();
            }
        }

        // 고리가 홈에 닿았다. 미끄러진 횟수와 걸린 시간이 점수를 정한다.
        private void Hook()
        {
            float late = Clamp01((_hookClock - _difficulty.ParTime) / _difficulty.ParTime);
            float quality = Clamp01(1f - 0.2f * _hookSlips - 0.4f * late);
            _flexQuality[FlexIndex] = quality;
            Raise(MiniEventKind.Good, quality, FlexIndex, _hookSlips);
            Hooked++;
            FlexIndex++;
            Flex = 0f;
            Slide = 0f;
            _hookClock = 0f;
            _hookSlips = 0;
            _wasOver = false;
            _needRelease = true;
            if (FlexIndex >= FlexCount)
            {
                Finish();
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
