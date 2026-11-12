using System.Collections.Generic;
using Blacksmith.Core.Battle;
using Blacksmith.Inventory;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 실제로 얻는 재료를 몬스터가 쓰러질 때 바닥에 떨어뜨려 보여 준다 (docs/GAME_CONCEPT.md 7.1 "보상과 경험치").
    // 보상 계산(BattleRewards)과 같은 값을 쓰고, 토벌한 무리의 재료만 보여 준다. 결과에는 영향을 주지 않는다.
    // 몬스터마다 자기가 떨어뜨린 재료를 보여 주고, 에메랄드로 늘어난 몫은 마지막으로 토벌한 무리에서 마지막에 쓰러진 몬스터에 더한다.
    public class BattleLootDrops : MonoBehaviour
    {
        [SerializeField] private BattlePlayer _player;
        [Tooltip("아이템 이름과 색을 찾는 데 쓴다")]
        [SerializeField] private ItemCatalog _catalog;
        [SerializeField] private BattleLootDropView _dropPrefab;
        [SerializeField] private Transform _dropRoot;
        [Tooltip("드랍 연출이 바라볼 카메라")]
        [SerializeField] private Camera _camera;
        [Tooltip("쓰러진 자리에서 카메라 쪽으로 떨어지는 거리. 쓰러진 몸에 가려지지 않게 한다")]
        [SerializeField, Min(0f)] private float _forwardDistance = 1.3f;
        [Tooltip("전리품끼리 옆으로 벌어지는 간격")]
        [SerializeField, Min(0f)] private float _spacing = 0.7f;
        [Tooltip("한 줄에 놓는 전리품 수. 넘치면 다음 줄로 카메라 쪽에 더 놓는다")]
        [SerializeField, Min(1)] private int _perRow = 4;

        // 무리마다 몬스터별로 떨어뜨릴 재료와 그 무리에서 지금까지 쓰러진 수
        private readonly List<List<List<MaterialReward>>> _groupLoots = new List<List<List<MaterialReward>>>();
        private readonly List<int> _fallenCounts = new List<int>();
        private readonly List<BattleLootDropView> _drops = new List<BattleLootDropView>();
        private IReadOnlyList<MaterialReward> _bonusMaterials = new List<MaterialReward>();
        private int _lastClearedGroup = -1;

        private void OnEnable()
        {
            _player.UnitFell += HandleUnitFell;
        }

        private void OnDisable()
        {
            _player.UnitFell -= HandleUnitFell;
        }

        // 재생을 시작할 때 부른다.
        public void Prepare(BattleSetup setup, BattleRewards rewards, BattleResult result)
        {
            Clear();
            for (int i = 0; i < setup.Groups.Count; i++)
            {
                var monsters = new List<List<MaterialReward>>();
                if (i < rewards.GroupLoots.Count)
                {
                    foreach (MonsterLoot monster in rewards.GroupLoots[i].Monsters)
                    {
                        var loot = new List<MaterialReward>();
                        Merge(loot, monster.Materials);
                        Merge(loot, monster.Gems);
                        monsters.Add(loot);
                    }
                }

                _groupLoots.Add(monsters);
                _fallenCounts.Add(0);
            }

            _bonusMaterials = rewards.BonusMaterials;
            _lastClearedGroup = result.GroupsCleared > 0 ? result.ClearedGroups[result.GroupsCleared - 1] : -1;
        }

        private void HandleUnitFell(BattleUnitView view)
        {
            BattleUnit unit = view.Unit;
            if (unit.Team != BattleTeam.Monster || unit.GroupIndex < 0 || unit.GroupIndex >= _groupLoots.Count)
            {
                return;
            }

            List<List<MaterialReward>> monsters = _groupLoots[unit.GroupIndex];
            if (unit.GroupSlot < 0 || unit.GroupSlot >= monsters.Count)
            {
                return;
            }

            var mine = new List<MaterialReward>(monsters[unit.GroupSlot]);
            int fallen = ++_fallenCounts[unit.GroupIndex];
            if (fallen == monsters.Count && unit.GroupIndex == _lastClearedGroup)
            {
                Merge(mine, _bonusMaterials);
            }

            for (int i = 0; i < mine.Count; i++)
            {
                Spawn(view.transform.position, mine[i], i, mine.Count);
            }
        }

        private void Spawn(Vector3 origin, MaterialReward material, int index, int count)
        {
            ItemData item = _catalog.Find(material.MaterialId);
            Vector3 toward = -_camera.transform.forward;
            toward.y = 0f;
            toward.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, toward);
            int row = index / _perRow;
            int rowCount = Mathf.Min(_perRow, count - row * _perRow);
            float column = index % _perRow - (rowCount - 1) * 0.5f;
            Vector3 target = origin + toward * (_forwardDistance + row * _spacing) + side * (column * _spacing);
            BattleLootDropView drop = Instantiate(_dropPrefab, origin, Quaternion.identity, _dropRoot);
            string name = item != null ? item.DisplayName : $"#{material.MaterialId}";
            drop.Play(origin, target, item != null ? item.Color : Color.white, $"{name} ×{material.Count}",
                _camera.transform.rotation);
            _drops.Add(drop);
        }

        private static void Merge(List<MaterialReward> total, IReadOnlyList<MaterialReward> materials)
        {
            foreach (MaterialReward material in materials)
            {
                int index = total.FindIndex(existing => existing.MaterialId == material.MaterialId);
                if (index < 0)
                {
                    total.Add(material);
                    continue;
                }

                total[index] = new MaterialReward { MaterialId = material.MaterialId, Count = total[index].Count + material.Count };
            }
        }

        private void Clear()
        {
            foreach (BattleLootDropView drop in _drops)
            {
                Destroy(drop.gameObject);
            }

            _drops.Clear();
            _groupLoots.Clear();
            _fallenCounts.Clear();
            _bonusMaterials = new List<MaterialReward>();
            _lastClearedGroup = -1;
        }
    }
}
