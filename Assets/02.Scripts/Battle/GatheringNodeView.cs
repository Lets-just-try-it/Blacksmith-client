using Blacksmith.Network;
using TMPro;
using UnityEngine;

namespace Blacksmith.Battle
{
    // 채집 맵의 채집할 것 하나(시체 더미, 광맥, 나무, 바닥의 광석·목재)다. GatheringSiteView가 맵을 깔 때 만든다.
    // 내 캐릭터가 범위 안에서 상호작용 키를 누르면 호스트에 캐겠다고 요청한다. 캐는 동안 캐릭터 조작을 잠그고 남은 시간을 띄운다.
    // 다 캐면 모든 참가자에게서 사라진다. 상태는 GatheringNodesState(호스트)가 정하고, 이 컴포넌트는 보여 주기만 한다.
    [RequireComponent(typeof(Collider))]
    public class GatheringNodeView : MonoBehaviour
    {
        [Tooltip("다 캐면 숨기는 모습")]
        [SerializeField] private GameObject _visual;
        [Tooltip("내 캐릭터가 범위 안에 있을 때 띄우는 안내")]
        [SerializeField] private GameObject _prompt;
        [SerializeField] private TMP_Text _promptText;
        [Tooltip("상호작용 안내에 쓰는 동작 이름. 예: 캐기, 베기")]
        [SerializeField] private string _actionLabel = "채집";

        private Collider _range;
        private GatheringSiteView _site;
        private int _index;
        private NetworkPlayer _localPlayer;
        private NetworkGatheringNode _node;
        private bool _isLocking;

        // 다 캐면 함께 숨길 것(시체 더미의 시체)을 넣는 곳
        public Transform Visual => _visual.transform;

        private bool IsMine => _node.Status == GatheringNodeStatus.InProgress && _localPlayer != null
            && _node.User == _localPlayer.Object.InputAuthority;

        public void Init(GatheringSiteView site, int index)
        {
            _site = site;
            _index = index;
            _site.Nodes.Changed += Refresh;
            Refresh();
        }

        private void Awake()
        {
            _range = GetComponent<Collider>();
            _prompt.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_site != null)
            {
                _site.Nodes.Changed -= Refresh;
                SetLocked(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            NetworkPlayer player = other.GetComponentInParent<NetworkPlayer>();
            if (player != null && player.HasInputAuthority)
            {
                _localPlayer = player;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            NetworkPlayer player = other.GetComponentInParent<NetworkPlayer>();
            if (player != null && player == _localPlayer && !IsMine)
            {
                _localPlayer = null;
            }
        }

        private void Refresh()
        {
            _node = _site.Nodes.Get(_site.Seed, _index);
            _visual.SetActive(_node.Status != GatheringNodeStatus.Taken);
            SetLocked(IsMine);
        }

        private void Update()
        {
            if (_site == null || _node.Status == GatheringNodeStatus.Taken)
            {
                _prompt.SetActive(false);
                return;
            }

            // 순간이동 등으로 OnTriggerExit가 오지 않아도 범위를 벗어나면 놓는다.
            if (_localPlayer != null && !IsMine && !IsInRange(_localPlayer.transform.position))
            {
                _localPlayer = null;
            }

            if (IsMine)
            {
                _prompt.SetActive(true);
                _promptText.text = $"{_actionLabel} 중… {Mathf.CeilToInt(_site.Nodes.GetRemainingSeconds(_node))}";
                return;
            }

            bool canRequest = _localPlayer != null && _node.Status == GatheringNodeStatus.Available && !_site.InputLock.IsLocked;
            _prompt.SetActive(canRequest);
            if (!canRequest)
            {
                return;
            }

            _promptText.text = $"E: {_actionLabel}";
            if (_site.Interact.action.WasPressedThisFrame())
            {
                _site.RequestGather(_localPlayer, _index);
            }
        }

        private void SetLocked(bool isLocked)
        {
            if (isLocked == _isLocking)
            {
                return;
            }

            _isLocking = isLocked;
            if (isLocked)
            {
                _site.InputLock.Lock(this);
            }
            else
            {
                _site.InputLock.Release(this);
            }
        }

        // 높이는 보지 않고 바닥 기준 범위만 확인한다.
        private bool IsInRange(Vector3 position)
        {
            Bounds bounds = _range.bounds;
            return bounds.Contains(new Vector3(position.x, bounds.center.y, position.z));
        }
    }
}
