using System.Collections.Generic;
using System.Linq;
using Blacksmith.Core.Forge;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class PolishGameTests
    {
        private const float Dt = 0.01f;

        // 테스트 전용 수치다. 실제 밸런스는 ScriptableObject 데이터를 따른다.
        private static PolishRules CreateRules()
        {
            return new PolishRules
            {
                IntroTime = 0.9f, EndDelay = 0.8f,
                SegmentGoal = 0.9f, GrindRate = 0.5f, HardRate = 0.55f, ContactRadius = 0.07f,
                AngleAmplitude = 0.12f, AngleSpeedMin = 0.6f, AngleSpeedMax = 1.1f,
                HeatUp = 0.3f, OffAngleHeat = 1.2f, HeatCool = 0.12f, DipCool = 0.7f,
                BurnHeat = 1f, ResumeHeat = 0.5f, WarnHeat = 0.7f, BurnPenalty = 0.07f, BurnSharpLoss = 0.25f,
                ThinLossPerSecond = 0.04f, MaxThin = 0.25f,
                InitialMin = 0.05f, InitialMax = 0.45f, PriorThreshold = 0.5f, PriorDrop = 0.15f,
                ComboBonusPer = 0.008f, ComboBonusMax = 0.06f, SuccessScore = 0.6f,
                Difficulties = new[]
                {
                    new PolishDifficulty { AngleTolerance = 0.07f, TimeLimit = 40f, HeatScale = 1f, HardChance = 0.1f },
                    new PolishDifficulty { AngleTolerance = 0.05f, TimeLimit = 30f, HeatScale = 1.3f, HardChance = 0.4f },
                },
            };
        }

        private static PolishGame CreateGame(int difficulty = 0, int seed = 1, float prior = 0.8f)
        {
            return new PolishGame(CreateRules(), seed, difficulty, prior);
        }

        private static MiniInput At(float x, float y, bool held, bool dip = false)
        {
            return new MiniInput { PointerX = x, PointerY = y, PointerHeld = held, ActionHeld = dip };
        }

        private static void StartGrind(PolishGame game)
        {
            Advance(game, 1f);
            Assert.AreEqual(PolishGame.PhaseKind.Grind, game.Phase);
        }

        private static void Advance(PolishGame game, float seconds)
        {
            for (float elapsed = 0f; elapsed < seconds; elapsed += Dt)
            {
                game.Tick(Dt, default);
            }
        }

        // 알맞은 각도를 따라가며 x 위치에서 seconds초 동안 누른다. 앞 틱의 알맞은 각도를 쓰므로 허용 폭 안에 든다.
        private static void Grind(PolishGame game, float x, float seconds, float angleOffset = 0f)
        {
            for (float elapsed = 0f; elapsed < seconds; elapsed += Dt)
            {
                game.Tick(Dt, At(x, game.TargetAngle + angleOffset, true));
            }
        }

        private static List<MiniEvent> Drain(PolishGame game)
        {
            var list = new List<MiniEvent>();
            while (game.Events.Count > 0)
            {
                list.Add(game.Events.Dequeue());
            }
            return list;
        }

        // 가장 덜 간 칸으로 가서 각도를 맞춰 갈고, 뜨거워지면 물에 담가 식히는 봇
        private sealed class Bot
        {
            private bool _cooling;

            public void Step(PolishGame game)
            {
                if (game.Heat > 0.75f)
                {
                    _cooling = true;
                }
                else if (game.Heat < 0.35f)
                {
                    _cooling = false;
                }

                int target = 0;
                for (int i = 0; i < PolishGame.SegmentCount; i++)
                {
                    if (game.Sharp[i] < game.SegmentGoal)
                    {
                        target = i;
                        break;
                    }
                }

                float x = PolishGame.SegmentCenter(target);
                game.Tick(Dt, At(x, game.TargetAngle, !_cooling, _cooling));
            }
        }

        private static void PlayToEnd(PolishGame game, Bot bot, float limitSeconds = 80f)
        {
            for (float elapsed = 0f; elapsed < limitSeconds && !game.Finished; elapsed += Dt)
            {
                bot.Step(game);
            }
        }

        [Test]
        public void SameSeed_SameLayoutAndAngle()
        {
            PolishGame a = CreateGame(0, 7);
            PolishGame b = CreateGame(0, 7);
            CollectionAssert.AreEqual(a.Sharp, b.Sharp);
            CollectionAssert.AreEqual(a.Hard, b.Hard);

            StartGrind(a);
            StartGrind(b);
            Advance(a, 3f);
            Advance(b, 3f);
            Assert.AreEqual(a.TargetAngle, b.TargetAngle);

            PolishGame other = CreateGame(0, 8);
            CollectionAssert.AreNotEqual(a.Sharp, other.Sharp);
        }

        [Test]
        public void StartingSharp_IsPartialAndBelowGoal()
        {
            PolishGame game = CreateGame();
            Assert.IsTrue(game.Sharp.All(sharp => sharp >= 0f && sharp < game.SegmentGoal));
            Assert.Less(game.Clean, 0.6f);
            Assert.AreEqual(0, game.DoneCount);
        }

        [Test]
        public void LowPrior_StartsLessSharp()
        {
            float lowTotal = 0f;
            float highTotal = 0f;
            for (int seed = 0; seed < 20; seed++)
            {
                lowTotal += CreateGame(0, seed, 0.2f).Clean;
                highTotal += CreateGame(0, seed, 0.9f).Clean;
            }
            Assert.Less(lowTotal, highTotal);
        }

        [Test]
        public void HigherDifficulty_HasMoreHardSegments()
        {
            int easy = 0;
            int hard = 0;
            for (int seed = 0; seed < 20; seed++)
            {
                easy += CreateGame(0, seed).Hard.Count(h => h);
                hard += CreateGame(1, seed).Hard.Count(h => h);
            }
            Assert.Greater(hard, easy);
            Assert.AreEqual(1, CreateGame(5).Difficulty);
        }

        [Test]
        public void Intro_ThenGrindPhaseWithEvent()
        {
            PolishGame game = CreateGame();
            Advance(game, 0.5f);
            Assert.AreEqual(PolishGame.PhaseKind.Intro, game.Phase);
            Advance(game, 0.5f);
            Assert.AreEqual(PolishGame.PhaseKind.Grind, game.Phase);
            Assert.IsTrue(Drain(game).Any(e => e.Kind == MiniEventKind.PhaseChanged));
        }

        [Test]
        public void TargetAngle_MovesWithinAmplitude()
        {
            PolishGame game = CreateGame();
            StartGrind(game);
            float min = float.MaxValue;
            float max = float.MinValue;
            for (int i = 0; i < 1500; i++)
            {
                game.Tick(Dt, default);
                min = System.Math.Min(min, game.TargetAngle);
                max = System.Math.Max(max, game.TargetAngle);
            }
            Assert.GreaterOrEqual(min, -0.121f);
            Assert.LessOrEqual(max, 0.121f);
            Assert.Greater(max - min, 0.1f);
        }

        [Test]
        public void Grinding_RaisesSharpOnlyUnderContact()
        {
            PolishGame game = CreateGame();
            StartGrind(game);
            float[] before = (float[])game.Sharp.Clone();
            Grind(game, PolishGame.SegmentCenter(2), 0.5f);
            Assert.Greater(game.Sharp[2], before[2]);
            Assert.AreEqual(before[20], game.Sharp[20]);
            Assert.IsTrue(game.Grinding);
        }

        [Test]
        public void NoPress_NoGrinding()
        {
            PolishGame game = CreateGame();
            StartGrind(game);
            float before = game.Sharp[5];
            for (int i = 0; i < 100; i++)
            {
                game.Tick(Dt, At(PolishGame.SegmentCenter(5), game.TargetAngle, false));
            }
            Assert.AreEqual(before, game.Sharp[5]);
            Assert.IsFalse(game.Grinding);
        }

        [Test]
        public void PointerOffscreen_DoesNotGrind()
        {
            PolishGame game = CreateGame();
            StartGrind(game);
            float before = game.Clean;
            for (int i = 0; i < 100; i++)
            {
                game.Tick(Dt, new MiniInput { PointerX = -100f, PointerHeld = true });
            }
            Assert.AreEqual(before, game.Clean);
        }

        [Test]
        public void OffAngle_GrindsLessAndHeatsFaster()
        {
            PolishGame good = CreateGame();
            PolishGame bad = CreateGame();
            StartGrind(good);
            StartGrind(bad);
            Grind(good, PolishGame.SegmentCenter(4), 1f);
            Grind(bad, PolishGame.SegmentCenter(4), 1f, 0.2f);
            Assert.AreEqual(1f, good.Quality);
            Assert.AreEqual(0f, bad.Quality);
            Assert.Greater(good.Sharp[4], bad.Sharp[4]);
            Assert.Greater(bad.Heat, good.Heat);
        }

        [Test]
        public void Quality_FallsGraduallyOutsideTolerance()
        {
            PolishGame game = CreateGame();
            StartGrind(game);
            game.Tick(Dt, At(0f, game.TargetAngle + game.AngleTolerance * 1.5f, true));
            Assert.AreEqual(0.5f, game.Quality, 0.05f);
        }

        [Test]
        public void Heat_RisesWhileGrindingAndFallsWhenReleased()
        {
            PolishGame game = CreateGame();
            StartGrind(game);
            Grind(game, PolishGame.SegmentCenter(0), 1f);
            float hot = game.Heat;
            Assert.Greater(hot, 0.2f);
            for (int i = 0; i < 100; i++)
            {
                game.Tick(Dt, At(0f, game.TargetAngle, false));
            }
            Assert.Less(game.Heat, hot);
        }

        [Test]
        public void Dipping_CoolsFasterAndStopsGrinding()
        {
            PolishGame game = CreateGame();
            StartGrind(game);
            Grind(game, PolishGame.SegmentCenter(0), 3f);
            float hot = game.Heat;
            float sharp = game.Sharp[0];
            Assert.Greater(hot, 0.8f);
            for (int i = 0; i < 50; i++)
            {
                game.Tick(Dt, At(PolishGame.SegmentCenter(0), game.TargetAngle, true, true));
            }
            Assert.IsTrue(game.Dipping);
            Assert.IsFalse(game.Grinding);
            Assert.AreEqual(sharp, game.Sharp[0]);
            Assert.AreEqual(hot - 0.35f, game.Heat, 0.02f);
            Assert.AreEqual(1, game.Dips);
            Assert.IsTrue(Drain(game).Any(e => e.Kind == MiniEventKind.Hit));
        }

        [Test]
        public void Overheating_BurnsLocksAndResumesAfterCooling()
        {
            PolishGame game = CreateGame();
            StartGrind(game);
            Drain(game);
            Grind(game, PolishGame.SegmentCenter(10), 5f, 0.2f);
            Assert.AreEqual(1, game.Burned);
            Assert.IsTrue(game.Locked);
            Assert.IsTrue(game.Burnt[10]);
            Assert.IsTrue(Drain(game).Any(e => e.Kind == MiniEventKind.Hazard));

            float sharp = game.Sharp[10];
            Grind(game, PolishGame.SegmentCenter(10), 0.2f);
            Assert.AreEqual(sharp, game.Sharp[10]);

            for (int i = 0; i < 400 && game.Locked; i++)
            {
                game.Tick(Dt, At(0f, 0f, false, true));
            }
            Assert.IsFalse(game.Locked);
            Grind(game, PolishGame.SegmentCenter(10), 0.3f);
            Assert.IsTrue(game.Grinding);
        }

        [Test]
        public void Burn_CutsComboAndScore()
        {
            PolishGame game = CreateGame();
            StartGrind(game);
            var bot = new Bot();
            for (int i = 0; i < 600; i++)
            {
                bot.Step(game);
            }
            Assert.Greater(game.Combo, 0);
            Assert.AreEqual(0f, game.BurnLoss);
            float score = game.ProjectedScore;
            Grind(game, PolishGame.SegmentCenter(20), 6f, 0.2f);
            Assert.AreEqual(0, game.Combo);
            Assert.GreaterOrEqual(game.BestCombo, 1);
            Assert.Greater(game.BurnLoss, 0f);
            Assert.Less(game.ProjectedScore, score + game.BurnLoss);
        }

        [Test]
        public void SegmentReachingGoal_RaisesGoodWithCombo()
        {
            PolishGame game = CreateGame();
            StartGrind(game);
            Drain(game);
            Grind(game, PolishGame.SegmentCenter(0), 2.2f);
            Assert.IsTrue(game.Sharp[0] >= game.SegmentGoal || game.Burned > 0);
            MiniEvent good = Drain(game).FirstOrDefault(e => e.Kind == MiniEventKind.Good);
            Assert.AreEqual(MiniEventKind.Good, good.Kind);
            Assert.AreEqual(1f, good.Value);
        }

        [Test]
        public void GrindingFinishedSegments_ThinsTheBlade()
        {
            PolishGame game = CreateGame();
            StartGrind(game);
            var bot = new Bot();
            for (int i = 0; i < 600; i++)
            {
                bot.Step(game);
            }
            Assert.Greater(game.DoneCount, 0);
            int done = Enumerable.Range(0, PolishGame.SegmentCount).First(i => game.Sharp[i] >= game.SegmentGoal);
            float thin = game.ThinLoss;
            for (int i = 0; i < 100; i++)
            {
                game.Tick(Dt, At(PolishGame.SegmentCenter(done), game.TargetAngle, true));
                if (game.Locked)
                {
                    break;
                }
            }
            Assert.Greater(game.ThinLoss, thin);
        }

        [Test]
        public void SkilledPlayer_FinishesWithHighScore()
        {
            PolishGame game = CreateGame();
            StartGrind(game);
            PlayToEnd(game, new Bot());
            Assert.AreEqual(PolishGame.SegmentCount, game.DoneCount);
            Assert.AreEqual(0, game.Burned);
            Assert.GreaterOrEqual(game.Accuracy, 0.9f);
            Assert.IsTrue(game.Finished);
        }

        [Test]
        public void IdlePlayer_TimesOutWithLowScore()
        {
            PolishGame game = CreateGame();
            Advance(game, 60f);
            Assert.IsTrue(game.Finished);
            Assert.AreEqual(0, game.DoneCount);
            Assert.Less(game.Accuracy, game.SuccessScore);
        }

        [Test]
        public void End_CompleteThenFinishAfterDelay()
        {
            PolishGame game = CreateGame();
            StartGrind(game);
            Drain(game);
            for (int i = 0; i < 5000 && game.Phase != PolishGame.PhaseKind.Done; i++)
            {
                game.Tick(Dt, default);
            }
            List<MiniEvent> events = Drain(game);
            Assert.IsTrue(events.Any(e => e.Kind == MiniEventKind.Complete));
            Assert.IsFalse(game.Finished);
            Advance(game, 1f);
            Assert.IsTrue(game.Finished);
            Assert.IsTrue(Drain(game).Any(e => e.Kind == MiniEventKind.Finish));
        }

        [Test]
        public void SameInputs_SameResult()
        {
            float Play()
            {
                PolishGame game = CreateGame(0, 3);
                StartGrind(game);
                PlayToEnd(game, new Bot());
                return game.Accuracy;
            }

            Assert.AreEqual(Play(), Play());
        }

        [Test]
        public void SegmentHelpers_MapPositions()
        {
            Assert.AreEqual(0, PolishGame.SegmentAt(-1f));
            Assert.AreEqual(PolishGame.SegmentCount - 1, PolishGame.SegmentAt(1f));
            Assert.AreEqual(PolishGame.SegmentCount - 1, PolishGame.SegmentAt(5f));
            Assert.AreEqual(12, PolishGame.SegmentAt(PolishGame.SegmentCenter(12)));
            Assert.AreEqual(0f, PolishGame.SegmentCenter(11) + PolishGame.SegmentCenter(12), 0.0001f);
        }
    }
}
