using System;

namespace Blacksmith.Core.Forge
{
    // 활 몸체 휘기: 달군 쇠막대를 활 모양 형틀에 맞춰 휜다. 막대는 가운데(손잡이)를 잡고 양쪽 팔이 세 마디씩이다.
    // 마디를 누른 채 마우스를 아래로 끌어 내리면 끈 만큼 그 마디가 휜다(한 번 휜 쇠는 되돌릴 수 없다). 놓으면 탄성으로 조금 되돌아온다.
    // 마디마다 열이 따로 있어 시간이 갈수록 식는다. 식으면 같은 거리를 끌어도 덜 휘고 더 되돌아오므로,
    // E를 누르고 있으면 가리키는 마디를 불에 달굴 수 있다(달구는 동안은 휘지 못한다). 어느 마디를 언제 달구고 휠지가 요령이다.
    // 목표 안에 놓으면 그 마디가 굳고, 지나치게 휘면 그대로 굳어 오차가 남는다. 덜 휜 마디는 다시 끌어 더 휠 수 있다.
    // 정확도는 마디별 목표와의 차이로 정한다.
    public sealed class BowBendGame : MiniGame
    {
        public enum PhaseKind { Intro, Bend, Done }

        public const int SegmentCount = 6;
        // 한 마디가 화면(게임 좌표, 칸)에서 차지하는 폭
        public const float SegmentWidth = 2.4f;
        private static readonly float[] BaseTargets = { 0.85f, 0.6f, 0.35f, 0.35f, 0.6f, 0.85f };

        private readonly BowBendRules _rules;
        private readonly BowBendDifficulty _difficulty;
        private readonly float[] _targets = new float[SegmentCount];
        private readonly float[] _bends = new float[SegmentCount];
        private readonly float[] _heat = new float[SegmentCount];
        private readonly float[] _coolMultiplier = new float[SegmentCount];
        private readonly bool[] _locked = new bool[SegmentCount];
        // 한 번이라도 눌러 본 마디만 점수가 있다.
        private readonly bool[] _touched = new bool[SegmentCount];
        private float _endTimer;
        private bool _wasHeld;
        private int _active = -1;
        private float _pressY;
        private float _bendAtPress;

        public PhaseKind Phase { get; private set; } = PhaseKind.Intro;
        public int Difficulty { get; }
        public float Clock { get; private set; }
        public float PhaseClock { get; private set; }
        public float TimeLeft { get; private set; }
        public float TimeLimit => _difficulty.TimeLimit;
        public bool TimedOut { get; private set; }
        public float Tolerance => _difficulty.Tolerance;
        public float MaxBend => _rules.MaxBend;
        public float SuccessScore => _rules.SuccessScore;
        public float SpringBack => _rules.SpringBack;

        // 마우스가 가리키는 마디(없으면 -1), 지금 누르고 있는 마디(없으면 -1), 불에 달구는 중인 마디(없으면 -1)
        public int Hovered { get; private set; } = -1;
        public int Active => _active;
        public int Heating { get; private set; } = -1;
        public bool Bending => _active >= 0 && Phase == PhaseKind.Bend;
        public int LockedCount { get; private set; }
        public int Overbent { get; private set; }
        public int Reheats { get; private set; }

        public float TargetOf(int segment) => _targets[segment];
        public float BendOf(int segment) => _bends[segment];
        public float HeatOf(int segment) => _heat[segment];
        public bool IsLocked(int segment) => _locked[segment];

        // 마디가 지금 식은 정도에 따라 놓았을 때 되돌아올 비율
        public float SpringOf(int segment)
        {
            return _rules.SpringBack * (1f + _rules.SpringCold * (1f - _heat[segment]));
        }

        // 굳지 않은 마디들의 평균 열
        public float AverageHeat
        {
            get
            {
                float sum = 0f;
                int count = 0;
                for (int i = 0; i < SegmentCount; i++)
                {
                    if (!_locked[i])
                    {
                        sum += _heat[i];
                        count++;
                    }
                }

                return count == 0 ? 0f : sum / count;
            }
        }

        // 지금까지의 점수(0~1)
        public float Progress
        {
            get
            {
                float sum = 0f;
                for (int i = 0; i < SegmentCount; i++)
                {
                    sum += SegmentScore(i);
                }

                return sum / SegmentCount;
            }
        }

        public BowBendGame(BowBendRules rules, int seed, int difficulty)
        {
            _rules = rules;
            Difficulty = Math.Max(0, Math.Min(rules.Difficulties.Length - 1, difficulty));
            _difficulty = rules.Difficulties[Difficulty];
            TimeLeft = _difficulty.TimeLimit;
            var random = new Random(seed);
            for (int i = 0; i < SegmentCount; i++)
            {
                float jitter = 1f + _difficulty.Jitter * (float)(random.NextDouble() * 2.0 - 1.0);
                _targets[i] = Math.Min(rules.MaxBend * 0.9f, BaseTargets[i] * jitter);
                _heat[i] = 1f;
                _coolMultiplier[i] = 0.7f + 0.6f * (float)random.NextDouble();
            }
        }

        // 가운데가 0인 게임 좌표에서 마우스가 놓인 마디. 가로로 여섯 칸으로 나눈다.
        public static int SegmentAt(float x)
        {
            float half = SegmentCount * SegmentWidth * 0.5f;
            if (x < -half || x >= half)
            {
                return -1;
            }

            return (int)Math.Floor((x + half) / SegmentWidth);
        }

        private float SegmentScore(int segment)
        {
            if (!_touched[segment])
            {
                return 0f;
            }

            float error = Math.Abs(_bends[segment] - _targets[segment]);
            if (error <= _difficulty.Tolerance)
            {
                return 1f;
            }

            return Clamp01(1f - (error - _difficulty.Tolerance) / _rules.ErrorScale);
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
            Hovered = hasPointer ? SegmentAt(input.PointerX) : -1;

            switch (Phase)
            {
                case PhaseKind.Intro:
                    if (PhaseClock >= _rules.IntroTime)
                    {
                        Phase = PhaseKind.Bend;
                        PhaseClock = 0f;
                        Raise(MiniEventKind.PhaseChanged, 0f, 0);
                    }
                    break;
                case PhaseKind.Bend:
                    TickBend(deltaTime, input, hasPointer);
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

        private void TickBend(float deltaTime, MiniInput input, bool hasPointer)
        {
            TimeLeft -= deltaTime;
            bool held = hasPointer && input.PointerHeld;

            // 굳지 않은 마디는 모두 천천히 식는다.
            for (int i = 0; i < SegmentCount; i++)
            {
                if (!_locked[i])
                {
                    _heat[i] = Math.Max(0f, _heat[i] - _rules.CoolRate * _coolMultiplier[i] * deltaTime);
                }
            }

            // E를 누르고 있으면 가리키는 마디를 달군다. 누르고 있는 마디(휘는 중)는 달굴 수 없다.
            int previousHeating = Heating;
            Heating = -1;
            if (input.ActionHeld && !held && Hovered >= 0 && !_locked[Hovered])
            {
                Heating = Hovered;
                _heat[Hovered] = Math.Min(1f, _heat[Hovered] + _rules.HeatRate * deltaTime);
                if (previousHeating != Hovered)
                {
                    Reheats++;
                    Raise(MiniEventKind.Swing, 0f, Hovered);
                }
            }

            if (held && !_wasHeld && Hovered >= 0 && !_locked[Hovered] && !input.ActionHeld)
            {
                _active = Hovered;
                _touched[Hovered] = true;
                _pressY = input.PointerY;
                _bendAtPress = _bends[Hovered];
                Raise(MiniEventKind.Charge, 0f, Hovered);
            }

            if (held && _active >= 0)
            {
                float sensitivity = _rules.ColdFactor + (1f - _rules.ColdFactor) * _heat[_active];
                float drag = Math.Max(0f, input.PointerY - _pressY);
                float target = Math.Min(_rules.MaxBend, _bendAtPress + drag * _rules.BendPerCell * sensitivity);
                _bends[_active] = Math.Max(_bends[_active], target);
            }
            else if (_active >= 0)
            {
                Release();
            }

            _wasHeld = held;

            if (LockedCount >= SegmentCount)
            {
                Finish();
            }
            else if (TimeLeft <= 0f)
            {
                TimeLeft = 0f;
                TimedOut = true;
                if (_active >= 0)
                {
                    Release();
                }

                Finish();
            }
        }

        // 누르던 것을 놓는다. 탄성으로 되돌아온 정도가 목표 안이면 굳고, 지나치면 그대로 굳고, 모자라면 다시 휠 수 있다.
        private void Release()
        {
            int segment = _active;
            _active = -1;
            float settled = _bends[segment] * (1f - SpringOf(segment));
            _bends[segment] = settled;
            float error = settled - _targets[segment];
            if (Math.Abs(error) <= _difficulty.Tolerance)
            {
                Lock(segment);
                Raise(MiniEventKind.Hit, 1f, segment, error);
            }
            else if (error > 0f)
            {
                Lock(segment);
                Overbent++;
                Raise(MiniEventKind.Miss, SegmentScore(segment), segment, error);
            }
        }

        private void Lock(int segment)
        {
            _locked[segment] = true;
            LockedCount++;
        }

        private void Finish()
        {
            if (_active >= 0)
            {
                Release();
            }

            Accuracy = Progress;
            Phase = PhaseKind.Done;
            PhaseClock = 0f;
            _endTimer = 0f;
            Raise(MiniEventKind.Complete, Accuracy);
        }
    }
}
