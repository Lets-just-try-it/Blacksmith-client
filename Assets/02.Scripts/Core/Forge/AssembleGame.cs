using System;
using System.Collections.Generic;

namespace Blacksmith.Core.Forge
{
    // 조립: 오려 낸 조각을 리벳으로 잇는다. 판금 방어구의 리벳은 식은 채 박는다.
    // 이음새마다 겹쳐 댈 판이 어긋나 놓여 있다.
    // 1. 판을 마우스로 끌어 아래 판의 구멍에 맞춘다. 구멍이 겹치는 곳에서 놓으면 핀으로 고정되고, 더 정확히 맞출수록 품질이 좋다.
    // 2. 구멍을 눌러 리벳을 끼우고, 바깥 링이 안쪽 원에 닿는 순간 눌러 두 번 친다.
    // 3. 이음새의 구멍을 모두 박으면 다음 이음새로 간다. 이음새마다 제한시간이 있다.
    // 정확도는 이음새 점수의 평균이고, 판 성형 점수가 낮으면 조금 깎인다.
    public sealed class AssembleGame : MiniGame
    {
        public enum PhaseKind { Intro, Work, SeamDone, Done }

        public sealed class Hole
        {
            // 조립한 방어구 위의 위치(칸)
            public float X;
            public float Y;
            // 0 빈 구멍, 1 리벳을 끼움(망치 대기), 2 완료
            public int State;
            public int Hits;
            public int Misses;
            public float GradeSum;
            // 완료한 뒤의 품질(0~1)
            public float Quality;
        }

        public sealed class Seam
        {
            public string Name;
            public Hole[] Holes;
            public float Time;
            public float Limit;
            public float Score;
            // 판을 얼마나 정확히 맞춰 고정했는가(0~1)
            public float AlignScore = 1f;
            // 덮는 판의 범위(구멍 둘레)
            public float MinX;
            public float MaxX;
            public float MinY;
            public float MaxY;

            public int Done
            {
                get
                {
                    int count = 0;
                    foreach (Hole hole in Holes)
                    {
                        if (hole.State == 2)
                        {
                            count++;
                        }
                    }
                    return count;
                }
            }
        }

        public struct PartPos
        {
            public string Name;
            public float X;
            public float Y;
        }

        private readonly AssembleRules _rules;
        private readonly AssembleDifficulty _difficulty;
        private readonly Random _random;
        private readonly float _plateScore;
        private float _grabX;
        private float _grabY;
        private float _endTimer;

        public PhaseKind Phase { get; private set; } = PhaseKind.Intro;
        public List<Seam> Seams { get; } = new List<Seam>();
        public int Current { get; private set; }
        // 0 투구, 1 갑옷
        public int Kind { get; }
        public int Difficulty { get; }
        public float Clock { get; private set; }
        public float PhaseClock { get; private set; }
        public bool Hammering { get; private set; }
        public int HammerHole { get; private set; } = -1;
        // 링이 줄어든 시간. 0 아래는 링이 시작되기 전 대기다.
        public float RingTime { get; private set; }
        // 마지막 망치질의 타이밍 오차(초). 음수면 일찍, 양수면 늦게 쳤다.
        public float LastError { get; private set; }
        // 덮는 판이 구멍에서 어긋난 정도(칸)
        public float OffsetX { get; private set; }
        public float OffsetY { get; private set; }
        public bool Locked { get; private set; }
        public bool Dragging { get; private set; }
        // 어긋난 채 놓은 횟수
        public int Misaligned { get; private set; }
        public float PointerX { get; private set; } = -100f;
        public float PointerY { get; private set; }

        public float AlignTolerance => _difficulty.AlignTolerance;
        public float RingDuration => _difficulty.RingDuration;
        public float PerfectWindow => _difficulty.PerfectWindow;
        public float GoodWindow => _difficulty.GoodWindow;
        public float SuccessScore => _rules.SuccessScore;
        public float AlignError => (float)Math.Sqrt(OffsetX * OffsetX + OffsetY * OffsetY);
        public bool Aligned => AlignError <= _difficulty.AlignTolerance;
        public Seam Active => Current < Seams.Count ? Seams[Current] : null;

        public int TotalHoles
        {
            get
            {
                int count = 0;
                foreach (Seam seam in Seams)
                {
                    count += seam.Holes.Length;
                }
                return count;
            }
        }

        public int DoneHoles
        {
            get
            {
                int count = 0;
                foreach (Seam seam in Seams)
                {
                    count += seam.Done;
                }
                return count;
            }
        }

        public float AlignAverage
        {
            get
            {
                float sum = 0f;
                foreach (Seam seam in Seams)
                {
                    sum += seam.AlignScore;
                }
                return Seams.Count == 0 ? 0f : sum / Seams.Count;
            }
        }

        // plateScore는 앞 단계(판 성형)의 정확도(0~1)다.
        public AssembleGame(AssembleRules rules, int seed, int difficulty, float plateScore, int kind)
        {
            _rules = rules;
            Difficulty = Math.Max(0, Math.Min(rules.Difficulties.Length - 1, difficulty));
            _difficulty = rules.Difficulties[Difficulty];
            Kind = Math.Max(0, Math.Min(ArmorKinds.Count - 1, kind));
            _plateScore = Clamp01(plateScore);
            _random = new Random(seed);
            BuildSeams();
        }

        // 조립한 방어구에서 조각들의 위치(뒤에서 앞 순서). 방어구 가운데가 (0, 0)이다.
        public PartPos[] Layout()
        {
            var list = new List<PartPos>();
            int optional = _difficulty.OptionalSeams;
            if (Kind == (int)ArmorKind.Helmet)
            {
                if (optional >= 2) list.Add(new PartPos { Name = "목 보호대", X = 0f, Y = 4.5f });
                list.Add(new PartPos { Name = "왼 볼가리개", X = -5.0f, Y = 2.0f });
                list.Add(new PartPos { Name = "오른 볼가리개", X = 5.0f, Y = 2.0f });
                list.Add(new PartPos { Name = "얼굴 가리개", X = 0f, Y = 2.0f });
                list.Add(new PartPos { Name = "투구 몸통", X = 0f, Y = -1.6f });
                if (optional >= 1) list.Add(new PartPos { Name = "이마 보강판", X = 0f, Y = -4.1f });
            }
            else
            {
                list.Add(new PartPos { Name = "등판", X = -0.5f, Y = 0.4f });
                if (optional >= 1) list.Add(new PartPos { Name = "옆구리판", X = 4.6f, Y = 0.8f });
                list.Add(new PartPos { Name = "허리 치마", X = 0f, Y = 4.8f });
                list.Add(new PartPos { Name = "가슴판", X = 0f, Y = 0f });
                if (optional >= 2) list.Add(new PartPos { Name = "목 가리개", X = 0f, Y = -4.4f });
                list.Add(new PartPos { Name = "왼 어깨판", X = -5.0f, Y = -3.4f });
                list.Add(new PartPos { Name = "오른 어깨판", X = 5.0f, Y = -3.4f });
            }
            return list.ToArray();
        }

        private static Hole[] Line(float x0, float y0, float x1, float y1, int count)
        {
            var holes = new Hole[count];
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : (float)i / (count - 1);
                holes[i] = new Hole { X = x0 + (x1 - x0) * t, Y = y0 + (y1 - y0) * t };
            }
            return holes;
        }

        private static Hole[] Join(Hole[] a, Hole[] b)
        {
            var result = new Hole[a.Length + b.Length];
            a.CopyTo(result, 0);
            b.CopyTo(result, a.Length);
            return result;
        }

        private void AddSeam(string name, Hole[] holes)
        {
            var seam = new Seam
            {
                Name = name,
                Holes = holes,
                Limit = _rules.SeamBaseTime + _rules.SeamTimePerHole * holes.Length,
                MinX = float.MaxValue,
                MinY = float.MaxValue,
                MaxX = float.MinValue,
                MaxY = float.MinValue,
            };
            foreach (Hole hole in holes)
            {
                seam.MinX = Math.Min(seam.MinX, hole.X - _rules.StripPad);
                seam.MaxX = Math.Max(seam.MaxX, hole.X + _rules.StripPad);
                seam.MinY = Math.Min(seam.MinY, hole.Y - _rules.StripPad);
                seam.MaxY = Math.Max(seam.MaxY, hole.Y + _rules.StripPad);
            }
            Seams.Add(seam);
        }

        // 이음새의 모양은 방어구마다 정해져 있고, 구멍 수와 선택 이음새만 난이도가 정한다.
        private void BuildSeams()
        {
            int bonus = _difficulty.HoleBonus;
            int side = _difficulty.SideHoles;
            int optional = _difficulty.OptionalSeams;
            if (Kind == (int)ArmorKind.Helmet)
            {
                AddSeam("몸통과 얼굴 가리개", Line(-3.4f, 0.8f, 3.4f, 0.8f, 4 + bonus));
                AddSeam("볼가리개", Join(Line(-3.6f, 1.0f, -3.6f, 3.0f, side), Line(3.6f, 1.0f, 3.6f, 3.0f, side)));
                if (optional >= 1) AddSeam("이마 보강판", Line(-2.8f, -3.9f, 2.8f, -3.9f, 4));
                if (optional >= 2) AddSeam("목 보호대", Line(-3.0f, 3.4f, 3.0f, 3.4f, 5));
            }
            else
            {
                AddSeam("어깨판", Join(Line(-2.6f, -3.8f, -2.6f, -2.8f, side), Line(2.6f, -3.8f, 2.6f, -2.8f, side)));
                AddSeam("가슴판과 등판", Line(-3.2f, -1.0f, -3.2f, 2.8f, 4 + bonus));
                AddSeam("허리 치마", Line(-3.0f, 3.4f, 3.0f, 3.4f, 4 + bonus));
                if (optional >= 1) AddSeam("옆구리판", Line(3.3f, -0.4f, 3.3f, 2.8f, 4));
                if (optional >= 2) AddSeam("목 가리개", Line(-2.6f, -3.8f, 2.6f, -3.8f, 4));
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

            switch (Phase)
            {
                case PhaseKind.Intro:
                    if (PhaseClock >= _rules.IntroTime)
                    {
                        BeginSeam(0);
                    }
                    break;
                case PhaseKind.Work:
                    TickWork(deltaTime, input, hasPointer);
                    break;
                case PhaseKind.SeamDone:
                    TickSeamDone();
                    break;
                case PhaseKind.Done:
                    TickDone(deltaTime);
                    break;
            }
        }

        private void TickSeamDone()
        {
            if (PhaseClock < _rules.SeamEndTime)
            {
                return;
            }

            if (Current + 1 < Seams.Count)
            {
                BeginSeam(Current + 1);
            }
            else
            {
                FinishAll();
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

        // 덮는 판은 구멍에서 어긋난 채 놓여 있다.
        private void BeginSeam(int index)
        {
            Current = index;
            Phase = PhaseKind.Work;
            PhaseClock = 0f;
            Hammering = false;
            HammerHole = -1;
            Locked = false;
            Dragging = false;

            double angle = _random.NextDouble() * Math.PI * 2.0;
            float distance = _difficulty.StartOffset * (0.8f + 0.4f * (float)_random.NextDouble());
            OffsetX = distance * (float)Math.Cos(angle);
            OffsetY = distance * (float)Math.Sin(angle);
            Raise(MiniEventKind.PhaseChanged, index, index);
        }

        private void TickWork(float deltaTime, MiniInput input, bool hasPointer)
        {
            Seam seam = Seams[Current];
            seam.Time += deltaTime;

            if (!Locked)
            {
                TickAlign(seam, input, hasPointer);
            }
            else
            {
                TickRivet(seam, deltaTime, input, hasPointer);
            }

            if (Phase == PhaseKind.Work && (seam.Done == seam.Holes.Length || seam.Time >= seam.Limit))
            {
                EndSeam(seam);
            }
        }

        // 판을 잡아 끌고, 놓은 자리가 허용 오차 안이면 핀으로 고정한다.
        private void TickAlign(Seam seam, MiniInput input, bool hasPointer)
        {
            if (!Dragging && input.PointerPressed && hasPointer && OnStrip(seam))
            {
                Dragging = true;
                _grabX = PointerX - OffsetX;
                _grabY = PointerY - OffsetY;
            }

            if (!Dragging)
            {
                return;
            }

            if (hasPointer && input.PointerHeld)
            {
                OffsetX = PointerX - _grabX;
                OffsetY = PointerY - _grabY;
            }

            if (input.PointerHeld)
            {
                return;
            }

            Dragging = false;
            if (Aligned)
            {
                seam.AlignScore = Clamp01(1f - _rules.AlignLoss * AlignError / _difficulty.AlignTolerance);
                OffsetX = 0f;
                OffsetY = 0f;
                Locked = true;
                Raise(MiniEventKind.Pin, seam.AlignScore, Current);
            }
            else
            {
                Misaligned++;
                Raise(MiniEventKind.Misalign, AlignError, Current);
            }
        }

        private bool OnStrip(Seam seam)
        {
            float pad = _rules.GrabPad;
            return PointerX >= seam.MinX + OffsetX - pad && PointerX <= seam.MaxX + OffsetX + pad
                && PointerY >= seam.MinY + OffsetY - pad && PointerY <= seam.MaxY + OffsetY + pad;
        }

        private void TickRivet(Seam seam, float deltaTime, MiniInput input, bool hasPointer)
        {
            if (Hammering)
            {
                RingTime += deltaTime;
                if (RingTime > _difficulty.RingDuration + _difficulty.GoodWindow + _rules.RingOvertime)
                {
                    seam.Holes[HammerHole].Misses++;
                    RingTime = -_rules.RingRest;
                    Raise(MiniEventKind.Miss, 0f, HammerHole);
                }

                if ((input.PointerPressed || input.ActionPressed) && RingTime >= 0f)
                {
                    Strike(seam);
                }
            }
            else if (input.PointerPressed && hasPointer)
            {
                Seat(seam);
            }
        }

        // 누른 자리에서 가장 가까운 빈 구멍에 리벳을 끼운다.
        private void Seat(Seam seam)
        {
            int best = -1;
            float bestDistance = _rules.SeatRadius * _rules.SeatRadius;
            for (int i = 0; i < seam.Holes.Length; i++)
            {
                if (seam.Holes[i].State != 0)
                {
                    continue;
                }

                float dx = PointerX - seam.Holes[i].X;
                float dy = PointerY - seam.Holes[i].Y;
                float distance = dx * dx + dy * dy;
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            if (best < 0)
            {
                return;
            }

            Hole hole = seam.Holes[best];
            hole.State = 1;
            hole.Hits = 0;
            Hammering = true;
            HammerHole = best;
            RingTime = -_rules.SeatDelay;
            Raise(MiniEventKind.Rivet, 0f, best);
        }

        private void Strike(Seam seam)
        {
            Hole hole = seam.Holes[HammerHole];
            LastError = RingTime - _difficulty.RingDuration;
            float error = Math.Abs(LastError);

            if (error > _difficulty.GoodWindow)
            {
                hole.Misses++;
                RingTime = -_rules.RingRest;
                Raise(MiniEventKind.Miss, 0f, HammerHole, LastError);
                return;
            }

            bool perfect = error <= _difficulty.PerfectWindow;
            hole.Hits++;
            hole.GradeSum += perfect ? _rules.PerfectScore : _rules.GoodScore;
            Raise(perfect ? MiniEventKind.Perfect : MiniEventKind.Hit, hole.Hits, HammerHole, LastError);

            if (hole.Hits >= _rules.HitsNeeded)
            {
                hole.Quality = Clamp01(hole.GradeSum / _rules.HitsNeeded - _rules.MissPenalty * hole.Misses - _rules.HoleQualityOffset);
                hole.State = 2;
                Hammering = false;
                Raise(MiniEventKind.Good, hole.Quality, HammerHole);
            }
            else
            {
                RingTime = -_rules.RingRest;
            }
        }

        // 판을 고정하지 못했으면 박은 리벳도 의미가 없다.
        private float SeamQuality(Seam seam)
        {
            float sum = 0f;
            foreach (Hole hole in seam.Holes)
            {
                if (hole.State == 2)
                {
                    sum += hole.Quality;
                }
            }
            float align = seam.Done > 0 ? seam.AlignScore : 0f;
            return Clamp01(sum / seam.Holes.Length * (_rules.AlignBase + _rules.AlignWeight * align));
        }

        private void EndSeam(Seam seam)
        {
            if (!Locked)
            {
                seam.AlignScore = 0f;
            }
            seam.Score = SeamQuality(seam);
            Hammering = false;
            Dragging = false;
            Phase = PhaseKind.SeamDone;
            PhaseClock = 0f;
            Raise(MiniEventKind.SeamDone, seam.Score, Current);
        }

        // 이음새 점수는 구멍 수로 가중 평균하고, 앞 단계(판 성형)가 서툴수록 조금 깎인다.
        private void FinishAll()
        {
            float sum = 0f;
            int count = 0;
            foreach (Seam seam in Seams)
            {
                sum += seam.Score * seam.Holes.Length;
                count += seam.Holes.Length;
            }
            float average = count == 0 ? 0f : sum / count;
            Accuracy = Clamp01(average * (_rules.PlateBase + _rules.PlateWeight * _plateScore));
            Phase = PhaseKind.Done;
            _endTimer = 0f;
            Raise(MiniEventKind.Complete, Accuracy);
        }
    }
}
