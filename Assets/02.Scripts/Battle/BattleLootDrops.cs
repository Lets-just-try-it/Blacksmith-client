using System.Collections.Generic;
using Blacksmith.Core.Battle;
using Blacksmith.Inventory;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 실제로 얻는 재료를 몬스터가 쓰러질 때 바닥에 떨어뜨려 보여 준다 (docs/GAME_CONCEPT.md 7.1 "보상과 경험치").
    // 보상 계산(BattleRewards)과 같은 값을 쓰고, 클리어한 스테이지의 재료만 보여 준다. 결과에는 영향을 주지 않는다.
    // 한 스테이지의 재료는 그 스테이지 몬스터들에게 나눠 주며, 마지막에 쓰러지는 몬스터(보스 스테이지는 보스)부터 받는다.
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

        // 스테이지마다 떨어뜨릴 재료와 그 스테이지의 몬스터 수, 지금까지 쓰러진 수
        private readonly List<List<MaterialReward>> _stageLoots = new List<List<MaterialReward>>();
        private readonly List<int> _monsterCounts = new List<int>();
        private readonly List<int> _fallenCounts = new List<int>();
        private readonly List<BattleLootDropView> _drops = new List<BattleLootDropView>();

        private void OnEnable()
        {
            _player.UnitFell += HandleUnitFell;
        }

        private void OnDisable()
        {
            _player.UnitFell -= HandleUnitFell;
        }

        // 재생을 시작할 때 부른다. 에메랄드로 늘어난 재료는 마지막으로 클리어한 스테이지에 더해 보여 준다.
        public void Prepare(BattleSetup setup, BattleRewards rewards)
        {
            Clear();
            for (int i = 0; i < setup.Stages.Count; i++)
            {
                var loot = new List<MaterialReward>();
                if (i < rewards.StageLoots.Count)
                {
                    Merge(loot, rewards.StageLoots[i].Materials);
                    if (i == rewards.StageLoots.Count - 1)
                    {
                        Merge(loot, rewards.BonusMaterials);
                    }

                    Merge(loot, rewards.StageLoots[i].Gems);
                }

                _stageLoots.Add(loot);
                _monsterCounts.Add(setup.Stages[i].Monsters.Count);
                _fallenCounts.Add(0);
            }
        }

        private void HandleUnitFell(BattleUnitView view)
        {
            BattleUnit unit = view.Unit;
            if (unit.Team != BattleTeam.Monster || unit.StageIndex < 0 || unit.StageIndex >= _stageLoots.Count)
            {
                return;
            }

            int stage = unit.StageIndex;
            int fallOrder = _fallenCounts[stage]++;
            int monsterCount = _monsterCounts[stage];
            List<MaterialReward> loot = _stageLoots[stage];
            var mine = new List<MaterialReward>();
            for (int i = 0; i < loot.Count; i++)
            {
                if (monsterCount - 1 - i % monsterCount == fallOrder)
                {
                    mine.Add(loot[i]);
                }
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
            _stageLoots.Clear();
            _monsterCounts.Clear();
            _fallenCounts.Clear();
        }
    }
}
