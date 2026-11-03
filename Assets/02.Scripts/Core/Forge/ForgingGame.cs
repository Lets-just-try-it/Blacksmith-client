using System;
using System.Collections.Generic;

namespace Blacksmith.Core.Forge
{
    // 단조: 박자에 맞춰 망치질해 칼날을 도안 두께로 만든다. 입력은 행동 키 하나다.
    // 작은 박자는 판정선에 닿을 때 톡 누르고, 큰 박자(강타)는 시작 표시에서 눌러 힘을 모은 뒤 끝 표시에서 뗀다.
    // 망치는 알아서 다음 부위로 옮겨 가고, 부위마다 필요한 박자 수가 다르다. 박자표의 길이가 곧 한 판의 제한시간이다.
    // 마지막에 도안과 칼날 두께가 얼마나 일치하는지가 정확도(0~1)가 된다.
    public sealed class ForgingGame : MiniGame
    {
        public sealed class Beat
        {
            // 맞는 순간. 작은 박자는 누르는 순간, 큰 박자는 떼는 순간이다.
            public float Time;
            // 큰 박자에서 눌러야 하는 순간. 작은 박자는 Time과 같다.
            public float PressTime;
            // 1이면 작은 박자, 2이면 큰 박자(두 번 친 효과)
            public int Unit;
            public int Segment;
            public bool Judged;
            // 판정 점수. 판정 전에는 -1이다.
            public float Quality = -1f;
            // 맞춘 순간이 박자보다 빨랐으면 음수, 늦었으면 양수(초)
            public float Offset;
            // 큰 박자에서 눌러서 힘을 모으기 시작했는가
            public bool Started;
            public float PressQuality;
            public bool Sounded;
            public bool EndSounded;

            public bool IsHeavy => Unit == 2;
        }

        private readonly ForgingRules _rules;
        private readonly ForgingDifficulty _difficulty;
        private int _unjudged;
        private float _endTimer;

        public IReadOnlyList<Beat> Beats => _beats;
        private readonly List<Beat> _beats = new List<Beat>();

        // 부위별 현재 두께, 목표 두께, 진행도, 필요한 박자 수
        public float[] Thickness { get; }
        public float[] Target { get; }
        public float[] Progress { get; }
        public int[] Need { get; }

        public float Clock { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public int Perfects { get; private set; }
        public int Misses { get; private set; }

        // 지금 꾹 누르고 있는 큰 박자(힘을 모으는 중). 없으면 null이다.
        public Beat Holding { get; private set; }

        public float PerfectWindow => _difficulty.PerfectWindow;
        public float GoodWindow => _difficulty.GoodWindow;
        public float WeakWindow => _difficulty.WeakWindow;
        public int BeatsLeft => _unjudged;

        public ForgingGame(ForgingRules rules, int seed, int difficulty)
        {
            _rules = rules;
            int index = Math.Max(0, Math.Min(rules.Difficulties.Length - 1, difficulty));
            _difficulty = rules.Difficulties[index];

            int count = rules.SegmentCount;
            Thickness = new float[count];
            Target = new float[count];
            Progress = new float[count];
            Need = new int[count];

            var random = new Random(seed);
            BuildTargets(random);
            BuildBeats(random);
        }

        // 망치가 지금 향하는 부위(아직 치지 않은 첫 박자의 부위)
        public int Cursor
        {
            get
            {
                for (int i = 0; i < _beats.Count; i++)
                {
                    if (!_beats[i].Judged)
                    {
                        return _beats[i].Segment;
                    }
                }
                return _rules.SegmentCount - 1;
            }
        }

        // 다음에 쳐야 할 박자. 없으면 null이다.
        public Beat NextBeat
        {
            get
            {
                for (int i = 0; i < _beats.Count; i++)
                {
                    if (!_beats[i].Judged)
                    {
                        return _beats[i];
                    }
                }
                return null;
            }
        }

        // 힘 모으기 진행(0~1). 꾹 누르는 중이 아니면 0이다.
        public float HoldProgress => Holding == null ? 0f : Clamp01((Clock - Holding.PressTime) / _rules.HeavyHold);

        public float MeanError
        {
            get
            {
                float sum = 0f;
                for (int i = 0; i < Thickness.Length; i++)
                {
                    sum += Math.Abs(Thickness[i] - Target[i]);
                }
                return sum / Thickness.Length;
            }
        }

        // 도안 일치율(0~1)
        public float Match => Clamp01(1f - MeanError / _rules.MatchTolerance);

        private void BuildTargets(Random random)
        {
            int count = _rules.SegmentCount;
            for (int i = 0; i < count; i++)
            {
                Thickness[i] = 1f;
                float target = i < _rules.HeadSegments
                    ? _rules.HeadThickness
                    : _rules.BodyThickness + (float)(random.NextDouble() - 0.5) * _rules.BodyJitter;
                if (i >= count - _rules.TipSegments)
                {
                    float t = (i - (count - _rules.TipSegments - 1)) / (float)_rules.TipSegments;
                    target = Lerp(target, _rules.TipThickness, t);
                }
                Target[i] = target;
                // 많이 깎아야 하는 부위일수록 박자가 많다.
                Need[i] = Math.Max(_rules.MinNeed, Math.Min(_rules.MaxNeed, (int)Math.Round((1f - target) * _rules.NeedScale)));
            }
        }

        // 부위마다 필요한 양을 작은 박자(1)와 큰 박자(2)로 나눠 박자표를 만든다.
        private void BuildBeats(Random random)
        {
            float time = _rules.LeadIn;
            for (int segment = 0; segment < _rules.SegmentCount; segment++)
            {
                int remaining = Need[segment];
                while (remaining > 0)
                {
                    bool heavy = remaining >= 2 && random.NextDouble() < _difficulty.HeavyChance;
                    if (heavy)
                    {
                        _beats.Add(new Beat { PressTime = time, Time = time + _rules.HeavyHold, Unit = 2, Segment = segment });
                        remaining -= 2;
                        time += _rules.HeavyHold + _difficulty.BeatSpacing;
                    }
                    else
                    {
                        _beats.Add(new Beat { PressTime = time, Time = time, Unit = 1, Segment = segment });
                        remaining -= 1;
                        time += _difficulty.BeatSpacing;
                    }

                    if (remaining <= 0)
                    {
                        time += _rules.SegmentGap;
                    }
                }
            }
            _unjudged = _beats.Count;
        }

        public override void Tick(float deltaTime, MiniInput input)
        {
            if (Finished)
            {
                return;
            }

            Clock += deltaTime;
            RaiseBeatSounds();

            if (input.ActionPressed)
            {
                HandlePress();
            }
            if (Holding != null && (input.ActionReleased || (!input.ActionHeld && !input.ActionPressed)))
            {
                HandleRelease();
            }

            JudgePassedBeats();

            if (_unjudged == 0)
            {
                _endTimer += deltaTime;
                if (_endTimer >= _rules.EndDelay)
                {
                    Finish();
                }
            }
        }

        // 박자 소리(메트로놈): 큰 박자는 시작과 끝에서 한 번씩 낸다.
        private void RaiseBeatSounds()
        {
            for (int i = 0; i < _beats.Count; i++)
            {
                Beat beat = _beats[i];
                if (Clock < beat.PressTime)
                {
                    break;
                }

                if (!beat.Sounded)
                {
                    beat.Sounded = true;
                    Raise(MiniEventKind.Beat, beat.Unit, beat.Segment);
                }
                if (beat.IsHeavy && !beat.EndSounded && Clock >= beat.Time)
                {
                    beat.EndSounded = true;
                    Raise(MiniEventKind.Beat, 3f, beat.Segment);
                }
            }
        }

        // 지나가 버린 박자는 놓친 것이다. 꾹 누른 채 너무 오래 있어도 놓친 것이다.
        private void JudgePassedBeats()
        {
            for (int i = 0; i < _beats.Count; i++)
            {
                Beat beat = _beats[i];
                if (beat.Judged)
                {
                    continue;
                }

                if (Clock > beat.Time + _difficulty.WeakWindow)
                {
                    if (Holding == beat)
                    {
                        Holding = null;
                    }
                    Judge(beat, 0f, 0f);
                }
                else if (!beat.IsHeavy && beat.Time > Clock + _difficulty.WeakWindow)
                {
                    break;
                }
            }
        }

        private float WindowQuality(float absoluteOffset)
        {
            if (absoluteOffset <= _difficulty.PerfectWindow)
            {
                return _rules.PerfectQuality;
            }
            if (absoluteOffset <= _difficulty.GoodWindow)
            {
                return _rules.GoodQuality;
            }
            return absoluteOffset <= _difficulty.WeakWindow ? _rules.WeakQuality : 0f;
        }

        private void HandlePress()
        {
            if (Holding != null)
            {
                return;
            }

            // 후보: 1 작은 박자를 톡 치기, 2 큰 박자의 시작을 누르기, 3 큰 박자를 시작 못 하고 끝에서 톡 치기(약하게만 인정)
            Beat best = null;
            float bestOffset = _difficulty.WeakWindow + 0.0001f;
            int mode = 0;
            for (int i = 0; i < _beats.Count; i++)
            {
                Beat beat = _beats[i];
                if (beat.Judged)
                {
                    continue;
                }
                if (beat.PressTime > Clock + _difficulty.WeakWindow)
                {
                    break;
                }

                if (!beat.IsHeavy)
                {
                    float offset = Math.Abs(Clock - beat.Time);
                    if (offset <= bestOffset)
                    {
                        bestOffset = offset;
                        best = beat;
                        mode = 1;
                    }
                }
                else if (!beat.Started)
                {
                    float offset = Math.Abs(Clock - beat.PressTime);
                    if (offset <= bestOffset)
                    {
                        bestOffset = offset;
                        best = beat;
                        mode = 2;
                        continue;
                    }

                    offset = Math.Abs(Clock - beat.Time);
                    if (offset <= bestOffset)
                    {
                        bestOffset = offset;
                        best = beat;
                        mode = 3;
                    }
                }
            }

            if (best == null)
            {
                // 허공 치기
                Combo = 0;
                Raise(MiniEventKind.Swing, 1f);
                Raise(MiniEventKind.Miss, -1f, Cursor);
                return;
            }

            if (mode == 1)
            {
                Raise(MiniEventKind.Swing, 1f);
                float offset = Clock - best.Time;
                Judge(best, offset, WindowQuality(Math.Abs(offset)));
            }
            else if (mode == 2)
            {
                best.Started = true;
                best.PressQuality = WindowQuality(bestOffset);
                Holding = best;
                Raise(MiniEventKind.Charge, best.Unit, best.Segment, Clock - best.PressTime);
            }
            else
            {
                Raise(MiniEventKind.Swing, 1f);
                float offset = Clock - best.Time;
                Judge(best, offset, Math.Min(_rules.UnchargedHeavyQuality, WindowQuality(Math.Abs(offset))));
            }
        }

        private void HandleRelease()
        {
            Beat beat = Holding;
            Holding = null;
            if (beat == null)
            {
                return;
            }

            float offset = Clock - beat.Time;
            Raise(MiniEventKind.Swing, 1f);
            if (offset < -_difficulty.WeakWindow)
            {
                // 너무 일찍 뗐다.
                Judge(beat, offset, 0f);
                return;
            }

            Judge(beat, offset, Math.Min(WindowQuality(Math.Abs(offset)), beat.PressQuality));
        }

        private void Judge(Beat beat, float offset, float quality)
        {
            beat.Judged = true;
            beat.Quality = quality;
            beat.Offset = offset;
            _unjudged--;

            int segment = beat.Segment;
            Progress[segment] = Math.Min(Need[segment], Progress[segment] + beat.Unit * quality);
            Thickness[segment] = Lerp(1f, Target[segment], Progress[segment] / Need[segment]);

            if (quality >= _rules.GoodQuality)
            {
                Combo++;
                MaxCombo = Math.Max(MaxCombo, Combo);
            }
            else
            {
                Combo = 0;
            }
            if (quality >= _rules.PerfectQuality)
            {
                Perfects++;
            }
            if (quality <= 0f)
            {
                Misses++;
            }

            MiniEventKind kind = quality >= _rules.PerfectQuality ? MiniEventKind.Perfect
                : quality >= _rules.GoodQuality ? MiniEventKind.Good
                : quality > 0f ? MiniEventKind.Hit
                : MiniEventKind.Miss;
            Raise(kind, beat.Unit, segment, offset);

            if (_unjudged == 0)
            {
                Raise(MiniEventKind.Complete, Match);
            }
        }

        private void Finish()
        {
            Finished = true;
            Accuracy = Match;
            Raise(MiniEventKind.Finish, Accuracy);
        }

        private static float Lerp(float from, float to, float t)
        {
            return from + (to - from) * Clamp01(t);
        }
    }
}
