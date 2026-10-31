using System.Collections.Generic;
using Blacksmith.Core.Forge;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class QuenchGameTests
    {
        private const float Dt = 0.01f;
        private static readonly MiniInput Press = new MiniInput { ActionPressed = true };

        // 테스트 전용 수치다. 실제 밸런스는 ScriptableObject 데이터를 따른다.
        // 난이도 0은 기준 시간이 5초로 고정이고, 난이도 1은 8~10초 사이에서 시드로 정해진다.
        private static QuenchRules CreateRules()
        {
            return new QuenchRules
            {
                CarryTime = 0.45f,
                ShowSeconds = 2f,
                ReadyLockTime = 0.6f,
                ReadyAutoTime = 4f,
                MinPullTime = 0.25f,
                EndDelay = 1.6f,
                LimitTargetMultiplier = 2f,
                LimitExtraSeconds = 3f,
                TimeoutAccuracy = 0.1f,
                PerfectEventAccuracy = 0.95f,
                Difficulties = new[]
                {
                    new QuenchDifficulty { MinTarget = 5f, TargetRandomRange = 0f, PerfectFraction = 0.1f, ZeroFraction = 0.5f },
                    new QuenchDifficulty { MinTarget = 8f, TargetRandomRange = 2f, PerfectFraction = 0.05f, ZeroFraction = 0.25f },
                },
            };
        }

        private static void Advance(QuenchGame game, float seconds)
        {
            for (float elapsed = 0f; elapsed < seconds; elapsed += Dt)
            {
                game.Tick(Dt, default);
            }
        }

        private static QuenchGame StartDunking(int difficulty = 0, int seed = 1)
        {
            var rules = CreateRules();
            var game = new QuenchGame(rules, seed, difficulty);
            Advance(game, rules.ReadyAutoTime + 0.05f);
            return game;
        }

        private static void PullAtElapsed(QuenchGame game, float seconds)
        {
            while (game.Elapsed + Dt < seconds)
            {
                game.Tick(Dt, default);
            }
            game.Tick(Dt, Press);
        }

        private static List<MiniEvent> Drain(QuenchGame game)
        {
            var events = new List<MiniEvent>();
            while (game.Events.Count > 0)
            {
                events.Add(game.Events.Dequeue());
            }
            return events;
        }

        [Test]
        public void Target_SameSeedAndDifficulty_IsSame()
        {
            var first = new QuenchGame(CreateRules(), 42, 1);
            var second = new QuenchGame(CreateRules(), 42, 1);

            Assert.AreEqual(first.Target, second.Target);
        }

        [Test]
        public void Target_StaysInDifficultyRange()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                float target = new QuenchGame(CreateRules(), seed, 1).Target;

                Assert.That(target, Is.InRange(8f, 10f));
            }
        }

        [Test]
        public void Constructor_ClampsDifficultyToValidRange()
        {
            Assert.AreEqual(new QuenchGame(CreateRules(), 7, 1).Target, new QuenchGame(CreateRules(), 7, 99).Target);
            Assert.AreEqual(new QuenchGame(CreateRules(), 7, 0).Target, new QuenchGame(CreateRules(), 7, -3).Target);
        }

        [Test]
        public void Ready_StartsDunkingAutomaticallyWithoutInput()
        {
            var game = new QuenchGame(CreateRules(), 1, 0);

            Advance(game, 3.9f);
            Assert.AreEqual(QuenchGame.PhaseKind.Ready, game.Phase);

            Advance(game, 0.2f);
            Assert.AreEqual(QuenchGame.PhaseKind.Dunk, game.Phase);
        }

        [Test]
        public void Ready_IgnoresInputBeforeLockTime()
        {
            var game = new QuenchGame(CreateRules(), 1, 0);

            game.Tick(0.3f, Press);
            Assert.AreEqual(QuenchGame.PhaseKind.Ready, game.Phase);

            game.Tick(0.31f, Press);
            Assert.AreEqual(QuenchGame.PhaseKind.Dunk, game.Phase);
        }

        [Test]
        public void Dunk_IgnoresInputRightAfterSubmerging()
        {
            var game = StartDunking();
            Advance(game, 0.45f + 0.1f);

            game.Tick(Dt, Press);

            Assert.AreEqual(QuenchGame.PhaseKind.Dunk, game.Phase);
        }

        [Test]
        public void Pull_AtTarget_GivesFullAccuracyAndPerfectEvent()
        {
            var game = StartDunking();
            Drain(game);

            PullAtElapsed(game, game.Target);

            Assert.AreEqual(1f, game.Accuracy);
            Assert.IsTrue(Drain(game).Exists(e => e.Kind == MiniEventKind.Perfect));
        }

        [Test]
        public void Pull_FarEarlyOrLate_GivesZeroAccuracy()
        {
            var early = StartDunking();
            PullAtElapsed(early, early.Target * 0.45f);
            var late = StartDunking();
            PullAtElapsed(late, late.Target * 1.55f);

            Assert.AreEqual(0f, early.Accuracy);
            Assert.AreEqual(0f, late.Accuracy);
            Assert.Less(early.Diff, 0f);
            Assert.Greater(late.Diff, 0f);
        }

        [Test]
        public void Pull_HalfwayBetweenPerfectAndZero_GivesHalfAccuracy()
        {
            var game = StartDunking();

            PullAtElapsed(game, game.Target * 1.3f);

            Assert.AreEqual(0.5f, game.Accuracy, 0.03f);
        }

        [Test]
        public void Timeout_GivesTimeoutAccuracyWithoutPerfectEvent()
        {
            var game = StartDunking();
            Drain(game);

            for (int i = 0; i < 5000 && game.Phase != QuenchGame.PhaseKind.Done; i++)
            {
                game.Tick(Dt, default);
            }

            Assert.IsTrue(game.TimedOut);
            Assert.AreEqual(0.1f, game.Accuracy);
            Assert.IsFalse(Drain(game).Exists(e => e.Kind == MiniEventKind.Perfect));
        }

        [Test]
        public void Finish_AfterEndDelay_RaisesFinishEventWithAccuracy()
        {
            var game = StartDunking();
            PullAtElapsed(game, game.Target);
            Drain(game);

            Advance(game, 1.7f);

            Assert.IsTrue(game.Finished);
            var finish = Drain(game).Find(e => e.Kind == MiniEventKind.Finish);
            Assert.AreEqual(game.Accuracy, finish.Value);
        }

        [Test]
        public void Tick_AfterFinished_DoesNothing()
        {
            var game = StartDunking();
            PullAtElapsed(game, game.Target);
            Advance(game, 1.7f);
            Drain(game);
            float accuracy = game.Accuracy;

            game.Tick(1f, Press);

            Assert.AreEqual(accuracy, game.Accuracy);
            Assert.AreEqual(0, game.Events.Count);
        }

        [Test]
        public void SecondTick_OnlyRaisedWhileCountIsVisible()
        {
            var game = StartDunking();
            Drain(game);

            Advance(game, 0.45f + 3.5f);

            var seconds = Drain(game).FindAll(e => e.Kind == MiniEventKind.SecondTick);
            Assert.AreEqual(2, seconds.Count);
            Assert.AreEqual(1f, seconds[0].Value);
            Assert.AreEqual(2f, seconds[1].Value);
            Assert.IsFalse(game.IsCountVisible);
        }
    }
}
