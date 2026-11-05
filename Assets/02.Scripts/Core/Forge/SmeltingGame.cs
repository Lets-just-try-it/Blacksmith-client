using System;
using System.Collections.Generic;

namespace Blacksmith.Core.Forge
{
    // 정제: 원석을 쪼개고 → 풀무질로 녹이고 → 틀에 붓는다. 세 단계가 차례로 이어진다.
    //  ① 쪼개기: 고리가 줄어들어 균열에 닿는 순간 클릭한다. 보라색 저주 균열은 누르지 않고 넘긴다.
    //            얼마나 정확히 쪼갰는지가 쇳물의 양과 찌꺼기의 양을 정한다.
    //  ② 풀무질: 행동 키를 누르면 지시선이 오르고 떼면 내려간다. 움직이는 불꽃 띠 안에 붙들어 쇳물을 달군다.
    //  ③ 붓기:   도가니를 좌우로 옮겨 틀 위에 맞추고, 쥔 채 올리면 기울어 붓는다.
    //            틀의 눈금까지만 채우고, 흘리지 않고, 바닥의 찌꺼기는 따르지 않는다. 쇳물은 시간이 지나면 식는다.
    // 포인터 좌표: 쪼개기에서는 원석 위 위치(반지름 1, y는 아래쪽이 +), 붓기에서는 X가 도가니의 좌우 위치이고
    // Y가 쥔 채 올린 정도(0~1)다. 화면 쪽에서 단계에 맞게 바꿔 넘긴다.
    public sealed class SmeltingGame : MiniGame
    {
        public enum ActKind { Crack, Bellows, Pour, Done }
        public enum TargetKind { Vein, Decoy }
        public enum TargetState { Pending, Hit, Missed, Avoided, Struck }

        public sealed class Target
        {
            // 맞출 순간(쪼개기 시작 후 초)
            public float Time;
            public float X;
            public float Y;
            public TargetKind Kind;
            public TargetState State;
            // 맞혔을 때: 1 완벽, 0.75 좋음, 0.4 약함
            public float Quality;
            // 빨랐으면 음수, 늦었으면 양수(초)
            public float Offset;
        }

        // 포인터 X가 이 값보다 작으면 마우스가 도가니 높이 면 위를 가리키지 않는 것으로 본다.
        private const float NoPointer = -50f;

        private readonly SmeltingRules _rules;
        private readonly SmeltingDifficulty _difficulty;
        private readonly Random _random;
        private float _settle;
        private float _endTimer;
        private float _actEndTimer = -1f;
        private bool _slagWarned;
        private float _zoneTarget = 0.5f;
        private float _zoneHold;
        private float _spoutBase;
        private float _land;

        public ActKind Act { get; private set; } = ActKind.Crack;
        public float ActClock { get; private set; }
        public List<Target> Targets { get; } = new List<Target>();
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        public float StrayPenaltyTotal { get; private set; }

        // 풀무질
        public float Heat { get; private set; }
        public float Fluidity { get; private set; }
        public bool Molten { get; private set; }
        // 불꽃 띠의 가운데(0~1)
        public float Zone { get; private set; } = 0.5f;
        // 지시선의 가운데(0~1)
        public float Cursor { get; private set; } = 0.15f;
        public float CursorV { get; private set; }
        public bool InZone { get; private set; }
        public float InZoneTime { get; private set; }

        // 붓기
        public float Reserve { get; private set; }
        public float ReserveStart { get; private set; }
        public float Dross { get; private set; }
        public float MetalHeat { get; private set; }
        public float Tilt { get; private set; }
        public float Flow { get; private set; }
        public float Spout { get; private set; }
        public float Fill { get; private set; }
        public float Spilled { get; private set; }
        public float SlagIn { get; private set; }
        public bool Clogged { get; private set; }
        public bool Overflowed { get; private set; }
        // 틀 가운데 위치. 판마다 무작위이고 붓는 동안 고정이다.
        public float MoldX { get; }

        public float CrackScore { get; private set; }
        public float PourQuality { get; private set; }
        public float Purity { get; private set; } = 1f;
        public float FillScore { get; private set; }

        public float HitRadius => _difficulty.HitRadius;
        public float FlameHalfWidth => _difficulty.FlameHalfWidth;
        public float MoldHalfWidth => _difficulty.MoldHalfWidth;
        public float FillLow => _difficulty.FillLow;
        public float FillHigh => _difficulty.FillHigh;
        // 화면이 줄기 세기와 틀 가장자리 판정을 규칙과 같은 값으로 그리도록 열어 둔다.
        public float FlowMax => _rules.FlowMax;
        public float MoldEdge => _rules.MoldEdge;
        public float SpoutStart => _rules.SpoutStart;
        // 균열 고리가 나타나서 닿기까지의 시간
        public float Approach => _rules.Approach;
        public float CursorHalf => _rules.CursorHalf;
        public float BellowsLimit => _rules.BellowsMax;

        // 쇳물이 넘치기 시작하는 기울기. 도가니가 비어 갈수록 더 기울여야 한다.
        public float PourAngle => _rules.TiltDead + _rules.TiltEmptyBonus * (1f - Clamp01(Reserve / Math.Max(0.01f, ReserveStart)));
        public bool LandsInMold => Math.Abs(_land - MoldX) <= MoldHalfWidth;
        public float MissAmount => Math.Max(0f, Math.Abs(_land - MoldX) - MoldHalfWidth);

        public int Judged
        {
            get
            {
                int count = 0;
                foreach (Target target in Targets)
                {
                    if (target.State != TargetState.Pending)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public SmeltingGame(SmeltingRules rules, int seed, int difficulty)
        {
            _rules = rules;
            int index = Math.Max(0, Math.Min(rules.Difficulties.Length - 1, difficulty));
            _difficulty = rules.Difficulties[index];
            Difficulty = index;
            _random = new Random(seed);
            MoldX = rules.MoldMin + (rules.MoldMax - rules.MoldMin) * (float)_random.NextDouble();
            _spoutBase = rules.SpoutStart;
            _land = rules.SpoutStart;
            Heat = rules.HeatStart;
            GenerateCracks();
        }

        public int Difficulty { get; }

        // 쇳물 줄기의 f(0 주둥이, 1 닿는 곳) 지점의 좌우 위치.
        // 약하게 쏟으면 도가니 가까이 떨어지고, 세게 쏟으면 멀리 날아간다.
        public float StreamX(float f)
        {
            return Spout + (_land - Spout) * f;
        }

        // 지금까지 쪼갠 정확도(0~1)
        public float CrackSoFar => CrackAccuracy(true);

        public override void Tick(float deltaTime, MiniInput input)
        {
            if (Finished)
            {
                return;
            }

            ActClock += deltaTime;
            switch (Act)
            {
                case ActKind.Crack:
                    TickCrack(deltaTime, input);
                    break;
                case ActKind.Bellows:
                    TickBellows(deltaTime, input);
                    break;
                case ActKind.Pour:
                    TickPour(deltaTime, input);
                    break;
                case ActKind.Done:
                    _endTimer += deltaTime;
                    if (_endTimer >= _rules.EndDelay)
                    {
                        Finished = true;
                        Raise(MiniEventKind.Finish, Accuracy);
                    }
                    break;
            }
        }

        // ------------------------------------------------------------------ ① 쪼개기

        private void GenerateCracks()
        {
            float time = _rules.CrackLead;
            float px = 0f;
            float py = 0f;
            int decoyCool = _difficulty.DecoyGap;

            for (int i = 0; i < _difficulty.CrackCount; i++)
            {
                var target = new Target { Time = time };
                PlaceCrack(target, i, px, py);
                px = target.X;
                py = target.Y;

                double roll = _random.NextDouble();
                if (i >= _rules.DecoyStartIndex && decoyCool <= 0 && roll < _difficulty.DecoyChance)
                {
                    target.Kind = TargetKind.Decoy;
                    decoyCool = _difficulty.DecoyGap;
                }
                else
                {
                    target.Kind = TargetKind.Vein;
                    decoyCool--;
                }

                Targets.Add(target);

                double pick = _random.NextDouble();
                float gap = pick < 0.50 ? 0.62f : (pick < 0.65 ? 0.50f : (pick < 0.85 ? 0.78f : (pick < 0.95 ? 0.40f : 0.95f)));
                if (target.Kind == TargetKind.Decoy)
                {
                    gap = Math.Max(gap, 0.62f);
                }
                time += gap * _difficulty.GapScale;
            }
        }

        // 앞 균열에서 적당히 떨어지고, 같은 시간대에 보이는 다른 균열과는 겹치지 않는 곳에 놓는다.
        private void PlaceCrack(Target target, int index, float px, float py)
        {
            float x = 0f;
            float y = 0f;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                double angle = _random.NextDouble() * Math.PI * 2.0;
                float distance = index == 0
                    ? _rules.CrackFirstDistance
                    : _rules.CrackMinDistance + (float)_random.NextDouble() * (_rules.CrackMaxDistance - _rules.CrackMinDistance);
                float cx = px + (float)Math.Cos(angle) * distance;
                float cy = py + (float)Math.Sin(angle) * distance;
                float radius = (float)Math.Sqrt(cx * cx + cy * cy);
                if (radius > _rules.CrackRadiusLimit)
                {
                    cx *= _rules.CrackRadiusLimit / radius;
                    cy *= _rules.CrackRadiusLimit / radius;
                }

                x = cx;
                y = cy;
                if (!OverlapsVisibleCrack(target.Time, cx, cy))
                {
                    break;
                }
            }

            target.X = x;
            target.Y = y;
        }

        private bool OverlapsVisibleCrack(float time, float x, float y)
        {
            foreach (Target other in Targets)
            {
                if (Math.Abs(other.Time - time) > _rules.Approach + 0.4f)
                {
                    continue;
                }

                float dx = other.X - x;
                float dy = other.Y - y;
                if (Math.Sqrt(dx * dx + dy * dy) < _rules.CrackMinSeparation)
                {
                    return true;
                }
            }
            return false;
        }

        private float CrackAccuracy(bool onlyJudged)
        {
            float got = 0f;
            float total = 0f;
            foreach (Target target in Targets)
            {
                if (target.Kind == TargetKind.Decoy || (onlyJudged && target.State == TargetState.Pending))
                {
                    continue;
                }

                total += 1f;
                if (target.State == TargetState.Hit)
                {
                    got += target.Quality;
                }
            }
            return total <= 0f ? 1f : Clamp01(got / total - StrayPenaltyTotal);
        }

        private void TickCrack(float deltaTime, MiniInput input)
        {
            if (input.PointerPressed)
            {
                Click(input.PointerX, input.PointerY);
            }

            bool allDone = true;
            for (int i = 0; i < Targets.Count; i++)
            {
                Target target = Targets[i];
                if (target.State != TargetState.Pending)
                {
                    continue;
                }

                if (target.Kind == TargetKind.Decoy)
                {
                    if (ActClock > target.Time + _rules.DecoyWindow)
                    {
                        target.State = TargetState.Avoided;
                        Raise(MiniEventKind.Avoid, 1f, i);
                        continue;
                    }
                }
                else if (ActClock > target.Time + _rules.WeakWindow * _difficulty.WindowScale)
                {
                    target.State = TargetState.Missed;
                    Combo = 0;
                    Raise(MiniEventKind.Miss, 0f, i);
                    continue;
                }
                allDone = false;
            }

            if (!allDone)
            {
                return;
            }

            if (_actEndTimer < 0f)
            {
                CrackScore = CrackAccuracy(false);
                _actEndTimer = 0f;
                // 원석이 갈라진다.
                Raise(MiniEventKind.PhaseChanged, CrackScore, 0);
            }

            _actEndTimer += deltaTime;
            if (_actEndTimer >= _rules.SplitDelay)
            {
                BeginBellows();
            }
        }

        private void Click(float x, float y)
        {
            float weak = _rules.WeakWindow * _difficulty.WindowScale;
            int best = -1;
            float bestAbs = float.MaxValue;
            bool nearVisible = false;
            bool early = false;

            for (int i = 0; i < Targets.Count; i++)
            {
                Target target = Targets[i];
                if (target.State != TargetState.Pending || ActClock < target.Time - _rules.Approach)
                {
                    continue;
                }

                float dx = target.X - x;
                float dy = target.Y - y;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                if (distance > HitRadius * 1.3f)
                {
                    continue;
                }

                nearVisible = true;
                float diff = ActClock - target.Time;
                float late = target.Kind == TargetKind.Decoy ? _rules.DecoyWindow : weak;
                if (diff < -weak)
                {
                    early = true;
                    continue;
                }
                if (diff > late || distance > HitRadius)
                {
                    continue;
                }

                float abs = Math.Abs(diff);
                if (abs < bestAbs)
                {
                    bestAbs = abs;
                    best = i;
                }
            }

            if (best < 0)
            {
                MissClick(early, nearVisible);
                return;
            }

            Strike(best);
        }

        // 너무 일찍 누른 것은 벌점이 없고, 아무 균열 근처도 아닌 곳을 누르면 헛손질이다.
        private void MissClick(bool early, bool nearVisible)
        {
            if (early)
            {
                Raise(MiniEventKind.Early, 1f);
                return;
            }

            if (!nearVisible)
            {
                StrayPenaltyTotal += _rules.StrayPenalty;
                Raise(MiniEventKind.Swing, 1f, -1);
            }
        }

        private void Strike(int index)
        {
            Target hit = Targets[index];
            float offset = ActClock - hit.Time;
            hit.Offset = offset;

            if (hit.Kind == TargetKind.Decoy)
            {
                hit.State = TargetState.Struck;
                StrayPenaltyTotal += _rules.DecoyPenalty;
                Combo = 0;
                Raise(MiniEventKind.Crack, _rules.DecoyPenalty, index, offset);
                return;
            }

            float abs = Math.Abs(offset);
            float quality = abs <= _rules.PerfectWindow * _difficulty.WindowScale ? 1f
                : (abs <= _rules.GoodWindow * _difficulty.WindowScale ? 0.75f : 0.4f);
            hit.Quality = quality;
            hit.State = TargetState.Hit;
            if (quality >= 0.75f)
            {
                Combo++;
                BestCombo = Math.Max(BestCombo, Combo);
            }

            MiniEventKind kind = quality >= 1f ? MiniEventKind.Perfect : (quality >= 0.75f ? MiniEventKind.Good : MiniEventKind.Hit);
            Raise(kind, quality, index, offset);
        }

        // ------------------------------------------------------------------ ② 풀무질

        private void BeginBellows()
        {
            Act = ActKind.Bellows;
            ActClock = 0f;
            _actEndTimer = -1f;
            Raise(MiniEventKind.PhaseChanged, 0f, 1);
        }

        private void NextZoneTarget()
        {
            float half = _difficulty.FlameHalfWidth;
            _zoneTarget = half + (float)_random.NextDouble() * (1f - 2f * half);
            _zoneHold = (_rules.ZoneHoldMin + (float)_random.NextDouble() * _rules.ZoneHoldRange) / _difficulty.ZoneHoldDivisor;
        }

        private void TickBellows(float deltaTime, MiniInput input)
        {
            if (Molten)
            {
                _actEndTimer += deltaTime;
                if (_actEndTimer >= _rules.MoltenHold)
                {
                    BeginPour();
                }
                return;
            }

            if (ActClock <= deltaTime + 0.0001f)
            {
                Zone = 0.5f;
                Cursor = 0.15f;
                CursorV = 0f;
                NextZoneTarget();
            }

            MoveZone(deltaTime);
            MoveCursor(deltaTime, input.ActionHeld || input.PointerHeld);

            bool inZone = Math.Abs(Cursor - Zone) < _difficulty.FlameHalfWidth + _rules.CursorHalf * 0.5f;
            if (inZone != InZone)
            {
                Raise(MiniEventKind.Beat, inZone ? 1f : 0f);
            }
            InZone = inZone;

            if (inZone)
            {
                Heat = Clamp01(Heat + _difficulty.HeatGain * deltaTime);
                InZoneTime += deltaTime;
            }
            else
            {
                Heat = Clamp01(Heat - _difficulty.HeatDrain * deltaTime);
            }

            if (Heat >= 0.995f)
            {
                Heat = 1f;
                Fluidity = Math.Max(_rules.FluidityMin, Clamp01(1f - _rules.FluidityLoss * (ActClock - _rules.FluidityGrace)));
                BecomeMolten(0);
            }
            else if (ActClock >= _rules.BellowsMax)
            {
                // 시간 안에 못 달궜다: 덜 녹은 쇳물로 붓는다.
                Fluidity = _rules.TimeoutFluidityBase + _rules.TimeoutFluidityHeat * Heat;
                BecomeMolten(1);
            }
        }

        private void BecomeMolten(int timedOut)
        {
            Molten = true;
            _actEndTimer = 0f;
            Raise(MiniEventKind.Molten, Fluidity, timedOut);
        }

        // 불꽃 띠는 목표 지점으로 부드럽게 헤엄치다가 가끔 방향을 튼다.
        private void MoveZone(float deltaTime)
        {
            _zoneHold -= deltaTime;
            if (_zoneHold <= 0f)
            {
                NextZoneTarget();
            }

            float delta = _zoneTarget - Zone;
            Zone += Math.Sign(delta) * Math.Min(Math.Abs(delta), _difficulty.ZoneSpeed * deltaTime);
        }

        // 누르면 오르고 떼면 떨어진다. 관성이 있고 위아래 끝에서 튕긴다.
        private void MoveCursor(float deltaTime, bool hold)
        {
            CursorV += (hold ? _rules.RiseAccel : -_rules.FallAccel) * deltaTime;
            CursorV = Math.Max(-_rules.MaxFall, Math.Min(_rules.MaxRise, CursorV));
            Cursor += CursorV * deltaTime;

            float half = _rules.CursorHalf;
            if (Cursor < half)
            {
                Cursor = half;
                if (CursorV < 0f)
                {
                    CursorV = -CursorV * 0.25f;
                }
            }
            if (Cursor > 1f - half)
            {
                Cursor = 1f - half;
                if (CursorV > 0f)
                {
                    CursorV = -CursorV * 0.25f;
                }
            }
        }

        // ------------------------------------------------------------------ ③ 붓기

        private void BeginPour()
        {
            Act = ActKind.Pour;
            ActClock = 0f;
            _actEndTimer = -1f;
            ReserveStart = _rules.ReserveBase + _rules.ReserveCrackBonus * CrackScore;
            Reserve = ReserveStart;
            Dross = _rules.DrossBase + _rules.DrossCrackPenalty * (1f - CrackScore);
            MetalHeat = Fluidity;
            Spout = _rules.SpoutStart;
            _spoutBase = _rules.SpoutStart;
            _land = _rules.SpoutStart;
            Raise(MiniEventKind.PhaseChanged, 0f, 2);
        }

        private void TickPour(float deltaTime, MiniInput input)
        {
            // 도가니는 마우스를 따라 좌우로 움직인다(약간의 관성).
            if (input.PointerX > NoPointer)
            {
                float want = Math.Max(-_rules.SpoutLimit, Math.Min(_rules.SpoutLimit, input.PointerX));
                _spoutBase += (want - _spoutBase) * (1f - (float)Math.Exp(-12f * deltaTime));
            }
            Spout = _spoutBase;

            // 도가니를 쥐고 올린 만큼 기울어진다.
            bool grab = input.PointerHeld && !Clogged && Reserve > 0f;
            float tiltWant = grab ? Clamp01(input.PointerY) : 0f;
            Tilt += (tiltWant - Tilt) * (1f - (float)Math.Exp(-12f * deltaTime));

            MetalHeat = Math.Max(0f, MetalHeat - _rules.HeatLoss * _difficulty.HeatLossScale * deltaTime);
            if (MetalHeat < _rules.ColdLimit && !Clogged && Fill < FillLow && Tilt > 0.01f)
            {
                Clogged = true;
                Raise(MiniEventKind.Solidify, MetalHeat);
            }

            UpdateFlow(deltaTime);
            if (Flow > 0f)
            {
                PourIntoMold(deltaTime);
            }

            if (ShouldFinishPour(deltaTime, input))
            {
                FinishPour();
            }
        }

        private void UpdateFlow(float deltaTime)
        {
            Flow = 0f;
            float angle = PourAngle;
            float excess = Tilt > angle ? (Tilt - angle) / (1f - angle) : 0f;
            _land += (Spout - _rules.Reach * excess - _land) * (1f - (float)Math.Exp(-14f * deltaTime));
            if (Tilt > angle && Reserve > 0f && !Clogged)
            {
                Flow = _rules.FlowMax * _difficulty.FlowScale * (Tilt - angle) / (1f - angle) * (0.35f + 0.65f * MetalHeat);
            }
        }

        private void PourIntoMold(float deltaTime)
        {
            float amount = Math.Min(Reserve, Flow * deltaTime);
            Reserve -= amount;
            // 도가니 맨 아래 Dross만큼은 찌꺼기다. 그 높이 아래로 내려간 만큼이 찌꺼기로 따라 나온다.
            float slagAmount = amount * Clamp01((Dross - Reserve) / Math.Max(0.0001f, amount));

            // 틀에 들어가는 비율: 열린 입구 안이면 전부, 가장자리 밖으로 조금 벗어나면 일부, 더 벗어나면 전부 바닥으로 간다.
            float inFraction = MissAmount <= 0f ? 1f : Clamp01(1f - MissAmount / _rules.MoldEdge);
            if (inFraction > 0f)
            {
                Fill += amount * inFraction;
                if (Fill > FillHigh)
                {
                    // 눈금을 넘으면 틀 가장자리로 넘쳐 흐른다.
                    Spilled += amount * 0.6f;
                    Raise(MiniEventKind.Spill, amount * 0.6f);
                }
                SlagIn += slagAmount * inFraction;
                if (Fill > FillHigh && !Overflowed && Fill >= FillHigh + 0.05f)
                {
                    Overflowed = true;
                    Raise(MiniEventKind.Overflow, Fill - 1f);
                }
            }

            if (inFraction < 1f)
            {
                Spilled += amount * (1f - inFraction);
                Raise(MiniEventKind.Spill, amount * (1f - inFraction));
            }

            if (!_slagWarned && Reserve < Dross + 0.1f)
            {
                _slagWarned = true;
                Raise(MiniEventKind.Warn, 1f);
            }
        }

        private bool ShouldFinishPour(float deltaTime, MiniInput input)
        {
            bool done = false;
            // "다 부었다": 헤매는 사람이 제한시간을 기다리지 않도록 4초 뒤에는 언제든 끝낼 수 있다.
            if (input.ActionPressed && (Fill > 0.2f || ActClock > 4f))
            {
                done = true;
            }

            // 눈금 근처까지 채우고 도가니를 완전히 내려 잠시 두면 끝난다.
            if (Fill >= FillLow && Tilt <= 0.04f)
            {
                _settle += deltaTime;
                if (_settle >= 0.8f)
                {
                    done = true;
                }
            }
            else
            {
                _settle = 0f;
            }

            return done
                || (Reserve <= 0.0001f && Tilt <= 0.1f)
                || (Clogged && Tilt <= 0.1f)
                || Fill >= _rules.OverflowEnd
                || ActClock >= _rules.PourMax;
        }

        // 지금 끝냈을 때의 붓기 점수와 정확도. 마무리 계산과 같은 함수를 쓰므로 화면에 보인 값이 결과와 같다.
        public float PreviewFillScore => ComputeFillScore();
        public float PreviewPurity => Clamp01(1f - SlagIn / _rules.SlagZero);
        public float PreviewPourQuality => ComputePourQuality(ComputeFillScore(), PreviewPurity);
        public float PreviewAccuracy => ComputeAccuracy(PreviewPourQuality);
        // 눈금의 끝. 이 값까지 채우면 넘쳐서 끝난다.
        public float FillMax => _rules.OverflowEnd;

        private float ComputeFillScore()
        {
            float center = (FillLow + FillHigh) * 0.5f;
            float deviation = Math.Max(0f, Math.Abs(Fill - center) - (FillHigh - FillLow) * 0.5f);
            return Clamp01(1f - deviation / _difficulty.FillTolerance);
        }

        private float ComputePourQuality(float fillScore, float purity)
        {
            float waste = Clamp01(1f - Spilled / _rules.SpillZero);
            return fillScore * (0.55f + 0.45f * waste) * (0.6f + 0.4f * purity);
        }

        private float ComputeAccuracy(float pourQuality)
        {
            return Clamp01(_rules.CrackWeight * CrackScore + _rules.FluidityWeight * Fluidity + _rules.PourWeight * pourQuality);
        }

        private void FinishPour()
        {
            FillScore = ComputeFillScore();
            Purity = PreviewPurity;
            PourQuality = ComputePourQuality(FillScore, Purity);

            Accuracy = ComputeAccuracy(PourQuality);
            Act = ActKind.Done;
            ActClock = 0f;
            _endTimer = 0f;
            Raise(MiniEventKind.Complete, Accuracy, 3, PourQuality);
        }
    }
}
