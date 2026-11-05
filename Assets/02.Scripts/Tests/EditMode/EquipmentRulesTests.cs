using Blacksmith.Core.Inventory;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class EquipmentRulesTests
    {
        [TestCase(WeaponType.GreatSword)]
        [TestCase(WeaponType.Bow)]
        [TestCase(WeaponType.Staff)]
        public void Weapon_MatchingJobWeapon_CanEquip(WeaponType weapon)
        {
            Assert.IsTrue(EquipmentRules.CanEquip(EquipmentSlot.Weapon, EquipmentSlot.Weapon, weapon, weapon));
        }

        [Test]
        public void Weapon_OtherJobWeapon_CannotEquip()
        {
            Assert.IsFalse(EquipmentRules.CanEquip(EquipmentSlot.Weapon, EquipmentSlot.Weapon, WeaponType.Bow, WeaponType.GreatSword));
            Assert.IsFalse(EquipmentRules.CanEquip(EquipmentSlot.Weapon, EquipmentSlot.Weapon, WeaponType.Bow, WeaponType.Staff));
        }

        [Test]
        public void Weapon_WithoutWeaponType_CannotEquip()
        {
            Assert.IsFalse(EquipmentRules.CanEquip(EquipmentSlot.Weapon, EquipmentSlot.Weapon, WeaponType.None, WeaponType.None));
        }

        [Test]
        public void Weapon_CannotBeUnequipped_OtherSlotsCan()
        {
            Assert.IsFalse(EquipmentRules.CanUnequip(EquipmentSlot.Weapon));
            Assert.IsFalse(EquipmentRules.CanUnequip(EquipmentSlot.None));
            Assert.IsTrue(EquipmentRules.CanUnequip(EquipmentSlot.Helmet));
            Assert.IsTrue(EquipmentRules.CanUnequip(EquipmentSlot.Armor));
            Assert.IsTrue(EquipmentRules.CanUnequip(EquipmentSlot.Accessory));
        }

        [Test]
        public void Armor_InWeaponSlot_CannotEquip()
        {
            Assert.IsFalse(EquipmentRules.CanEquip(EquipmentSlot.Weapon, EquipmentSlot.Armor, WeaponType.None, WeaponType.GreatSword));
            Assert.IsFalse(EquipmentRules.CanEquip(EquipmentSlot.Weapon, EquipmentSlot.Helmet, WeaponType.None, WeaponType.GreatSword));
        }

        [Test]
        public void Weapon_InArmorSlot_CannotEquip()
        {
            Assert.IsFalse(EquipmentRules.CanEquip(EquipmentSlot.Armor, EquipmentSlot.Weapon, WeaponType.GreatSword, WeaponType.GreatSword));
        }

        [TestCase(WeaponType.GreatSword)]
        [TestCase(WeaponType.Staff)]
        public void HelmetAndArmor_AnyJob_CanEquip(WeaponType jobWeapon)
        {
            Assert.IsTrue(EquipmentRules.CanEquip(EquipmentSlot.Helmet, EquipmentSlot.Helmet, WeaponType.None, jobWeapon));
            Assert.IsTrue(EquipmentRules.CanEquip(EquipmentSlot.Armor, EquipmentSlot.Armor, WeaponType.None, jobWeapon));
        }

        [Test]
        public void NoneSlot_CannotEquip()
        {
            Assert.IsFalse(EquipmentRules.CanEquip(EquipmentSlot.None, EquipmentSlot.None, WeaponType.None, WeaponType.None));
        }
    }
}
