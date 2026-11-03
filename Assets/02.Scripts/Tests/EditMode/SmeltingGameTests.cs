using System;
using System.Collections.Generic;
using System.Linq;
using Blacksmith.Core.Forge;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class SmeltingGameTests
    {
        private const float Dt = 0.01f;

        // 테스트 전용 수치다. 실제 밸런스는 ScriptableObject 데이터를 따른다.
        private static SmeltingRules CreateRules()
        {
            return new SmeltingRules
            {
                CrackLead = 1.5f, Approach = 1f, PerfectWindow = 0.065f, GoodWindow = 0.13f, WeakWindow = 0.22f, DecoyWindow = 0.12f,
                StrayPenalty = 0.015f, DecoyPenalty = 0.12f, DecoyStartIndex = 3, SplitDelay = 0.9f,
                CrackFirstDistance = 0.2f, CrackMinDistance = 0.45f, CrackMaxDistance = 0.95f, CrackRadiusLimit = 0.8f, CrackMinSeparation = 0.5f,
                BellowsMax = 24f, CursorHalf = 0.09f, RiseAccel = 1.9f, FallAccel = 2.1f, MaxRise = 0.65f, MaxFall = 0.8f, HeatStart = 0.12f,
                ZoneHoldMin = 1f, ZoneHoldRange = 1.4f, MoltenHold = 1f, TimeoutFluidityBase = 0.45f, TimeoutFluidityHeat = 0.3f,
                FluidityLoss = 0.055f, FluidityGrace = 4.5f, FluidityMin = 0.6f,
                PourMax = 40f, FlowMax = 0.34f, TiltDead = 0.12f, TiltEmptyBonus = 0.3f, MoldEdge = 0.3f, SpoutLimit = 2.6f,
                SpoutStart = 1f, MoldMin = -2f, MoldMax = 1f, Reach = 2.6f, OverflowEnd = 1.25f, ColdLimit = 0.2f, HeatLoss = 0.032f,
                ReserveBase = 1.15f, ReserveCrackBonus = 0.3f, DrossBase = 0.05f, DrossCrackPenalty = 0.13f, SpillZero = 0.7f, SlagZero = 0.12f,
                EndDelay = 0.9f, CrackWeight = 0.28f, FluidityWeight = 0.12f, PourWeight = 0.6f,
                Difficulties = new[]
                {
                    new SmeltingDifficulty
                    {
                        WindowScale = 1.25f, HitRadius = 0.21f, GapScale = 1.15f, CrackCount = 15, DecoyChance = 0.2f, DecoyGap = 3,
                        FlameHalfWidth = 0.15f, ZoneSpeed = 0.22f, ZoneHoldDivisor = 1f, HeatGain = 0.21f, HeatDrain = 0.07f,
                        MoldHalfWidth = 0.5f, FillLow = 0.94f, FillHigh = 1.04f, FillTolerance = 0.4f, FlowScale = 1f, HeatLossScale = 1f,
                    },
                    new SmeltingDifficulty
                    {
                        WindowScale = 0.65f, HitRadius = 0.15f, GapScale = 0.68f, CrackCount = 21, DecoyChance = 0.36f, DecoyGap = 1,
                        FlameHalfWidth = 0.095f, ZoneSpeed = 0.4f, ZoneHoldDivisor = 1.6f, HeatGain = 0.16f, HeatDrain = 0.11f,
                        MoldHalfWidth = 0.28f, FillLow = 0.95f, FillHigh = 1.03f, FillTolerance = 0.28f, FlowScale = 1.2f, HeatLossScale = 1.35f,
                    },
                },
            };
        }

        private static SmeltingGame CreateGame(int difficulty = 0, int seed = 1)
        {
            return new SmeltingGame(CreateRules(), seed, difficulty);
        }

        private static MiniInput ClickAt(float x, float y)
        {
            return new MiniInput { PointerPressed = true, PointerX = x, PointerY = y };
        }

        private static void Advance(SmeltingGame game, float seconds)
        {
            for (float elapsed = 0f; elapsed < seconds; elapsed += Dt)
            {
                game.Tick(Dt, default);
            }
        }

        private static void AdvanceUntil(SmeltingGame game, Func<bool> condition, float limit = 120f)
        {
            for (float elapsed = 0f; elapsed < limit && !condition(); elapsed += Dt)
            {
                game.Tick(Dt, default);
            }
        }

        private static List<MiniEvent> Drain(SmeltingGame game)
        {
            var list = new List<MiniEvent>();
            while (game.Events.Count > 0)
            {
                list.Add(game.Events.Dequeue());
            }
            return list;
        }

        // 클릭 시간이 정확히 되도록 균열 시간까지 한 틱 전까지 진행한다.
        private static void AdvanceToCrack(SmeltingGame game, SmeltingGame.Target target, float offset = 0f)
        {
            while (game.ActClock + Dt < target.Time + offset)
            {
                game.Tick(Dt, default);
            }
        }

        private static SmeltingGame.Target FirstVein(SmeltingGame game)
        {
            return game.Targets.First(t => t.Kind == SmeltingGame.TargetKind.Vein);
        }

        // 쪼개기를 봇으로 끝까지 진행한다. 저주 균열은 넘기고, 나머지는 닿는 순간 누른다.
        private static void PlayCrack(SmeltingGame game, float offset = 0f)
        {
            while (game.Act == SmeltingGame.ActKind.Crack)
            {
                MiniInput input = default;
                SmeltingGame.Target due = game.Targets.FirstOrDefault(t =>
                    t.State == SmeltingGame.TargetState.Pending
                    && t.Kind == SmeltingGame.TargetKind.Vein
                    && game.ActClock + Dt >= t.Time + offset);
                if (due != null)
                {
                    input = ClickAt(due.X, due.Y);
                }
                game.Tick(Dt, input);
            }
        }

        // 풀무질을 봇으로 진행한다. 지시선이 불꽃 띠 아래에 있으면 누르고 위에 있으면 뗀다.
        private static void PlayBellows(SmeltingGame game, bool hold = true)
        {
            while (game.Act == SmeltingGame.ActKind.Bellows)
            {
                bool press = hold && game.Cursor + game.CursorV * 0.25f < game.Zone;
                game.Tick(Dt, new MiniInput { ActionHeld = press });
            }
        }

        // 붓기를 봇으로 진행한다. 틀 위로 도가니를 옮기고, 줄기가 틀에 들어갈 만큼만 기울여 눈금까지 채운 뒤 내린다.
        private static void PlayPour(SmeltingGame game)
        {
            while (game.Act == SmeltingGame.ActKind.Pour)
            {
                float target = (game.FillLow + game.FillHigh) * 0.5f - 0.03f;
                bool pour = game.Fill < target;
                float pourAngle = game.PourAngle;
                float tilt = pour ? pourAngle + 0.18f * (1f - pourAngle) : 0f;
                game.Tick(Dt, new MiniInput { PointerHeld = pour, PointerX = game.MoldX, PointerY = tilt });
            }
        }

        private static void Finish(SmeltingGame game)
        {
            AdvanceUntil(game, () => game.Finished);
        }

        // ---------------- ① 쪼개기

        [Test]
        public void Crack_PerfectClickOnVein_RaisesPerfectAndKeepsCombo()
        {
            SmeltingGame game = CreateGame();
            SmeltingGame.Target target = FirstVein(game);
            AdvanceToCrack(game, target);
            game.Tick(Dt, ClickAt(target.X, target.Y));

            MiniEvent hit = Drain(game).First(e => e.Kind == MiniEventKind.Perfect);
            Assert.AreEqual(0, hit.Index);
            Assert.AreEqual(SmeltingGame.TargetState.Hit, target.State);
            Assert.AreEqual(1f, target.Quality);
            Assert.AreEqual(1, game.Combo);
        }

        [Test]
        public void Crack_LateClick_IsWeaker()
        {
            SmeltingGame game = CreateGame();
            SmeltingGame.Target target = FirstVein(game);
            AdvanceToCrack(game, target, 0.18f);
            game.Tick(Dt, ClickAt(target.X, target.Y));

            Assert.AreEqual(SmeltingGame.TargetState.Hit, target.State);
            Assert.Less(target.Quality, 1f);
            Assert.Greater(target.Offset, 0f);
        }

        [Test]
        public void Crack_EarlyClick_HasNoPenalty()
        {
            SmeltingGame game = CreateGame();
            SmeltingGame.Target target = game.Targets[0];
            AdvanceToCrack(game, target, -0.5f);
            game.Tick(Dt, ClickAt(target.X, target.Y));

            Assert.That(Drain(game).Any(e => e.Kind == MiniEventKind.Early));
            Assert.AreEqual(0f, game.StrayPenaltyTotal);
            Assert.AreEqual(SmeltingGame.TargetState.Pending, target.State);
        }

        [Test]
        public void Crack_ClickingEmptySpot_CostsAccuracy()
        {
            SmeltingGame game = CreateGame();
            game.Tick(Dt, ClickAt(-5f, -5f));

            Assert.That(Drain(game).Any(e => e.Kind == MiniEventKind.Swing));
            Assert.Greater(game.StrayPenaltyTotal, 0f);
        }

        [Test]
        public void Crack_UnpressedVein_IsMissedAndBreaksCombo()
        {
            SmeltingGame game = CreateGame();
            SmeltingGame.Target target = FirstVein(game);
            AdvanceUntil(game, () => target.State != SmeltingGame.TargetState.Pending);

            Assert.AreEqual(SmeltingGame.TargetState.Missed, target.State);
            Assert.AreEqual(0, game.Combo);
        }

        [Test]
        public void Crack_StrikingDecoy_IsPenalised()
        {
            SmeltingGame game = CreateGame(1);
            SmeltingGame.Target decoy = game.Targets.First(t => t.Kind == SmeltingGame.TargetKind.Decoy);
            AdvanceToCrack(game, decoy);
            game.Tick(Dt, ClickAt(decoy.X, decoy.Y));

            Assert.AreEqual(SmeltingGame.TargetState.Struck, decoy.State);
            Assert.Greater(game.StrayPenaltyTotal, 0f);
            Assert.That(Drain(game).Any(e => e.Kind == MiniEventKind.Crack));
        }

        [Test]
        public void Crack_IgnoringDecoy_IsAvoidedWithoutPenalty()
        {
            SmeltingGame game = CreateGame(1);
            SmeltingGame.Target decoy = game.Targets.First(t => t.Kind == SmeltingGame.TargetKind.Decoy);
            AdvanceUntil(game, () => decoy.State != SmeltingGame.TargetState.Pending);

            Assert.AreEqual(SmeltingGame.TargetState.Avoided, decoy.State);
            Assert.AreEqual(0f, game.StrayPenaltyTotal);
        }

        [Test]
        public void Crack_PerfectRun_ScoresFullAndMovesToBellows()
        {
            SmeltingGame game = CreateGame();
            PlayCrack(game);

            Assert.AreEqual(SmeltingGame.ActKind.Bellows, game.Act);
            Assert.AreEqual(1f, game.CrackScore, 0.0001f);
            Assert.That(game.Targets.All(t => t.State != SmeltingGame.TargetState.Pending));
        }

        [Test]
        public void Crack_SkippingEverything_StillMovesOnWithZeroScore()
        {
            SmeltingGame game = CreateGame();
            AdvanceUntil(game, () => game.Act != SmeltingGame.ActKind.Crack);

            Assert.AreEqual(SmeltingGame.ActKind.Bellows, game.Act);
            Assert.AreEqual(0f, game.CrackScore);
        }

        // ---------------- ② 풀무질

        [Test]
        public void Bellows_FollowingTheZone_MeltsAndMovesToPour()
        {
            SmeltingGame game = CreateGame();
            PlayCrack(game);
            PlayBellows(game);

            Assert.AreEqual(SmeltingGame.ActKind.Pour, game.Act);
            Assert.GreaterOrEqual(game.Fluidity, 0.6f);
            Assert.Greater(game.InZoneTime, 0f);
        }

        [Test]
        public void Bellows_DoingNothing_TimesOutWithThinnerMetal()
        {
            SmeltingGame game = CreateGame();
            PlayCrack(game);
            PlayBellows(game, false);

            Assert.AreEqual(SmeltingGame.ActKind.Pour, game.Act);
            Assert.Less(game.Fluidity, 0.6f);
            Assert.That(Drain(game).Any(e => e.Kind == MiniEventKind.Molten && e.Index == 1));
        }

        [Test]
        public void Bellows_CursorStaysInsideTheGauge()
        {
            SmeltingGame game = CreateGame();
            PlayCrack(game);
            for (int i = 0; i < 600; i++)
            {
                game.Tick(Dt, new MiniInput { ActionHeld = true });
                Assert.LessOrEqual(game.Cursor, 1f - 0.09f + 0.0001f);
            }
            for (int i = 0; i < 600 && game.Act == SmeltingGame.ActKind.Bellows; i++)
            {
                game.Tick(Dt, default);
                Assert.GreaterOrEqual(game.Cursor, 0.09f - 0.0001f);
            }
        }

        // ---------------- ③ 붓기

        [Test]
        public void Pour_MoldPositionStaysInsideRange()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                SmeltingGame game = CreateGame(0, seed);
                Assert.GreaterOrEqual(game.MoldX, -2f);
                Assert.LessOrEqual(game.MoldX, 1f);
            }
        }

        [Test]
        public void Pour_TiltedOverTheMold_FillsIt()
        {
            SmeltingGame game = ReachPour();
            PlayPour(game);

            Assert.GreaterOrEqual(game.Fill, game.FillLow - 0.02f);
            Assert.Greater(game.FillScore, 0.8f);
            Assert.AreEqual(SmeltingGame.ActKind.Done, game.Act);
        }

        [Test]
        public void Pour_AwayFromTheMold_SpillsInsteadOfFilling()
        {
            SmeltingGame game = ReachPour();
            float far = game.MoldX < 0f ? 2.6f : -2.6f;
            for (int i = 0; i < 1500 && game.Act == SmeltingGame.ActKind.Pour; i++)
            {
                float pourAngle = game.PourAngle;
                game.Tick(Dt, new MiniInput { PointerHeld = true, PointerX = far, PointerY = pourAngle + 0.3f * (1f - pourAngle) });
            }

            Assert.Greater(game.Spilled, 0.1f);
            Assert.Less(game.Fill, 0.3f);
        }

        [Test]
        public void Pour_HoldingTooLong_Overflows()
        {
            SmeltingGame game = ReachPour();
            while (game.Act == SmeltingGame.ActKind.Pour)
            {
                float pourAngle = game.PourAngle;
                game.Tick(Dt, new MiniInput { PointerHeld = true, PointerX = game.MoldX, PointerY = pourAngle + 0.18f * (1f - pourAngle) });
            }

            Assert.IsTrue(game.Overflowed);
            Assert.That(Drain(game).Any(e => e.Kind == MiniEventKind.Overflow));
            Assert.Less(game.FillScore, 1f);
        }

        [Test]
        public void Pour_NotTilting_NothingFlows()
        {
            SmeltingGame game = ReachPour();
            Advance(game, 3f);

            Assert.AreEqual(0f, game.Fill);
            Assert.AreEqual(0f, game.Flow);
        }

        [Test]
        public void Pour_SlowToPour_MetalCoolsAndSolidifies()
        {
            SmeltingGame game = CreateGame(1);
            PlayCrack(game);
            PlayBellows(game, false);
            // 쇳물이 식어 굳을 때까지 조금씩 쏟기만 하며 시간을 보낸다.
            for (int i = 0; i < 5000 && game.Act == SmeltingGame.ActKind.Pour && !game.Clogged; i++)
            {
                float pourAngle = game.PourAngle;
                game.Tick(Dt, new MiniInput { PointerHeld = true, PointerX = 2.5f, PointerY = pourAngle - 0.05f });
            }

            Assert.IsTrue(game.Clogged);
        }

        // ---------------- 전체

        [Test]
        public void FullRun_SkilledPlay_ScoresHigh()
        {
            SmeltingGame game = CreateGame();
            PlayCrack(game);
            PlayBellows(game);
            PlayPour(game);
            Finish(game);

            Assert.IsTrue(game.Finished);
            Assert.Greater(game.Accuracy, 0.8f);
            Assert.That(Drain(game).Any(e => e.Kind == MiniEventKind.Finish));
        }

        [Test]
        public void FullRun_DoingNothing_StillFinishesWithLowScore()
        {
            SmeltingGame game = CreateGame();
            Finish(game);

            Assert.IsTrue(game.Finished);
            Assert.Less(game.Accuracy, 0.3f);
        }

        [Test]
        public void FullRun_SameSeedAndInput_GivesSameResult()
        {
            float first = PlaySkilled(7);
            float second = PlaySkilled(7);

            Assert.AreEqual(first, second);
        }

        [Test]
        public void FullRun_AccuracyAlwaysInRange_AcrossSeedsAndDifficulties()
        {
            for (int difficulty = 0; difficulty < 2; difficulty++)
            {
                for (int seed = 0; seed < 8; seed++)
                {
                    float accuracy = PlaySkilled(seed, difficulty);
                    Assert.GreaterOrEqual(accuracy, 0f);
                    Assert.LessOrEqual(accuracy, 1f);
                }
            }
        }

        [Test]
        public void Complete_IsRaisedBeforeFinish()
        {
            SmeltingGame game = CreateGame();
            PlayCrack(game);
            PlayBellows(game);
            PlayPour(game);
            List<MiniEvent> events = Drain(game);

            Assert.That(events.Any(e => e.Kind == MiniEventKind.Complete));
            Assert.IsFalse(game.Finished);
        }

        [Test]
        public void Difficulty_OutOfRange_IsClamped()
        {
            Assert.AreEqual(1, CreateGame(9).Difficulty);
            Assert.AreEqual(0, CreateGame(-3).Difficulty);
        }

        [Test]
        public void Difficulty_HigherGradeHasMoreCracksAndNarrowerMold()
        {
            SmeltingGame easy = CreateGame(0);
            SmeltingGame hard = CreateGame(1);

            Assert.Greater(hard.Targets.Count, easy.Targets.Count);
            Assert.Less(hard.MoldHalfWidth, easy.MoldHalfWidth);
        }

        private static SmeltingGame ReachPour(int difficulty = 0, int seed = 1)
        {
            SmeltingGame game = CreateGame(difficulty, seed);
            PlayCrack(game);
            PlayBellows(game);
            return game;
        }

        private static float PlaySkilled(int seed, int difficulty = 0)
        {
            SmeltingGame game = CreateGame(difficulty, seed);
            PlayCrack(game);
            PlayBellows(game);
            PlayPour(game);
            Finish(game);
            return game.Accuracy;
        }
    }
}
