using System.Collections.Generic;
using System.Linq;
using Blacksmith.Core.Forge;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class AssembleGameTests
    {
        private const float Dt = 0.01f;

        // 테스트 전용 수치다. 실제 값은 AssembleRules 데이터를 따른다.
        private static AssembleRules CreateRules()
        {
            return new AssembleRules
            {
                IntroTime = 1f,
                SeamEndTime = 1.2f,
                EndDelay = 1f,
                SeamBaseTime = 38f,
                SeamTimePerHole = 8f,
                StripPad = 0.9f,
                GrabPad = 0.5f,
                SeatRadius = 0.95f,
                AlignLoss = 0.5f,
                SeatDelay = 0.15f,
                RingRest = 0.25f,
                RingOvertime = 0.1f,
                HitsNeeded = 2,
                PerfectScore = 1f,
                GoodScore = 0.75f,
                MissPenalty = 0.08f,
                HoleQualityOffset = 0f,
                AlignBase = 0.75f,
                AlignWeight = 0.25f,
                PlateBase = 0.92f,
                PlateWeight = 0.08f,
                SuccessScore = 0.6f,
                Difficulties = new[]
                {
                    new AssembleDifficulty { AlignTolerance = 0.34f, StartOffset = 1.3f, RingDuration = 0.85f, PerfectWindow = 0.07f, GoodWindow = 0.18f, HoleBonus = 0, SideHoles = 2, OptionalSeams = 0 },
                    new AssembleDifficulty { AlignTolerance = 0.26f, StartOffset = 1.7f, RingDuration = 0.7f, PerfectWindow = 0.06f, GoodWindow = 0.15f, HoleBonus = 1, SideHoles = 3, OptionalSeams = 1 },
                    new AssembleDifficulty { AlignTolerance = 0.18f, StartOffset = 2.1f, RingDuration = 0.6f, PerfectWindow = 0.05f, GoodWindow = 0.12f, HoleBonus = 2, SideHoles = 3, OptionalSeams = 2 },
                },
            };
        }

        private static AssembleGame Create(int kind = 0, int seed = 1, int difficulty = 0, float plate = 1f)
        {
            return new AssembleGame(CreateRules(), seed, difficulty, plate, kind);
        }

        private static MiniInput Pointer(float x, float y, bool pressed, bool held)
        {
            return new MiniInput { PointerX = x, PointerY = y, PointerPressed = pressed, PointerHeld = held };
        }

        private static MiniInput NoPointer()
        {
            return new MiniInput { PointerX = -100f };
        }

        private static void WaitForWork(AssembleGame game)
        {
            while (game.Phase != AssembleGame.PhaseKind.Work)
            {
                game.Tick(Dt, NoPointer());
            }
        }

        // 판을 구멍 위로 끌어다 놓는다. 오차가 0이 되는 자리까지 끈다.
        private static void AlignPlate(AssembleGame game, float residual = 0f)
        {
            AssembleGame.Seam seam = game.Active;
            float centerX = (seam.MinX + seam.MaxX) * 0.5f;
            float centerY = (seam.MinY + seam.MaxY) * 0.5f;
            float grabX = centerX;
            float grabY = centerY;
            game.Tick(Dt, Pointer(centerX + game.OffsetX, centerY + game.OffsetY, true, true));
            game.Tick(Dt, Pointer(grabX + residual, grabY, false, true));
            game.Tick(Dt, Pointer(grabX + residual, grabY, false, false));
        }

        // 구멍 하나를 눌러 끼우고, 링이 닿는 순간 두 번 친다. timingShift만큼 일찍(-)/늦게(+) 친다.
        private static void RivetHole(AssembleGame game, int index, float timingShift = 0f)
        {
            AssembleGame.Hole hole = game.Active.Holes[index];
            game.Tick(Dt, Pointer(hole.X, hole.Y, true, true));
            for (int hit = 0; hit < 2; hit++)
            {
                float target = game.RingDuration + timingShift;
                int guard = 0;
                while (game.RingTime < target && guard++ < 1000)
                {
                    game.Tick(Dt, NoPointer());
                }
                game.Tick(Dt, new MiniInput { PointerX = -100f, ActionPressed = true });
            }
        }

        private static void PlayPerfectly(AssembleGame game, float timingShift = 0f)
        {
            int guard = 0;
            while (!game.Finished && guard++ < 200000)
            {
                if (game.Phase == AssembleGame.PhaseKind.Work)
                {
                    if (!game.Locked)
                    {
                        AlignPlate(game);
                    }
                    else if (!game.Hammering)
                    {
                        int next = System.Array.FindIndex(game.Active.Holes, hole => hole.State == 0);
                        if (next < 0)
                        {
                            game.Tick(Dt, NoPointer());
                        }
                        else
                        {
                            RivetHole(game, next, timingShift);
                        }
                    }
                    else
                    {
                        game.Tick(Dt, NoPointer());
                    }
                }
                else
                {
                    game.Tick(Dt, NoPointer());
                }
            }
        }

        private static List<MiniEvent> Drain(AssembleGame game)
        {
            var events = new List<MiniEvent>();
            while (game.Events.Count > 0)
            {
                events.Add(game.Events.Dequeue());
            }
            return events;
        }

        [Test]
        public void SeamCounts_FollowKindAndDifficulty()
        {
            Assert.AreEqual(2, Create(0, 1, 0).Seams.Count);
            Assert.AreEqual(3, Create(0, 1, 1).Seams.Count);
            Assert.AreEqual(4, Create(0, 1, 2).Seams.Count);
            Assert.AreEqual(3, Create(1, 1, 0).Seams.Count);
            Assert.AreEqual(4, Create(1, 1, 1).Seams.Count);
            Assert.AreEqual(5, Create(1, 1, 2).Seams.Count);
        }

        [Test]
        public void HoleCounts_GrowWithDifficulty()
        {
            Assert.Less(Create(1, 1, 0).TotalHoles, Create(1, 1, 1).TotalHoles);
            Assert.Less(Create(1, 1, 1).TotalHoles, Create(1, 1, 2).TotalHoles);
            Assert.Less(Create(0, 1, 0).TotalHoles, Create(0, 1, 2).TotalHoles);
        }

        [Test]
        public void Layout_HasOnePartPerSeamOrMore()
        {
            foreach (int kind in new[] { 0, 1 })
            {
                foreach (int difficulty in new[] { 0, 1, 2 })
                {
                    AssembleGame game = Create(kind, 1, difficulty);
                    Assert.GreaterOrEqual(game.Layout().Length, game.Seams.Count);
                }
            }
        }

        [Test]
        public void StartsWithIntro_ThenBeginsFirstSeam_WithMisplacedPlate()
        {
            AssembleGame game = Create();
            Assert.AreEqual(AssembleGame.PhaseKind.Intro, game.Phase);

            WaitForWork(game);

            Assert.AreEqual(0, game.Current);
            Assert.IsFalse(game.Locked);
            Assert.Greater(game.AlignError, 0f);
            Assert.IsFalse(game.Aligned);
        }

        [Test]
        public void SameSeed_GivesSameStartingOffsets()
        {
            AssembleGame first = Create(1, 7, 1);
            AssembleGame second = Create(1, 7, 1);
            WaitForWork(first);
            WaitForWork(second);

            Assert.AreEqual(first.OffsetX, second.OffsetX);
            Assert.AreEqual(first.OffsetY, second.OffsetY);
        }

        [Test]
        public void ReleasingMisaligned_DoesNotLock_AndCountsMisalignment()
        {
            AssembleGame game = Create();
            WaitForWork(game);
            Drain(game);

            AlignPlate(game, residual: 1.5f);

            Assert.IsFalse(game.Locked);
            Assert.AreEqual(1, game.Misaligned);
            Assert.IsTrue(Drain(game).Any(e => e.Kind == MiniEventKind.Misalign));
        }

        [Test]
        public void ReleasingAligned_LocksPlate_AndScoresByAccuracy()
        {
            AssembleGame exact = Create();
            WaitForWork(exact);
            AlignPlate(exact);
            Assert.IsTrue(exact.Locked);
            Assert.AreEqual(1f, exact.Active.AlignScore, 0.001f);

            AssembleGame rough = Create();
            WaitForWork(rough);
            AlignPlate(rough, residual: rough.AlignTolerance * 0.9f);
            Assert.IsTrue(rough.Locked);
            Assert.Less(rough.Active.AlignScore, 1f);
            Assert.AreEqual(0f, rough.AlignError);
        }

        [Test]
        public void RivetBeforeLocking_DoesNothing()
        {
            AssembleGame game = Create();
            WaitForWork(game);
            AssembleGame.Hole hole = game.Active.Holes[0];

            game.Tick(Dt, Pointer(hole.X, hole.Y, true, true));

            Assert.IsFalse(game.Hammering);
            Assert.AreEqual(0, hole.State);
        }

        [Test]
        public void ClickingFarFromHoles_DoesNotSeatRivet()
        {
            AssembleGame game = Create();
            WaitForWork(game);
            AlignPlate(game);

            game.Tick(Dt, Pointer(40f, 40f, true, true));

            Assert.IsFalse(game.Hammering);
        }

        [Test]
        public void StrikingOnTime_FinishesHoleAfterTwoPerfectHits()
        {
            AssembleGame game = Create();
            WaitForWork(game);
            AlignPlate(game);
            Drain(game);

            RivetHole(game, 0);

            AssembleGame.Hole hole = game.Active.Holes[0];
            Assert.AreEqual(2, hole.State);
            Assert.AreEqual(1f, hole.Quality, 0.001f);
            Assert.IsFalse(game.Hammering);
            List<MiniEvent> events = Drain(game);
            Assert.AreEqual(1, events.Count(e => e.Kind == MiniEventKind.Rivet));
            Assert.AreEqual(2, events.Count(e => e.Kind == MiniEventKind.Perfect));
            Assert.AreEqual(1, events.Count(e => e.Kind == MiniEventKind.Good));
        }

        [Test]
        public void StrikingTooEarly_MissesAndDoesNotAdvance()
        {
            AssembleGame game = Create();
            WaitForWork(game);
            AlignPlate(game);
            AssembleGame.Hole hole = game.Active.Holes[0];
            game.Tick(Dt, Pointer(hole.X, hole.Y, true, true));
            while (game.RingTime < 0.1f)
            {
                game.Tick(Dt, NoPointer());
            }
            Drain(game);

            game.Tick(Dt, new MiniInput { PointerX = -100f, ActionPressed = true });

            Assert.AreEqual(0, hole.Hits);
            Assert.AreEqual(1, hole.Misses);
            Assert.IsTrue(Drain(game).Any(e => e.Kind == MiniEventKind.Miss));
        }

        [Test]
        public void StrikeWithinGoodWindowButOutsidePerfect_GivesLowerQuality()
        {
            AssembleGame game = Create();
            WaitForWork(game);
            AlignPlate(game);

            RivetHole(game, 0, timingShift: 0.12f);

            AssembleGame.Hole hole = game.Active.Holes[0];
            Assert.AreEqual(2, hole.State);
            Assert.AreEqual(0.75f, hole.Quality, 0.02f);
        }

        [Test]
        public void IgnoringRing_CountsMissWhenItRunsOut()
        {
            AssembleGame game = Create();
            WaitForWork(game);
            AlignPlate(game);
            AssembleGame.Hole hole = game.Active.Holes[0];
            game.Tick(Dt, Pointer(hole.X, hole.Y, true, true));

            for (int i = 0; i < 300; i++)
            {
                game.Tick(Dt, NoPointer());
            }

            Assert.GreaterOrEqual(hole.Misses, 1);
            Assert.AreEqual(1, hole.State);
        }

        [Test]
        public void SeamTimeLimit_EndsSeamWithoutInput()
        {
            AssembleGame game = Create();
            WaitForWork(game);
            Drain(game);
            float limit = game.Active.Limit;

            for (float t = 0f; t < limit + 0.1f; t += Dt)
            {
                game.Tick(Dt, NoPointer());
            }

            Assert.AreEqual(AssembleGame.PhaseKind.SeamDone, game.Phase);
            Assert.AreEqual(0f, game.Seams[0].Score);
            Assert.IsTrue(Drain(game).Any(e => e.Kind == MiniEventKind.SeamDone));
        }

        [Test]
        public void NoInput_FinishesWithZeroAccuracy()
        {
            AssembleGame game = Create(1, 1, 2);
            int guard = 0;
            while (!game.Finished && guard++ < 400000)
            {
                game.Tick(Dt, NoPointer());
            }

            Assert.IsTrue(game.Finished);
            Assert.AreEqual(0f, game.Accuracy);
        }

        [Test]
        public void PerfectPlay_GivesFullAccuracy_ForEveryKindAndDifficulty()
        {
            foreach (int kind in new[] { 0, 1 })
            {
                foreach (int difficulty in new[] { 0, 1, 2 })
                {
                    AssembleGame game = Create(kind, 3, difficulty);
                    PlayPerfectly(game);

                    Assert.IsTrue(game.Finished, $"kind {kind} difficulty {difficulty}");
                    Assert.AreEqual(1f, game.Accuracy, 0.001f, $"kind {kind} difficulty {difficulty}");
                    Assert.AreEqual(game.TotalHoles, game.DoneHoles);
                }
            }
        }

        [Test]
        public void LowPlateScore_LowersAccuracySlightly()
        {
            AssembleGame good = Create(0, 3, 0, 1f);
            AssembleGame bad = Create(0, 3, 0, 0f);
            PlayPerfectly(good);
            PlayPerfectly(bad);

            Assert.AreEqual(1f, good.Accuracy, 0.001f);
            Assert.AreEqual(0.92f, bad.Accuracy, 0.001f);
        }

        [Test]
        public void SloppyTiming_GivesLowerAccuracyThanPerfect()
        {
            AssembleGame perfect = Create(0, 3, 0);
            AssembleGame sloppy = Create(0, 3, 0);
            PlayPerfectly(perfect);
            PlayPerfectly(sloppy, timingShift: 0.12f);

            Assert.Less(sloppy.Accuracy, perfect.Accuracy);
            Assert.Greater(sloppy.Accuracy, 0.5f);
        }

        [Test]
        public void SameSeedAndInput_GiveSameAccuracy()
        {
            AssembleGame first = Create(1, 11, 2);
            AssembleGame second = Create(1, 11, 2);
            PlayPerfectly(first, 0.05f);
            PlayPerfectly(second, 0.05f);

            Assert.AreEqual(first.Accuracy, second.Accuracy);
        }

        [Test]
        public void Finish_RaisesCompleteBeforeFinishAfterEndDelay()
        {
            AssembleGame game = Create();
            PlayPerfectly(game);

            List<MiniEvent> events = Drain(game);
            int complete = events.FindIndex(e => e.Kind == MiniEventKind.Complete);
            int finish = events.FindIndex(e => e.Kind == MiniEventKind.Finish);
            Assert.GreaterOrEqual(complete, 0);
            Assert.Greater(finish, complete);
            Assert.AreEqual(game.Accuracy, events[finish].Value);
        }

        [Test]
        public void AfterFinish_InputIsIgnored()
        {
            AssembleGame game = Create();
            PlayPerfectly(game);
            Drain(game);
            float accuracy = game.Accuracy;

            game.Tick(1f, Pointer(0f, 0f, true, true));

            Assert.AreEqual(accuracy, game.Accuracy);
            Assert.AreEqual(0, game.Events.Count);
        }
    }
}
