using UnityEngine;

namespace Blacksmith.Characters
{
    // 인간형 모델의 자세 표시만 맡는다: 상반신 들기 자세(UpperBody 레이어)와 작업 동작(WorkPose 파라미터).
    // 어떤 자세를 취할지는 부르는 쪽(손에 든 아이템, 대장간 작업)이 정한다. 작업 동작 중에는 들기 자세를 끈다.
    [RequireComponent(typeof(Animator))]
    public class HumanoidPose : MonoBehaviour
    {
        public const int NoWork = 0;

        private static readonly int WorkPoseHash = Animator.StringToHash("WorkPose");

        [Tooltip("팔만 덮어쓰는 상반신 레이어 이름")]
        [SerializeField] private string _upperBodyLayer = "UpperBody";
        [Tooltip("들기 자세를 켜고 끄는 데 걸리는 시간(초)")]
        [SerializeField, Min(0.01f)] private float _blendSeconds = 0.15f;

        private Animator _animator;
        private int _layerIndex = -1;
        private bool _isHolding;
        private int _workPose;
        private float _weight;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _layerIndex = _animator.GetLayerIndex(_upperBodyLayer);
            if (_layerIndex < 0)
            {
                Debug.LogWarning($"애니메이터에 상반신 레이어가 없다: {_upperBodyLayer}", this);
            }
        }

        public void SetHolding(bool isHolding)
        {
            _isHolding = isHolding;
        }

        // pose: 애니메이터의 WorkPose 값. NoWork(0)면 작업 동작을 끝낸다.
        public void SetWork(int pose)
        {
            _workPose = pose;
            _animator.SetInteger(WorkPoseHash, pose);
        }

        private void Update()
        {
            if (_layerIndex < 0)
            {
                return;
            }

            float target = _isHolding && _workPose == NoWork ? 1f : 0f;
            if (Mathf.Approximately(_weight, target))
            {
                return;
            }

            _weight = Mathf.MoveTowards(_weight, target, Time.deltaTime / _blendSeconds);
            _animator.SetLayerWeight(_layerIndex, _weight);
        }
    }
}
