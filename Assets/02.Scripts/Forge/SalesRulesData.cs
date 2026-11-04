using Blacksmith.Core.Sales;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 손님 방문 수치. 모두 임시 값이며 플레이 테스트 후 조정한다.
    [CreateAssetMenu(fileName = "SalesRules", menuName = "Blacksmith/Balance/Sales Rules")]
    public class SalesRulesData : ScriptableObject
    {
        [Tooltip("손님이 새로 오는 간격(초)")]
        [SerializeField, Min(1f)] private float _customerInterval = 12f;
        [Tooltip("한 곳에서 둘러보는 시간(초)")]
        [SerializeField, Min(0f)] private float _browseTime = 3f;
        [Tooltip("손님이 걷는 속도(m/초)")]
        [SerializeField, Min(0.1f)] private float _walkSpeed = 1.8f;
        [Tooltip("가게 안에 동시에 있을 수 있는 손님 수")]
        [SerializeField, Min(1)] private int _maxCustomers = 2;

        [Tooltip("장비가 올라간 진열대 앞에서 사는 확률(0~1). 안 사면 다른 곳을 둘러본다")]
        [SerializeField, Range(0f, 1f)] private float _buyChance = 0.5f;
        [Tooltip("나가기 전에 들르는 곳 수의 최소")]
        [SerializeField, Min(1)] private int _minStops = 3;
        [Tooltip("나가기 전에 들르는 곳 수의 최대")]
        [SerializeField, Min(1)] private int _maxStops = 5;

        public SalesRules ToRules()
        {
            return new SalesRules
            {
                CustomerInterval = _customerInterval,
                BrowseTime = _browseTime,
                WalkSpeed = _walkSpeed,
                MaxCustomers = _maxCustomers,
                BuyChance = _buyChance,
                MinStops = _minStops,
                MaxStops = _maxStops,
            };
        }
    }
}
