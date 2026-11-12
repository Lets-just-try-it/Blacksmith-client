using System;
using System.Collections;
using System.Collections.Generic;
using Blacksmith.Core.Battle;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 쓰러진 몬스터를 쓰러지는 동작 뒤에 시체로 바꿔 그 자리에 남긴다 (docs/GAME_CONCEPT.md 7.1, 7.4).
    // 무리로 싸우므로 한 무리의 시체가 흩어져 모인 곳이 시체 더미가 된다. 표시 전용이라 결과에는 영향이 없다.
    // 시체 자리는 몬스터가 쓰러진 자리가 아니라 BattleResult.Corpses의 자리다. 다음 날 채집하는 자리와 같다.
    public class BattleCorpses : MonoBehaviour
    {
        [SerializeField] private BattlePlayer _player;
        [SerializeField] private Transform _corpseRoot;
        [Tooltip("몬스터 데이터에 시체 프리팹이 없을 때 쓴다")]
        [SerializeField] private GameObject _defaultCorpsePrefab;
        [Tooltip("[추정] 쓰러지는 동작을 보여 준 뒤 시체로 바꾸기까지 기다리는 시간(초)")]
        [SerializeField, Min(0f)] private float _delay = 1f;
        [Tooltip("[추정] 보스 시체의 크기 배율")]
        [SerializeField, Min(0.1f)] private float _bossScale = 1.5f;

        private readonly List<GameObject> _corpses = new List<GameObject>();
        private Func<int, MonsterData> _findMonster;
        private Func<float, float, Vector3> _toWorld;
        private IReadOnlyList<BattleCorpse> _corpseSpots = new BattleCorpse[0];

        private void OnEnable()
        {
            _player.UnitFell += HandleUnitFell;
        }

        private void OnDisable()
        {
            _player.UnitFell -= HandleUnitFell;
        }

        // 재생을 시작할 때 부른다. 이전 전투의 시체를 지운다. toWorld는 전투 좌표를 월드 좌표로 바꾼다.
        public void Prepare(BattleResult result, Func<int, MonsterData> findMonster, Func<float, float, Vector3> toWorld)
        {
            Clear();
            _corpseSpots = result.Corpses;
            _findMonster = findMonster;
            _toWorld = toWorld;
        }

        private void HandleUnitFell(BattleUnitView view)
        {
            if (view.Unit.Team == BattleTeam.Monster)
            {
                StartCoroutine(TurnIntoCorpse(view));
            }
        }

        private IEnumerator TurnIntoCorpse(BattleUnitView view)
        {
            yield return new WaitForSeconds(_delay);
            if (view == null)
            {
                yield break;
            }

            MonsterData monster = _findMonster?.Invoke(view.Unit.SourceId);
            GameObject prefab = monster != null && monster.CorpsePrefab != null ? monster.CorpsePrefab : _defaultCorpsePrefab;
            if (prefab != null)
            {
                // 같은 방향으로 눕지 않게 유닛 번호로 방향을 정한다. 참가자마다 같다.
                Quaternion rotation = Quaternion.Euler(0f, view.Unit.UnitId * 137 % 360, 0f);
                GameObject corpse = Instantiate(prefab, GetSpot(view.Unit, view.transform.position), rotation, _corpseRoot);
                if (monster != null && monster.IsBoss)
                {
                    corpse.transform.localScale *= _bossScale;
                }

                _corpses.Add(corpse);
            }

            view.gameObject.SetActive(false);
        }

        private Vector3 GetSpot(BattleUnit unit, Vector3 fallback)
        {
            foreach (BattleCorpse spot in _corpseSpots)
            {
                if (spot.GroupIndex == unit.GroupIndex && spot.GroupSlot == unit.GroupSlot)
                {
                    return _toWorld(spot.X, spot.Z);
                }
            }

            return fallback;
        }

        private void Clear()
        {
            StopAllCoroutines();
            foreach (GameObject corpse in _corpses)
            {
                Destroy(corpse);
            }

            _corpses.Clear();
        }
    }
}
