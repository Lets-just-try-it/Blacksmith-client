using System;
using System.Collections.Generic;

namespace Blacksmith.Core.Forge
{
    // 장신구 제작(보석 작업대): 던전에서 얻은 보석으로 목걸이를 만든다. 체인 잇기 → 보석 깎기 → 장착 → 광내기.
    // 효과 종류와 티어는 만드는 항목(보석과 개수)이 정하고, 얼마나 잘 만드느냐가 정확도(등급)를 정한다.
    //
    // ① 체인 잇기: 난이도에 따라 목걸이 모양(단순/복잡/매우 복잡)이 정해진다. 조각 상자에서 조각을 꺼내 가닥의 빈 칸에 놓아
    //    이웃과 끝의 모양·색이 같게 맞물리게 잇는다. 조각은 뒤집을 수 있다. 어긋난 연결을 만들며 놓으면 실수로 쌓인다.
    // ② 보석 깎기: 거친 원석을 목표 모양(점선)으로 깎는다. 실제 면 가공처럼 보석을 24칸 눈금(인덱스)으로 돌려 깎을 면을
    //    아래 숫돌에 맞추고, 행동 키를 누르고 있는 동안 그 면이 갈린다. 거친 숫돌은 빠르지만 면이 거칠어지고, 고운 숫돌은
    //    느리지만 면이 곱고 정밀하다. 깎은 것은 되돌릴 수 없고, 목표보다 더 깎으면 점수가 깎인다.
    // ③ 장착: 깎아 낸 보석과 똑같은 모양의 구멍이 펜던트 틀에 있다. 보석을 끌어다 구멍 위에 놓고, 좌우 키로 돌려 맞춘 뒤
    //    행동 키를 꾹 눌러 박는다.
    // ④ 광내기: 깎인 보석의 면마다 천으로 문질러 윤을 낸다. 거칠게 깎은 면일수록 오래 닦아야 한다.
    //
    // 체인 단계의 포인터 좌표는 화면이 가리킨 대상을 번호로 알려 준다: 상자 안 조각은 PieceCode, 가닥의 칸은 SlotCode,
    // 상자의 빈 곳은 CrateCode이고, 아무것도 아니면 -100이다. 장착과 광내기는 보석 가운데를 (0,0)으로 한 칸 좌표다.
    public sealed class NecklaceGame : MiniGame
    {
        public enum PhaseKind { Intro, Chain, Cut, Set, Polish, StepDone, Done }

        public sealed class Seg
        {
            public int Id;
            // 정답 위치(가닥, 번호)
            public int Row;
            public int Pos;
            // 연결 모양 번호(모양 = 번호 % 4, 색 = 번호 / 4)
            public int LSym;
            public int RSym;
            // 뒤집어 놓았다(좌우가 바뀐다)
            public bool Flipped;
            public int SlotRow = -1;
            public int SlotIdx = -1;

            public bool Placed => SlotRow >= 0;
            public int Left => Flipped ? RSym : LSym;
            public int Right => Flipped ? LSym : RSym;
        }

        public sealed class Row
        {
            public int Len;
            // 시작 쪽, 끝 쪽 연결 모양
            public int LeftSym;
            public int RightSym;
            // 칸마다 놓인 조각 번호(없으면 -1)
            public int[] Slots;
        }

        public sealed class TFacet
        {
            // 면이 향하는 방향(도, 보석 기준, 15의 배수)
            public int Phi;
            // 보석 가운데에서 면까지의 거리(보석 반지름 = 1)
            public float D;
        }

        public const int SymCount = 16;
        // 보석을 크게 보여 줄 때의 반지름(칸)
        public const float GemR = 2.3f;
        // 인덱스(돌림 눈금) 칸 수와 한 칸의 각도
        public const int IndexCount = 24;
        public const int IndexDeg = 15;

        // 체인 단계의 포인터 좌표 번호
        public const float PieceCodeBase = 100f;
        public const float SlotCodeBase = 200f;
        public const float CrateCode = 350f;
        public const float NoTarget = -100f;

        public static readonly string[] StepNames = { "체인", "깎기", "장착", "광내기" };

        private static readonly float[] CosTable = BuildCosTable();

        private readonly NecklaceRules _rules;
        private readonly NecklaceDifficulty _difficulty;
        private readonly int _level;
        private readonly Random _random;
        private float _endTimer;
        private float _stepTimer;
        private int _lastJoined;
        private PhaseKind _pending;
        private float _baseX;
        private float _baseY;
        private float _grabDx;
        private float _grabDy;
        private float _seatSum;
        private float _seatPosSum;
        private float _slipTimer;
        private float _lastX;
        private float _lastY;
        private bool _haveLast;

        public NecklaceGame(NecklaceRules rules, int seed, int difficulty)
        {
            _rules = rules;
            _level = Math.Max(0, Math.Min(rules.Difficulties.Length - 1, difficulty));
            _difficulty = rules.Difficulties[_level];
            _random = new Random(seed);
        }

        public PhaseKind Phase { get; private set; } = PhaseKind.Intro;
        public int Level => _level;
        // 0 체인, 1 깎기, 2 장착, 3 광내기
        public int Step { get; private set; }
        public string ShapeName => NecklaceShape.Names[Math.Min(_level, NecklaceShape.Names.Length - 1)];

        // ---- 체인 잇기
        public List<Seg> Segs { get; } = new List<Seg>();
        public List<Row> Rows { get; } = new List<Row>();
        // 집은 조각
        public int Held { get; private set; } = -1;
        public float ChainLeft { get; private set; }
        public float ChainLimit { get; private set; }
        public float ChainScore { get; private set; }
        public int Joined { get; private set; }
        public int Mistakes { get; private set; }

        public int JointTotal
        {
            get
            {
                int total = 0;
                foreach (Row row in Rows)
                {
                    total += row.Len + 1;
                }

                return total;
            }
        }

        // ---- 보석 깎기
        public List<TFacet> TargetFacets { get; } = new List<TFacet>();
        // 지금 보석의 모양(각도별 반지름)과 목표 모양
        public float[] Profile { get; } = new float[360];
        public float[] Target { get; } = new float[360];
        // 방향(인덱스)별 면의 거칠기 0~1
        public float[] Rough { get; } = new float[IndexCount];
        public int Index { get; private set; }
        public int RotDeg => Index * IndexDeg;
        // 0 거친 숫돌, 1 고운 숫돌
        public int Tool { get; private set; }
        public bool Grinding { get; private set; }
        // 갈고 있는 면의 지금 거리
        public float Plane { get; private set; }
        // 목표와 얼마나 같은가 0~1
        public float Match { get; private set; }
        public float CutLeft { get; private set; }
        public float CutLimit { get; private set; }
        public float CutScore { get; private set; }
        public float Smooth { get; private set; }
        public int Overcuts { get; private set; }

        // ---- 장착
        // 보석의 지금 위치(칸, 틀 가운데가 (0,0))와 각도(인덱스), 구멍의 각도
        public float GemX { get; private set; }
        public float GemY { get; private set; }
        public int GemIdx { get; private set; }
        public int SocketIdx { get; private set; }
        public float PosErr { get; private set; }
        // 보석 - 구멍(-12..12 칸)
        public int IdxDiff { get; private set; }
        public float PosTol { get; private set; }
        public bool Aligned { get; private set; }
        public bool Near { get; private set; }
        public bool Dragging { get; private set; }
        public bool Seating { get; private set; }
        public float SeatProgress { get; private set; }
        public float SetLeft { get; private set; }
        public float SetLimit { get; private set; }
        public float Damage { get; private set; }
        public float SetScore { get; private set; }
        public int Slips { get; private set; }

        // ---- 광내기
        // 면마다 윤 0~1, 면마다 닦아야 할 거리(칸)
        public float[] Shine { get; private set; }
        public float[] ShineNeed { get; private set; }
        public bool Scrubbing { get; private set; }
        // 지금 천이 닿은 면
        public int Hover { get; private set; } = -1;
        public float PolishLeft { get; private set; }
        public float PolishLimit { get; private set; }
        public float PolishScore { get; private set; }
        public int PolishRot => SocketIdx * IndexDeg;

        public float AvgShine
        {
            get
            {
                if (Shine == null || Shine.Length == 0)
                {
                    return 0f;
                }

                float sum = 0f;
                foreach (float value in Shine)
                {
                    sum += value;
                }

                return sum / Shine.Length;
            }
        }

        public int ShinedCount
        {
            get
            {
                int count = 0;
                if (Shine != null)
                {
                    foreach (float value in Shine)
                    {
                        if (value >= 0.999f)
                        {
                            count++;
                        }
                    }
                }

                return count;
            }
        }

        public float PointerX { get; private set; } = NoTarget;
        public float PointerY { get; private set; }
        public float Clock { get; private set; }
        public float PhaseClock { get; private set; }
        public float SuccessScore => _rules.SuccessScore;

        // 보석 반지름을 1로 한 허용 오차
        public float Tol => _difficulty.CutTolCm / _rules.GemCm;
        public float TolCm => _difficulty.CutTolCm;

        public static int PieceCode(int segId)
        {
            return (int)PieceCodeBase + segId;
        }

        public static int SlotCode(int row, int slot)
        {
            return (int)SlotCodeBase + row * 10 + slot;
        }

        public string Cm(float remaining)
        {
            return (remaining * _rules.GemCm).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "cm";
        }

        // ------------------------------------------------------------------ 공통

        private static float[] BuildCosTable()
        {
            float[] table = new float[360];
            for (int i = 0; i < 360; i++)
            {
                table[i] = (float)Math.Cos(i * Math.PI / 180.0);
            }

            return table;
        }

        private static int Mod360(int angle)
        {
            return ((angle % 360) + 360) % 360;
        }

        public static float Wrap(float angle)
        {
            while (angle > 180f)
            {
                angle -= 360f;
            }

            while (angle <= -180f)
            {
                angle += 360f;
            }

            return angle;
        }

        private float Support(int phiDeg, float[] profile)
        {
            float best = 0f;
            for (int k = 0; k < 360; k++)
            {
                float c = CosTable[Mod360(k - phiDeg)];
                if (c <= 0.001f)
                {
                    continue;
                }

                float value = profile[k] * c;
                if (value > best)
                {
                    best = value;
                }
            }

            return best;
        }

        // ------------------------------------------------------------------ 진행

        public override void Tick(float deltaTime, MiniInput input)
        {
            if (Finished)
            {
                return;
            }

            Clock += deltaTime;
            PhaseClock += deltaTime;
            if (input.PointerX > -50f)
            {
                PointerX = input.PointerX;
                PointerY = input.PointerY;
            }
            else
            {
                PointerX = NoTarget;
            }

            switch (Phase)
            {
                case PhaseKind.Intro:
                    if (PhaseClock >= _rules.IntroTime)
                    {
                        BeginChain();
                    }

                    break;
                case PhaseKind.Chain:
                    TickChain(deltaTime, input);
                    break;
                case PhaseKind.Cut:
                    TickCut(deltaTime, input);
                    break;
                case PhaseKind.Set:
                    TickSet(deltaTime, input);
                    break;
                case PhaseKind.Polish:
                    TickPolish(deltaTime, input);
                    break;
                case PhaseKind.StepDone:
                    _stepTimer += deltaTime;
                    if (_stepTimer >= _rules.StepDoneTime)
                    {
                        if (_pending == PhaseKind.Cut)
                        {
                            BeginCut();
                        }
                        else if (_pending == PhaseKind.Set)
                        {
                            BeginSet();
                        }
                        else
                        {
                            BeginPolish();
                        }
                    }

                    break;
                case PhaseKind.Done:
                    _endTimer += deltaTime;
                    if (_endTimer >= _rules.EndTime)
                    {
                        Finished = true;
                        Raise(MiniEventKind.Finish, Accuracy);
                    }

                    break;
            }
        }

        private void FinishStep(int step, float score, PhaseKind next)
        {
            Step = step;
            Phase = PhaseKind.StepDone;
            _pending = next;
            _stepTimer = 0f;
            Raise(MiniEventKind.Beat, score, step);
        }

        // ------------------------------------------------------------------ ① 체인 잇기

        private void BeginChain()
        {
            int[] lengths = NecklaceShape.Lens[Math.Min(_level, NecklaceShape.Lens.Length - 1)];
            int id = 0;
            for (int r = 0; r < lengths.Length; r++)
            {
                int length = lengths[r];
                var symbols = new List<int>();
                for (int i = 0; i < SymCount; i++)
                {
                    symbols.Add(i);
                }

                Shuffle(symbols);
                var joints = new int[length + 1];
                for (int k = 0; k <= length; k++)
                {
                    joints[k] = symbols[k];
                }

                var row = new Row { Len = length, LeftSym = joints[0], RightSym = joints[length], Slots = new int[length] };
                for (int k = 0; k < length; k++)
                {
                    row.Slots[k] = -1;
                }

                Rows.Add(row);
                for (int k = 0; k < length; k++)
                {
                    Segs.Add(new Seg { Id = id++, Row = r, Pos = k, LSym = joints[k], RSym = joints[k + 1], Flipped = _random.NextDouble() < 0.5 });
                }
            }

            for (int i = Segs.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                Seg swap = Segs[i];
                Segs[i] = Segs[j];
                Segs[j] = swap;
            }

            ChainLimit = _difficulty.ChainBaseTime + _difficulty.ChainTimePerPiece * Segs.Count;
            ChainLeft = ChainLimit;
            Held = -1;
            Joined = 0;
            _lastJoined = 0;
            Mistakes = 0;
            Step = 0;
            Phase = PhaseKind.Chain;
            PhaseClock = 0f;
            Raise(MiniEventKind.PhaseChanged, 0f, 0);
        }

        private void Shuffle(List<int> values)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                int swap = values[i];
                values[i] = values[j];
                values[j] = swap;
            }
        }

        public Seg SegById(int id)
        {
            foreach (Seg seg in Segs)
            {
                if (seg.Id == id)
                {
                    return seg;
                }
            }

            return null;
        }

        // 테스트용: 이 칸의 정답 조각 번호
        public int SolutionId(int row, int pos)
        {
            foreach (Seg seg in Segs)
            {
                if (seg.Row == row && seg.Pos == pos)
                {
                    return seg.Id;
                }
            }

            return -1;
        }

        public bool JointOk(int r, int j)
        {
            Row row = Rows[r];
            int left = j == 0 ? row.LeftSym : (row.Slots[j - 1] >= 0 ? SegById(row.Slots[j - 1]).Right : -1);
            int right = j == row.Len ? row.RightSym : (row.Slots[j] >= 0 ? SegById(row.Slots[j]).Left : -1);
            return left >= 0 && right >= 0 && left == right;
        }

        private bool JointFilled(int r, int j)
        {
            Row row = Rows[r];
            return (j == 0 || row.Slots[j - 1] >= 0) && (j == row.Len || row.Slots[j] >= 0);
        }

        private void TickChain(float deltaTime, MiniInput input)
        {
            ChainLeft -= deltaTime;
            float x = input.PointerX;

            bool flip = input.SecondaryPressed || input.ActionPressed;
            if (flip && Held >= 0)
            {
                Seg held = SegById(Held);
                held.Flipped = !held.Flipped;
                Raise(MiniEventKind.Tick, 1f, Held);
            }
            else if (input.PointerPressed && x > -50f)
            {
                HandleChainClick(x);
            }

            Joined = 0;
            for (int r = 0; r < Rows.Count; r++)
            {
                for (int j = 0; j <= Rows[r].Len; j++)
                {
                    if (JointOk(r, j))
                    {
                        Joined++;
                    }
                }
            }

            if (Joined > _lastJoined)
            {
                Raise(MiniEventKind.Hit, Joined, Joined - 1);
            }

            _lastJoined = Joined;

            bool full = true;
            foreach (Row row in Rows)
            {
                foreach (int segId in row.Slots)
                {
                    if (segId < 0)
                    {
                        full = false;
                    }
                }
            }

            if (full && Joined == JointTotal)
            {
                EndChain(true);
            }
            else if (ChainLeft <= 0f)
            {
                ChainLeft = 0f;
                EndChain(false);
            }
            else if (input.FinishPressed)
            {
                EndChain(false);
            }
        }

        private void HandleChainClick(float x)
        {
            if (x >= SlotCodeBase && x < SlotCodeBase + 100f)
            {
                int code = (int)Math.Round(x) - (int)SlotCodeBase;
                int r = code / 10;
                int j = code % 10;
                if (r < 0 || r >= Rows.Count || j >= Rows[r].Len)
                {
                    return;
                }

                if (Held >= 0)
                {
                    if (Rows[r].Slots[j] >= 0)
                    {
                        // 이미 조각이 있는 칸: 놓을 수 없다.
                        Raise(MiniEventKind.Warn, 0f, Rows[r].Slots[j]);
                    }
                    else
                    {
                        Place(r, j);
                    }
                }
                else if (Rows[r].Slots[j] >= 0)
                {
                    Seg seg = SegById(Rows[r].Slots[j]);
                    Held = seg.Id;
                    seg.SlotRow = -1;
                    seg.SlotIdx = -1;
                    Rows[r].Slots[j] = -1;
                    Raise(MiniEventKind.Spawn, 0f, Held);
                }
            }
            else if (x >= PieceCodeBase && x < SlotCodeBase)
            {
                int id = (int)Math.Round(x) - (int)PieceCodeBase;
                Seg seg = SegById(id);
                if (seg != null && !seg.Placed && Held != id)
                {
                    // 집고 있던 조각은 상자로 돌아간다.
                    Held = id;
                    Raise(MiniEventKind.Spawn, 0f, id);
                }
            }
            else if (Math.Abs(x - CrateCode) < 5f && Held >= 0)
            {
                // 상자의 빈 곳을 누르면 집은 조각을 상자에 도로 넣는다.
                Held = -1;
                Raise(MiniEventKind.Tick, 0f, 0);
            }
        }

        private void Place(int r, int j)
        {
            Row row = Rows[r];
            Seg seg = SegById(Held);
            row.Slots[j] = seg.Id;
            seg.SlotRow = r;
            seg.SlotIdx = j;
            Held = -1;
            bool bad = (JointFilled(r, j) && !JointOk(r, j)) || (JointFilled(r, j + 1) && !JointOk(r, j + 1));
            if (bad)
            {
                Mistakes++;
                Raise(MiniEventKind.Miss, Mistakes, seg.Id);
            }
            else
            {
                Raise(MiniEventKind.Charge, 0f, seg.Id);
            }
        }

        private void EndChain(bool secure)
        {
            Held = -1;
            float fraction = (float)Joined / JointTotal;
            float time = secure ? 0.85f + 0.15f * Clamp01(ChainLeft / ChainLimit) : 0.8f;
            ChainScore = Clamp01(fraction * time - 0.02f * Math.Min(8, Mistakes));
            FinishStep(0, ChainScore, PhaseKind.Cut);
        }

        // ------------------------------------------------------------------ ② 보석 깎기

        private void BeginCut()
        {
            Step = 1;
            int count = _difficulty.FacetCount;
            float baseD = 0.82f;
            float spread = _difficulty.FacetSpread;
            int spacing = 360 / count;
            int offset = IndexDeg * _random.Next(IndexCount);
            for (int i = 0; i < count; i++)
            {
                TargetFacets.Add(new TFacet { Phi = Mod360(offset + i * spacing), D = baseD + ((float)_random.NextDouble() - 0.5f) * 2f * spread });
            }

            // 목표 모양: 모든 면의 안쪽을 겹친 것
            for (int k = 0; k < 360; k++)
            {
                float t = 1f;
                foreach (TFacet facet in TargetFacets)
                {
                    float c = CosTable[Mod360(k - facet.Phi)];
                    if (c > 0.0001f)
                    {
                        t = Math.Min(t, facet.D / c);
                    }
                }

                Target[k] = t;
            }

            // 거친 원석: 목표보다 항상 크다(울퉁불퉁한 둥근 덩어리)
            float a = (float)_random.NextDouble() * 6.28f;
            float b = (float)_random.NextDouble() * 6.28f;
            float c2 = (float)_random.NextDouble() * 6.28f;
            for (int k = 0; k < 360; k++)
            {
                double theta = k * Math.PI / 180.0;
                float noise = 1.04f + 0.05f * (float)Math.Sin(3 * theta + a) + 0.035f * (float)Math.Sin(5 * theta + b) + 0.02f * (float)Math.Sin(9 * theta + c2);
                Profile[k] = Math.Max(Target[k] + 0.04f, noise);
            }

            for (int i = 0; i < IndexCount; i++)
            {
                Rough[i] = 1f;
            }

            Index = _random.Next(IndexCount);
            Tool = 0;
            Grinding = false;
            CutLimit = (_rules.CutBaseTime + _rules.CutTimePerFacet * count) * _difficulty.CutTimeMul;
            CutLeft = CutLimit;
            UpdateMatch();
            Phase = PhaseKind.Cut;
            PhaseClock = 0f;
            Raise(MiniEventKind.PhaseChanged, 0f, 1);
        }

        // 지금 숫돌 쪽을 향한 보석 방향(도, 보석 기준)
        public int FacingDeg => Mod360(90 - RotDeg);
        public float FacingExtent => Support(FacingDeg, Profile);
        public float FacingTarget => Support(FacingDeg, Target);

        // 이 방향으로 더 갈아야 하는 양(음수면 이미 너무 깎았다)
        public float Remaining => FacingExtent - FacingTarget;

        private void UpdateMatch()
        {
            float error = 0f;
            float total = 0f;
            for (int k = 0; k < 360; k++)
            {
                float diff = Profile[k] - Target[k];
                // 너무 깎은 쪽을 두 배로 친다.
                error += diff > 0f ? diff : -2f * diff;
                total += Target[k];
            }

            Match = Clamp01(1f - (error / total) / _difficulty.CutMatchSpan);
        }

        private void TickCut(float deltaTime, MiniInput input)
        {
            CutLeft -= deltaTime;
            if (input.ToolNumber == 1 || input.ToolNumber == 2)
            {
                Tool = input.ToolNumber - 1;
            }

            if (input.Arrow != 0 && !Grinding)
            {
                Index = (Index + input.Arrow + IndexCount) % IndexCount;
                Raise(MiniEventKind.Tick, 1f, Index);
            }

            int phi = FacingDeg;
            if (input.ActionHeld)
            {
                if (!Grinding)
                {
                    Grinding = true;
                    Plane = Support(phi, Profile);
                }

                float rate = Tool == 0 ? _rules.CoarseRate : _rules.FineRate;
                Plane = Math.Max(0.3f, Plane - rate * deltaTime);
                for (int k = 0; k < 360; k++)
                {
                    float c = CosTable[Mod360(k - phi)];
                    if (c <= 0.001f)
                    {
                        continue;
                    }

                    float limit = Plane / c;
                    if (Profile[k] > limit)
                    {
                        Profile[k] = limit;
                    }
                }

                int bucket = ((phi / IndexDeg) % IndexCount + IndexCount) % IndexCount;
                if (Tool == 0)
                {
                    Rough[bucket] = 1f;
                }
                else
                {
                    Rough[bucket] = Math.Max(0f, Rough[bucket] - _rules.PolishSmoothRate * deltaTime);
                }

                UpdateMatch();
            }
            else if (Grinding)
            {
                Grinding = false;
                float remaining = Remaining;
                if (Math.Abs(remaining) <= Tol)
                {
                    Raise(MiniEventKind.Good, Match, Index);
                }
                else if (remaining < -Tol)
                {
                    Overcuts++;
                    Raise(MiniEventKind.Miss, -remaining, Index);
                }
            }

            if (Match >= 0.995f || CutLeft <= 0f || input.FinishPressed)
            {
                EndCut();
            }
        }

        private void EndCut()
        {
            Grinding = false;
            float roughSum = 0f;
            int count = 0;
            foreach (TFacet facet in TargetFacets)
            {
                roughSum += Rough[((facet.Phi / IndexDeg) % IndexCount + IndexCount) % IndexCount];
                count++;
            }

            Smooth = count == 0 ? 1f : 1f - roughSum / count;
            CutScore = Clamp01(0.85f * Match + 0.15f * Smooth);
            FinishStep(1, CutScore, PhaseKind.Set);
        }

        // ------------------------------------------------------------------ ③ 장착

        private void BeginSet()
        {
            Step = 2;
            SocketIdx = _random.Next(IndexCount);
            // 구멍과 2~6칸 다르게 시작한다.
            int offset = 2 + _random.Next(5);
            if (_random.NextDouble() < 0.5)
            {
                offset = IndexCount - offset;
            }

            GemIdx = (SocketIdx + offset) % IndexCount;
            _baseX = _random.NextDouble() < 0.5 ? -3.8f : 3.8f;
            _baseY = 2.2f;
            GemX = _baseX;
            GemY = _baseY;
            PosTol = _difficulty.SetPosTol;
            SetLimit = _difficulty.SetLimit;
            SetLeft = SetLimit;
            Phase = PhaseKind.Set;
            PhaseClock = 0f;
            Raise(MiniEventKind.PhaseChanged, 0f, 2);
        }

        private void TickSet(float deltaTime, MiniInput input)
        {
            SetLeft -= deltaTime;
            _slipTimer = Math.Max(0f, _slipTimer - deltaTime);

            if (input.Arrow != 0)
            {
                GemIdx = (GemIdx + input.Arrow + IndexCount) % IndexCount;
                Raise(MiniEventKind.Tick, 1f, GemIdx);
            }

            bool onBoard = input.PointerX > -50f;
            if (input.PointerPressed && onBoard && !Dragging)
            {
                float dx = input.PointerX - GemX;
                float dy = input.PointerY - GemY;
                if (dx * dx + dy * dy <= (GemR * 1.1f) * (GemR * 1.1f))
                {
                    Dragging = true;
                    _grabDx = _baseX - input.PointerX;
                    _grabDy = _baseY - input.PointerY;
                    Raise(MiniEventKind.Spawn, 0f, 0);
                }
            }

            if (!input.PointerHeld)
            {
                Dragging = false;
            }

            if (Dragging && onBoard)
            {
                _baseX = Math.Max(-7f, Math.Min(7f, input.PointerX + _grabDx));
                _baseY = Math.Max(-4.2f, Math.Min(4.2f, input.PointerY + _grabDy));
            }

            GemX = _baseX;
            GemY = _baseY;

            PosErr = (float)Math.Sqrt(GemX * GemX + GemY * GemY);
            int diff = ((GemIdx - SocketIdx) % IndexCount + IndexCount) % IndexCount;
            IdxDiff = diff > IndexCount / 2 ? diff - IndexCount : diff;
            bool was = Aligned;
            Aligned = PosErr <= PosTol && IdxDiff == 0;
            Near = PosErr <= PosTol * 2f && Math.Abs(IdxDiff) <= 1;
            if (Aligned && !was)
            {
                Raise(MiniEventKind.Good, 1f, 0);
            }

            Seating = input.ActionHeld;
            if (Seating && Aligned)
            {
                SeatProgress = Math.Min(1f, SeatProgress + deltaTime / _rules.SeatTime);
                _seatSum += deltaTime;
                _seatPosSum += PosErr * deltaTime;
            }
            else
            {
                SeatProgress = Math.Max(0f, SeatProgress - (Seating ? 1.5f : 0.8f) * deltaTime);
                if (Seating && !Near)
                {
                    Damage = Math.Min(1f, Damage + 0.25f * deltaTime);
                    if (_slipTimer <= 0f)
                    {
                        Slips++;
                        _slipTimer = 1.0f;
                        Raise(MiniEventKind.Warn, Damage, 0);
                    }
                }
            }

            if (SeatProgress >= 1f)
            {
                EndSet(true);
            }
            else if (SetLeft <= 0f)
            {
                SetLeft = 0f;
                EndSet(false);
            }
            else if (input.FinishPressed)
            {
                EndSet(false);
            }
        }

        private void EndSet(bool secure)
        {
            Dragging = false;
            Seating = false;
            if (secure)
            {
                float posError = _seatSum > 0f ? _seatPosSum / _seatSum / PosTol : 1f;
                float fit = 1f - 0.5f * Clamp01(posError);
                float time = 0.85f + 0.15f * Clamp01(SetLeft / SetLimit);
                SetScore = Clamp01((0.6f + 0.4f * fit) * time * (1f - 0.6f * Clamp01(Damage)));
                Raise(MiniEventKind.Perfect, SetScore, 2);
            }
            else
            {
                SetScore = Clamp01(0.25f * SeatProgress * (1f - 0.6f * Clamp01(Damage)));
            }

            FinishStep(2, SetScore, PhaseKind.Polish);
        }

        // ------------------------------------------------------------------ ④ 광내기

        // 보석 위 점 (px, py)(칸, 보석 가운데 기준)가 어느 면 위인가. 보석 밖이면 -1이다.
        public int FacetAt(float px, float py)
        {
            if (TargetFacets.Count == 0)
            {
                return -1;
            }

            float deg = (float)(Math.Atan2(py, px) * 180.0 / Math.PI) - PolishRot;
            int k = Mod360((int)Math.Round(deg));
            float rho = (float)Math.Sqrt(px * px + py * py) / GemR;
            if (rho > Profile[k] + 0.04f)
            {
                return -1;
            }

            int best = -1;
            float bestDistance = 999f;
            for (int i = 0; i < TargetFacets.Count; i++)
            {
                float distance = Math.Abs(Wrap(k - TargetFacets[i].Phi));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }

        public float FacetRough(int i)
        {
            return Rough[((TargetFacets[i].Phi / IndexDeg) % IndexCount + IndexCount) % IndexCount];
        }

        private void BeginPolish()
        {
            Step = 3;
            int count = TargetFacets.Count;
            Shine = new float[count];
            ShineNeed = new float[count];
            for (int i = 0; i < count; i++)
            {
                // 거칠게 깎은 면일수록 오래 닦는다.
                ShineNeed[i] = (1.8f + 3f * FacetRough(i)) * _difficulty.PolishNeedMul;
            }

            PolishLimit = _difficulty.PolishBaseTime + _difficulty.PolishTimePerFacet * count;
            PolishLeft = PolishLimit;
            _haveLast = false;
            Phase = PhaseKind.Polish;
            PhaseClock = 0f;
            Raise(MiniEventKind.PhaseChanged, 0f, 3);
        }

        private void TickPolish(float deltaTime, MiniInput input)
        {
            PolishLeft -= deltaTime;
            bool on = input.PointerX > -50f;
            float x = input.PointerX;

            Scrubbing = on && input.PointerHeld;
            float moved = 0f;
            if (on && _haveLast)
            {
                float dx = x - _lastX;
                float dy = input.PointerY - _lastY;
                moved = (float)Math.Sqrt(dx * dx + dy * dy);
            }

            if (on)
            {
                _lastX = x;
                _lastY = input.PointerY;
                _haveLast = true;
            }
            else
            {
                _haveLast = false;
            }

            moved = Math.Min(moved, 0.6f);
            Hover = on ? FacetAt(x, input.PointerY) : -1;
            if (Scrubbing && Hover >= 0 && moved > 0f)
            {
                float before = Shine[Hover];
                Shine[Hover] = Math.Min(1f, Shine[Hover] + moved / ShineNeed[Hover]);
                if (before < 0.999f && Shine[Hover] >= 0.999f)
                {
                    Raise(MiniEventKind.Good, 1f, Hover);
                }
            }

            bool all = true;
            foreach (float value in Shine)
            {
                if (value < 0.999f)
                {
                    all = false;
                }
            }

            if (all || PolishLeft <= 0f || input.FinishPressed)
            {
                EndPolish();
            }
        }

        private void EndPolish()
        {
            Scrubbing = false;
            PolishScore = Clamp01(AvgShine);
            Accuracy = Clamp01(_rules.ChainWeight * ChainScore + _rules.CutWeight * CutScore + _rules.SetWeight * SetScore + _rules.PolishWeight * PolishScore);
            Step = 3;
            Phase = PhaseKind.Done;
            _endTimer = 0f;
            PhaseClock = 0f;
            Raise(MiniEventKind.Complete, Accuracy, PolishScore >= 0.99f ? 1 : 0);
        }
    }
}
