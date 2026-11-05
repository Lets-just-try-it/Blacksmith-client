using System.Collections.Generic;
using Blacksmith.Core.Battle;
using Blacksmith.Inventory;
using Blacksmith.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Battle
{
    // 밤 파견의 첫 화면인 던전 지도. 대륙 위 길을 따라 모든 단계의 스테이지를 거점으로 잇고(동그라미는 스테이지, 별은 보스),
    // 거점을 눌러 이번에 싸울 스테이지 하나를 고른 뒤 파견 창(DispatchView)으로 넘어간다 (docs/GAME_CONCEPT.md 7.3).
    // 열린 단계에서는 클리어한 스테이지와 다음 스테이지를 고를 수 있다.
    // 창은 DispatchState가 열려 있고 지도 단계일 때 모든 참가자에게 열리고, 고르기는 호스트만 한다.
    public class DungeonMapView : MonoBehaviour
    {
        // 거점 하나가 가리키는 던전 단계와 스테이지
        private struct MapStop
        {
            public int Tier;
            public int Stage;
            public bool IsBoss;
        }

        [SerializeField] private DispatchState _state;
        [SerializeField] private DungeonProgressState _progress;
        [SerializeField] private PlayerNetworkInputLock _inputLock;
        [Tooltip("창이 열린 동안 하단 개인 슬롯을 숨기는 데 쓴다")]
        [SerializeField] private InventoryChannel _inventoryChannel;
        [Tooltip("지도에 보여 줄 전체 던전 단계 수 (docs/GAME_CONCEPT.md 7.2). 던전 데이터가 없는 단계는 잠긴 거점으로 보인다")]
        [SerializeField, Min(1)] private int _totalTierCount = 10;
        [Tooltip("던전 데이터가 없는 단계에 그릴 스테이지 수. 마지막이 보스다 (docs/GAME_CONCEPT.md 7.2: 잡몹 2 + 보스 1)")]
        [SerializeField, Min(1)] private int _defaultStageCount = 3;
        [Tooltip("길을 따라 놓을 거점 위치. 지도 그림 기준 0~1(왼쪽 아래가 0)이고, 1단계 1스테이지부터 차례로 하나씩이다")]
        [SerializeField] private Vector2[] _stopPositions;
        [SerializeField] private GameObject _panel;
        [Tooltip("대륙 그림. 거점과 길은 이 영역 안에 놓는다")]
        [SerializeField] private RectTransform _mapArea;
        [SerializeField] private RectTransform _roadRoot;
        [SerializeField] private RectTransform _stopRoot;
        [SerializeField] private DungeonMapNode _stageNodePrefab;
        [SerializeField] private DungeonMapNode _bossNodePrefab;
        [Tooltip("길 점선 한 칸의 크기와 간격(픽셀)")]
        [SerializeField] private Vector2 _roadDashSize = new Vector2(9f, 3f);
        [SerializeField] private float _roadDashSpacing = 16f;
        [SerializeField] private TMP_Text _infoText;
        [SerializeField] private TMP_Text _hintText;
        [Tooltip("고른 스테이지로 파견 창(파티 고르기)으로 넘어간다")]
        [SerializeField] private Button _partyButton;
        [Tooltip("파견하지 않고 다음 날로 넘긴다")]
        [SerializeField] private Button _skipButton;
        [SerializeField] private Color _clearedColor = new Color(0.36f, 0.56f, 0.3f, 1f);
        [SerializeField] private Color _openColor = new Color(0.95f, 0.72f, 0.25f, 1f);
        [SerializeField] private Color _lockedColor = new Color(0.55f, 0.5f, 0.45f, 0.75f);
        [SerializeField] private Color _roadColor = new Color(0.45f, 0.12f, 0.08f, 1f);
        [SerializeField] private Color _lockedRoadColor = new Color(0.3f, 0.22f, 0.15f, 0.4f);

        private readonly List<MapStop> _stops = new List<MapStop>();
        private readonly List<DungeonMapNode> _nodes = new List<DungeonMapNode>();
        // 길 점선. 각 점선이 향하는 거점 번호와 함께 둔다. 그 거점이 잠겼으면 흐리게 칠한다.
        private readonly List<Image> _roadDashes = new List<Image>();
        private readonly List<int> _roadDashTargets = new List<int>();
        private bool _isShown;
        private bool _isLinked;

        private void Awake()
        {
            _panel.SetActive(false);
        }

        // 씬마다 파견 상태와 진행 상황을 연결한다. 연결하지 않은 씬에서는 지도를 쓰지 않고 파견 창부터 열린다.
        private void OnEnable()
        {
            _isLinked = _state != null && _progress != null;
            if (!_isLinked)
            {
                return;
            }

            _state.RegisterMap();
            _state.Changed += Refresh;
            _progress.Changed += Refresh;
            _partyButton.onClick.AddListener(ShowParty);
            _skipButton.onClick.AddListener(Skip);
        }

        private void OnDisable()
        {
            if (!_isLinked)
            {
                return;
            }

            _state.UnregisterMap();
            _state.Changed -= Refresh;
            _progress.Changed -= Refresh;
            _partyButton.onClick.RemoveListener(ShowParty);
            _skipButton.onClick.RemoveListener(Skip);
            SetShown(false);
        }

        private void ShowParty()
        {
            _state.ShowParty();
        }

        private void Skip()
        {
            _state.Skip();
        }

        private void Refresh()
        {
            bool isOpen = _state.IsReady && _progress.IsReady && _state.IsOpen && _state.Screen == DispatchScreen.Map;
            SetShown(isOpen);
            if (!isOpen)
            {
                return;
            }

            if (_nodes.Count == 0)
            {
                BuildMap();
            }

            bool canEdit = _state.CanEdit;
            ShowStops(canEdit);
            ShowSelection(canEdit);
        }

        // 창이 열린 동안 캐릭터 조작을 잠그고 하단 개인 슬롯을 숨긴다.
        private void SetShown(bool isShown)
        {
            if (_isShown == isShown)
            {
                return;
            }

            _isShown = isShown;
            _panel.SetActive(isShown);
            if (isShown)
            {
                _inputLock.Lock(this);
                _inventoryChannel.HideHud(this);
                return;
            }

            _inputLock.Release(this);
            _inventoryChannel.ShowHud(this);
        }

        // 거점과 길은 한 번만 만든다. 던전 데이터와 거점 위치는 플레이 중에 바뀌지 않는다.
        private void BuildMap()
        {
            int tierCount = Mathf.Max(_totalTierCount, _progress.TierCount);
            for (int tier = 0; tier < tierCount; tier++)
            {
                DungeonData dungeon = tier < _progress.TierCount ? _progress.GetDungeon(tier) : null;
                int stageCount = dungeon != null ? dungeon.StageCount : _defaultStageCount;
                for (int stage = 0; stage < stageCount; stage++)
                {
                    bool isBoss = dungeon != null ? dungeon.IsBossStage(stage) : stage == stageCount - 1;
                    _stops.Add(new MapStop { Tier = tier, Stage = stage, IsBoss = isBoss });
                }
            }

            if (_stops.Count > _stopPositions.Length)
            {
                Debug.LogWarning($"던전 지도 거점 위치가 {_stopPositions.Length}개뿐이라 스테이지 {_stops.Count}개 중 일부를 그리지 않는다.", this);
                _stops.RemoveRange(_stopPositions.Length, _stops.Count - _stopPositions.Length);
            }

            for (int i = 1; i < _stops.Count; i++)
            {
                AddRoad(_stopPositions[i - 1], _stopPositions[i], i);
            }

            for (int i = 0; i < _stops.Count; i++)
            {
                DungeonMapNode node = Instantiate(_stops[i].IsBoss ? _bossNodePrefab : _stageNodePrefab, _stopRoot);
                Place((RectTransform)node.transform, _stopPositions[i]);
                node.Clicked += HandleNodeClicked;
                _nodes.Add(node);
            }
        }

        // 두 거점 사이에 점선을 깐다. 점선 기울기는 지도 영역의 실제 가로세로 비율로 구한다.
        private void AddRoad(Vector2 from, Vector2 to, int targetStop)
        {
            Vector2 size = _mapArea.rect.size;
            Vector2 delta = Vector2.Scale(to - from, size);
            float length = delta.magnitude;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            int count = Mathf.Max(1, Mathf.FloorToInt(length / _roadDashSpacing));
            for (int i = 1; i < count; i++)
            {
                var dash = new GameObject("RoadDash", typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)dash.transform;
                rect.SetParent(_roadRoot, false);
                rect.sizeDelta = _roadDashSize;
                rect.localRotation = Quaternion.Euler(0f, 0f, angle);
                Place(rect, Vector2.Lerp(from, to, i / (float)count));
                var image = dash.GetComponent<Image>();
                image.raycastTarget = false;
                _roadDashes.Add(image);
                _roadDashTargets.Add(targetStop);
            }
        }

        private static void Place(RectTransform rect, Vector2 normalizedPosition)
        {
            rect.anchorMin = normalizedPosition;
            rect.anchorMax = normalizedPosition;
            rect.anchoredPosition = Vector2.zero;
        }

        private void ShowStops(bool canEdit)
        {
            var isOpen = new bool[_stops.Count];
            for (int i = 0; i < _stops.Count; i++)
            {
                MapStop stop = _stops[i];
                bool isTierOpen = stop.Tier < _progress.TierCount && _progress.IsTierSelectable(stop.Tier);
                DungeonData dungeon = stop.Tier < _progress.TierCount ? _progress.GetDungeon(stop.Tier) : null;
                int cleared = dungeon != null ? _progress.GetClearedStages(dungeon.DungeonId) : 0;
                bool isCleared = isTierOpen && DungeonProgressRules.IsStageCleared(cleared, stop.Stage);
                bool canSelect = isTierOpen && DungeonProgressRules.CanSelectStage(cleared, dungeon.StageCount, stop.Stage);
                isOpen[i] = canSelect;

                Color color = isCleared ? _clearedColor : canSelect ? _openColor : _lockedColor;
                bool isSelected = stop.Tier == _state.TierIndex && stop.Stage == _state.Stage;
                _nodes[i].Show(i, stop.IsBoss ? GetTierLabel(stop.Tier, dungeon, isTierOpen) : "", color, canEdit && canSelect, isSelected);
            }

            for (int i = 0; i < _roadDashes.Count; i++)
            {
                _roadDashes[i].color = isOpen[_roadDashTargets[i]] ? _roadColor : _lockedRoadColor;
            }
        }

        private static string GetTierLabel(int tier, DungeonData dungeon, bool isTierOpen)
        {
            string name = dungeon != null && isTierOpen ? dungeon.DisplayName : "???";
            return $"{tier + 1}단계\n{name}";
        }

        private void ShowSelection(bool canEdit)
        {
            DungeonData dungeon = _state.SelectedDungeon;
            _infoText.text = _state.Stage < 0
                ? "고를 스테이지가 없다"
                : $"{_state.TierIndex + 1}단계 {dungeon.DisplayName} · {DungeonMapLabels.GetStageName(dungeon, _state.Stage)} · 보상 골드 {dungeon.GetStageGold(_state.Stage)}"
                    + (_state.IsReplay() ? "\n이미 클리어한 스테이지다. 다시 이겨도 진행은 늘지 않는다" : "");
            _hintText.text = canEdit
                ? "길 위의 거점을 눌러 싸울 스테이지를 고른 뒤 파티를 꾸린다 · 별은 보스"
                : "파견은 호스트가 고른다";

            _partyButton.gameObject.SetActive(canEdit);
            _partyButton.interactable = _state.Stage >= 0;
            _skipButton.gameObject.SetActive(canEdit);
        }

        private void HandleNodeClicked(DungeonMapNode node)
        {
            MapStop stop = _stops[node.Index];
            _state.Select(stop.Tier, stop.Stage);
        }
    }
}
