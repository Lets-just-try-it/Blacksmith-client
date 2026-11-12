using System;
using System.Collections.Generic;
using Blacksmith.Core.Battle;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 전투 맵에 다음 날 채집할 수 있는 광맥·나무·바닥의 광석·목재를 보여 준다 (docs/GAME_CONCEPT.md 7.1, 7.4).
    // 채집 맵과 같은 함수(GatheringSiteLayout.BuildNodes)로 같은 시드와 시체 자리에서 만들어, 낮에 들어가면 같은 자리에 있다.
    // 시체 더미는 BattleCorpses가 시체로 보여 주므로 여기서는 놓지 않는다. 표시 전용이라 결과에는 영향이 없다.
    public class BattleGatheringPreview : MonoBehaviour
    {
        [Serializable]
        private class VisualPrefab
        {
            public GatheringNodeKind Kind;
            public GameObject Prefab;
        }

        [SerializeField] private GatheringRulesData _rules;
        [SerializeField] private StageMapView _stageMap;
        [SerializeField] private Transform _root;
        [Tooltip("종류별 모습. 채집 맵의 채집할 것 프리팹 안에 있는 모습과 같은 프리팹을 쓴다")]
        [SerializeField] private List<VisualPrefab> _visuals = new List<VisualPrefab>();

        private readonly List<GameObject> _instances = new List<GameObject>();

        // 재생을 시작할 때 맵을 깐 뒤 부른다. 이전 전투의 것을 지운다.
        public void Show(BattleSetup setup, BattleResult result)
        {
            Clear();
            foreach (GatheringNode node in GatheringSiteLayout.BuildNodes(setup.Map, result.Corpses, setup.Seed, _rules))
            {
                GameObject prefab = FindPrefab(node.Kind);
                if (prefab != null)
                {
                    _instances.Add(Instantiate(prefab, _stageMap.ToWorld(node.X, node.Z), Quaternion.identity, _root));
                }
            }
        }

        private GameObject FindPrefab(GatheringNodeKind kind)
        {
            foreach (VisualPrefab entry in _visuals)
            {
                if (entry.Kind == kind)
                {
                    return entry.Prefab;
                }
            }

            return null;
        }

        private void Clear()
        {
            foreach (GameObject instance in _instances)
            {
                if (instance != null)
                {
                    Destroy(instance);
                }
            }

            _instances.Clear();
        }
    }
}
