using System;

namespace Blacksmith.Core.Forge
{
    // 시험 사격: 제한시간 동안 만든 활로 볏짚 허수아비를 최대한 많이 맞힌다. 양궁 게임처럼 마우스로 조준하고 눌러 시위를 당긴 뒤 놓는다.
    // 시위를 당기는 동안 E(스페이스바)를 누르면 숨을 참아 손 떨림이 줄지만, 숨이 다하면 헐떡여 더 흔들린다.
    // 다 당기기 전에 쏘면 화살이 처지고, 오래 버티면 팔이 떨린다. 바람은 화살을 옆으로 민다.
    // 가슴을 맞히면 1점, 머리를 맞히면 치명타로 2점이다. 맞히면 다음 허수아비가 다른 자리와 거리에 나타난다. 못 맞혀도 같은 허수아비를 다시 쏜다.
    // 제한시간이 끝날 때까지 계속 쏜다(목표 점수를 채워도 끝나지 않고 점수는 계속 쌓인다). 정확도는 얻은 점수를 목표 점수로 나눈 값(최대 1)이다.
    public sealed class BowRangeGame : MiniGame
    {
        public enum PhaseKind { Intro, Aim, Draw, Result, Done }

        private readonly BowRangeRules _rules;
        private readonly BowRangeDifficulty _difficulty;
        private readonly Random _random;
        private readonly float[] _phase = new float[4];
        private float _endTimer;
        private float _targetPhase;
        private float _gasp;
        private float _baseX;
        private float _baseY;
        private float _wind;
        private float _distance;

        // 머리는 가슴 가운데에서 맞는 구역 반지름의 이 배수만큼 위에 있고, 머리의 맞는 구역 반지름은 가슴의 이 배수다.
        public const float HeadOffset = 1.55f;
        public const float HeadRatio = 0.4f;

        public PhaseKind Phase { get; private set; } = PhaseKind.Intro;
        public int Difficulty { get; }
        public float Clock { get; private set; }
        public float PhaseClock { get; private set; }
        public float TimeLeft { get; private set; }
        public float TimeLimit => _difficulty.TimeLimit;
        public bool TimedOut { get; private set; }

        // 쏜 화살 수, 명중 수, 머리를 맞힌 수(치명타), 얻은 점수(가슴 1점, 머리 2점), 목표 점수, 연속 명중
        public int Shots { get; private set; }
        public int Hits { get; private set; }
        public int Headshots { get; private set; }
        public int Points { get; private set; }
        public int GoalHits => _difficulty.GoalHits;
        public int Streak { get; private set; }
        // 지금 허수아비가 몇 번째로 나온 것인지(새 허수아비가 나타날 때마다 1 늘어난다)
        public int TargetNumber { get; private set; }
        public float PointerX { get; private set; } = -100f;
        public float PointerY { get; private set; }

        // 지금 허수아비의 가슴 위치(움직이는 허수아비는 지금 위치), 바람(음수 왼쪽, 양수 오른쪽)
        public float TargetX { get; private set; }
        public float TargetY { get; private set; }
        public float Wind => _wind;
        // 지금 허수아비의 거리(m). 멀수록 허수아비가 작고 바람과 중력의 영향이 크다.
        public float Distance => _distance;
        // 가슴의 맞는 구역 반지름(칸)
        public float TargetRadius => RadiusAt(_distance);
        // 이번 화살을 다 당겨 쏘면 바람이 옆으로 미는 거리와 중력으로 처지는 거리(칸). 이만큼 보정해서 겨냥해야 한다.
        public float Drift => DriftAt(_wind, _distance);
        public float Drop => DropAt(_distance);
        public float RefDistance => _rules.RefDistance;

        // 시위를 당긴 정도(0~1), 당기고 있는 시간, 조준점이 흔들려 벗어난 정도
        public float Power { get; private set; }
        public float DrawClock { get; private set; }
        public float SwayX { get; private set; }
        public float SwayY { get; private set; }
        // 남은 숨(1 가득 ~ 0), 숨을 참는 중인지, 숨이 다해 헐떡이는 중인지
        public float Breath { get; private set; } = 1f;
        public bool HoldingBreath { get; private set; }
        public bool Gasping => _gasp > 0f;
        public float FlightTime => _rules.FlightTime;

        // 마지막으로 쏜 화살이 꽂힌 자리와 맞았는지
        public float ImpactX { get; private set; }
        public float ImpactY { get; private set; }
        public bool LastHit { get; private set; }
        public bool LastHead { get; private set; }
        public int LastPoints => LastHead ? 2 : (LastHit ? 1 : 0);
        public float SuccessScore => _rules.SuccessScore;

        public float Progress => Clamp01(_difficulty.GoalHits <= 0 ? 0f : Points / (float)_difficulty.GoalHits);

        public BowRangeGame(BowRangeRules rules, int seed, int difficulty)
        {
            _rules = rules;
            Difficulty = Math.Max(0, Math.Min(rules.Difficulties.Length - 1, difficulty));
            _difficulty = rules.Difficulties[Difficulty];
            TimeLeft = _difficulty.TimeLimit;
            _random = new Random(seed);
            for (int i = 0; i < _phase.Length; i++)
            {
                _phase[i] = (float)(_random.NextDouble() * Math.PI * 2.0);
            }

            NewTarget();
            PlaceTarget();
        }

        // 거리(m)에 따른 맞는 구역 반지름, 바람이 미는 거리, 중력으로 처지는 거리(칸)
        public float RadiusAt(float distance)
        {
            return _difficulty.TargetRadius * _rules.RefDistance / Math.Max(1f, distance);
        }

        public float DriftAt(float wind, float distance)
        {
            return wind * _rules.WindScale * distance / _rules.RefDistance;
        }

        public float DropAt(float distance)
        {
            float ratio = distance / _rules.RefDistance;
            return _rules.GravityDrop * ratio * ratio;
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
            bool press = input.PointerPressed;
            bool held = input.PointerHeld;

            // 움직이는 허수아비는 계속 좌우로 오간다.
            _targetPhase += deltaTime * _difficulty.TargetSpeed;
            PlaceTarget();

            switch (Phase)
            {
                case PhaseKind.Intro:
                    if (PhaseClock >= _rules.IntroTime)
                    {
                        BeginAim();
                    }
                    break;
                case PhaseKind.Aim:
                    TickTime(deltaTime);
                    if (Phase != PhaseKind.Aim)
                    {
                        break;
                    }

                    RecoverBreath(deltaTime);
                    if (press && hasPointer)
                    {
                        Phase = PhaseKind.Draw;
                        PhaseClock = 0f;
                        DrawClock = 0f;
                        Power = 0f;
                        Raise(MiniEventKind.Charge, 0f, Shots);
                    }
                    break;
                case PhaseKind.Draw:
                    TickTime(deltaTime);
                    if (Phase == PhaseKind.Draw)
                    {
                        TickDraw(deltaTime, held, hasPointer, input.ActionHeld);
                    }
                    break;
                case PhaseKind.Result:
                    TickTime(deltaTime);
                    if (PhaseClock >= _rules.ResultTime)
                    {
                        if (TimedOut)
                        {
                            Finish();
                        }
                        else
                        {
                            BeginAim();
                        }
                    }
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

        // 시간이 다 되면, 쏘기 전이면 바로 끝내고 날아가는 중이면 결과를 보고 끝낸다.
        private void TickTime(float deltaTime)
        {
            if (TimedOut)
            {
                return;
            }

            TimeLeft -= deltaTime;
            if (TimeLeft > 0f)
            {
                return;
            }

            TimeLeft = 0f;
            TimedOut = true;
            if (Phase == PhaseKind.Aim || Phase == PhaseKind.Draw)
            {
                Finish();
            }
        }

        // 새 허수아비: 자리와 거리, 바람을 정한다.
        private void NewTarget()
        {
            _baseX = (float)(_random.NextDouble() * 2.0 - 1.0) * _rules.RangeX;
            _baseY = (float)(_random.NextDouble() * 2.0 - 1.0) * _rules.RangeY + _rules.CenterY;
            _wind = (float)(_random.NextDouble() * 2.0 - 1.0) * _difficulty.Wind;
            int steps = Math.Max(0, (int)Math.Round(_difficulty.DistanceMax - _difficulty.DistanceMin));
            _distance = _difficulty.DistanceMin + _random.Next(steps + 1);
            TargetNumber++;
        }

        // 허수아비의 위치: 처음 정한 자리를 가운데로 좌우로 오간다.
        private void PlaceTarget()
        {
            TargetX = _baseX + _difficulty.TargetMove * (float)Math.Sin(_targetPhase + _phase[3]);
            TargetY = _baseY;
        }

        private void BeginAim()
        {
            // 맞혔으면 다음 허수아비가 나타난다. 못 맞혔으면 같은 허수아비를 다시 쏜다.
            if (Shots > 0 && LastHit)
            {
                NewTarget();
                PlaceTarget();
            }

            Phase = PhaseKind.Aim;
            PhaseClock = 0f;
            Power = 0f;
            DrawClock = 0f;
            SwayX = 0f;
            SwayY = 0f;
            Raise(MiniEventKind.PhaseChanged, Shots, Shots);
        }

        private void RecoverBreath(float deltaTime)
        {
            _gasp = Math.Max(0f, _gasp - deltaTime);
            if (_rules.BreathRecover > 0f)
            {
                Breath = Math.Min(1f, Breath + deltaTime / _rules.BreathRecover);
            }
        }

        // 숨을 참는 동안 숨이 줄어든다. 다 줄어들면 잠깐 헐떡이며 손이 더 떨린다.
        private float BreathSway(float deltaTime, bool breathHeld)
        {
            HoldingBreath = false;
            if (_rules.BreathTime <= 0f)
            {
                return 1f;
            }

            if (_gasp > 0f)
            {
                _gasp = Math.Max(0f, _gasp - deltaTime);
                return _rules.GaspSway;
            }

            if (breathHeld && Breath > 0f)
            {
                HoldingBreath = true;
                Breath = Math.Max(0f, Breath - deltaTime / _rules.BreathTime);
                if (Breath <= 0f)
                {
                    _gasp = 1.2f;
                    HoldingBreath = false;
                }

                return _rules.BreathSteady;
            }

            return 1f;
        }

        private void TickDraw(float deltaTime, bool held, bool hasPointer, bool breathHeld)
        {
            DrawClock += deltaTime;
            float breathFactor = BreathSway(deltaTime, breathHeld);
            Power = Math.Min(1f, DrawClock / _rules.DrawTime);
            float tired = Math.Max(0f, DrawClock - _rules.DrawTime - _rules.FatigueStart);
            float amplitude = _difficulty.Sway * (0.4f + 0.6f * Power) * (1f + _rules.SwayGrowth * tired) * breathFactor;
            SwayX = amplitude * (0.6f * (float)Math.Sin(DrawClock * _rules.SwayFrequency + _phase[0]) + 0.4f * (float)Math.Sin(DrawClock * _rules.SwayFrequency * 1.7 + _phase[1]));
            SwayY = amplitude * (0.6f * (float)Math.Sin(DrawClock * _rules.SwayFrequency * 1.3 + _phase[2]) + 0.4f * (float)Math.Sin(DrawClock * _rules.SwayFrequency * 0.7 + _phase[0]));
            if (!held || !hasPointer)
            {
                Release();
            }
        }

        // 놓은 순간 조준점 + 손 떨림 + 바람이 미는 만큼 + 덜 당겨서 처지는 만큼이 화살이 꽂히는 자리다. 가슴의 맞는 구역 안이면 명중이다.
        private void Release()
        {
            // 마우스가 그림판 밖이면 아무 데나 쏜 것이라 허수아비를 크게 벗어난다.
            float aimX = PointerX > -50f ? PointerX : 100f;
            float aimY = PointerX > -50f ? PointerY : 100f;
            ImpactX = aimX + SwayX + Drift;
            ImpactY = aimY + SwayY + (1f - Power) * _rules.DropScale + Drop;
            float miss = (float)Math.Sqrt((ImpactX - TargetX) * (ImpactX - TargetX) + (ImpactY - TargetY) * (ImpactY - TargetY));
            float headX = TargetX;
            float headY = TargetY - HeadOffset * TargetRadius;
            float headMiss = (float)Math.Sqrt((ImpactX - headX) * (ImpactX - headX) + (ImpactY - headY) * (ImpactY - headY));
            LastHead = headMiss <= HeadRatio * TargetRadius;
            LastHit = LastHead || miss <= TargetRadius;
            Shots++;
            HoldingBreath = false;
            if (LastHit)
            {
                Hits++;
                Streak++;
                Points += LastHead ? 2 : 1;
                if (LastHead)
                {
                    Headshots++;
                }
            }
            else
            {
                Streak = 0;
            }

            Phase = PhaseKind.Result;
            PhaseClock = 0f;
            MiniEventKind kind = LastHead ? MiniEventKind.Perfect : (LastHit ? MiniEventKind.Hit : MiniEventKind.Miss);
            Raise(kind, LastPoints, Shots, Power);
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
