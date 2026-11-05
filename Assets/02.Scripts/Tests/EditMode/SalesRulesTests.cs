using System;
using System.Linq;
using Blacksmith.Core.Sales;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class SalesRulesTests
    {
        [Test]
        public void PlanRoute_NoPoints_ReturnsEmpty()
        {
            Assert.IsEmpty(CustomerChoice.PlanRoute(0, 2, 4, new Random(1)));
        }

        [Test]
        public void PlanRoute_StopCountStaysInRange()
        {
            var random = new Random(5);
            for (int i = 0; i < 200; i++)
            {
                int length = CustomerChoice.PlanRoute(6, 3, 5, random).Length;
                Assert.GreaterOrEqual(length, 3);
                Assert.LessOrEqual(length, 5);
            }
        }

        [Test]
        public void PlanRoute_StaysInsidePointRange()
        {
            var random = new Random(9);
            for (int i = 0; i < 200; i++)
            {
                Assert.IsTrue(CustomerChoice.PlanRoute(4, 3, 5, random).All(p => p >= 0 && p < 4));
            }
        }

        [Test]
        public void PlanRoute_NeverRepeatsSamePointInARow()
        {
            var random = new Random(3);
            for (int i = 0; i < 300; i++)
            {
                int[] route = CustomerChoice.PlanRoute(3, 4, 6, random);
                for (int j = 1; j < route.Length; j++)
                {
                    Assert.AreNotEqual(route[j - 1], route[j]);
                }
            }
        }

        [Test]
        public void PlanRoute_SamePointCountOfOne_StillWorks()
        {
            int[] route = CustomerChoice.PlanRoute(1, 2, 3, new Random(2));
            Assert.IsTrue(route.All(p => p == 0));
        }

        [Test]
        public void PlanRoute_SameSeed_SameRoute()
        {
            CollectionAssert.AreEqual(
                CustomerChoice.PlanRoute(5, 3, 5, new Random(11)),
                CustomerChoice.PlanRoute(5, 3, 5, new Random(11)));
        }

        [Test]
        public void WillBuy_ZeroChance_NeverBuys()
        {
            var random = new Random(1);
            for (int i = 0; i < 200; i++)
            {
                Assert.IsFalse(CustomerChoice.WillBuy(0f, random));
            }
        }

        [Test]
        public void WillBuy_FullChance_AlwaysBuys()
        {
            var random = new Random(1);
            for (int i = 0; i < 200; i++)
            {
                Assert.IsTrue(CustomerChoice.WillBuy(1f, random));
            }
        }

        [Test]
        public void WillBuy_HalfChance_MixesBuyersAndBrowsers()
        {
            var random = new Random(8);
            int buyers = 0;
            for (int i = 0; i < 1000; i++)
            {
                if (CustomerChoice.WillBuy(0.5f, random))
                {
                    buyers++;
                }
            }

            Assert.Greater(buyers, 400);
            Assert.Less(buyers, 600);
        }
    }
}
