using System;
using System.Linq;
using Blacksmith.Core.Forge;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class BowStringGameTests
    {
        private const float Dt = 0.01f;

        // 테스트 전용 수치다. 실제 값은 BowStringRules 데이터를 따른다.
        private static BowStringRules CreateRules()
        {
            return new BowStringRules
            {
                IntroTime = 1f,
                EndDelay = 1f,
                MinRadius = 0.8f,
                CenterFollow = 1.2f,
                FlexPerCell = 0.3f,
                SlidePerCell = 0.11f,
                SlipRate = 0.5f,
                WrapWeight = 0.6f,
                SuccessScore = 0.6f,
                Difficulties = new[]
                {
                    new BowStringDifficulty { Wraps = 3, SpeedLo = 3.5f, SpeedHi = 9f, SlipSpeed = 14f, TimeLimit = 50f, WindowWidth = 0.22f, WindowDrift = 0.06f, ParTime = 6f },
                    new BowStringDifficulty { Wraps = 4, SpeedLo = 4f, SpeedHi = 8f, SlipSpeed = 13f, TimeLimit = 50f, WindowWidth = 0.17f, WindowDrift = 0.1f, ParTime = 5f },
                    new BowStringDifficulty { Wraps = 5, SpeedLo = 4.5f, SpeedHi = 7.5f, SlipSpeed = 12f, TimeLimit = 50f, WindowWidth = 0.13f, WindowDrift = 0.14f, ParTime = 4.5f },
                },
            };
        }

        private static BowStringGame Create(int seed = 1, int difficulty = 0)
        {
            return new BowStringGame(CreateRules(), seed, difficulty);
        }

        private static void Start(BowStringGame game)
        {
            for (int i = 0; i < 300 && game.Phase != BowStringGame.PhaseKind.Wrap; i++)
            {
                game.Tick(Dt, new MiniInput { PointerX = -100f });
            }
        }

        // 반지름 2칸의 원을 시계 방향으로 omega(rad/초)로 돌며 wraps바퀴만큼 감는다. 가운데는 (cx, cy)다.
        private static void Circle(BowStringGame game, float omega, int wraps, float cx = 0f, float cy = 0f, bool clockwise = true)
        {
            float angle = 0f;
            int ticks = (int)Math.Ceiling(wraps * 2f * Math.PI / omega / Dt * 1.15) + 30;
            for (int i = 0; i < ticks && game.Phase == BowStringGame.PhaseKind.Wrap; i++)
            {
                angle += (clockwise ? 1f : -1f) * omega * Dt;
                game.Tick(Dt, new MiniInput { PointerX = cx + 2f * (float)Math.Cos(angle), PointerY = cy + 2f * (float)Math.Sin(angle), PointerHeld = true });
            }
        }

        private static void Release(BowStringGame game, int ticks = 5)
        {
            for (int i = 0; i < ticks; i++)
            {
                game.Tick(Dt, new MiniInput { PointerX = 0f, PointerY = 0f, PointerHeld = false });
            }
        }

        // 한 끝을 건다: 누르고 활을 알맞은 구간(창 가운데를 따라 움직이며)으로 휘고 그 상태에서 옆으로 끌어 고리를 민다.
        private static void HookEnd(BowStringGame game, bool overFlex = false)
        {
            Release(game);
            int end = game.FlexIndex;
            float x = 0f;
            float pressY = 0f;
            game.Tick(Dt, new MiniInput { PointerX = x, PointerY = pressY, PointerHeld = true });
            for (int i = 0; i < 20000 && game.FlexIndex == end && game.Phase == BowStringGame.PhaseKind.Flex; i++)
            {
                float flex = overFlex ? 1f : game.WindowCenter;
                float y = pressY + flex / 0.3f;
                x += game.SlideDirection * 0.04f;
                game.Tick(Dt, new MiniInput { PointerX = x, PointerY = y, PointerHeld = true });
            }

            Release(game);
        }

        [Test]
        public void CirclingAtASteadySpeed_WrapsTheSinewAndMovesToTheHookPhase()
        {
            BowStringGame game = Create();
            Start(game);

            Circle(game, 6f, 3);

            Assert.AreEqual(3, game.WrapsDone);
            Assert.AreEqual(BowStringGame.PhaseKind.Flex, game.Phase);
            Assert.AreEqual(1f, game.WrapScore, 0.0001f);
        }

        [Test]
        public void CirclingTheWrongWay_DoesNotWrap()
        {
            BowStringGame game = Create();
            Start(game);

            Circle(game, 6f, 3, clockwise: false);

            Assert.AreEqual(0, game.WrapsDone);
            Assert.AreEqual(BowStringGame.PhaseKind.Wrap, game.Phase);
        }

        [Test]
        public void CirclingFarTooFast_SlipsAndDoesNotCount()
        {
            BowStringGame game = Create();
            Start(game);

            Circle(game, 20f, 2);

            Assert.Greater(game.Slips, 0);
            Assert.AreEqual(0, game.WrapsDone);
        }

        [Test]
        public void CirclingSlowly_WrapsLooselyForLessScore()
        {
            BowStringGame steady = Create();
            BowStringGame slow = Create();
            Start(steady);
            Start(slow);

            Circle(steady, 6f, 3);
            Circle(slow, 2f, 3);

            Assert.AreEqual(3, slow.WrapsDone);
            Assert.Less(slow.WrapScore, steady.WrapScore);
        }

        [Test]
        public void TinyWiggles_AreNotCountedAsCircles()
        {
            BowStringGame game = Create();
            Start(game);

            for (int i = 0; i < 500; i++)
            {
                float a = i * 0.2f;
                game.Tick(Dt, new MiniInput { PointerX = 0.3f * (float)Math.Cos(a), PointerY = 0.3f * (float)Math.Sin(a), PointerHeld = true });
            }

            Assert.AreEqual(0, game.WrapsDone);
        }

        [Test]
        public void CirclingAroundAnyCenter_Works()
        {
            BowStringGame game = Create();
            Start(game);

            Circle(game, 6f, 3, cx: -30f, cy: 20f);

            Assert.AreEqual(3, game.WrapsDone);
        }

        [Test]
        public void DraggingDown_FlexesTheBowByTheDraggedDistance()
        {
            BowStringGame game = Create();
            Start(game);
            Circle(game, 6f, 3);
            Release(game);

            game.Tick(Dt, new MiniInput { PointerX = 0f, PointerY = 0f, PointerHeld = true });
            game.Tick(Dt, new MiniInput { PointerX = 0f, PointerY = 2f, PointerHeld = true });

            Assert.AreEqual(0.6f, game.Flex, 0.02f);
        }

        [Test]
        public void SlidingWhileInsideTheWindow_MovesTheLoopTowardTheNotch()
        {
            BowStringGame game = Create();
            Start(game);
            Circle(game, 6f, 3);
            Release(game);
            float y = game.WindowCenter / 0.3f;

            game.Tick(Dt, new MiniInput { PointerX = 0f, PointerY = 0f, PointerHeld = true });
            game.Tick(Dt, new MiniInput { PointerX = 0f, PointerY = y, PointerHeld = true });
            Assert.IsTrue(game.InWindow);
            float before = game.Slide;
            game.Tick(Dt, new MiniInput { PointerX = game.SlideDirection * 2f, PointerY = y, PointerHeld = true });

            Assert.Greater(game.Slide, before + 0.15f);
        }

        [Test]
        public void SlidingWithoutFlexing_DoesNotMoveTheLoop()
        {
            BowStringGame game = Create();
            Start(game);
            Circle(game, 6f, 3);
            Release(game);

            game.Tick(Dt, new MiniInput { PointerX = 0f, PointerY = 0f, PointerHeld = true });
            game.Tick(Dt, new MiniInput { PointerX = game.SlideDirection * 3f, PointerY = 0f, PointerHeld = true });

            Assert.AreEqual(0f, game.Slide, 0.0001f);
        }

        [Test]
        public void OverFlexing_SlipsTheLoopBackAndCountsASlip()
        {
            BowStringGame game = Create();
            Start(game);
            Circle(game, 6f, 3);
            Release(game);
            float y = game.WindowCenter / 0.3f;
            game.Tick(Dt, new MiniInput { PointerX = 0f, PointerY = 0f, PointerHeld = true });
            game.Tick(Dt, new MiniInput { PointerX = 0f, PointerY = y, PointerHeld = true });
            game.Tick(Dt, new MiniInput { PointerX = game.SlideDirection * 3f, PointerY = y, PointerHeld = true });
            float slid = game.Slide;

            for (int i = 0; i < 50; i++)
            {
                game.Tick(Dt, new MiniInput { PointerX = game.SlideDirection * 3f, PointerY = 3.3f, PointerHeld = true });
            }

            Assert.IsTrue(game.OverFlexed);
            Assert.Less(game.Slide, slid);
            Assert.AreEqual(1, game.HookSlips);
        }

        [Test]
        public void HookingBothEndsCleanly_ScoresFullAndFinishes()
        {
            BowStringGame game = Create();
            Start(game);
            Circle(game, 6f, 3);

            HookEnd(game);
            Assert.AreEqual(1, game.Hooked);
            HookEnd(game);

            for (int i = 0; i < 300 && !game.Finished; i++)
            {
                game.Tick(Dt, new MiniInput { PointerX = -100f });
            }

            Assert.AreEqual(2, game.Hooked);
            Assert.IsTrue(game.Finished);
            Assert.GreaterOrEqual(game.Accuracy, 0.9f);
            Assert.Contains(MiniEventKind.Good, game.Events.Select(e => e.Kind).ToList());
        }

        [Test]
        public void SlippingDuringTheHook_LowersTheHookScore()
        {
            BowStringGame clean = Create();
            BowStringGame sloppy = Create();
            Start(clean);
            Start(sloppy);
            Circle(clean, 6f, 3);
            Circle(sloppy, 6f, 3);

            HookEnd(clean);
            // 일부러 과하게 휘어 미끄러진 뒤 제대로 건다.
            Release(sloppy);
            sloppy.Tick(Dt, new MiniInput { PointerX = 0f, PointerY = 0f, PointerHeld = true });
            for (int i = 0; i < 30; i++)
            {
                sloppy.Tick(Dt, new MiniInput { PointerX = 0f, PointerY = 3.3f, PointerHeld = true });
            }

            HookEnd(sloppy);

            Assert.AreEqual(1, sloppy.Hooked);
            Assert.Less(sloppy.FlexScore, clean.FlexScore);
        }

        [Test]
        public void DoingNothing_RunsOutOfTimeAndScoresZero()
        {
            BowStringGame game = Create();

            for (int i = 0; i < 100000 && !game.Finished; i++)
            {
                game.Tick(Dt, new MiniInput { PointerX = -100f });
            }

            Assert.IsTrue(game.Finished);
            Assert.IsTrue(game.TimedOut);
            Assert.AreEqual(0f, game.Accuracy, 0.0001f);
        }

        [Test]
        public void SameSeed_GivesSameWindows_AndHarderDifficultyNeedsMoreWraps()
        {
            BowStringGame first = Create(seed: 7, difficulty: 2);
            BowStringGame second = Create(seed: 7, difficulty: 2);
            BowStringGame easy = Create(difficulty: 0);

            Assert.AreEqual(first.WindowCenter, second.WindowCenter);
            Assert.Greater(first.Wraps, easy.Wraps);
            Assert.Less(first.WindowHi - first.WindowLo, easy.WindowHi - easy.WindowLo);
        }
    }
}
