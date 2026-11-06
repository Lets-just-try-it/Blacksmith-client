using System;
using System.Linq;
using Blacksmith.Core.Forge;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class NecklaceGameTests
    {
        private const float Dt = 0.02f;

        // 테스트 전용 수치다. 실제 값은 NecklaceRules 데이터를 따른다.
        private static NecklaceRules CreateRules()
        {
            return new NecklaceRules
            {
                IntroTime = 0.6f,
                StepDoneTime = 0.9f,
                EndTime = 1.2f,
                GemCm = 12f,
                CoarseRate = 0.24f,
                FineRate = 0.08f,
                PolishSmoothRate = 0.7f,
                SeatTime = 0.7f,
                CutBaseTime = 20f,
                CutTimePerFacet = 6f,
                ChainWeight = 0.3f,
                CutWeight = 0.3f,
                SetWeight = 0.15f,
                PolishWeight = 0.25f,
                SuccessScore = 0.6f,
                Difficulties = new[]
                {
                    new NecklaceDifficulty { ChainBaseTime = 10f, ChainTimePerPiece = 4.5f, CutTolCm = 0.08f, CutMatchSpan = 0.14f, CutTimeMul = 1f, FacetCount = 4, FacetSpread = 0.03f, SetPosTol = 0.26f, SetLimit = 30f, PolishBaseTime = 10f, PolishTimePerFacet = 3f, PolishNeedMul = 1f },
                    new NecklaceDifficulty { ChainBaseTime = 9f, ChainTimePerPiece = 4f, CutTolCm = 0.05f, CutMatchSpan = 0.10f, CutTimeMul = 0.95f, FacetCount = 6, FacetSpread = 0.05f, SetPosTol = 0.19f, SetLimit = 30f, PolishBaseTime = 9f, PolishTimePerFacet = 2.8f, PolishNeedMul = 1.25f },
                    new NecklaceDifficulty { ChainBaseTime = 8f, ChainTimePerPiece = 3.5f, CutTolCm = 0.03f, CutMatchSpan = 0.07f, CutTimeMul = 0.9f, FacetCount = 8, FacetSpread = 0.07f, SetPosTol = 0.15f, SetLimit = 28f, PolishBaseTime = 8f, PolishTimePerFacet = 2.6f, PolishNeedMul = 1.5f },
                },
            };
        }

        private static NecklaceGame CreateGame(int seed = 1, int difficulty = 0)
        {
            return new NecklaceGame(CreateRules(), seed, difficulty);
        }

        private static void Run(NecklaceGame game, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt)
            {
                game.Tick(Dt, default);
            }
        }

        private static void TickUntil(NecklaceGame game, NecklaceGame.PhaseKind phase, int maxTicks = 5000)
        {
            for (int i = 0; i < maxTicks && game.Phase != phase; i++)
            {
                game.Tick(Dt, default);
            }
        }

        private static NecklaceGame InChain(int seed = 1, int difficulty = 0)
        {
            NecklaceGame game = CreateGame(seed, difficulty);
            TickUntil(game, NecklaceGame.PhaseKind.Chain);
            return game;
        }

        private static void Click(NecklaceGame game, float code)
        {
            game.Tick(Dt, new MiniInput { PointerPressed = true, PointerX = code, PointerY = 0f });
        }

        // 정답대로 모든 조각을 놓는다. 뒤집힌 조각은 뒤집고 놓는다.
        private static void SolveChain(NecklaceGame game)
        {
            for (int r = 0; r < game.Rows.Count; r++)
            {
                for (int j = 0; j < game.Rows[r].Len; j++)
                {
                    NecklaceGame.Seg seg = game.SegById(game.SolutionId(r, j));
                    Click(game, NecklaceGame.PieceCode(seg.Id));
                    if (seg.Flipped)
                    {
                        game.Tick(Dt, new MiniInput { SecondaryPressed = true });
                    }

                    Click(game, NecklaceGame.SlotCode(r, j));
                }
            }
        }

        private static NecklaceGame InCut(int seed = 1, int difficulty = 0)
        {
            NecklaceGame game = InChain(seed, difficulty);
            SolveChain(game);
            TickUntil(game, NecklaceGame.PhaseKind.Cut);
            return game;
        }

        private static int IndexFacing(int phi)
        {
            // FacingDeg = (90 - Index * 15) mod 360 이 phi와 같아지는 인덱스
            return (((90 - phi) / NecklaceGame.IndexDeg) % NecklaceGame.IndexCount + NecklaceGame.IndexCount) % NecklaceGame.IndexCount;
        }

        // 모든 면을 고운 숫돌로 목표까지 간다.
        private static void GrindAllFacets(NecklaceGame game)
        {
            game.Tick(Dt, new MiniInput { ToolNumber = 2 });
            foreach (NecklaceGame.TFacet facet in game.TargetFacets.ToList())
            {
                int want = IndexFacing(facet.Phi);
                for (int guard = 0; guard < NecklaceGame.IndexCount && game.Index != want; guard++)
                {
                    game.Tick(Dt, new MiniInput { Arrow = 1 });
                }

                for (int guard = 0; guard < 4000 && game.Remaining > game.Tol * 0.4f; guard++)
                {
                    game.Tick(Dt, new MiniInput { ActionHeld = true });
                }

                game.Tick(Dt, default);
            }
        }

        private static NecklaceGame InSet(int seed = 1, int difficulty = 0)
        {
            NecklaceGame game = InCut(seed, difficulty);
            GrindAllFacets(game);
            game.Tick(Dt, new MiniInput { FinishPressed = true });
            TickUntil(game, NecklaceGame.PhaseKind.Set);
            return game;
        }

        // 보석을 끌어 틀 가운데에 놓고 눈금을 맞춘 뒤 꾹 눌러 박는다.
        private static void SeatGem(NecklaceGame game)
        {
            game.Tick(Dt, new MiniInput { PointerPressed = true, PointerHeld = true, PointerX = game.GemX, PointerY = game.GemY });
            for (int i = 0; i < 400 && game.PosErr > game.PosTol * 0.2f; i++)
            {
                game.Tick(Dt, new MiniInput { PointerHeld = true, PointerX = 0f, PointerY = 0f });
            }

            game.Tick(Dt, default);
            for (int guard = 0; guard < NecklaceGame.IndexCount && game.IdxDiff != 0; guard++)
            {
                game.Tick(Dt, new MiniInput { Arrow = game.IdxDiff > 0 ? -1 : 1 });
            }

            for (int i = 0; i < 400 && game.Phase == NecklaceGame.PhaseKind.Set; i++)
            {
                game.Tick(Dt, new MiniInput { ActionHeld = true });
            }
        }

        private static NecklaceGame InPolish(int seed = 1, int difficulty = 0)
        {
            NecklaceGame game = InSet(seed, difficulty);
            SeatGem(game);
            TickUntil(game, NecklaceGame.PhaseKind.Polish);
            return game;
        }

        // 면마다 가운데 근처를 왔다 갔다 문지른다.
        private static void ScrubAll(NecklaceGame game)
        {
            for (int facet = 0; facet < game.TargetFacets.Count; facet++)
            {
                double angle = (game.TargetFacets[facet].Phi + game.PolishRot) * Math.PI / 180.0;
                float cx = (float)Math.Cos(angle) * NecklaceGame.GemR * 0.5f;
                float cy = (float)Math.Sin(angle) * NecklaceGame.GemR * 0.5f;
                for (int i = 0; i < 400 && game.Shine[facet] < 0.999f && game.Phase == NecklaceGame.PhaseKind.Polish; i++)
                {
                    float offset = (i % 2 == 0 ? -0.25f : 0.25f);
                    game.Tick(Dt, new MiniInput { PointerHeld = true, PointerX = cx + offset, PointerY = cy });
                }
            }
        }

        [TestCase(0, 2, 6)]
        [TestCase(1, 3, 9)]
        [TestCase(2, 5, 15)]
        public void Chain_StartsWithShapeForTheDifficulty(int difficulty, int rows, int pieces)
        {
            NecklaceGame game = InChain(1, difficulty);

            Assert.AreEqual(rows, game.Rows.Count);
            Assert.AreEqual(pieces, game.Segs.Count);
            Assert.AreEqual(NecklaceGame.PhaseKind.Chain, game.Phase);
        }

        [Test]
        public void SameSeed_SameChainPieces()
        {
            NecklaceGame a = InChain(7, 1);
            NecklaceGame b = InChain(7, 1);

            Assert.AreEqual(a.Segs.Select(s => s.Id * 100 + s.LSym * 10 + s.RSym).ToArray(), b.Segs.Select(s => s.Id * 100 + s.LSym * 10 + s.RSym).ToArray());
        }

        [Test]
        public void Chain_PlacingTheRightPieces_FinishesTheStep()
        {
            NecklaceGame game = InChain();
            SolveChain(game);

            Assert.AreEqual(NecklaceGame.PhaseKind.StepDone, game.Phase);
            Assert.AreEqual(game.JointTotal, game.Joined);
            Assert.AreEqual(0, game.Mistakes);
            Assert.Greater(game.ChainScore, 0.85f);
        }

        [Test]
        public void Chain_SecondClickOnAFilledSlot_WhileHolding_WarnsAndKeepsThePiece()
        {
            NecklaceGame game = InChain();
            NecklaceGame.Seg first = game.SegById(game.SolutionId(0, 0));
            Click(game, NecklaceGame.PieceCode(first.Id));
            Click(game, NecklaceGame.SlotCode(0, 0));
            NecklaceGame.Seg second = game.SegById(game.SolutionId(0, 1));
            Click(game, NecklaceGame.PieceCode(second.Id));
            game.Events.Clear();

            Click(game, NecklaceGame.SlotCode(0, 0));

            Assert.AreEqual(second.Id, game.Held);
            Assert.That(game.Events.Any(e => e.Kind == MiniEventKind.Warn));
            Assert.AreEqual(first.Id, game.Rows[0].Slots[0]);
        }

        [Test]
        public void Chain_WrongNeighbour_CountsAMistake()
        {
            NecklaceGame game = InChain();
            // 같은 가닥의 두 조각을 서로 바뀐 칸에 놓는다(첫 칸에 둘째 조각, 둘째 칸에 첫 조각).
            NecklaceGame.Seg first = game.SegById(game.SolutionId(0, 0));
            NecklaceGame.Seg second = game.SegById(game.SolutionId(0, 1));
            if (first.Flipped)
            {
                Click(game, NecklaceGame.PieceCode(first.Id));
                game.Tick(Dt, new MiniInput { SecondaryPressed = true });
                Click(game, NecklaceGame.SlotCode(0, 1));
            }
            else
            {
                Click(game, NecklaceGame.PieceCode(first.Id));
                Click(game, NecklaceGame.SlotCode(0, 1));
            }

            Click(game, NecklaceGame.PieceCode(second.Id));
            Click(game, NecklaceGame.SlotCode(0, 0));

            Assert.GreaterOrEqual(game.Mistakes, 1);
        }

        [Test]
        public void Chain_ClickingTheCrate_PutsTheHeldPieceBack()
        {
            NecklaceGame game = InChain();
            NecklaceGame.Seg piece = game.Segs[0];
            Click(game, NecklaceGame.PieceCode(piece.Id));
            Assert.AreEqual(piece.Id, game.Held);

            Click(game, NecklaceGame.CrateCode);

            Assert.AreEqual(-1, game.Held);
        }

        [Test]
        public void Chain_PickingAPlacedPiece_TakesItBack()
        {
            NecklaceGame game = InChain();
            NecklaceGame.Seg piece = game.SegById(game.SolutionId(0, 0));
            Click(game, NecklaceGame.PieceCode(piece.Id));
            Click(game, NecklaceGame.SlotCode(0, 0));

            Click(game, NecklaceGame.SlotCode(0, 0));

            Assert.AreEqual(piece.Id, game.Held);
            Assert.AreEqual(-1, game.Rows[0].Slots[0]);
        }

        [Test]
        public void Chain_TimeOut_EndsWithAPartialScore()
        {
            NecklaceGame game = InChain();

            TickUntil(game, NecklaceGame.PhaseKind.StepDone, 5000);

            Assert.AreEqual(NecklaceGame.PhaseKind.StepDone, game.Phase);
            Assert.AreEqual(0f, game.ChainScore, 0.001f);
            Assert.Less(game.ChainLeft, 0.001f);
        }

        [TestCase(0, 4)]
        [TestCase(1, 6)]
        [TestCase(2, 8)]
        public void Cut_StartsWithRoughGemLargerThanTheTarget(int difficulty, int facets)
        {
            NecklaceGame game = InCut(1, difficulty);

            Assert.AreEqual(facets, game.TargetFacets.Count);
            for (int k = 0; k < 360; k++)
            {
                Assert.Greater(game.Profile[k], game.Target[k]);
            }
        }

        [Test]
        public void Cut_Grinding_ShavesTheFacingFace()
        {
            NecklaceGame game = InCut();
            int phi = game.TargetFacets[0].Phi;
            for (int guard = 0; guard < NecklaceGame.IndexCount && game.Index != IndexFacing(phi); guard++)
            {
                game.Tick(Dt, new MiniInput { Arrow = 1 });
            }

            float before = game.FacingExtent;
            for (int i = 0; i < 50; i++)
            {
                game.Tick(Dt, new MiniInput { ActionHeld = true });
            }

            Assert.Less(game.FacingExtent, before);
            Assert.IsTrue(game.Grinding);
        }

        [Test]
        public void Cut_CoarseWheelIsFasterAndRoughensTheFace()
        {
            NecklaceGame coarse = InCut();
            NecklaceGame fine = InCut();
            fine.Tick(Dt, new MiniInput { ToolNumber = 2 });
            for (int i = 0; i < 50; i++)
            {
                coarse.Tick(Dt, new MiniInput { ActionHeld = true });
                fine.Tick(Dt, new MiniInput { ActionHeld = true });
            }

            Assert.Less(coarse.FacingExtent, fine.FacingExtent);
            Assert.AreEqual(0, coarse.Tool);
            Assert.AreEqual(1, fine.Tool);
        }

        [Test]
        public void Cut_Rotating_StepsTheIndexAndCannotRotateWhileGrinding()
        {
            NecklaceGame game = InCut();
            int start = game.Index;

            game.Tick(Dt, new MiniInput { Arrow = 1 });
            Assert.AreEqual((start + 1) % NecklaceGame.IndexCount, game.Index);

            game.Tick(Dt, new MiniInput { ActionHeld = true });
            int grinding = game.Index;
            game.Tick(Dt, new MiniInput { ActionHeld = true, Arrow = 1 });
            Assert.AreEqual(grinding, game.Index);
        }

        [Test]
        public void Cut_TakingTooMuchOff_CountsAnOvercut()
        {
            NecklaceGame game = InCut();
            int phi = game.TargetFacets[0].Phi;
            for (int guard = 0; guard < NecklaceGame.IndexCount && game.Index != IndexFacing(phi); guard++)
            {
                game.Tick(Dt, new MiniInput { Arrow = 1 });
            }

            for (int i = 0; i < 2000 && game.Remaining > -game.Tol * 3f; i++)
            {
                game.Tick(Dt, new MiniInput { ActionHeld = true });
            }

            game.Tick(Dt, default);

            Assert.AreEqual(1, game.Overcuts);
        }

        [Test]
        public void Cut_GrindingEveryFaceToTheTarget_ScoresHigh()
        {
            NecklaceGame game = InCut();
            GrindAllFacets(game);
            game.Tick(Dt, new MiniInput { FinishPressed = true });

            Assert.AreEqual(NecklaceGame.PhaseKind.StepDone, game.Phase);
            Assert.Greater(game.CutScore, 0.85f);
            Assert.AreEqual(0, game.Overcuts);
        }

        [Test]
        public void Cut_FinishingEarly_ScoresLow()
        {
            NecklaceGame game = InCut();
            game.Tick(Dt, new MiniInput { FinishPressed = true });

            Assert.Less(game.CutScore, 0.3f);
        }

        [Test]
        public void Set_StartsWithTheGemOffTheSocketAndTheAngleOff()
        {
            NecklaceGame game = InSet();
            game.Tick(Dt, default);

            Assert.Greater(game.PosErr, game.PosTol);
            Assert.AreNotEqual(0, game.IdxDiff);
            Assert.IsFalse(game.Aligned);
        }

        [Test]
        public void Set_AlignedAndHeld_SeatsTheGem()
        {
            NecklaceGame game = InSet();
            SeatGem(game);

            Assert.AreEqual(NecklaceGame.PhaseKind.StepDone, game.Phase);
            Assert.Greater(game.SetScore, 0.6f);
            Assert.AreEqual(0f, game.Damage, 0.001f);
        }

        [Test]
        public void Set_PressingWhileFarAway_DamagesTheGem()
        {
            NecklaceGame game = InSet();

            for (int i = 0; i < 100; i++)
            {
                game.Tick(Dt, new MiniInput { ActionHeld = true });
            }

            Assert.Greater(game.Damage, 0f);
            Assert.GreaterOrEqual(game.Slips, 1);
            Assert.AreEqual(0f, game.SeatProgress, 0.001f);
        }

        [Test]
        public void Set_ReleasingTheKey_DrainsTheProgress()
        {
            NecklaceGame game = InSet();
            game.Tick(Dt, new MiniInput { PointerPressed = true, PointerHeld = true, PointerX = game.GemX, PointerY = game.GemY });
            for (int i = 0; i < 400 && game.PosErr > game.PosTol * 0.2f; i++)
            {
                game.Tick(Dt, new MiniInput { PointerHeld = true, PointerX = 0f, PointerY = 0f });
            }

            for (int guard = 0; guard < NecklaceGame.IndexCount && game.IdxDiff != 0; guard++)
            {
                game.Tick(Dt, new MiniInput { Arrow = game.IdxDiff > 0 ? -1 : 1 });
            }

            for (int i = 0; i < 10; i++)
            {
                game.Tick(Dt, new MiniInput { ActionHeld = true });
            }

            float held = game.SeatProgress;
            for (int i = 0; i < 10; i++)
            {
                game.Tick(Dt, default);
            }

            Assert.Greater(held, 0f);
            Assert.Less(game.SeatProgress, held);
        }

        [Test]
        public void Polish_ScrubbingEveryFace_FinishesWithHighScore()
        {
            NecklaceGame game = InPolish();
            ScrubAll(game);

            Assert.AreEqual(NecklaceGame.PhaseKind.Done, game.Phase);
            Assert.AreEqual(game.TargetFacets.Count, game.ShinedCount);
            Assert.Greater(game.PolishScore, 0.95f);
        }

        [Test]
        public void Polish_StandingStill_DoesNotShine()
        {
            NecklaceGame game = InPolish();
            double angle = (game.TargetFacets[0].Phi + game.PolishRot) * Math.PI / 180.0;
            float cx = (float)Math.Cos(angle) * NecklaceGame.GemR * 0.5f;
            float cy = (float)Math.Sin(angle) * NecklaceGame.GemR * 0.5f;

            for (int i = 0; i < 100; i++)
            {
                game.Tick(Dt, new MiniInput { PointerHeld = true, PointerX = cx, PointerY = cy });
            }

            Assert.AreEqual(0f, game.Shine[0], 0.001f);
        }

        [Test]
        public void Polish_RoughFacesNeedMoreScrubbing()
        {
            NecklaceGame game = InPolish();

            for (int i = 0; i < game.TargetFacets.Count; i++)
            {
                Assert.AreEqual((1.8f + 3f * game.FacetRough(i)) * 1f, game.ShineNeed[i], 0.001f);
            }
        }

        [Test]
        public void FullRun_AccuracyIsBetweenZeroAndOne_AndFinishes()
        {
            NecklaceGame game = InPolish();
            ScrubAll(game);
            Run(game, 3f);

            Assert.IsTrue(game.Finished);
            Assert.GreaterOrEqual(game.Accuracy, 0f);
            Assert.LessOrEqual(game.Accuracy, 1f);
            Assert.Greater(game.Accuracy, 0.8f);
            Assert.That(game.Events.Any(e => e.Kind == MiniEventKind.Complete));
            Assert.That(game.Events.Any(e => e.Kind == MiniEventKind.Finish));
        }

        [Test]
        public void SameSeedAndInputs_GiveTheSameAccuracy()
        {
            NecklaceGame a = InPolish(3, 1);
            ScrubAll(a);
            Run(a, 3f);
            NecklaceGame b = InPolish(3, 1);
            ScrubAll(b);
            Run(b, 3f);

            Assert.AreEqual(a.Accuracy, b.Accuracy, 0.0001f);
        }

        [Test]
        public void HarderDifficulties_CompleteWithTheSameBot()
        {
            for (int difficulty = 1; difficulty <= 2; difficulty++)
            {
                NecklaceGame game = InPolish(2, difficulty);
                ScrubAll(game);
                Run(game, 3f);

                Assert.IsTrue(game.Finished, "난이도 " + difficulty);
            }
        }
    }
}
