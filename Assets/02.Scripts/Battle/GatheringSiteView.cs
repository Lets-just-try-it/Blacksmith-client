using Blacksmith.Core.Battle;
using Blacksmith.Network;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 낮 씬에 채집할 맵의 바닥을 깐다 (docs/GAME_CONCEPT.md 7.4). 전투와 같은 시드로 다시 만들어 모든 참가자에게 같은 맵이 나온다.
    // 맵의 시작 지점이 StageMapView의 위치에 온다. 광장의 DungeonGate가 플레이어를 시작 지점으로 보낸다.
    // 메모리를 아끼려고 누군가 들어갈 때 만들고(EnsureBuilt), 채집할 맵이 바뀌어(다음 날) 지난 맵이 되면 버린다.
    // 참가자마다 따로 만들고 버린다. 지난 맵이어도 안에 플레이어가 있으면 바닥이 꺼지지 않게, 모두 나갈 때까지 기다렸다가 버린다.
    public class GatheringSiteView : MonoBehaviour
    {
        // 맵 영역 안의 콜라이더를 모으는 버퍼 크기. 바닥·벽과 플레이어(최대 4명)보다 넉넉하다.
        private const int OverlapBufferSize = 32;

        [SerializeField] private GatheringSiteState _site;
        [SerializeField] private DungeonProgressState _progress;
        [SerializeField] private StageMapView _stageMap;
        [Tooltip("지난 맵 안에 플레이어가 남았는지 확인하는 간격(초)")]
        [SerializeField, Min(0.1f)] private float _releaseCheckInterval = 1f;

        private readonly Collider[] _overlaps = new Collider[OverlapBufferSize];
        private int _builtDungeonId;
        private int _builtSeed;
        private float _nextReleaseCheck;

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
        }

        // 채집할 맵이 있고 아직 깔지 않았으면 깐다. 화면이 가려진 동안(문으로 들어갈 때) 부른다.
        public void EnsureBuilt()
        {
            if (!_site.IsReady || !_site.HasSite || IsCurrent)
            {
                return;
            }

            DungeonData dungeon = _progress.FindDungeon(_site.DungeonId);
            if (dungeon == null)
            {
                Debug.LogError($"채집할 던전 데이터를 찾을 수 없다: {_site.DungeonId}", this);
                return;
            }

            StageMap map = dungeon.GenerateMap(_site.Seed);
            _stageMap.Build(map, dungeon.StageMap);
            _builtDungeonId = _site.DungeonId;
            _builtSeed = _site.Seed;
            enabled = false;
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
                _stageMap.Clear();
                enabled = false;
            }
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
