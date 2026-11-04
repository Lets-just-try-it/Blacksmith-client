using Blacksmith.Core;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class DayCycleRulesTests
    {
        [Test]
        public void Day_StaysUntilTimeIsOver()
        {
            Assert.AreEqual(DayPhase.Day, DayCycleRules.Next(DayPhase.Day, false, false));
            Assert.AreEqual(DayPhase.Day, DayCycleRules.Next(DayPhase.Day, false, true));
        }

        [Test]
        public void DayTimeOver_WithoutWork_StartsNightTransition()
        {
            Assert.AreEqual(DayPhase.Dusk, DayCycleRules.Next(DayPhase.Day, true, false));
        }

        [Test]
        public void DayTimeOver_WithWork_WaitsForWorkToFinish()
        {
            Assert.AreEqual(DayPhase.Closing, DayCycleRules.Next(DayPhase.Day, true, true));
            Assert.AreEqual(DayPhase.Closing, DayCycleRules.Next(DayPhase.Closing, true, true));
            Assert.AreEqual(DayPhase.Dusk, DayCycleRules.Next(DayPhase.Closing, true, false));
        }

        [Test]
        public void NightTransition_BecomesNightAfterItsTime()
        {
            Assert.AreEqual(DayPhase.Dusk, DayCycleRules.Next(DayPhase.Dusk, false, false));
            Assert.AreEqual(DayPhase.Night, DayCycleRules.Next(DayPhase.Dusk, true, false));
        }

        [Test]
        public void NightLook_StartsWithTransition()
        {
            Assert.IsFalse(DayCycleRules.IsNightLook(DayPhase.Day));
            Assert.IsFalse(DayCycleRules.IsNightLook(DayPhase.Closing));
            Assert.IsTrue(DayCycleRules.IsNightLook(DayPhase.Dusk));
            Assert.IsTrue(DayCycleRules.IsNightLook(DayPhase.Night));
        }

        [Test]
        public void Night_DoesNotEndByTime()
        {
            Assert.AreEqual(DayPhase.Night, DayCycleRules.Next(DayPhase.Night, true, false));
        }

        [Test]
        public void Work_StartsOnlyDuringDay()
        {
            Assert.IsTrue(DayCycleRules.CanStartWork(DayPhase.Day));
            Assert.IsFalse(DayCycleRules.CanStartWork(DayPhase.Closing));
            Assert.IsFalse(DayCycleRules.CanStartWork(DayPhase.Dusk));
            Assert.IsFalse(DayCycleRules.CanStartWork(DayPhase.Night));
        }

        [Test]
        public void Clock_RunsFromStartToEndHour()
        {
            // 낮 600초를 10시~20시로 두면 실제 1초가 게임 1분이다.
            Assert.AreEqual(10 * 60, DayCycleRules.GetClockMinutes(0f, 600f, 10, 20));
            Assert.AreEqual(10 * 60 + 10, DayCycleRules.GetClockMinutes(10f, 600f, 10, 20));
            Assert.AreEqual(15 * 60, DayCycleRules.GetClockMinutes(300f, 600f, 10, 20));
            Assert.AreEqual(20 * 60, DayCycleRules.GetClockMinutes(600f, 600f, 10, 20));
        }

        [Test]
        public void Clock_StaysInsideDay()
        {
            Assert.AreEqual(10 * 60, DayCycleRules.GetClockMinutes(-5f, 600f, 10, 20));
            Assert.AreEqual(20 * 60, DayCycleRules.GetClockMinutes(900f, 600f, 10, 20));
        }

        [Test]
        public void NextDay_StartsOnlyFromNight()
        {
            Assert.IsFalse(DayCycleRules.CanStartNextDay(DayPhase.Day));
            Assert.IsFalse(DayCycleRules.CanStartNextDay(DayPhase.Closing));
            Assert.IsFalse(DayCycleRules.CanStartNextDay(DayPhase.Dusk));
            Assert.IsTrue(DayCycleRules.CanStartNextDay(DayPhase.Night));
        }
    }
}
