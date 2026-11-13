namespace Blacksmith.Core.Adventurers
{
    // 사망한 모험가의 기록이다. 관리 목록에서 빠져도 부활과 공동묘지에 쓰도록 남긴다. (docs/GAME_CONCEPT.md 6.5)
    public sealed class FallenAdventurer
    {
        // 입었던 장비는 모험가와 함께 사라졌으므로 비어 있다.
        public AdventurerData Adventurer { get; set; }
        // 사망한 밤의 날짜.
        public int Day { get; set; }
        public int DungeonId { get; set; }
        // 사망한 스테이지 번호(0부터).
        public int Stage { get; set; }

        // adventurer의 장비를 비워 기록으로 만든다.
        public static FallenAdventurer Create(AdventurerData adventurer, int day, int dungeonId, int stage)
        {
            adventurer.WeaponId = AdventurerData.NoEquipment;
            adventurer.WeaponGrade = 0;
            adventurer.HelmetId = AdventurerData.NoEquipment;
            adventurer.HelmetGrade = 0;
            adventurer.ArmorId = AdventurerData.NoEquipment;
            adventurer.ArmorGrade = 0;
            adventurer.AccessoryId = AdventurerData.NoEquipment;
            adventurer.AccessoryGrade = 0;
            return new FallenAdventurer { Adventurer = adventurer, Day = day, DungeonId = dungeonId, Stage = stage };
        }
    }
}
