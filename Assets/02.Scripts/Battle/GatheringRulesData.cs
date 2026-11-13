using Blacksmith.Core.Battle;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 채집 맵에 놓는 것의 수, 채집량, 채집 시간 (docs/GAME_CONCEPT.md 7.4). 광석 종류는 던전 데이터가 정한다.
    // 시체 더미에서 나오는 부산물·보석은 던전 데이터의 몬스터 보상(드랍 확률)을 쓴다.
    [CreateAssetMenu(fileName = "GatheringRules", menuName = "Blacksmith/Battle/Gathering Rules")]
    public class GatheringRulesData : ScriptableObject
    {
        [Header("놓는 수")]
        [Tooltip("[추정] 광맥 수")]
        [SerializeField, Min(0)] private int _oreVeins = 6;
        [Tooltip("[추정] 나무 수")]
        [SerializeField, Min(0)] private int _trees = 5;
        [Tooltip("[추정] 바닥에 떨어진 광석 수")]
        [SerializeField, Min(0)] private int _looseOre = 6;
        [Tooltip("[추정] 바닥에 떨어진 목재 수")]
        [SerializeField, Min(0)] private int _looseWood = 6;
        [Tooltip("[추정] 약초 수")]
        [SerializeField, Min(0)] private int _herbs = 4;

        [Header("자리 (칸 단위)")]
        [Tooltip("[추정] 채집할 것끼리(시체 더미 포함) 떨어지는 최소 거리")]
        [SerializeField, Min(0f)] private float _minSpacing = 6f;
        [Tooltip("[추정] 시작 지점(돌아가는 포털)에서 떨어지는 최소 거리")]
        [SerializeField, Min(0f)] private float _startClearance = 8f;
        [Tooltip("이 칸 수 안의 이웃이 모두 바닥인 칸에만 놓아 벽에 붙지 않게 한다")]
        [SerializeField, Min(0)] private int _wallMargin = 2;

        [Header("채집량 (양 끝 포함)")]
        [SerializeField] private int _woodItemId = 54;
        [Tooltip("[추정] 광맥 하나에서 나오는 광석 수")]
        [SerializeField, Min(0)] private int _oreVeinMin = 3;
        [SerializeField, Min(0)] private int _oreVeinMax = 5;
        [Tooltip("[추정] 나무 하나에서 나오는 목재 수")]
        [SerializeField, Min(0)] private int _treeMin = 2;
        [SerializeField, Min(0)] private int _treeMax = 4;
        [Tooltip("[추정] 바닥에서 줍는 광석 수")]
        [SerializeField, Min(0)] private int _looseOreMin = 1;
        [SerializeField, Min(0)] private int _looseOreMax = 1;
        [Tooltip("[추정] 바닥에서 줍는 목재 수")]
        [SerializeField, Min(0)] private int _looseWoodMin = 1;
        [SerializeField, Min(0)] private int _looseWoodMax = 2;
        [Tooltip("약초 아이템 번호(ItemCatalog)")]
        [SerializeField] private int _herbItemId = 64;
        [Tooltip("[추정] 약초 하나에서 나오는 약초 수")]
        [SerializeField, Min(0)] private int _herbMin = 1;
        [SerializeField, Min(0)] private int _herbMax = 2;

        [Header("채집 시간 (초)")]
        [Tooltip("[추정] 시체 더미 갈무리")]
        [SerializeField, Min(0f)] private float _corpsePileSeconds = 3f;
        [Tooltip("[추정] 광맥 캐기")]
        [SerializeField, Min(0f)] private float _oreVeinSeconds = 3f;
        [Tooltip("[추정] 나무 베기")]
        [SerializeField, Min(0f)] private float _treeSeconds = 3f;
        [Tooltip("[추정] 바닥에서 줍기")]
        [SerializeField, Min(0f)] private float _looseSeconds = 0.5f;

        public GatheringLayoutSettings ToLayoutSettings()
        {
            return new GatheringLayoutSettings
            {
                OreVeins = _oreVeins,
                Trees = _trees,
                LooseOre = _looseOre,
                LooseWood = _looseWood,
                Herbs = _herbs,
                MinSpacing = _minSpacing,
                StartClearance = _startClearance,
                WallMargin = _wallMargin,
            };
        }

        public GatheringYieldSettings ToYieldSettings(int oreItemId)
        {
            return new GatheringYieldSettings
            {
                OreItemId = oreItemId,
                WoodItemId = _woodItemId,
                OreVeinMin = _oreVeinMin,
                OreVeinMax = _oreVeinMax,
                TreeMin = _treeMin,
                TreeMax = _treeMax,
                LooseOreMin = _looseOreMin,
                LooseOreMax = _looseOreMax,
                LooseWoodMin = _looseWoodMin,
                LooseWoodMax = _looseWoodMax,
                HerbItemId = _herbItemId,
                HerbMin = _herbMin,
                HerbMax = _herbMax,
            };
        }

        public float GetSeconds(GatheringNodeKind kind)
        {
            switch (kind)
            {
                case GatheringNodeKind.CorpsePile:
                    return _corpsePileSeconds;
                case GatheringNodeKind.OreVein:
                    return _oreVeinSeconds;
                case GatheringNodeKind.Tree:
                    return _treeSeconds;
                default:
                    return _looseSeconds;
            }
        }
    }
}
