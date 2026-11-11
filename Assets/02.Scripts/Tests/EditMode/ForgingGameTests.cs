using System.Collections.Generic;
using System.Linq;
using Blacksmith.Core.Forge;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class ForgingGameTests
    {
        private const float Dt = 0.01f;
        private static readonly MiniInput Press = new MiniInput { ActionPressed = true, ActionHeld = true };
        private static readonly MiniInput Hold = new MiniInput { ActionHeld = true };
        private static readonly MiniInput Release = new MiniInput { ActionReleased = true };

        // 테스트 전용 수치다. 실제 밸런스는 ScriptableObject 데이터를 따른다.
        private static ForgingRules CreateRules()
        {
            return new ForgingRules
            {
                SegmentCount = 12,
                LeadIn = 2.2f,
                TravelTime = 1.7f,
                EndDelay = 0.9f,
                InputOffset = 0f,
                SegmentGap = 0.2f,
                PerfectQuality = 1f,
                GoodQuality = 0.75f,
                WeakQuality = 0.4f,
                UnchargedHeavyQuality = 0.25f,
                HeadThickness = 0.8f,
                HeadSegments = 2,
                BodyThickness = 0.55f,
                BodyJitter = 0.14f,
                TipThickness = 0.3f,
                TipSegments = 3,
                MinNeed = 2,
                Difficulties = new[]
                {
                    new ForgingDifficulty { PerfectWindow = 0.05f, GoodWindow = 0.10f, WeakWindow = 0.20f, BeatSpacing = 0.55f, HeavyChance = 0.5f, HeavyHoldMin = 0.7f, HeavyHoldMax = 0.7f, NeedScale = 7f, MaxNeed = 5 },
                    new ForgingDifficulty { PerfectWindow = 0.04f, GoodWindow = 0.08f, WeakWindow = 0.15f, BeatSpacing = 0.45f, HeavyChance = 0.6f, HeavyHoldMin = 0.7f, HeavyHoldMax = 0.7f, NeedScale = 7f, MaxNeed = 5 },
                },
            };
        }

        private static void AdvanceTo(ForgingGame game, float time)
        {
            while (game.Clock + Dt < time)
            {
                game.Tick(Dt, default);
            }
        }

        private static List<MiniEvent> Drain(ForgingGame game)
        {
            var events = new List<MiniEvent>();
            while (game.Events.Count > 0)
            {
                events.Add(game.Events.Dequeue());
            }
            return events;
        }

        // 모든 박자를 딱 맞춰 친다.
        private static void PlayPerfectly(ForgingGame game)
        {
            for (int i = 0; i < 20000 && !game.Finished; i++)
            {
                MiniInput input = default;
                ForgingGame.Beat beat = game.NextBeat;
                if (beat != null)
                {
                    float next = game.Clock + Dt;
                    if (game.Holding != null)
                    {
                        input = next >= game.Holding.Time ? Release : Hold;
                    }
                    else if (!beat.IsHeavy && next >= beat.Time)
                    {
                        input = Press;
                    }
                    else if (beat.IsHeavy && !beat.Started && next >= beat.PressTime)
                    {
                        input = Press;
                    }
                }
                game.Tick(Dt, input);
            }
        }

        private static ForgingGame CreateGameWhereFirstBeat(bool heavy)
        {
            for (int seed = 0; seed < 200; seed++)
            {
                var game = new ForgingGame(CreateRules(), seed, 0);
                if (game.Beats[0].IsHeavy == heavy)
                {
                    return game;
                }
            }
            Assert.Fail("조건에 맞는 시드를 찾지 못했다.");
            return null;
        }

        private static ForgingGame CreateGameWhereFirstBeatsAreSmall(int count)
        {
            for (int seed = 0; seed < 500; seed++)
            {
                var game = new ForgingGame(CreateRules(), seed, 0);
                if (game.Beats.Take(count).All(b => !b.IsHeavy))
                {
                    return game;
                }
            }
            Assert.Fail("조건에 맞는 시드를 찾지 못했다.");
            return null;
        }

        [Test]
        public void Construct_SameSeed_GivesSameBlueprintAndBeats()
        {
            var first = new ForgingGame(CreateRules(), 42, 0);
            var second = new ForgingGame(CreateRules(), 42, 0);

            CollectionAssert.AreEqual(first.Target, second.Target);
            CollectionAssert.AreEqual(first.Need, second.Need);
            CollectionAssert.AreEqual(first.Beats.Select(b => b.Time).ToArray(), second.Beats.Select(b => b.Time).ToArray());
            CollectionAssert.AreEqual(first.Beats.Select(b => b.Unit).ToArray(), second.Beats.Select(b => b.Unit).ToArray());
        }

        [Test]
        public void Construct_DifferentSeeds_GiveDifferentBlueprints()
        {
            var first = new ForgingGame(CreateRules(), 1, 0);
            var second = new ForgingGame(CreateRules(), 2, 0);

            CollectionAssert.AreNotEqual(first.Target, second.Target);
        }

        [Test]
        public void Blueprint_HeadIsThickAndTipIsThin_AndNeedStaysInRange()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                var game = new ForgingGame(CreateRules(), seed, 0);

                Assert.AreEqual(0.8f, game.Target[0]);
                Assert.AreEqual(0.8f, game.Target[1]);
                Assert.AreEqual(0.3f, game.Target[11]);
                Assert.Less(game.Target[11], game.Target[5]);
                Assert.IsTrue(game.Need.All(n => n >= 2 && n <= 5));
            }
        }

        [Test]
        public void Beats_StartAfterLeadIn_AndStayInOrder()
        {
            var game = new ForgingGame(CreateRules(), 7, 0);

            Assert.AreEqual(2.2f, game.Beats[0].PressTime, 0.0001f);
            for (int i = 1; i < game.Beats.Count; i++)
            {
                Assert.GreaterOrEqual(game.Beats[i].PressTime, game.Beats[i - 1].Time);
            }
            foreach (var beat in game.Beats.Where(b => b.IsHeavy))
            {
                Assert.AreEqual(beat.PressTime + 0.7f, beat.Time, 0.0001f);
            }
        }

        [Test]
        public void Beats_ProvideExactlyTheNeededAmountPerSegment()
        {
            var game = new ForgingGame(CreateRules(), 11, 0);

            for (int segment = 0; segment < 12; segment++)
            {
                Assert.AreEqual(game.Need[segment], game.Beats.Where(b => b.Segment == segment).Sum(b => b.Unit));
            }
        }

        [Test]
        public void Construct_ClampsDifficultyToValidRange()
        {
            Assert.AreEqual(new ForgingGame(CreateRules(), 5, 1).Beats.Count, new ForgingGame(CreateRules(), 5, 99).Beats.Count);
            Assert.AreEqual(new ForgingGame(CreateRules(), 5, 0).Beats.Count, new ForgingGame(CreateRules(), 5, -3).Beats.Count);
        }

        [Test]
        public void PerfectPlay_MatchesBlueprintAndGivesFullAccuracy()
        {
            var game = new ForgingGame(CreateRules(), 3, 0);

            PlayPerfectly(game);

            Assert.IsTrue(game.Finished);
            Assert.AreEqual(1f, game.Accuracy, 0.001f);
            Assert.AreEqual(0, game.Misses);
            Assert.AreEqual(game.Beats.Count, game.Perfects);
            Assert.AreEqual(game.Beats.Count, game.MaxCombo);
        }

        [Test]
        public void NoInput_MissesEveryBeat_AndGivesZeroAccuracy()
        {
            var game = new ForgingGame(CreateRules(), 3, 0);

            for (int i = 0; i < 20000 && !game.Finished; i++)
            {
                game.Tick(Dt, default);
            }

            Assert.IsTrue(game.Finished);
            Assert.AreEqual(game.Beats.Count, game.Misses);
            Assert.AreEqual(0f, game.Accuracy);
        }

        [Test]
        public void Tap_OnSmallBeatAtItsTime_IsPerfectAndMovesTheSegment()
        {
            var game = CreateGameWhereFirstBeat(false);
            var beat = game.Beats[0];
            AdvanceTo(game, beat.Time);
            Drain(game);

            game.Tick(Dt, Press);

            Assert.IsTrue(beat.Judged);
            Assert.AreEqual(1f, beat.Quality);
            Assert.AreEqual(1f, game.Progress[0]);
            Assert.AreEqual(1f + (game.Target[0] - 1f) / game.Need[0], game.Thickness[0], 0.0001f);
            Assert.IsTrue(Drain(game).Exists(e => e.Kind == MiniEventKind.Perfect && e.Index == 0));
        }

        [Test]
        public void InputOffset_ShiftsJudgementTimeByTheDelay()
        {
            var rules = CreateRules();
            rules.InputOffset = 0.06f;
            ForgingGame game = null;
            for (int seed = 0; seed < 200 && game == null; seed++)
            {
                var candidate = new ForgingGame(rules, seed, 0);
                if (!candidate.Beats[0].IsHeavy)
                {
                    game = candidate;
                }
            }
            var beat = game.Beats[0];
            AdvanceTo(game, beat.Time + 0.06f);

            game.Tick(Dt, Press);

            Assert.AreEqual(1f, beat.Quality);
            Assert.AreEqual(0f, beat.Offset, 0.02f);
            Assert.AreEqual(0f, game.MeanOffset, 0.02f);
        }

        [Test]
        public void InputOffset_DelaysTheMissDeadlineToo()
        {
            var rules = CreateRules();
            rules.InputOffset = 0.06f;
            var game = new ForgingGame(rules, 3, 0);
            var beat = game.Beats.First(b => !b.IsHeavy);
            AdvanceTo(game, beat.Time + 0.2f + 0.04f);

            Assert.IsFalse(beat.Judged);

            AdvanceTo(game, beat.Time + 0.2f + 0.06f + Dt * 2);

            Assert.IsTrue(beat.Judged);
        }

        [Test]
        public void MeanOffset_AveragesTheOffsetsOfStruckBeats_AndIgnoresMisses()
        {
            ForgingGame game = CreateGameWhereFirstBeatsAreSmall(2);
            AdvanceTo(game, game.Beats[0].Time + 0.08f);
            game.Tick(Dt, Press);
            AdvanceTo(game, game.Beats[1].Time + 0.04f);
            game.Tick(Dt, Press);

            Assert.AreEqual(0.06f, game.MeanOffset, 0.02f);
            AdvanceTo(game, game.Beats[2].Time + 0.4f);
            Assert.AreEqual(0.06f, game.MeanOffset, 0.02f);
        }

        [Test]
        public void Tap_FarFromAnyBeat_IsAnAirSwingAndResetsCombo()
        {
            var game = CreateGameWhereFirstBeat(false);
            AdvanceTo(game, game.Beats[0].Time);
            game.Tick(Dt, Press);
            Assert.AreEqual(1, game.Combo);
            Drain(game);

            game.Tick(Dt, Press);

            Assert.AreEqual(0, game.Combo);
            Assert.AreEqual(1, game.Beats.Count(b => b.Judged));
            var events = Drain(game);
            Assert.IsTrue(events.Exists(e => e.Kind == MiniEventKind.Swing));
            Assert.IsTrue(events.Exists(e => e.Kind == MiniEventKind.Miss && e.Value < 0f));
        }

        [Test]
        public void Tap_SlightlyOff_GivesGoodOrWeakQuality()
        {
            var good = CreateGameWhereFirstBeat(false);
            AdvanceTo(good, good.Beats[0].Time + 0.08f);
            good.Tick(Dt, Press);
            var weak = CreateGameWhereFirstBeat(false);
            AdvanceTo(weak, weak.Beats[0].Time - 0.17f);
            weak.Tick(Dt, Press);

            Assert.AreEqual(0.75f, good.Beats[0].Quality);
            Assert.AreEqual(0.4f, weak.Beats[0].Quality);
            Assert.Greater(good.Beats[0].Offset, 0f);
            Assert.Less(weak.Beats[0].Offset, 0f);
        }

        [Test]
        public void MissedBeat_AfterWeakWindow_IsJudgedAsMiss()
        {
            var game = CreateGameWhereFirstBeat(false);
            var beat = game.Beats[0];

            AdvanceTo(game, beat.Time + 0.21f + Dt);

            Assert.IsTrue(beat.Judged);
            Assert.AreEqual(0f, beat.Quality);
            Assert.AreEqual(1, game.Misses);
        }

        [Test]
        public void Heavy_ChargedAndReleasedOnTime_IsPerfect()
        {
            var game = CreateGameWhereFirstBeat(true);
            var beat = game.Beats[0];
            AdvanceTo(game, beat.PressTime);
            game.Tick(Dt, Press);
            Assert.AreSame(beat, game.Holding);
            AdvanceHolding(game, beat.Time);

            game.Tick(Dt, Release);

            Assert.IsNull(game.Holding);
            Assert.AreEqual(1f, beat.Quality);
            Assert.AreEqual(2f, game.Progress[beat.Segment]);
        }

        [Test]
        public void Heavy_ReleasedTooEarly_IsMiss()
        {
            var game = CreateGameWhereFirstBeat(true);
            var beat = game.Beats[0];
            AdvanceTo(game, beat.PressTime);
            game.Tick(Dt, Press);
            AdvanceHolding(game, beat.PressTime + 0.2f);

            game.Tick(Dt, Release);

            Assert.IsTrue(beat.Judged);
            Assert.AreEqual(0f, beat.Quality);
            Assert.AreEqual(0f, game.Progress[beat.Segment]);
        }

        [Test]
        public void Heavy_TappedWithoutCharging_IsCappedAtWeakHit()
        {
            var game = CreateGameWhereFirstBeat(true);
            var beat = game.Beats[0];
            AdvanceTo(game, beat.Time);

            game.Tick(Dt, Press);

            Assert.IsTrue(beat.Judged);
            Assert.AreEqual(0.25f, beat.Quality);
        }

        [Test]
        public void Heavy_HeldForeverPastTheEnd_IsMiss()
        {
            var game = CreateGameWhereFirstBeat(true);
            var beat = game.Beats[0];
            AdvanceTo(game, beat.PressTime);
            game.Tick(Dt, Press);

            AdvanceHolding(game, beat.Time + 0.3f);

            Assert.IsTrue(beat.Judged);
            Assert.IsNull(game.Holding);
            Assert.AreEqual(0f, beat.Quality);
        }

        [Test]
        public void Combo_GrowsOnGoodHits_AndResetsOnWeakOnes()
        {
            ForgingGame game = CreateGameWhereFirstBeatsAreSmall(3);
            for (int i = 0; i < 2; i++)
            {
                AdvanceTo(game, game.NextBeat.Time);
                game.Tick(Dt, Press);
            }
            Assert.AreEqual(2, game.Combo);

            AdvanceTo(game, game.NextBeat.Time - 0.17f);
            game.Tick(Dt, Press);

            Assert.AreEqual(0, game.Combo);
            Assert.AreEqual(2, game.MaxCombo);
        }

        [Test]
        public void Finish_AfterLastBeatAndEndDelay_RaisesFinishEventWithAccuracy()
        {
            var game = new ForgingGame(CreateRules(), 3, 0);
            PlayPerfectly(game);

            var finish = Drain(game).Find(e => e.Kind == MiniEventKind.Finish);

            Assert.AreEqual(MiniEventKind.Finish, finish.Kind);
            Assert.AreEqual(game.Accuracy, finish.Value);
        }

        [Test]
        public void Complete_IsRaisedOnceWhenTheLastBeatIsJudged_BeforeFinish()
        {
            var game = new ForgingGame(CreateRules(), 3, 0);
            PlayPerfectly(game);

            var events = Drain(game);

            Assert.AreEqual(1, events.Count(e => e.Kind == MiniEventKind.Complete));
            Assert.Less(events.FindIndex(e => e.Kind == MiniEventKind.Complete), events.FindIndex(e => e.Kind == MiniEventKind.Finish));
            Assert.AreEqual(1f, events.Find(e => e.Kind == MiniEventKind.Complete).Value, 0.001f);
        }

        [Test]
        public void Tick_AfterFinished_DoesNothing()
        {
            var game = new ForgingGame(CreateRules(), 3, 0);
            PlayPerfectly(game);
            Drain(game);
            float accuracy = game.Accuracy;

            game.Tick(1f, Press);

            Assert.AreEqual(accuracy, game.Accuracy);
            Assert.AreEqual(0, game.Events.Count);
        }

        [Test]
        public void HigherDifficulty_HasNarrowerWindows()
        {
            var easy = new ForgingGame(CreateRules(), 1, 0);
            var hard = new ForgingGame(CreateRules(), 1, 1);

            Assert.Less(hard.PerfectWindow, easy.PerfectWindow);
            Assert.Less(hard.WeakWindow, easy.WeakWindow);
        }

        [Test]
        public void HeavyHold_IsDrawnFromDifficultyRange_AndSameSeedRepeats()
        {
            ForgingRules rules = CreateRules();
            rules.Difficulties[1].HeavyHoldMin = 0.4f;
            rules.Difficulties[1].HeavyHoldMax = 1.7f;
            rules.Difficulties[1].SpacingJitter = 0.25f;
            rules.Difficulties[1].HeavyChance = 1f;

            var first = new ForgingGame(rules, 7, 1);
            var second = new ForgingGame(rules, 7, 1);

            var holds = first.Beats.Where(beat => beat.IsHeavy).Select(beat => beat.Hold).ToList();
            Assert.IsNotEmpty(holds);
            Assert.IsTrue(holds.All(hold => hold >= 0.4f - 0.001f && hold <= 1.7f + 0.001f));
            Assert.Greater(holds.Distinct().Count(), 1);
            CollectionAssert.AreEqual(first.Beats.Select(beat => beat.Time).ToList(), second.Beats.Select(beat => beat.Time).ToList());
        }

        [Test]
        public void LiveAccuracy_StartsAtFull_AndDropsOnlyByMissedAmount()
        {
            var game = new ForgingGame(CreateRules(), 3, 0);
            Assert.AreEqual(1f, game.LiveAccuracy);

            float total = game.Beats.Sum(beat => (float)beat.Unit);
            AdvanceTo(game, game.Beats[0].Time + 0.5f);

            // 첫 박자를 놓치면 그 박자 크기만큼만 줄어든다.
            Assert.IsTrue(game.Beats[0].Judged);
            Assert.AreEqual(1f - game.Beats[0].Unit / total, game.LiveAccuracy, 0.0001f);
        }

        // 눌러 힘을 모으는 동안 시간을 흘려보낸다.
        private static void AdvanceHolding(ForgingGame game, float time)
        {
            while (game.Clock + Dt < time)
            {
                game.Tick(Dt, Hold);
            }
        }
    }
}
