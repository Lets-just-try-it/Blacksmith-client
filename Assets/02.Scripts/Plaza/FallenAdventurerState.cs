using System;
using Blacksmith.Core.Adventurers;
using Fusion;

namespace Blacksmith.Plaza
{
    // 사망한 모험가 기록이다. 관리 목록(AdventurerRosterState)과 따로 두어 슬롯을 차지하지 않는다. (docs/GAME_CONCEPT.md 6.5)
    // 호스트만 값을 바꾸고, 클라이언트는 복제된 값을 읽기만 한다. 오래된 기록부터 순서대로 담는다.
    public class FallenAdventurerState : NetworkBehaviour
    {
        // [Networked] 배열 크기는 컴파일할 때 정해야 해서 상수로 둔다. 넘치면 가장 오래된 기록을 지운다.
        public const int MaxCapacity = 32;

        [Networked, Capacity(MaxCapacity)] private NetworkArray<NetworkFallenAdventurer> Records => default;
        [Networked] public int Count { get; private set; }

        public FallenAdventurer Get(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return Records[index].ToData();
        }

        // 호스트 전용.
        public bool TryAdd(FallenAdventurer fallen)
        {
            if (!HasStateAuthority)
            {
                return false;
            }

            if (Count == MaxCapacity)
            {
                for (int i = 0; i < Count - 1; i++)
                {
                    Records.Set(i, Records[i + 1]);
                }

                Count--;
            }

            Records.Set(Count, NetworkFallenAdventurer.FromData(fallen));
            Count++;
            return true;
        }
    }
}
