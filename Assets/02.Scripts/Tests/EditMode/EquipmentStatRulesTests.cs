using Blacksmith.Core.Adventurers;
using Blacksmith.Core.Battle;
using Blacksmith.Core.Inventory;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class EquipmentStatRulesTests
    {
        private const int Sword = 3;
        private const int Helmet = 4;
        private static readonly int[] GradePercents = { 60, 80, 100, 120, 150 };

        private static AdventurerStats BonusOf(ItemStack item)
        {
            AdventurerStats itemBonus = item.ItemId == Sword
                ? new AdventurerStats { Attack = 10 }
                : new AdventurerStats { MaxHp = 20, Defense = 3 };
            return EquipmentStatRules.GetItemBonus(itemBonus, item.Grade, GradePercents);
        }

        private static AdventurerData CreateAdventurer()
        {
            return new AdventurerData
            {
                AdventurerId = 1,
                Level = 1,
                BaseStats = new AdventurerStats { MaxHp = 400, Attack = 40, Defense = 6 },
                WeaponId = Sword,
                WeaponGrade = 5,
                HelmetId = Helmet,
                HelmetGrade = 1,
            };
        }

        [Test]
        public void GetItemBonus_MultipliesByGradePercent()
        {
            var itemBonus = new AdventurerStats { MaxHp = 20, Attack = 10, Defense = 3 };

            AdventurerStats bonus = EquipmentStatRules.GetItemBonus(itemBonus, 5, GradePercents);

            Assert.AreEqual(30, bonus.MaxHp);
            Assert.AreEqual(15, bonus.Attack);
            Assert.AreEqual(4, bonus.Defense);
        }

        [Test]
        public void GetItemBonus_GradeOutsideTable_UsesFullBonus()
        {
            var itemBonus = new AdventurerStats { Attack = 10 };

            Assert.AreEqual(10, EquipmentStatRules.GetItemBonus(itemBonus, 0, GradePercents).Attack);
            Assert.AreEqual(10, EquipmentStatRules.GetItemBonus(itemBonus, 9, GradePercents).Attack);
        }

        [Test]
        public void GetEquipmentBonus_SumsEquippedItems()
        {
            AdventurerStats bonus = EquipmentStatRules.GetEquipmentBonus(CreateAdventurer(), BonusOf);

            Assert.AreEqual(12, bonus.MaxHp);
            Assert.AreEqual(15, bonus.Attack);
            Assert.AreEqual(1, bonus.Defense);
        }

        [Test]
        public void GetEquipmentBonus_ReplacedSlot_UsesPreviewItem()
        {
            AdventurerStats bonus = EquipmentStatRules.GetEquipmentBonus(CreateAdventurer(), BonusOf,
                EquipmentSlot.Weapon, new ItemStack(Sword, 3, 1));

            Assert.AreEqual(10, bonus.Attack);
            Assert.AreEqual(12, bonus.MaxHp);
        }

        [Test]
        public void GetEquipped_ReadsAccessory()
        {
            var adventurer = new AdventurerData { AccessoryId = 11, AccessoryGrade = 2 };

            Assert.AreEqual(new ItemStack(11, 2, 1), EquipmentRules.GetEquipped(adventurer, EquipmentSlot.Accessory));
        }

        [Test]
        public void BattleSetup_AddsEquipmentBonus()
        {
            AdventurerData adventurer = CreateAdventurer();
            AdventurerStats bonus = EquipmentStatRules.GetEquipmentBonus(adventurer, BonusOf);

            UnitStats stats = AdventurerSetupRules.Create(adventurer, default, default, bonus).Stats;

            Assert.AreEqual(412, stats.MaxHp);
            Assert.AreEqual(55, stats.Attack);
            Assert.AreEqual(7, stats.Defense);
        }
    }
}
