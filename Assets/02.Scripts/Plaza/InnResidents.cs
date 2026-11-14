using System.Collections.Generic;
using Blacksmith.Core.Adventurers;
using Blacksmith.Network;
using Fusion;
using UnityEngine;

namespace Blacksmith.Plaza
{
    // 관리 중인 모험가를 여관에 세운다. 씬 오브젝트이며 호스트에서만 동작한다.
    // 관리 목록이 바뀌면 새로 영입한 모험가는 빈 침대 자리에 스폰하고, 목록에서 빠진(사망한) 모험가는 디스폰하고, 남은 모험가의 정보를 갱신한다.
    public class InnResidents : NetworkBehaviour
    {
        [SerializeField] private AdventurerRosterState _roster;
        [Tooltip("여관 대화의 하루 한 번 제한에 날짜를 쓴다")]
        [SerializeField] private GameState _gameState;
        [SerializeField] private InnResident _residentPrefab;
        [Tooltip("모험가마다 하나씩 쓰는 침대 옆 자리. 관리 슬롯 최대 수만큼 둔다")]
        [SerializeField] private InnWaypoint[] _beds = new InnWaypoint[0];

        // 호스트에서만 쓴다. 침대 번호마다 머무는 모험가. 비어 있으면 null이다.
        private InnResident[] _residents;
        private readonly HashSet<int> _rosterIds = new HashSet<int>();

        public override void Spawned()
        {
            if (!HasStateAuthority)
            {
                return;
            }

            _residents = new InnResident[_beds.Length];
            _roster.Changed += Sync;
            Sync();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _roster.Changed -= Sync;
        }

        // 관리 목록이 아직 스폰되지 않았으면 스폰될 때 Changed로 다시 불린다.
        private void Sync()
        {
            if (!HasStateAuthority || !_roster.IsReady)
            {
                return;
            }

            _rosterIds.Clear();
            for (int i = 0; i < _roster.Count; i++)
            {
                _rosterIds.Add(_roster.Get(i).AdventurerId);
            }

            RemoveLeftResidents();
            for (int i = 0; i < _roster.Count; i++)
            {
                AdventurerData data = _roster.Get(i);
                InnResident resident = Find(data.AdventurerId);
                if (resident != null)
                {
                    resident.SetData(data);
                }
                else
                {
                    SpawnResident(data);
                }
            }
        }

        private void RemoveLeftResidents()
        {
            for (int bed = 0; bed < _residents.Length; bed++)
            {
                InnResident resident = _residents[bed];
                if (resident != null && !_rosterIds.Contains(resident.AdventurerId))
                {
                    Runner.Despawn(resident.Object);
                    _residents[bed] = null;
                }
            }
        }

        private void SpawnResident(AdventurerData data)
        {
            int bed = System.Array.IndexOf(_residents, null);
            if (bed < 0)
            {
                Debug.LogWarning($"여관 침대 자리 {_beds.Length}곳이 모두 차서 {data.Name}을(를) 세우지 못했다.", this);
                return;
            }

            InnWaypoint bedPoint = _beds[bed];
            _residents[bed] = Runner.Spawn(_residentPrefab, bedPoint.transform.position, bedPoint.transform.rotation,
                onBeforeSpawned: (runner, residentObject) =>
                {
                    InnResident resident = residentObject.GetComponent<InnResident>();
                    resident.SetData(data);
                    resident.SetTalkHandler(OnTalked);
                    residentObject.GetComponent<InnWander>().Initialize(bedPoint);
                });
        }

        private void OnTalked(int adventurerId)
        {
            _roster.TryTalk(adventurerId, _gameState.Day);
        }

        private InnResident Find(int adventurerId)
        {
            foreach (InnResident resident in _residents)
            {
                if (resident != null && resident.AdventurerId == adventurerId)
                {
                    return resident;
                }
            }

            return null;
        }
    }
}
