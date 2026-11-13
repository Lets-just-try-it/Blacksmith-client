using Blacksmith.Core.Adventurers;
using Fusion;

namespace Blacksmith.Plaza
{
    // FallenAdventurer를 [Networked]에 담기 위한 형식이다.
    public struct NetworkFallenAdventurer : INetworkStruct
    {
        public NetworkAdventurer Adventurer;
        public int Day;
        public int DungeonId;
        public int Stage;

        public static NetworkFallenAdventurer FromData(FallenAdventurer data)
        {
            return new NetworkFallenAdventurer
            {
                Adventurer = NetworkAdventurer.FromData(data.Adventurer),
                Day = data.Day,
                DungeonId = data.DungeonId,
                Stage = data.Stage,
            };
        }

        public FallenAdventurer ToData()
        {
            return new FallenAdventurer
            {
                Adventurer = Adventurer.ToData(),
                Day = Day,
                DungeonId = DungeonId,
                Stage = Stage,
            };
        }
    }
}
