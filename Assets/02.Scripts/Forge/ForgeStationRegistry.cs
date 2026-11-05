using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 스폰된 작업대 목록. 씬의 작업대를 인스펙터로 참조할 수 없는 Player 프리팹이 "누가 어느 작업대를 쓰는지" 찾을 때 쓴다.
    // 작업대(ForgeStationState)가 스폰될 때 등록하고 디스폰될 때 뺀다. 런타임 상태라 저장하지 않는다.
    [CreateAssetMenu(fileName = "ForgeStationRegistry", menuName = "Blacksmith/Runtime/Forge Station Registry")]
    public class ForgeStationRegistry : ScriptableObject
    {
        private readonly List<ForgeStationState> _stations = new List<ForgeStationState>();

        public void Register(ForgeStationState station)
        {
            if (!_stations.Contains(station))
            {
                _stations.Add(station);
            }
        }

        public void Unregister(ForgeStationState station)
        {
            _stations.Remove(station);
        }

        // player가 사용권을 가진 작업대를 찾는다. 한 번에 한 작업대만 쓸 수 있다.
        public bool TryFindUsedBy(PlayerRef player, out ForgeStationState station)
        {
            foreach (ForgeStationState candidate in _stations)
            {
                if (candidate != null && candidate.InUse && candidate.User == player)
                {
                    station = candidate;
                    return true;
                }
            }

            station = null;
            return false;
        }

        // 에디터에서 도메인 리로드 없이 플레이를 다시 시작해도 지난 목록이 남지 않게 한다.
        private void OnEnable()
        {
            _stations.Clear();
        }
    }
}
