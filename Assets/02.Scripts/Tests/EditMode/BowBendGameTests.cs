using System;
using System.Linq;
using Blacksmith.Core.Forge;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class BowBendGameTests
    {
        private const float Dt = 0.01f;

        // 테스트 전용 수치다. 실제 값은 BowBendRules 데이터를 따른다.
        private static BowBendRules CreateRules()
        {
            return new BowBendRules
            {
                IntroTime = 1f,
                EndDelay = 1f,
                BendPerCell = 0.3f,
                ColdFactor = 0.35f,
                HeatRate = 0.9f,
                CoolRate = 0.05f,
                SpringBack = 0.12f,
                SpringCold = 0.8f,
                MaxBend = 1.2f,
                ErrorScale = 0.35f,
                SuccessScore = 0.6f,
                Difficulties = new[]
                {
                    new BowBendDifficulty { TimeLimit = 55f, Tolerance = 0.07f, Jitter = 0.1f },
                    new BowBendDifficulty { TimeLimit = 50f, Tolerance = 0.055f, Jitter = 0.15f },
                    new BowBendDifficulty { TimeLimit = 45f, Tolerance = 0.04f, Jitter = 0.2f },
                },
            };
        }

        private static BowBendGame Create(int seed = 1, int difficulty = 0)
        {
            return new BowBendGame(CreateRules(), seed, difficulty);
        }

        private static void Start(BowBendGame game)
        {
            for (int i = 0; i < 300 && game.Phase != BowBendGame.PhaseKind.Bend; i++)
            {
                game.Tick(Dt, new MiniInput { PointerX = -100f });
            }
        }

        // 마디의 가로 칸 가운데 좌표
        private static float CenterOf(int segment)
        {
            return -BowBendGame.SegmentCount * BowBendGame.SegmentWidth * 0.5f + (segment + 0.5f) * BowBendGame.SegmentWidth;
        }

        private static void Press(BowBendGame game, int segment, float y)
        {
            game.Tick(Dt, new MiniInput { PointerX = CenterOf(segment), PointerY = y, PointerHeld = true });
        }

        private static void Release(BowBendGame game, int segment, float y)
        {
            game.Tick(Dt, new MiniInput { PointerX = CenterOf(segment), PointerY = y, PointerHeld = false });
        }

        // 마디를 누르고 아래로 천천히 끌어 휜 정도가 until에 이르면 놓는다. 열이 낮으면 더 많이 끌어야 한다.
        private static void DragAndRelease(BowBendGame game, int segment, float until)
        {
            float y = 0f;
            Press(game, segment, y);
            for (int i = 0; i < 100000 && game.BendOf(segment) < until && !game.Finished; i++)
            {
                y += 0.02f;
                Press(game, segment, y);
            }

            Release(game, segment, y);
        }

        private static void Heat(BowBendGame game, int segment, float seconds)
        {
            for (int i = 0; i < (int)(seconds / Dt); i++)
            {
                game.Tick(Dt, new MiniInput { PointerX = CenterOf(segment), ActionHeld = true });
            }
        }

        private static float Overshoot(BowBendGame game, int segment)
        {
            return game.TargetOf(segment) / (1f - game.SpringOf(segment));
        }

        [Test]
        public void SegmentAt_MapsSixColumnsAroundTheCenter()
        {
            Assert.AreEqual(0, BowBendGame.SegmentAt(-7f));
            Assert.AreEqual(2, BowBendGame.SegmentAt(-0.1f));
            Assert.AreEqual(3, BowBendGame.SegmentAt(0.1f));
            Assert.AreEqual(5, BowBendGame.SegmentAt(7f));
            Assert.AreEqual(-1, BowBendGame.SegmentAt(-8f));
            Assert.AreEqual(-1, BowBendGame.SegmentAt(8f));
        }

        [Test]
        public void DraggingDown_BendsTheSegmentByTheDraggedDistance()
        {
            BowBendGame game = Create();
            Start(game);

            Press(game, 2, 0f);
            Press(game, 2, 2f);

            Assert.AreEqual(0.3f * 2f, game.BendOf(2), 0.02f);
            Assert.AreEqual(2, game.Active);
            Assert.AreEqual(0f, game.BendOf(0), 0.0001f);
        }

        [Test]
        public void DraggingBackUp_DoesNotUnbendTheIron()
        {
            BowBendGame game = Create();
            Start(game);

            Press(game, 2, 0f);
            Press(game, 2, 2f);
            float bent = game.BendOf(2);
            Press(game, 2, 0.5f);

            Assert.AreEqual(bent, game.BendOf(2), 0.0001f);
        }

        [Test]
        public void ReleasingNearTheTarget_LocksTheSegmentAsAHit()
        {
            BowBendGame game = Create();
            Start(game);

            DragAndRelease(game, 5, Overshoot(game, 5));

            Assert.IsTrue(game.IsLocked(5));
            Assert.AreEqual(game.TargetOf(5), game.BendOf(5), game.Tolerance);
            Assert.Contains(MiniEventKind.Hit, game.Events.Select(e => e.Kind).ToList());
        }

        [Test]
        public void ReleasingWayPastTheTarget_LocksItWithAnError()
        {
            BowBendGame game = Create();
            Start(game);

            DragAndRelease(game, 5, 1.2f);

            Assert.IsTrue(game.IsLocked(5));
            Assert.AreEqual(1, game.Overbent);
        }

        [Test]
        public void ReleasingTooEarly_LeavesTheSegmentOpenToBendMore()
        {
            BowBendGame game = Create();
            Start(game);

            DragAndRelease(game, 5, 0.3f);

            Assert.IsFalse(game.IsLocked(5));

            DragAndRelease(game, 5, Overshoot(game, 5));

            Assert.IsTrue(game.IsLocked(5));
        }

        [Test]
        public void SegmentsCoolDownOverTime()
        {
            BowBendGame game = Create();
            Start(game);

            for (int i = 0; i < 1500; i++)
            {
                game.Tick(Dt, new MiniInput { PointerX = -100f });
            }

            Assert.Less(game.HeatOf(0), 0.6f);
            Assert.Less(game.AverageHeat, 0.6f);
        }

        [Test]
        public void ColdIron_NeedsALongerDragAndSpringsBackMore()
        {
            BowBendGame hot = Create();
            BowBendGame cold = Create();
            Start(hot);
            Start(cold);
            for (int i = 0; i < 2500; i++)
            {
                cold.Tick(Dt, new MiniInput { PointerX = -100f });
            }

            Press(hot, 1, 0f);
            Press(hot, 1, 2f);
            Press(cold, 1, 0f);
            Press(cold, 1, 2f);

            Assert.Less(cold.BendOf(1), hot.BendOf(1) * 0.7f);
            Assert.Greater(cold.SpringOf(1), hot.SpringOf(1));
        }

        [Test]
        public void HoldingTheHeatKey_ReheatsTheHoveredSegmentButNotWhileBending()
        {
            BowBendGame game = Create();
            Start(game);
            for (int i = 0; i < 2000; i++)
            {
                game.Tick(Dt, new MiniInput { PointerX = -100f });
            }

            float before = game.HeatOf(3);
            Heat(game, 3, 1f);

            Assert.Greater(game.HeatOf(3), before + 0.5f);
            Assert.AreEqual(3, game.Heating);
            Assert.AreEqual(1, game.Reheats);

            float afterHeating = game.HeatOf(3);
            game.Tick(Dt, new MiniInput { PointerX = CenterOf(3), PointerY = 0f, PointerHeld = true, ActionHeld = true });

            Assert.AreEqual(-1, game.Active);
            Assert.GreaterOrEqual(game.HeatOf(3), afterHeating - 0.01f);
        }

        [Test]
        public void LockingEverySegment_FinishesWithHighAccuracy()
        {
            BowBendGame game = Create();
            Start(game);

            for (int segment = 0; segment < BowBendGame.SegmentCount; segment++)
            {
                Heat(game, segment, 1f);
                DragAndRelease(game, segment, Overshoot(game, segment));
            }

            Assert.AreEqual(BowBendGame.SegmentCount, game.LockedCount);
            for (int i = 0; i < 300 && !game.Finished; i++)
            {
                game.Tick(Dt, new MiniInput { PointerX = -100f });
            }

            Assert.IsTrue(game.Finished);
            Assert.GreaterOrEqual(game.Accuracy, 0.99f);
        }

        [Test]
        public void DoingNothing_RunsOutOfTimeAndScoresZero()
        {
            BowBendGame game = Create();

            for (int i = 0; i < 100000 && !game.Finished; i++)
            {
                game.Tick(Dt, new MiniInput { PointerX = -100f });
            }

            Assert.IsTrue(game.Finished);
            Assert.IsTrue(game.TimedOut);
            Assert.AreEqual(0f, game.Accuracy, 0.0001f);
        }

        [Test]
        public void SameSeed_GivesSameTargets_AndHarderDifficultyHasLessTime()
        {
            BowBendGame first = Create(seed: 9, difficulty: 2);
            BowBendGame second = Create(seed: 9, difficulty: 2);
            BowBendGame easy = Create(difficulty: 0);

            for (int i = 0; i < BowBendGame.SegmentCount; i++)
            {
                Assert.AreEqual(first.TargetOf(i), second.TargetOf(i));
            }

            Assert.Less(first.TimeLimit, easy.TimeLimit);
            Assert.Less(first.Tolerance, easy.Tolerance);
        }
    }
}
