using System;
using System.Collections.Generic;
using Blacksmith.Core.Battle;
using Blacksmith.Forge;
using Blacksmith.Network;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Blacksmith.Battle
{
    // 낮 씬에 채집할 맵의 바닥, 시체, 채집할 것을 깐다 (docs/GAME_CONCEPT.md 7.4). 전투와 같은 시드로 다시 만들어 모든 참가자에게 같은 맵이 나온다.
    // 맵의 시작 지점이 StageMapView의 위치에 온다. 광장의 DungeonGate가 플레이어를 시작 지점으로 보낸다.
    // 메모리를 아끼려고 누군가 들어갈 때 만들고(EnsureBuilt), 채집할 맵이 바뀌어(다음 날) 지난 맵이 되면 버린다.
    // 참가자마다 따로 만들고 버린다. 지난 맵이어도 안에 플레이어가 있으면 바닥이 꺼지지 않게, 모두 나갈 때까지 기다렸다가 버린다.
    public class GatheringSiteView : MonoBehaviour
    {
        // 맵 영역 안의 콜라이더를 모으는 버퍼 크기. 바닥·벽, 채집할 것(최대 64)과 플레이어(최대 4명)보다 넉넉하다.
        private const int OverlapBufferSize = 128;

        [Serializable]
        private class NodePrefab
        {
            public GatheringNodeKind Kind;
            public GatheringNodeView Prefab;
        }

        [SerializeField] private GatheringSiteState _site;
        [SerializeField] private GatheringSiteLayout _layout;
        [SerializeField] private GatheringNodesState _nodes;
        [SerializeField] private StageMapView _stageMap;
        [Tooltip("시체와 채집할 것을 넣는 부모")]
        [SerializeField] private Transform _objectRoot;
        [SerializeField] private List<NodePrefab> _nodePrefabs = new List<NodePrefab>();
        [Tooltip("몬스터 데이터에 시체 프리팹이 없을 때 쓴다")]
        [SerializeField] private GameObject _defaultCorpsePrefab;
        [Tooltip("[추정] 보스 시체의 크기 배율")]
        [SerializeField, Min(0.1f)] private float _bossScale = 1.5f;
        [SerializeField] private InputActionReference _interact;
        [SerializeField] private PlayerNetworkInputLock _inputLock;
        [SerializeField] private ForgeNotice _notice;
        [Tooltip("지난 맵 안에 플레이어가 남았는지 확인하는 간격(초)")]
        [SerializeField, Min(0.1f)] private float _releaseCheckInterval = 1f;

        private readonly Collider[] _overlaps = new Collider[OverlapBufferSize];
        private readonly List<GameObject> _objects = new List<GameObject>();
        private int _builtDungeonId;
        private int _builtSeed;
        private float _nextReleaseCheck;
        private GatheringPlayerActions _rejectSource;

        public GatheringNodesState Nodes => _nodes;
        // 깔아 둔 맵의 시드. 채집할 것의 상태를 이 시드로 읽고 요청한다.
        public int Seed => _builtSeed;
        public InputActionReference Interact => _interact;
        public PlayerNetworkInputLock InputLock => _inputLock;

        private bool IsCurrent => _stageMap.IsBuilt && _site.IsReady && _site.HasSite
            && _builtDungeonId == _site.DungeonId && _builtSeed == _site.Seed;

        // Update는 지난 맵을 버릴 때까지만 돈다.
        private void Awake()
        {
            _site.Changed += OnSiteChanged;
            enabled = false;
        }

        private void OnDestroy()
        {
            _site.Changed -= OnSiteChanged;
            SetRejectSource(null);
        }

        // 채집할 맵이 있고 아직 깔지 않았으면 깐다. 화면이 가려진 동안(문으로 들어갈 때) 부른다.
        public void EnsureBuilt()
        {
            if (IsCurrent || !_layout.TryRefresh())
            {
                return;
            }

            _interact.action.Enable();
            _stageMap.Build(_layout.Map, _layout.Dungeon.StageMap);
            _builtDungeonId = _site.DungeonId;
            _builtSeed = _site.Seed;
            ClearObjects();
            SpawnCorpses(SpawnNodes());
            enabled = false;
        }

        // 내 캐릭터가 채집할 것을 캐겠다고 할 때 GatheringNodeView가 부른다. 거절되면 이유를 띄운다.
        public void RequestGather(NetworkPlayer player, int index)
        {
            if (player.TryGetComponent(out GatheringPlayerActions actions))
            {
                SetRejectSource(actions);
                actions.RequestGather(_nodes, _builtSeed, index);
            }
        }

        // 시체는 그 무리의 시체 더미 모습 아래에 두어, 다 갈무리하면 함께 사라진다.
        private void SpawnCorpses(Dictionary<int, GatheringNodeView> piles)
        {
            foreach (BattleCorpse corpse in _layout.Corpses)
            {
                MonsterData monster = _layout.Dungeon.FindMonster(corpse.MonsterId);
                GameObject prefab = monster != null && monster.CorpsePrefab != null ? monster.CorpsePrefab : _defaultCorpsePrefab;
                if (prefab == null)
                {
                    continue;
                }

                // 같은 방향으로 눕지 않게 무리와 순서로 방향을 정한다. 참가자마다 같다.
                Quaternion rotation = Quaternion.Euler(0f, (corpse.GroupIndex * 31 + corpse.GroupSlot) * 137 % 360, 0f);
                Transform parent = piles.TryGetValue(corpse.GroupIndex, out GatheringNodeView pile) ? pile.Visual : _objectRoot;
                GameObject instance = Instantiate(prefab, _stageMap.ToWorld(corpse.X, corpse.Z), rotation, parent);
                if (monster != null && monster.IsBoss)
                {
                    instance.transform.localScale *= _bossScale;
                }

                _objects.Add(instance);
            }
        }

        // 무리 번호별 시체 더미를 돌려준다.
        private Dictionary<int, GatheringNodeView> SpawnNodes()
        {
            var piles = new Dictionary<int, GatheringNodeView>();
            for (int i = 0; i < _layout.Nodes.Count; i++)
            {
                GatheringNode node = _layout.Nodes[i];
                GatheringNodeView prefab = FindPrefab(node.Kind);
                if (prefab == null)
                {
                    Debug.LogWarning($"채집할 것의 프리팹이 없다: {node.Kind}", this);
                    continue;
                }

                GatheringNodeView view = Instantiate(prefab, _stageMap.ToWorld(node.X, node.Z), Quaternion.identity, _objectRoot);
                view.Init(this, i);
                _objects.Add(view.gameObject);
                if (node.Kind == GatheringNodeKind.CorpsePile)
                {
                    piles[node.GroupIndex] = view;
                }
            }

            return piles;
        }

        private GatheringNodeView FindPrefab(GatheringNodeKind kind)
        {
            foreach (NodePrefab entry in _nodePrefabs)
            {
                if (entry.Kind == kind)
                {
                    return entry.Prefab;
                }
            }

            return null;
        }

        private void SetRejectSource(GatheringPlayerActions actions)
        {
            if (_rejectSource == actions)
            {
                return;
            }

            if (_rejectSource != null)
            {
                _rejectSource.Rejected -= ShowRejected;
            }

            _rejectSource = actions;
            if (_rejectSource != null)
            {
                _rejectSource.Rejected += ShowRejected;
            }
        }

        private void ShowRejected(GatheringRejectReason reason)
        {
            switch (reason)
            {
                case GatheringRejectReason.Busy:
                    _notice.Show("다른 사람이 채집하고 있다.");
                    break;
                case GatheringRejectReason.Closed:
                    _notice.Show("영업이 끝나 더는 채집할 수 없다.");
                    break;
                case GatheringRejectReason.InventoryFull:
                    _notice.Show("가방에 자리가 없다.");
                    break;
                default:
                    _notice.Show("지금은 채집할 수 없다.");
                    break;
            }
        }

        private void OnSiteChanged()
        {
            if (_stageMap.IsBuilt && !IsCurrent)
            {
                _nextReleaseCheck = 0f;
                enabled = true;
            }
        }

        // 표시·로컬 물리용 자원 정리라 공유 상태와 관계없이 각자의 시간으로 확인한다.
        private void Update()
        {
            if (Time.unscaledTime < _nextReleaseCheck)
            {
                return;
            }

            _nextReleaseCheck = Time.unscaledTime + _releaseCheckInterval;
            if (IsCurrent || !_stageMap.IsBuilt)
            {
                enabled = false;
                return;
            }

            if (!HasPlayerInside())
            {
                ClearObjects();
                _stageMap.Clear();
                enabled = false;
            }
        }

        private void ClearObjects()
        {
            foreach (GameObject instance in _objects)
            {
                if (instance != null)
                {
                    Destroy(instance);
                }
            }

            _objects.Clear();
        }

        private bool HasPlayerInside()
        {
            Bounds area = _stageMap.Bounds;
            area.Expand(new Vector3(0f, 20f, 0f));
            int count = Physics.OverlapBoxNonAlloc(area.center, area.extents, _overlaps, Quaternion.identity,
                Physics.AllLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (_overlaps[i].GetComponentInParent<PlayerAreaTravel>() != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
