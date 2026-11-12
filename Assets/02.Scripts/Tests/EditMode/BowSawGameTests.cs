using System;
using System.Linq;
using Blacksmith.Core.Forge;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class BowSawGameTests
    {
        private const float Dt = 0.01f;

        // 테스트 전용 수치다. 실제 값은 BowSawRules 데이터를 따른다.
        private static BowSawRules CreateRules()
        {
            return new BowSawRules
            {
                IntroTime = 1f,
                EndDelay = 1f,
                StrokeMin = 2.5f,
                Hysteresis = 0.25f,
                BindCooldown = 0.8f,
                BindFactor = 1.4f,
                SlowFactor = 0.6f,
                EdgeQuality = 0.5f,
                SlowQuality = 0.2f,
                PerfectStreak = 4,
                SuccessScore = 0.6f,
                Difficulties = new[]
                {
                    new BowSawDifficulty { Strokes = 8, ZoneLo = 6f, ZoneHi = 10f, Logs = 8, TimeLimit = 40f },
                    new BowSawDifficulty { Strokes = 9, ZoneLo = 6.5f, ZoneHi = 9.5f, Logs = 10, TimeLimit = 40f },
                    new BowSawDifficulty { Strokes = 10, ZoneLo = 7f, ZoneHi = 9f, Logs = 12, TimeLimit = 40f },
                },
            };
        }

        private static BowSawGame Create(int difficulty = 0)
        {
            return new BowSawGame(CreateRules(), difficulty);
        }

        private static void Start(BowSawGame game)
        {
            for (int i = 0; i < 300 && game.Phase != BowSawGame.PhaseKind.Play; i++)
            {
                game.Tick(Dt, new MiniInput { PointerX = -100f });
            }
        }

        // 위아래 ±1.5칸을 speed(칸/초)로 오가며 톱질한다.
        private static void Saw(BowSawGame game, float speed, float seconds = 200f, bool held = true)
        {
            float y = 0f;
            float direction = 1f;
            for (int i = 0; i < (int)(seconds / Dt) && !game.Finished; i++)
            {
                y += direction * speed * Dt;
                if (Math.Abs(y) >= 1.5f)
                {
                    direction = -direction;
                    y = Math.Sign(y) * 1.5f;
                }

                game.Tick(Dt, new MiniInput { PointerX = 0f, PointerY = y, PointerHeld = held });
            }
        }

        [Test]
        public void SteadyForceInTheZone_CutsManyLogsAndScoresHigh()
        {
            BowSawGame game = Create();
            Start(game);

            Saw(game, 8f);

            Assert.IsTrue(game.Finished);
            Assert.GreaterOrEqual(game.LogsCut, game.LogsTarget);
            Assert.GreaterOrEqual(game.Accuracy, 0.99f);
        }

        [Test]
        public void StoppingAfterSomeTime_ScoresByHowManyLogsWereCut()
        {
            BowSawGame game = Create();
            Start(game);

            Saw(game, 8f, seconds: 15f);
            Assert.Greater(game.LogsCut, 0);
            Assert.Less(game.LogsCut, game.LogsTarget);
            float partial = game.Progress;

            Saw(game, 8f, seconds: 5f);

            Assert.Greater(game.Progress, partial);
        }

        [Test]
        public void SawingFromAnyMouseHeight_CountsTheSameStrokes()
        {
            BowSawGame low = Create();
            BowSawGame high = Create();
            Start(low);
            Start(high);

            SawAround(low, 0f);
            SawAround(high, 40f);

            Assert.AreEqual(low.Strokes, high.Strokes);
            Assert.Greater(high.Strokes, 5);
        }

        private static void SawAround(BowSawGame game, float baseY)
        {
            float y = 0f;
            float direction = 1f;
            for (int i = 0; i < 300; i++)
            {
                y += direction * 8f * Dt;
                if (Math.Abs(y) >= 1.5f)
                {
                    direction = -direction;
                    y = Math.Sign(y) * 1.5f;
                }

                game.Tick(Dt, new MiniInput { PointerX = 0f, PointerY = baseY + y, PointerHeld = true });
            }
        }

        [Test]
        public void SpeedReflectsHowFastTheMouseMoves()
        {
            BowSawGame slow = Create();
            BowSawGame fast = Create();
            Start(slow);
            Start(fast);

            Saw(slow, 3f, seconds: 2f);
            Saw(fast, 20f, seconds: 2f);

            Assert.Greater(fast.Speed, slow.Speed * 3f);
        }

        [Test]
        public void SawingFarTooFast_BindsTheSawAndNeverCuts()
        {
            BowSawGame game = Create();
            Start(game);

            Saw(game, 25f);

            Assert.Greater(game.Binds, 0);
            Assert.Less(game.Progress, 0.3f);
        }

        [Test]
        public void SawingVerySlowly_BarelyCuts()
        {
            BowSawGame slow = Create();
            BowSawGame steady = Create();
            Start(slow);
            Start(steady);

            Saw(slow, 1.5f, seconds: 15f);
            Saw(steady, 8f, seconds: 15f);

            Assert.Less(slow.Progress, steady.Progress * 0.5f);
        }

        [Test]
        public void ReleasingTheMouse_StopsSawing()
        {
            BowSawGame game = Create();
            Start(game);

            Saw(game, 8f, seconds: 5f, held: false);

            Assert.AreEqual(0, game.Strokes);
            Assert.AreEqual(0f, game.Progress, 0.0001f);
        }

        [Test]
        public void ShortWiggles_AreNotCountedAsStrokes()
        {
            BowSawGame game = Create();
            Start(game);

            for (int i = 0; i < 400; i++)
            {
                float y = (i / 10 % 2 == 0 ? 1 : -1) * 0.8f;
                game.Tick(Dt, new MiniInput { PointerX = 0f, PointerY = y, PointerHeld = true });
            }

            Assert.AreEqual(0, game.Strokes);
        }

        [Test]
        public void DoingNothing_RunsOutOfTimeAndScoresZero()
        {
            BowSawGame game = Create();

            for (int i = 0; i < 100000 && !game.Finished; i++)
            {
                game.Tick(Dt, new MiniInput { PointerX = -100f });
            }

            Assert.IsTrue(game.Finished);
            Assert.IsTrue(game.TimedOut);
            Assert.AreEqual(0f, game.Accuracy, 0.0001f);
        }

        [Test]
        public void Events_ReportStrokesStreakAndCompletion()
        {
            BowSawGame game = Create();
            Start(game);

            Saw(game, 8f);

            var kinds = game.Events.Select(e => e.Kind).ToList();
            Assert.Contains(MiniEventKind.PhaseChanged, kinds);
            Assert.Contains(MiniEventKind.Hit, kinds);
            Assert.Contains(MiniEventKind.Good, kinds);
            Assert.Contains(MiniEventKind.Complete, kinds);
        }
    }
}
