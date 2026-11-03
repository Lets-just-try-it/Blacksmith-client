using System;

namespace Blacksmith.Core.Forge
{
    // 연마: 빙글빙글 도는 숫돌(그라인더)에 날을 대고 날 끝을 세운다.
    // 날 끝은 가로로 늘어선 SegmentCount칸이고, 칸마다 얼마나 섰는지(Sharp 0~1)를 갖는다.
    // 마우스 버튼을 누른 채 가로로 밀면 닿은 칸이 갈리며 서고, 세로 위치가 날의 각도다.
    // 알맞은 각도(TargetAngle)는 위아래로 흔들리므로 허용 폭 안에 맞춰 따라가야 잘 갈린다.
    // 갈수록 날이 달아오르고(Heat), 한계에 닿으면 타서 점수가 깎이고 한동안 갈 수 없다.
    // 행동 키를 누르고 있으면 물에 담가 식힌다. 이미 다 간 곳을 계속 갈면 날이 얇아져 감점이다.
    // 모든 칸이 목표만큼 서거나 시간이 다 되면 끝난다. 점수는 평균으로 선 정도에서 감점과 보너스를 더한 값이다.
    // 포인터 좌표는 확대 화면 위 좌표다(x -1~1이 날 전체, y는 아래쪽이 +). 화면 밖이면 x가 -50보다 작다.
    public sealed class PolishGame : MiniGame
    {
        public enum PhaseKind { Intro, Grind, Done }

        public const int SegmentCount = 24;

        private readonly PolishRules _rules;
        private readonly PolishDifficulty _difficulty;
        private readonly Random _random;
        private readonly float _wave;
        private readonly float _phaseA;
        private readonly float _phaseB;
        private float _grindClock;
        private float _endTimer;
        private float _thinTime;
        private bool _wasDipping;
        private bool _warned;

        public PhaseKind Phase { get; private set; } = PhaseKind.Intro;
        public float Clock { get; private set; }
        // 칸마다 선 정도, 단단한 칸, 탄 칸
        public float[] Sharp { get; } = new float[SegmentCount];
        public bool[] Hard { get; } = new bool[SegmentCount];
        public bool[] Burnt { get; } = new bool[SegmentCount];
        public float PointerX { get; private set; } = -100f;
        public float PointerY { get; private set; }
        // 지금 숫돌에 대고 갈고 있는가(눌렀고, 타지 않았고, 물에 담그지 않았다)
        public bool Grinding { get; private set; }
        public bool Dipping { get; private set; }
        // 타서 식을 때까지 갈 수 없다.
        public bool Locked { get; private set; }
        public float Heat { get; private set; }
        public float TargetAngle { get; private set; }
        // 지금 각도가 알맞은 정도(0~1)
        public float Quality { get; private set; }
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        public int Burned { get; private set; }
        public int Dips { get; private set; }
        public float StageClock { get; private set; }

        public float AngleTolerance => _difficulty.AngleTolerance;
        public float TimeLimit => _difficulty.TimeLimit;
        public float WarnHeat => _rules.WarnHeat;
        public float BurnHeat => _rules.BurnHeat;
        public float ContactRadius => _rules.ContactRadius;
        public float SegmentGoal => _rules.SegmentGoal;
        public float SuccessScore => _rules.SuccessScore;
        public int Difficulty { get; }

        public float TimeLeft => Math.Max(0f, TimeLimit - _grindClock);

        // 몇 칸이 목표까지 갈렸는가
        public int DoneCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < SegmentCount; i++)
                {
                    if (Sharp[i] >= _rules.SegmentGoal)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        // 평균으로 선 정도(목표를 1로 본다)
        public float Clean
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < SegmentCount; i++)
                {
                    total += Math.Min(1f, Sharp[i] / _rules.SegmentGoal);
                }
                return total / SegmentCount;
            }
        }

        public float ThinLoss => Math.Min(_rules.MaxThin, _thinTime * _rules.ThinLossPerSecond);
        public float BurnLoss => Burned * _rules.BurnPenalty;

        // 지금까지의 점수(보너스까지)
        public float ProjectedScore => Clamp01(Clean - BurnLoss - ThinLoss + Math.Min(_rules.ComboBonusMax, BestCombo * _rules.ComboBonusPer));

        // prior: 앞 단계들의 평균 점수(낮을수록 처음 날이 덜 서 있다), difficulty: 광석 등급
        public PolishGame(PolishRules rules, int seed, int difficulty, float prior)
        {
            _rules = rules;
            Difficulty = Math.Max(0, Math.Min(rules.Difficulties.Length - 1, difficulty));
            _difficulty = rules.Difficulties[Difficulty];
            _random = new Random(seed);
            float drop = Clamp01(prior) < rules.PriorThreshold ? rules.PriorDrop : 0f;
            for (int i = 0; i < SegmentCount; i++)
            {
                float sharp = Range(rules.InitialMin, rules.InitialMax);
                bool hard = _random.NextDouble() < _difficulty.HardChance;
                Hard[i] = hard;
                Sharp[i] = Math.Max(0f, (hard ? sharp * 0.5f : sharp) - drop);
            }
            _wave = Range(rules.AngleSpeedMin, rules.AngleSpeedMax);
            _phaseA = Range(0f, 6.2832f);
            _phaseB = Range(0f, 6.2832f);
            TargetAngle = AngleAt(0f);
        }

        // 날 위 x 좌표가 속한 칸 번호(0~SegmentCount-1)
        public static int SegmentAt(float x)
        {
            int index = (int)Math.Floor((x + 1f) * 0.5f * SegmentCount);
            return Math.Max(0, Math.Min(SegmentCount - 1, index));
        }

        // 칸의 가운데 x 좌표
        public static float SegmentCenter(int index)
        {
            return -1f + (index + 0.5f) * 2f / SegmentCount;
        }

        private float Range(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }

        // 알맞은 각도: 두 가지 흔들림을 섞어 예측하기 어렵게 한다.
        private float AngleAt(float time)
        {
            return _rules.AngleAmplitude * (0.7f * (float)Math.Sin(_wave * time + _phaseA) + 0.3f * (float)Math.Sin(_wave * 1.9f * time + _phaseB));
        }

        public override void Tick(float deltaTime, MiniInput input)
        {
            if (Finished)
            {
                return;
            }

            Clock += deltaTime;
            StageClock += deltaTime;

            bool valid = input.PointerX > -50f;
            if (valid)
            {
                PointerX = Math.Max(-1f, Math.Min(1f, input.PointerX));
                PointerY = input.PointerY;
            }
            else
            {
                PointerX = -100f;
            }

            switch (Phase)
            {
                case PhaseKind.Intro:
                    if (StageClock >= _rules.IntroTime)
                    {
                        Phase = PhaseKind.Grind;
                        StageClock = 0f;
                        Raise(MiniEventKind.PhaseChanged, 1f, 1);
                    }
                    break;
                case PhaseKind.Grind:
                    TickGrind(deltaTime, input, valid);
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

        private void TickGrind(float deltaTime, MiniInput input, bool valid)
        {
            _grindClock += deltaTime;
            TargetAngle = AngleAt(_grindClock);
            float error = valid ? Math.Abs(PointerY - TargetAngle) : float.MaxValue;
            float tolerance = _difficulty.AngleTolerance;
            Quality = error <= tolerance ? 1f : Clamp01(2f - error / tolerance);

            Dipping = input.ActionHeld;
            if (Dipping && !_wasDipping)
            {
                Dips++;
                Raise(MiniEventKind.Hit, Heat, Dips);
            }
            _wasDipping = Dipping;

            Grinding = valid && input.PointerHeld && !Dipping && !Locked;
            UpdateHeat(deltaTime);
            if (Grinding)
            {
                Grind(deltaTime);
            }

            if (DoneCount == SegmentCount || _grindClock >= _difficulty.TimeLimit)
            {
                Grinding = false;
                Finish();
            }
        }

        private void UpdateHeat(float deltaTime)
        {
            if (Dipping)
            {
                Heat -= _rules.DipCool * deltaTime;
            }
            else if (Grinding)
            {
                Heat += _rules.HeatUp * _difficulty.HeatScale * (1f + _rules.OffAngleHeat * (1f - Quality)) * deltaTime;
            }
            else
            {
                Heat -= _rules.HeatCool * deltaTime;
            }
            Heat = Math.Max(0f, Heat);

            if (Locked && Heat <= _rules.ResumeHeat)
            {
                Locked = false;
            }

            if (Heat >= _rules.WarnHeat && !_warned && !Locked)
            {
                _warned = true;
                Raise(MiniEventKind.Warn, Heat);
            }
            else if (Heat < _rules.WarnHeat - 0.1f)
            {
                _warned = false;
            }

            if (Heat >= _rules.BurnHeat && !Locked)
            {
                Burn();
            }
        }

        // 날이 탔다: 닿은 칸이 상하고 콤보가 끊긴다.
        private void Burn()
        {
            Heat = _rules.BurnHeat;
            Locked = true;
            Grinding = false;
            Burned++;
            Combo = 0;
            for (int i = 0; i < SegmentCount; i++)
            {
                if (InContact(i))
                {
                    Burnt[i] = true;
                    Sharp[i] = Math.Max(0f, Sharp[i] - _rules.BurnSharpLoss);
                }
            }
            Raise(MiniEventKind.Hazard, BurnLoss, SegmentAt(PointerX));
        }

        private void Grind(float deltaTime)
        {
            bool untouched = true;
            for (int i = 0; i < SegmentCount; i++)
            {
                if (!InContact(i))
                {
                    continue;
                }

                float before = Sharp[i];
                if (before < _rules.SegmentGoal)
                {
                    untouched = false;
                }
                float rate = _rules.GrindRate * Quality * (Hard[i] ? _rules.HardRate : 1f);
                float after = Math.Min(1f, before + rate * deltaTime);
                Sharp[i] = after;
                if (before < _rules.SegmentGoal && after >= _rules.SegmentGoal)
                {
                    Combo++;
                    BestCombo = Math.Max(BestCombo, Combo);
                    Raise(MiniEventKind.Good, Combo, i);
                }
            }

            // 닿은 칸이 전부 이미 다 갈려 있었다면 날이 얇아진다.
            if (untouched)
            {
                _thinTime += deltaTime;
            }
        }

        // 숫돌에 닿는 칸인가. 닿는 폭 안에 조금이라도 들어오는 칸이 대상이다.
        private bool InContact(int index)
        {
            return PointerX > -50f && Math.Abs(SegmentCenter(index) - PointerX) <= _rules.ContactRadius + 1f / SegmentCount;
        }

        private void Finish()
        {
            Phase = PhaseKind.Done;
            Accuracy = ProjectedScore;
            Raise(MiniEventKind.Complete, Accuracy);
        }
    }
}
