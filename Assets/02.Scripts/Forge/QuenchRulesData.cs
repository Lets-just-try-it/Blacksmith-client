using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    [CreateAssetMenu(fileName = "QuenchRules", menuName = "Blacksmith/Balance/Quench Rules")]
    public class QuenchRulesData : ScriptableObject
    {
        [SerializeField, Min(0f)] private float _carryTime = 0.45f;
        [SerializeField, Min(0f)] private float _showSeconds = 2f;
        [SerializeField, Min(0f)] private float _readyLockTime = 0.6f;
        [SerializeField, Min(0f)] private float _readyAutoTime = 4f;
        [SerializeField, Min(0f)] private float _minPullTime = 0.25f;
        [SerializeField, Min(0f)] private float _endDelay = 1.6f;
        [SerializeField, Min(0f)] private float _limitTargetMultiplier = 2f;
        [SerializeField, Min(0f)] private float _limitExtraSeconds = 3f;
        [SerializeField, Range(0f, 1f)] private float _timeoutAccuracy = 0.1f;
        [SerializeField, Range(0f, 1f)] private float _perfectEventAccuracy = 0.95f;

        [Tooltip("난이도 등급 0, 1, 2 순서. 재료 단계가 올라갈수록 어렵다.")]
        [SerializeField] private QuenchDifficulty[] _difficulties =
        {
            new QuenchDifficulty { MinTarget = 4f, TargetRandomRange = 2.5f, PerfectFraction = 0.06f, ZeroFraction = 0.32f },
            new QuenchDifficulty { MinTarget = 6.5f, TargetRandomRange = 3f, PerfectFraction = 0.035f, ZeroFraction = 0.18f },
            new QuenchDifficulty { MinTarget = 10f, TargetRandomRange = 4f, PerfectFraction = 0.015f, ZeroFraction = 0.08f },
        };

        public QuenchRules ToRules()
        {
            return new QuenchRules
            {
                CarryTime = _carryTime,
                ShowSeconds = _showSeconds,
                ReadyLockTime = _readyLockTime,
                ReadyAutoTime = _readyAutoTime,
                MinPullTime = _minPullTime,
                EndDelay = _endDelay,
                LimitTargetMultiplier = _limitTargetMultiplier,
                LimitExtraSeconds = _limitExtraSeconds,
                TimeoutAccuracy = _timeoutAccuracy,
                PerfectEventAccuracy = _perfectEventAccuracy,
                Difficulties = _difficulties,
            };
        }

        private void OnValidate()
        {
            if (_difficulties.Length == 0)
            {
                Debug.LogWarning("난이도 등급이 하나 이상 있어야 한다.", this);
                return;
            }

            for (int i = 0; i < _difficulties.Length; i++)
            {
                if (_difficulties[i].ZeroFraction <= _difficulties[i].PerfectFraction)
                {
                    Debug.LogWarning($"난이도 {i}: 0점 비율은 만점 비율보다 커야 한다.", this);
                }
            }
        }
    }
}
