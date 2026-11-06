using System.Linq;
using Blacksmith.Core.Forge;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class PlateGameTests
    {
        private const float Dt = 0.02f;

        // 테스트 전용 수치다. 실제 값은 PlateRules 데이터를 따른다.
        private static PlateRules CreateRules()
        {
            return new PlateRules
            {
                PartPause = 0.9f,
                EndDelay = 1.2f,
                Ahead = 30,
                NarrowMul = 0.7f,
                NarrowLen = 18,
                StartReachScale = 1.8f,
                StartReachPad = 0.3f,
                StartErr = 0.3f,
                SlipPenalty = 0.02f,
                MaxSlipPenalty = 0.15f,
                TimeBonus = 0.05f,
                FitLoss = 0.6f,
                SuccessScore = 0.6f,
                PriorMin = 0.9f,
                PriorSpan = 0.2f,
                Difficulties = new[]
                {
                    new PlateDifficulty { BandWidth = 0.55f, NarrowCount = 0, ExtraParts = 0, PerimeterPerSecond = 2.2f, BaseTime = 20f },
                    new PlateDifficulty { BandWidth = 0.42f, NarrowCount = 1, ExtraParts = 1, PerimeterPerSecond = 2.0f, BaseTime = 20f },
                    new PlateDifficulty { BandWidth = 0.32f, NarrowCount = 2, ExtraParts = 2, PerimeterPerSecond = 1.8f, BaseTime = 20f },
                },
            };
        }

        private static PlateGame StartCutting(int kind, int seed = 1, int difficulty = 0, float prior = 1f)
        {
            return new PlateGame(CreateRules(), seed, difficulty, prior, kind);
        }

        // 외곽선 위의 점을 정확히 따라 오린다. offset만큼 선에서 떨어져 따라간다.
        private static void TraceAll(PlateGame game, float offset = 0f, int maxTicks = 100000)
        {
            for (int i = 0; i < maxTicks && !game.Finished; i++)
            {
                if (game.Phase != PlateGame.PhaseKind.Cut || game.Pausing)
                {
                    game.Tick(Dt, default);
                    continue;
                }

                PlateGame.Part part = game.CurrentPart;
                int head = part.Head;
                int target = System.Math.Min(part.N - 1, head + 6);
                bool grip = !game.Cutting;
                int at = grip ? head : target;
                game.Tick(Dt, new MiniInput
                {
                    PointerPressed = grip,
                    PointerHeld = true,
                    PointerX = part.X[at] + offset,
                    PointerY = part.Y[at],
                });
            }
        }

        [Test]
        public void NewGame_StartsCuttingTheGivenKind()
        {
            PlateGame game = StartCutting((int)ArmorKind.Chestplate);

            Assert.AreEqual(PlateGame.PhaseKind.Cut, game.Phase);
            Assert.AreEqual((int)ArmorKind.Chestplate, game.Kind);
            Assert.That(game.Events.Any(e => e.Kind == MiniEventKind.Spawn && e.Index == 0));
        }

        [Test]
        public void OutOfRangeKind_IsClamped()
        {
            Assert.AreEqual((int)ArmorKind.Chestplate, StartCutting(9).Kind);
            Assert.AreEqual((int)ArmorKind.Helmet, StartCutting(-3).Kind);
        }

        [TestCase(0, 0, 4)]
        [TestCase(0, 1, 5)]
        [TestCase(0, 2, 6)]
        [TestCase(1, 0, 5)]
        [TestCase(1, 1, 6)]
        [TestCase(1, 2, 7)]
        public void PartCount_GrowsWithDifficulty(int kind, int difficulty, int expected)
        {
            Assert.AreEqual(expected, StartCutting(kind, 1, difficulty).Parts.Count);
        }

        [Test]
        public void Outlines_AreClosedAndInsideTheBoard()
        {
            foreach (int kind in new[] { 0, 1 })
            {
                foreach (PlateGame.Part part in StartCutting(kind, 1, 2).Parts)
                {
                    Assert.AreEqual(part.X[0], part.X[part.N - 1], 0.001f, part.Name);
                    Assert.AreEqual(part.Y[0], part.Y[part.N - 1], 0.001f, part.Name);
                    Assert.IsTrue(part.X.All(x => x >= 0f && x <= PlateGame.Width), part.Name);
                    Assert.IsTrue(part.Y.All(y => y >= 0f && y <= PlateGame.Height), part.Name);
                    Assert.Greater(part.Perimeter, 5f, part.Name);
                }
            }
        }

        [Test]
        public void Narrows_AppearOnlyOnHarderDifficulties()
        {
            Assert.IsFalse(StartCutting(0, 3, 0).Parts.Any(p => p.Narrow.Any(n => n)));
            Assert.IsTrue(StartCutting(0, 3, 1).Parts.All(p => p.Narrow.Any(n => n)));
        }

        [Test]
        public void Narrow_ShrinksTheBand()
        {
            PlateGame game = StartCutting(0, 3, 2);
            PlateGame.Part part = game.Parts[0];
            int narrow = System.Array.IndexOf(part.Narrow, true);
            Assert.GreaterOrEqual(narrow, 0);
            Assert.Less(part.Tol[narrow], game.BandWidth);
        }

        [Test]
        public void SameSeed_SameNarrowPlaces()
        {
            PlateGame a = StartCutting(1, 5, 2);
            PlateGame b = StartCutting(1, 5, 2);
            for (int p = 0; p < a.Parts.Count; p++)
            {
                CollectionAssert.AreEqual(a.Parts[p].Narrow, b.Parts[p].Narrow);
            }
        }

        [Test]
        public void LowerPriorAccuracy_NarrowsTheBand()
        {
            Assert.Less(StartCutting(0, 1, 1, 0f).BandWidth, StartCutting(0, 1, 1, 1f).BandWidth);
        }

        [Test]
        public void Trace_FollowingTheLine_FinishesWithHighAccuracy()
        {
            PlateGame game = StartCutting(0);
            TraceAll(game);

            Assert.IsTrue(game.Finished);
            Assert.AreEqual(game.Parts.Count, game.DoneCount);
            Assert.AreEqual(0, game.Slips);
            Assert.Greater(game.Accuracy, 0.95f);
            Assert.That(game.Events.Any(e => e.Kind == MiniEventKind.Complete));
        }

        [Test]
        public void Trace_StayingNearTheBandEdge_ScoresLower()
        {
            PlateGame precise = StartCutting(1);
            TraceAll(precise);
            PlateGame loose = StartCutting(1);
            TraceAll(loose, 0.4f);

            Assert.Less(loose.Accuracy, precise.Accuracy);
            Assert.IsTrue(loose.Finished);
        }

        [Test]
        public void Leaving_TheBand_StopsAndCountsOneSlip()
        {
            PlateGame game = StartCutting(0);
            PlateGame.Part part = game.CurrentPart;
            game.Tick(Dt, new MiniInput { PointerPressed = true, PointerHeld = true, PointerX = part.X[0], PointerY = part.Y[0] });
            Assert.IsTrue(game.Cutting);

            for (int i = 0; i < 30; i++)
            {
                game.Tick(Dt, new MiniInput { PointerHeld = true, PointerX = part.X[0] + 3f, PointerY = part.Y[0] + 3f });
            }

            Assert.IsTrue(game.OffBand);
            Assert.AreEqual(1, game.Slips);
            Assert.AreEqual(0, part.Head);
        }

        [Test]
        public void Releasing_Pauses_AndPressingTheHeadResumes()
        {
            PlateGame game = StartCutting(0);
            PlateGame.Part part = game.CurrentPart;
            game.Tick(Dt, new MiniInput { PointerPressed = true, PointerHeld = true, PointerX = part.X[0], PointerY = part.Y[0] });
            for (int i = 0; i < 5; i++)
            {
                game.Tick(Dt, new MiniInput { PointerHeld = true, PointerX = part.X[i * 4 + 4], PointerY = part.Y[i * 4 + 4] });
            }

            int head = part.Head;
            Assert.Greater(head, 0);
            game.Tick(Dt, default);
            Assert.IsFalse(game.Cutting);
            Assert.AreEqual(head, part.Head);

            game.Tick(Dt, new MiniInput { PointerPressed = true, PointerHeld = true, PointerX = part.X[head], PointerY = part.Y[head] });
            Assert.IsTrue(game.Cutting);
        }

        [Test]
        public void Pressing_FarFromTheHead_DoesNotGrip()
        {
            PlateGame game = StartCutting(0);
            PlateGame.Part part = game.CurrentPart;
            game.Tick(Dt, new MiniInput { PointerPressed = true, PointerHeld = true, PointerX = part.X[0] + 5f, PointerY = part.Y[0] + 5f });
            Assert.IsFalse(game.Cutting);
        }

        [Test]
        public void ActionKey_EndsEarlyWithPartialScore()
        {
            PlateGame game = StartCutting(0);
            PlateGame.Part part = game.CurrentPart;
            game.Tick(Dt, new MiniInput { PointerPressed = true, PointerHeld = true, PointerX = part.X[0], PointerY = part.Y[0] });
            game.Tick(Dt, new MiniInput { ActionPressed = true });

            Assert.AreEqual(PlateGame.PhaseKind.Done, game.Phase);
            Assert.Less(game.Accuracy, 0.1f);
        }

        [Test]
        public void TimeOut_EndsTheCut()
        {
            PlateGame game = StartCutting(0);
            for (int i = 0; i < 20000 && game.Phase == PlateGame.PhaseKind.Cut; i++)
            {
                game.Tick(Dt, default);
            }

            Assert.AreEqual(PlateGame.PhaseKind.Done, game.Phase);
            Assert.AreEqual(0f, game.TimeLeft);
        }

        [Test]
        public void PreviewParts_MatchTheGameParts()
        {
            PlateGame game = StartCutting(1, 1, 1);
            var preview = game.PreviewParts(1);

            Assert.AreEqual(game.Parts.Count, preview.Count);
            Assert.AreEqual(game.Parts[0].X[10], preview[0].X[10], 0.0001f);
        }

        [Test]
        public void Accuracy_StaysBetweenZeroAndOne()
        {
            for (int seed = 0; seed < 5; seed++)
            {
                PlateGame game = StartCutting(seed % 2, seed, seed % 3);
                TraceAll(game, seed * 0.2f);
                Assert.GreaterOrEqual(game.Accuracy, 0f);
                Assert.LessOrEqual(game.Accuracy, 1f);
            }
        }
    }
}
