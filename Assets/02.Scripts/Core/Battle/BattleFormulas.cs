using System;

namespace Blacksmith.Core.Battle
{
    // [미정] 스탯·레벨·장비가 전투력에 미치는 영향이 정해지면 이 공식을 바꾼다.
    public static class BattleFormulas
    {
        public static int Damage(int attack, int powerPercent, int defense, int variancePercent, Random random)
        {
            int baseDamage = Math.Max(1, attack * powerPercent / 100 - defense);
            int roll = random.Next(-variancePercent, variancePercent + 1);
            return Math.Max(1, baseDamage * (100 + roll) / 100);
        }

        public static int Heal(int attack, int powerPercent)
        {
            return Math.Max(1, attack * powerPercent / 100);
        }

        public static bool RollDeath(int deathChancePercent, Random random)
        {
            return random.Next(100) < deathChancePercent;
        }
    }
}
