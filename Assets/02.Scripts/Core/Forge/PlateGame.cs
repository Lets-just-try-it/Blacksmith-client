using System;
using System.Collections.Generic;

namespace Blacksmith.Core.Forge
{
    // 판 성형: 정제한 쇠판에서 방어구 조각을 모양대로 오려 낸다.
    //  ① 만들 방어구(투구, 갑옷)를 고른다. 고르지 않으면 일정 시간 뒤 첫 번째로 시작한다.
    //  ② 조각마다 쇠판에 밑그림(외곽선)이 그려진다. 녹색 점(지금까지 오린 끝)에서 마우스를 누르고 외곽선을 따라 오린다.
    //     외곽선 둘레의 허용 띠 안이면 잘리고, 벗어나면 그 자리에서 멈춘다(돌아오면 이어진다). 마우스를 놓으면 잠깐 멈출 뿐이고
    //     녹색 점을 다시 눌러 이어 가면 된다. 띠가 좁아지는 불순물 구간이 있다.
    //  ③ 선에 가깝게 오릴수록 깔끔한 조각이 되고, 점수는 조각 모양의 정확도다. 행동 키로 그만 오릴 수 있다.
    // 포인터 좌표는 작업판 위 좌표(칸)다. 판 밖이면 x가 -50보다 작다. 무작위(불순물 위치)는 시드로 정한다.
    public sealed class PlateGame : MiniGame
    {
        public enum PhaseKind { Cut, Done }

        public sealed class Part
        {
            public string Name;
            // 외곽선 표본(간격 Step, 마지막 점은 처음 점과 같다)
            public float[] X;
            public float[] Y;
            // 표본마다 허용 폭
            public float[] Tol;
            // 지나간 표본의 오차(허용 폭에 대한 비율 0~1, 아직 안 지났으면 1)
            public float[] Err;
            // 불순물 구간인가
            public bool[] Narrow;
            // 어디까지 오렸는가(표본 번호)
            public int Head;
            public float Perimeter;
            public bool Done;
            public float Score;

            public int N => X.Length;
            public float Progress => N <= 1 ? 0f : (float)Head / (N - 1);
        }

        // 작업판 크기(칸)와 외곽선 표본 간격
        public const float Width = 16f;
        public const float Height = 10f;
        public const float Step = 0.1f;

        private readonly PlateRules _rules;
        private readonly PlateDifficulty _difficulty;
        private readonly float _priorAccuracy;
        private readonly Random _random;
        private float _endTimer;
        private float _pauseTimer;

        public PhaseKind Phase { get; private set; } = PhaseKind.Cut;
        public int Kind { get; private set; }
        public List<Part> Parts { get; } = new List<Part>();
        public int Current { get; private set; }
        // 지금 오리는 중(마우스를 누른 채 따라가고 있다)
        public bool Cutting { get; private set; }
        // 띠를 벗어났다.
        public bool OffBand { get; private set; }
        // 조각 하나를 마치고 다음 조각을 꺼내는 중
        public bool Pausing { get; private set; }
        // 띠를 벗어난 횟수
        public int Slips { get; private set; }
        public float TimeLeft { get; private set; }
        public float Limit { get; private set; }
        public float Clock { get; private set; }
        public float BandWidth { get; private set; }
        public float PointerX { get; private set; } = -100f;
        public float PointerY { get; private set; }
        public float SuccessScore => _rules.SuccessScore;

        public Part CurrentPart => Current < Parts.Count ? Parts[Current] : null;

        public int DoneCount
        {
            get
            {
                int count = 0;
                foreach (Part part in Parts)
                {
                    if (part.Done)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        // kind: 만들 방어구 종류(ArmorKind 번호). priorAccuracy: 앞 단계(정제) 평균 점수(0~1). 낮으면 띠가 좁아진다.
        public PlateGame(PlateRules rules, int seed, int difficulty, float priorAccuracy, int kind)
        {
            _rules = rules;
            int index = Math.Max(0, Math.Min(rules.Difficulties.Length - 1, difficulty));
            _difficulty = rules.Difficulties[index];
            _priorAccuracy = Clamp01(priorAccuracy);
            _random = new Random(seed);
            Build(Math.Max(0, Math.Min(ArmorKinds.Count - 1, kind)));
            Raise(MiniEventKind.Spawn, 0f, 0);
        }

        // 고르기 전에도 모양을 보여 줄 수 있게 조각 원본을 만든다. 오린 상태는 없다.
        public List<Part> PreviewParts(int kind)
        {
            return CreateParts(kind);
        }

        public override void Tick(float deltaTime, MiniInput input)
        {
            if (Finished)
            {
                return;
            }

            Clock += deltaTime;
            switch (Phase)
            {
                case PhaseKind.Cut:
                    TickCut(deltaTime, input);
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

        private void Build(int kind)
        {
            Kind = kind;
            BandWidth = _difficulty.BandWidth * (_rules.PriorMin + _rules.PriorSpan * _priorAccuracy);
            Parts.Clear();
            Parts.AddRange(CreateParts(kind));

            float perimeter = 0f;
            foreach (Part part in Parts)
            {
                perimeter += part.Perimeter;
                for (int i = 0; i < part.N; i++)
                {
                    part.Tol[i] = BandWidth;
                    part.Err[i] = 1f;
                }

                PlaceNarrows(part);
            }

            Limit = _difficulty.BaseTime + perimeter / _difficulty.PerimeterPerSecond;
            TimeLeft = Limit;
            Current = 0;
            Cutting = false;
            OffBand = false;
            Pausing = false;
            Slips = 0;
        }

        // 불순물 구간: 띠가 좁아진다. 처음과 끝 가까이에는 만들지 않고, 서로 겹치지 않게 한다.
        private void PlaceNarrows(Part part)
        {
            int placed = 0;
            for (int attempt = 0; attempt < 40 && placed < _difficulty.NarrowCount; attempt++)
            {
                int low = 30;
                int high = part.N - 1 - 30 - _rules.NarrowLen;
                if (high <= low)
                {
                    break;
                }

                int start = _random.Next(low, high);
                if (OverlapsNarrow(part, start))
                {
                    continue;
                }

                for (int i = start; i < start + _rules.NarrowLen; i++)
                {
                    part.Narrow[i] = true;
                    part.Tol[i] = BandWidth * _rules.NarrowMul;
                }

                placed++;
            }
        }

        private bool OverlapsNarrow(Part part, int start)
        {
            int from = Math.Max(0, start - 20);
            int to = Math.Min(part.N, start + _rules.NarrowLen + 20);
            for (int i = from; i < to; i++)
            {
                if (part.Narrow[i])
                {
                    return true;
                }
            }

            return false;
        }

        private float DistanceTo(Part part, int index)
        {
            float dx = PointerX - part.X[index];
            float dy = PointerY - part.Y[index];
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private void TickCut(float deltaTime, MiniInput input)
        {
            bool onBoard = input.PointerX > -50f;
            if (onBoard)
            {
                PointerX = input.PointerX;
                PointerY = input.PointerY;
            }
            else
            {
                PointerX = -100f;
            }

            if (Pausing)
            {
                TickPause(deltaTime);
                return;
            }

            TimeLeft -= deltaTime;
            Part part = Parts[Current];
            if (!input.PointerHeld)
            {
                Cutting = false;
                OffBand = false;
            }

            TryGrip(part, input, onBoard);
            if (Cutting)
            {
                Follow(part, onBoard);
            }

            if (Phase == PhaseKind.Cut && !Pausing)
            {
                if (input.ActionPressed)
                {
                    EndCut(false);
                }
                else if (TimeLeft <= 0f)
                {
                    TimeLeft = 0f;
                    EndCut(false);
                }
            }
        }

        private void TickPause(float deltaTime)
        {
            _pauseTimer -= deltaTime;
            if (_pauseTimer > 0f)
            {
                return;
            }

            Pausing = false;
            Current++;
            if (Current >= Parts.Count)
            {
                Current = Parts.Count - 1;
                EndCut(true);
            }
            else
            {
                Raise(MiniEventKind.Spawn, 0f, Current);
            }
        }

        // 녹색 점(오린 끝)을 눌러 시작하거나 이어 간다.
        private void TryGrip(Part part, MiniInput input, bool onBoard)
        {
            if (Cutting || !input.PointerPressed || !onBoard)
            {
                return;
            }

            if (DistanceTo(part, part.Head) > part.Tol[part.Head] * _rules.StartReachScale + _rules.StartReachPad)
            {
                return;
            }

            Cutting = true;
            OffBand = false;
            if (part.Head == 0)
            {
                part.Err[0] = _rules.StartErr;
            }

            Raise(MiniEventKind.Grip, 1f, Current);
        }

        // 앞쪽 범위에서 가장 가까운 표본이 띠 안이면 거기까지 오려 나가고, 아니면 멈춘다.
        private void Follow(Part part, bool onBoard)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            if (onBoard)
            {
                int last = Math.Min(part.N - 1, part.Head + _rules.Ahead);
                for (int i = part.Head; i <= last; i++)
                {
                    float distance = DistanceTo(part, i);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = i;
                    }
                }
            }

            if (best >= 0 && bestDistance <= part.Tol[best])
            {
                float error = Math.Min(1f, bestDistance / part.Tol[best]);
                for (int k = part.Head + 1; k <= best; k++)
                {
                    part.Err[k] = error;
                }

                part.Head = best;
                OffBand = false;
                if (part.Head >= part.N - 1)
                {
                    FinishPart(part);
                }
            }
            else if (!OffBand)
            {
                OffBand = true;
                Slips++;
                Raise(MiniEventKind.Miss, 1f, Current);
            }
        }

        // 지금까지 오린 만큼의 맵시. 선에 가깝게 오릴수록 크고, 다 오려야 1에 가깝다.
        private float FitOf(Part part)
        {
            float sum = 0f;
            for (int i = 0; i <= part.Head && i < part.N; i++)
            {
                sum += 1f - _rules.FitLoss * Math.Min(1f, part.Err[i]);
            }

            return sum / part.N;
        }

        private void FinishPart(Part part)
        {
            part.Done = true;
            part.Score = Clamp01(FitOf(part));
            Cutting = false;
            OffBand = false;
            Pausing = true;
            _pauseTimer = _rules.PartPause;
            Raise(MiniEventKind.Hit, part.Score, Current);
        }

        private void EndCut(bool complete)
        {
            Cutting = false;
            OffBand = false;
            Pausing = false;

            float sum = 0f;
            float weight = 0f;
            foreach (Part part in Parts)
            {
                if (!part.Done)
                {
                    part.Score = Clamp01(FitOf(part));
                }

                sum += part.Score * part.Perimeter;
                weight += part.Perimeter;
            }

            float average = weight <= 0f ? 0f : sum / weight;
            float bonus = complete ? _rules.TimeBonus * Clamp01(TimeLeft / Limit * 1.6f) : 0f;
            Accuracy = Clamp01(average - Math.Min(_rules.MaxSlipPenalty, _rules.SlipPenalty * Slips) + bonus);
            Phase = PhaseKind.Done;
            _endTimer = 0f;
            Raise(MiniEventKind.Complete, Accuracy, complete ? 1 : 0);
        }

        // ------------------------------------------------------------------ 조각 모양

        // pts: 시계 방향 x,y 쌍. bulge: 변마다 바깥쪽으로 휘는 정도(음수면 안쪽, 0이면 직선).
        private static Part Shape(string name, float[] pts, float[] bulge)
        {
            int count = pts.Length / 2;
            var dx = new List<float>();
            var dy = new List<float>();
            for (int i = 0; i < count; i++)
            {
                AddEdge(pts, bulge, i, count, dx, dy);
            }

            // 맨 위 점에서 시작한다.
            int top = 0;
            for (int i = 1; i < dx.Count; i++)
            {
                if (dy[i] < dy[top] - 0.0001f)
                {
                    top = i;
                }
            }

            int m = dx.Count;
            var qx = new float[m + 1];
            var qy = new float[m + 1];
            var cumulative = new float[m + 1];
            for (int i = 0; i < m; i++)
            {
                qx[i] = dx[(top + i) % m];
                qy[i] = dy[(top + i) % m];
            }

            qx[m] = qx[0];
            qy[m] = qy[0];
            for (int i = 1; i <= m; i++)
            {
                float ex = qx[i] - qx[i - 1];
                float ey = qy[i] - qy[i - 1];
                cumulative[i] = cumulative[i - 1] + (float)Math.Sqrt(ex * ex + ey * ey);
            }

            float total = cumulative[m];
            int samples = Math.Max(8, (int)Math.Round(total / Step));
            var rx = new float[samples + 1];
            var ry = new float[samples + 1];
            int segment = 0;
            for (int k = 0; k <= samples; k++)
            {
                float s = total * k / samples;
                while (segment < m - 1 && cumulative[segment + 1] < s)
                {
                    segment++;
                }

                float span = cumulative[segment + 1] - cumulative[segment];
                float t = span < 0.0001f ? 0f : (s - cumulative[segment]) / span;
                rx[k] = qx[segment] + (qx[segment + 1] - qx[segment]) * t;
                ry[k] = qy[segment] + (qy[segment + 1] - qy[segment]) * t;
            }

            CenterOnBoard(rx, ry);
            return new Part
            {
                Name = name,
                X = rx,
                Y = ry,
                Perimeter = total,
                Tol = new float[samples + 1],
                Err = new float[samples + 1],
                Narrow = new bool[samples + 1],
            };
        }

        // 변 하나를 점으로 풀어 담는다. 휘는 변은 가운데 조절점을 둔 곡선으로 나눈다.
        private static void AddEdge(float[] pts, float[] bulge, int i, int count, List<float> dx, List<float> dy)
        {
            float ax = pts[i * 2];
            float ay = pts[i * 2 + 1];
            int j = (i + 1) % count;
            float bx = pts[j * 2];
            float by = pts[j * 2 + 1];
            float b = bulge == null ? 0f : bulge[i];
            float length = (float)Math.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));
            if (b == 0f || length < 0.001f)
            {
                dx.Add(ax);
                dy.Add(ay);
                return;
            }

            float cx = (ax + bx) * 0.5f + (by - ay) / length * b * length;
            float cy = (ay + by) * 0.5f - (bx - ax) / length * b * length;
            int pieces = Math.Max(4, (int)(length * 8f));
            for (int s = 0; s < pieces; s++)
            {
                float t = (float)s / pieces;
                float u = 1f - t;
                dx.Add(u * u * ax + 2f * u * t * cx + t * t * bx);
                dy.Add(u * u * ay + 2f * u * t * cy + t * t * by);
            }
        }

        private static void CenterOnBoard(float[] rx, float[] ry)
        {
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            for (int k = 0; k < rx.Length; k++)
            {
                minX = Math.Min(minX, rx[k]);
                maxX = Math.Max(maxX, rx[k]);
                minY = Math.Min(minY, ry[k]);
                maxY = Math.Max(maxY, ry[k]);
            }

            float offsetX = Width * 0.5f - (minX + maxX) * 0.5f;
            float offsetY = Height * 0.5f - (minY + maxY) * 0.5f;
            for (int k = 0; k < rx.Length; k++)
            {
                rx[k] += offsetX;
                ry[k] += offsetY;
            }
        }

        // 오른쪽 모양: 왼쪽 모양을 좌우로 뒤집는다(시계 방향을 지키려고 순서를 뒤집는다).
        private static Part Mirror(string name, float[] pts, float[] bulge)
        {
            int count = pts.Length / 2;
            var mirrored = new float[pts.Length];
            var mirroredBulge = new float[count];
            for (int k = 0; k < count; k++)
            {
                int source = count - 1 - k;
                mirrored[k * 2] = -pts[source * 2];
                mirrored[k * 2 + 1] = pts[source * 2 + 1];
                mirroredBulge[k] = bulge == null ? 0f : bulge[(count - 2 - k + count) % count];
            }

            return Shape(name, mirrored, mirroredBulge);
        }

        private List<Part> CreateParts(int kind)
        {
            var list = new List<Part>();
            int extra = _difficulty.ExtraParts;
            if (kind == (int)ArmorKind.Helmet)
            {
                AddHelmet(list, extra);
            }
            else
            {
                AddChestplate(list, extra);
            }

            return list;
        }

        private static void AddHelmet(List<Part> list, int extra)
        {
            list.Add(Shape("투구 몸통", new[] { -4f, 0.2f, 4f, 0.2f, 4f, 2.8f, 2.2f, 2.8f, 1.6f, 1.8f, -1.6f, 1.8f, -2.2f, 2.8f, -4f, 2.8f },
                new[] { 0.85f, 0, 0, 0, 0, 0, 0, 0 }));
            list.Add(Shape("얼굴 가리개", new[] { -3.6f, -1.6f, 3.6f, -1.6f, 4.2f, 0.2f, 2.4f, 1.8f, 0.6f, 1.8f, 0f, 0.8f, -0.6f, 1.8f, -2.4f, 1.8f, -4.2f, 0.2f }, null));
            float[] cheek = { -1.6f, -3f, 1.6f, -3f, 2.0f, 0.6f, 0.4f, 3.0f, -1.6f, 3.0f };
            float[] cheekBulge = { 0, 0, 0, 0, 0.18f };
            list.Add(Shape("왼 볼가리개", cheek, cheekBulge));
            list.Add(Mirror("오른 볼가리개", cheek, cheekBulge));
            if (extra >= 1)
            {
                list.Add(Shape("이마 보강판", new[] { -3.4f, 0.4f, 3.4f, 0.4f, 3.0f, 1.8f, -3.0f, 1.8f }, new[] { 0.3f, 0, -0.3f, 0 }));
            }

            if (extra >= 2)
            {
                list.Add(Shape("목 보호대", new[] { -4.4f, -1f, 4.4f, -1f, 3.8f, 1.6f, 1.9f, 0.9f, 0f, 1.6f, -1.9f, 0.9f, -3.8f, 1.6f }, null));
            }
        }

        private static void AddChestplate(List<Part> list, int extra)
        {
            list.Add(Shape("가슴판", new[] { -1.6f, -4f, 0f, -3f, 1.6f, -4f, 3.6f, -3.5f, 3.6f, -1f, 3.0f, 1f, 3.4f, 3.6f, -3.4f, 3.6f, -3.0f, 1f, -3.6f, -1f, -3.6f, -3.5f },
                new[] { 0, 0, 0, 0, -0.25f, 0, 0.1f, 0, -0.25f, 0, 0 }));
            list.Add(Shape("등판", new[] { -1.8f, -4f, 1.8f, -4f, 3.6f, -3.4f, 3.6f, -1f, 3.0f, 1f, 3.4f, 3.6f, -3.4f, 3.6f, -3.0f, 1f, -3.6f, -1f, -3.6f, -3.4f },
                new[] { -0.15f, 0, 0, -0.25f, 0, 0.1f, 0, -0.25f, 0, 0 }));
            float[] shoulder = { -3.4f, 0.6f, 3.4f, 0.6f, 2.8f, 2.6f, -2.8f, 2.6f };
            float[] shoulderBulge = { 0.55f, 0, 0, 0 };
            list.Add(Shape("왼 어깨판", shoulder, shoulderBulge));
            list.Add(Shape("오른 어깨판", shoulder, shoulderBulge));
            list.Add(Shape("허리 치마", new[] { -4.8f, -1.6f, 4.8f, -1.6f, 5.2f, 1.8f, 2.6f, 1.0f, 0f, 2.0f, -2.6f, 1.0f, -5.2f, 1.8f }, null));
            if (extra >= 1)
            {
                list.Add(Shape("옆구리판", new[] { -1.6f, -3f, 1.6f, -3f, 1.6f, 3f, -1.6f, 3f }, new[] { 0, 0.2f, 0, 0.2f }));
            }

            if (extra >= 2)
            {
                list.Add(Shape("목 가리개", new[] { -3.6f, 0.4f, 3.6f, 0.4f, 3.0f, 1.8f, -3.0f, 1.8f }, new[] { 0.55f, 0, -0.45f, 0 }));
            }
        }
    }
}
