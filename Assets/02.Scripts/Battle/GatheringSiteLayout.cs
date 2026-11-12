using System.Collections.Generic;
using Blacksmith.Core.Battle;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 지금 채집할 맵의 바닥, 채집할 것의 자리, 채집량을 만든다 (docs/GAME_CONCEPT.md 7.4). 낮 씬에 둔다.
    // 모두 시드와 시체 자리로 다시 만들 수 있어 동기화하지 않는다. 참가자마다 같은 번호의 채집할 것이 같은 자리에 나온다.
    // 맵 생성이 무거워 채집할 맵이 바뀔 때만 다시 만든다.
    public class GatheringSiteLayout : MonoBehaviour
    {
        // 채집할 것의 수 상한. GatheringNodesState의 [Networked] 배열 크기와 같다.
        public const int MaxNodes = 64;

        [SerializeField] private GatheringSiteState _site;
        [SerializeField] private DungeonProgressState _progress;
        [SerializeField] private GatheringRulesData _rules;

        private bool _hasBuilt;
        private int _builtDungeonId;
        private int _builtSeed;
        private int _builtCorpseCount;

        public DungeonData Dungeon { get; private set; }
        public StageMap Map { get; private set; }
        public IReadOnlyList<BattleCorpse> Corpses { get; private set; } = new List<BattleCorpse>();
        public IReadOnlyList<GatheringNode> Nodes { get; private set; } = new List<GatheringNode>();
        public GatheringRulesData Rules => _rules;

        private IReadOnlyList<GroupLoot> _corpseLoots = new List<GroupLoot>();

        // 채집할 맵이 없거나 던전 데이터를 찾을 수 없으면 false다.
        public bool TryRefresh()
        {
            if (!_site.IsReady || !_site.HasSite)
            {
                return false;
            }

            List<BattleCorpse> corpses = _site.GetCorpses();
            if (_hasBuilt && _builtDungeonId == _site.DungeonId && _builtSeed == _site.Seed && _builtCorpseCount == corpses.Count)
            {
                return true;
            }

            DungeonData dungeon = _progress.FindDungeon(_site.DungeonId);
            if (dungeon == null)
            {
                Debug.LogError($"채집할 던전 데이터를 찾을 수 없다: {_site.DungeonId}", this);
                return false;
            }

            Dungeon = dungeon;
            Map = dungeon.GenerateMap(_site.Seed);
            Corpses = corpses;
            Nodes = GatheringLayoutRules.Generate(Map, corpses, _site.Seed, _rules.ToLayoutSettings(), MaxNodes);
            // 시체 더미는 그 전투에서 굴린 몬스터별 드랍과 같다.
            _corpseLoots = BattleDropRules.Roll(dungeon.GetGroupRewards(_site.Stage, _site.Seed), _site.Seed);
            _hasBuilt = true;
            _builtDungeonId = _site.DungeonId;
            _builtSeed = _site.Seed;
            _builtCorpseCount = corpses.Count;
            return true;
        }

        // TryRefresh가 성공한 뒤에 부른다.
        public List<MaterialReward> GetYield(int nodeIndex)
        {
            return GatheringYieldRules.Roll(Nodes[nodeIndex], nodeIndex, _builtSeed,
                _rules.ToYieldSettings(Dungeon.OreItemId), _corpseLoots);
        }
    }
}
