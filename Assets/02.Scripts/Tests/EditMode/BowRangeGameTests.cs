using System;
using System.Linq;
using Blacksmith.Core.Forge;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class BowRangeGameTests
    {
        private const float Dt = 0.01f;

        // 테스트 전용 수치다. 실제 값은 BowRangeRules 데이터를 따른다.
        private static BowRangeRules CreateRules()
        {
            return new BowRangeRules
            {
                IntroTime = 1f,
                ResultTime = 1.0f,
                EndDelay = 1f,
                FlightTime = 0.6f,
                BreathTime = 2.2f,
                BreathSteady = 0.25f,
                GaspSway = 1.8f,
                BreathRecover = 3f,
                DrawTime = 0.8f,
                FatigueStart = 0.6f,
                SwayGrowth = 0.6f,
                SwayFrequency = 2.2f,
                DropScale = 3f,
                RefDistance = 15f,
                WindScale = 0.8f,
                GravityDrop = 0.4f,
                CenterY = 1.2f,
                RangeX = 4f,
                RangeY = 0.8f,
                SuccessScore = 0.6f,
                Difficulties = new[]
                {
                    new BowRangeDifficulty { GoalHits = 12, TimeLimit = 45f, TargetRadius = 0.8f, Wind = 0.5f, TargetMove = 0f, TargetSpeed = 0f, Sway = 0.2f, DistanceMin = 8f, DistanceMax = 14f },
                    new BowRangeDifficulty { GoalHits = 16, TimeLimit = 45f, TargetRadius = 0.7f, Wind = 1f, TargetMove = 0.8f, TargetSpeed = 0.6f, Sway = 0.28f, DistanceMin = 10f, DistanceMax = 18f },
                    new BowRangeDifficulty { GoalHits = 20, TimeLimit = 45f, TargetRadius = 0.6f, Wind = 1.5f, TargetMove = 1.4f, TargetSpeed = 0.9f, Sway = 0.4f, DistanceMin = 12f, DistanceMax = 22f },
                },
            };
        }

        private static BowRangeGame Create(int seed = 1, int difficulty = 0)
        {
            return new BowRangeGame(CreateRules(), seed, difficulty);
        }

        private static void Step(BowRangeGame game, float x = -100f, float y = 0f, bool press = false, bool held = false)
        {
            game.Tick(Dt, new MiniInput { PointerX = x, PointerY = y, PointerPressed = press, PointerHeld = held });
        }

        private static void WaitForAim(BowRangeGame game)
        {
            for (int i = 0; i < 100000 && game.Phase != BowRangeGame.PhaseKind.Aim && !game.Finished; i++)
            {
                Step(game);
            }
        }

        // 한 발을 쏜다. 겨냥 점(offsetY: 가슴 가운데에서 위아래로 옮긴 칸)을 바람과 처짐, 손 떨림을 계산해 보정한다.
        private static void Shoot(BowRangeGame game, float drawSeconds, bool compensate, float offsetY = 0f)
        {
            WaitForAim(game);
            float drift = game.Drift;
            float drop = game.Drop;
            Step(game, game.TargetX, game.TargetY, press: true, held: true);
            int ticks = (int)Math.Round(drawSeconds / Dt);
            for (int i = 0; i < ticks; i++)
            {
                AimStep(game, drift, drop, compensate, offsetY, held: true);
            }

            AimStep(game, drift, drop, compensate, offsetY, held: false);
        }

        private static void AimStep(BowRangeGame game, float drift, float drop, bool compensate, float offsetY, bool held)
        {
            float x = compensate ? game.TargetX - drift - game.SwayX : game.TargetX;
            float y = (compensate ? game.TargetY - game.SwayY - (1f - game.Power) * 3f - drop : game.TargetY) + offsetY;
            Step(game, x, y, held: held);
        }

        private static float HeadOffsetCells(BowRangeGame game)
        {
            return -BowRangeGame.HeadOffset * game.TargetRadius;
        }

        [Test]
        public void FullDrawWithCompensation_HitsTheChest()
        {
            BowRangeGame game = Create();

            Shoot(game, 1.0f, compensate: true);

            Assert.IsTrue(game.LastHit);
            Assert.IsFalse(game.LastHead);
            Assert.AreEqual(1, game.Points);
            Assert.AreEqual(1, game.Hits);
        }

        [Test]
        public void AimingAtTheHead_ScoresACriticalForTwoPoints()
        {
            BowRangeGame game = Create();

            Shoot(game, 1.0f, compensate: true, offsetY: HeadOffsetCells(game));

            Assert.IsTrue(game.LastHit);
            Assert.IsTrue(game.LastHead);
            Assert.AreEqual(2, game.Points);
            Assert.AreEqual(1, game.Headshots);
            Assert.AreEqual(2, game.LastPoints);
            Assert.Contains(MiniEventKind.Perfect, game.Events.Select(e => e.Kind).ToList());
        }

        [Test]
        public void AimingFarAbove_Misses()
        {
            BowRangeGame game = Create();

            Shoot(game, 1.0f, compensate: true, offsetY: -6f);

            Assert.IsFalse(game.LastHit);
            Assert.AreEqual(0, game.Points);
            Assert.AreEqual(1, game.Shots);
        }

        [Test]
        public void AfterAHit_ANewTargetAppears_ButAMissKeepsTheSameOne()
        {
            BowRangeGame game = Create();
            Shoot(game, 1.0f, compensate: true, offsetY: -6f);
            float missX = game.TargetX;
            float missDistance = game.Distance;
            WaitForAim(game);

            Assert.AreEqual(missX, game.TargetX);
            Assert.AreEqual(missDistance, game.Distance);

            Shoot(game, 1.0f, compensate: true);
            float hitX = game.TargetX;
            float hitDistance = game.Distance;
            WaitForAim(game);

            Assert.IsTrue(game.TargetX != hitX || game.Distance != hitDistance);
        }

        [Test]
        public void AimingAtTheCenterIgnoringTheWind_MissesMoreOnTheHardestSetting()
        {
            int compensatedPoints = 0;
            int naivePoints = 0;
            for (int seed = 1; seed <= 6; seed++)
            {
                BowRangeGame compensated = Create(seed: seed, difficulty: 2);
                BowRangeGame naive = Create(seed: seed, difficulty: 2);
                for (int i = 0; i < 4; i++)
                {
                    Shoot(compensated, 1.0f, compensate: true);
                    Shoot(naive, 1.0f, compensate: false);
                }

                compensatedPoints += compensated.Points;
                naivePoints += naive.Points;
            }

            Assert.Less(naivePoints, compensatedPoints);
        }

        [Test]
        public void ReleasingBeforeFullDraw_DropsTheArrow()
        {
            BowRangeGame full = Create();
            BowRangeGame weak = Create();

            Shoot(full, 1.0f, compensate: false);
            Shoot(weak, 0.2f, compensate: false);

            Assert.Greater(weak.ImpactY, full.ImpactY);
        }

        [Test]
        public void ReachingTheGoalPoints_DoesNotEndTheGame_AndAccuracyStaysAtOne()
        {
            BowRangeGame game = Create();

            for (int i = 0; i < 20 && game.Points < game.GoalHits; i++)
            {
                Shoot(game, 1.0f, compensate: true, offsetY: HeadOffsetCells(game));
            }

            Assert.GreaterOrEqual(game.Points, game.GoalHits);
            WaitForAim(game);
            Assert.IsFalse(game.Finished);
            Assert.AreEqual(BowRangeGame.PhaseKind.Aim, game.Phase);

            Shoot(game, 1.0f, compensate: true, offsetY: HeadOffsetCells(game));
            for (int i = 0; i < 100000 && !game.Finished; i++)
            {
                Step(game);
            }

            Assert.IsTrue(game.Finished);
            Assert.IsTrue(game.TimedOut);
            Assert.AreEqual(1f, game.Accuracy, 0.0001f);
        }

        [Test]
        public void ShootingNothing_RunsOutOfTimeAndScoresZero()
        {
            BowRangeGame game = Create();

            for (int i = 0; i < 100000 && !game.Finished; i++)
            {
                Step(game);
            }

            Assert.IsTrue(game.Finished);
            Assert.IsTrue(game.TimedOut);
            Assert.AreEqual(0f, game.Accuracy, 0.0001f);
        }

        [Test]
        public void AccuracyIsThePointsOverTheGoal()
        {
            BowRangeGame game = Create();
            Shoot(game, 1.0f, compensate: true);
            Shoot(game, 1.0f, compensate: true, offsetY: HeadOffsetCells(game));

            for (int i = 0; i < 100000 && !game.Finished; i++)
            {
                Step(game);
            }

            Assert.AreEqual(game.Points / (float)game.GoalHits, game.Accuracy, 0.0001f);
        }

        [Test]
        public void MovingTargets_ChangePositionOverTime()
        {
            BowRangeGame game = Create(difficulty: 2);
            WaitForAim(game);
            float before = game.TargetX;

            for (int i = 0; i < 100; i++)
            {
                Step(game);
            }

            Assert.AreNotEqual(before, game.TargetX);
        }

        [Test]
        public void SameSeed_GivesSameTargetsAndWind_AndHarderIsSmallerAndFurther()
        {
            BowRangeGame first = Create(seed: 7, difficulty: 1);
            BowRangeGame second = Create(seed: 7, difficulty: 1);
            BowRangeGame easy = Create(difficulty: 0);
            BowRangeGame hard = Create(difficulty: 2);

            Assert.AreEqual(first.TargetX, second.TargetX);
            Assert.AreEqual(first.Wind, second.Wind);
            Assert.AreEqual(first.Distance, second.Distance);
            Assert.Greater(hard.GoalHits, easy.GoalHits);
            Assert.Less(hard.RadiusAt(15f), easy.RadiusAt(15f));
        }

        private static BowRangeGame CreateWithBreath()
        {
            return Create();
        }

        private static float MaxSwayWhileDrawing(BowRangeGame game, bool breath)
        {
            WaitForAim(game);
            Step(game, game.TargetX, game.TargetY, press: true, held: true);
            float max = 0f;
            for (int i = 0; i < 150; i++)
            {
                game.Tick(Dt, new MiniInput { PointerX = game.TargetX, PointerY = game.TargetY, PointerHeld = true, ActionHeld = breath });
                max = Math.Max(max, Math.Abs(game.SwayX));
            }

            return max;
        }

        [Test]
        public void HoldingBreath_ReducesTheHandShake()
        {
            float steady = MaxSwayWhileDrawing(CreateWithBreath(), breath: true);
            float normal = MaxSwayWhileDrawing(CreateWithBreath(), breath: false);

            Assert.Less(steady, normal * 0.6f);
        }

        [Test]
        public void HoldingBreathTooLong_RunsOutAndGasps()
        {
            BowRangeGame game = CreateWithBreath();
            WaitForAim(game);
            Step(game, game.TargetX, game.TargetY, press: true, held: true);
            for (int i = 0; i < 300; i++)
            {
                game.Tick(Dt, new MiniInput { PointerX = game.TargetX, PointerY = game.TargetY, PointerHeld = true, ActionHeld = true });
            }

            Assert.AreEqual(0f, game.Breath, 0.0001f);
            Assert.IsTrue(game.Gasping);
        }

        [Test]
        public void Events_ReportChargeHitsAndCompletion()
        {
            BowRangeGame game = Create();

            Shoot(game, 1.0f, compensate: true);
            for (int i = 0; i < 100000 && !game.Finished; i++)
            {
                Step(game);
            }

            var kinds = game.Events.Select(e => e.Kind).ToList();
            Assert.Contains(MiniEventKind.Charge, kinds);
            Assert.Contains(MiniEventKind.Hit, kinds);
            Assert.Contains(MiniEventKind.Complete, kinds);
            Assert.AreEqual(MiniEventKind.Finish, kinds.Last());
        }
    }
}
