using Blacksmith.Core.Adventurers;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class FallenAdventurerTests
    {
        [Test]
        public void Create_ClearsEquipmentAndKeepsAdventurer()
        {
            var adventurer = new AdventurerData
            {
                AdventurerId = 3,
                Name = "모험가",
                Level = 4,
                WeaponId = 10,
                WeaponGrade = 2,
                HelmetId = 11,
                HelmetGrade = 3,
                ArmorId = 12,
                ArmorGrade = 4,
                AccessoryId = 13,
                AccessoryGrade = 5,
                Injury = 95,
            };

            FallenAdventurer fallen = FallenAdventurer.Create(adventurer, 7, 2, 1);

            Assert.AreEqual(3, fallen.Adventurer.AdventurerId);
            Assert.AreEqual("모험가", fallen.Adventurer.Name);
            Assert.AreEqual(4, fallen.Adventurer.Level);
            Assert.AreEqual(95, fallen.Adventurer.Injury);
            Assert.AreEqual(7, fallen.Day);
            Assert.AreEqual(2, fallen.DungeonId);
            Assert.AreEqual(1, fallen.Stage);
            Assert.AreEqual(AdventurerData.NoEquipment, fallen.Adventurer.WeaponId);
            Assert.AreEqual(AdventurerData.NoEquipment, fallen.Adventurer.HelmetId);
            Assert.AreEqual(AdventurerData.NoEquipment, fallen.Adventurer.ArmorId);
            Assert.AreEqual(AdventurerData.NoEquipment, fallen.Adventurer.AccessoryId);
            Assert.AreEqual(0, fallen.Adventurer.WeaponGrade + fallen.Adventurer.HelmetGrade
                + fallen.Adventurer.ArmorGrade + fallen.Adventurer.AccessoryGrade);
        }
    }
}
