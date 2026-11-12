using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 활 깎기(톱질) 수치. 모두 프로토타입 값이며 플레이 테스트 후 조정한다.
    [CreateAssetMenu(fileName = "BowSawRules", menuName = "Blacksmith/Balance/Bow Saw Rules")]
    public class BowSawRulesData : ScriptableObject
    {
        [Header("진행")]
        [SerializeField, Min(0f)] private float _introTime = 1f;
        [SerializeField, Min(0f)] private float _endDelay = 1f;

        [Header("톱질")]
        [Tooltip("톱질 한 번(위로 올렸다 아래로 내리기)으로 치는 최소 거리(칸)")]
        [SerializeField, Min(0.5f)] private float _strokeMin = 2.5f;
        [Tooltip("이만큼(칸) 되돌아오면 방향을 바꾼 것으로 본다")]
        [SerializeField, Min(0.05f)] private float _hysteresis = 0.25f;
        [SerializeField, Min(0f)] private float _bindCooldown = 0.8f;
        [Tooltip("구간 위쪽 한계의 이 배수보다 세게 톱질하면 톱이 낀다")]
        [SerializeField, Min(1f)] private float _bindFactor = 1.4f;
        [Tooltip("구간 아래쪽 한계의 이 비율보다 약하면 거의 안 켜진다")]
        [SerializeField, Range(0.1f, 1f)] private float _slowFactor = 0.6f;
        [SerializeField, Range(0f, 1f)] private float _edgeQuality = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _slowQuality = 0.2f;

        [Header("점수")]
        [Tooltip("구간 안에서 연속으로 톱질한 횟수가 이만큼 쌓일 때마다 완벽으로 알린다")]
        [SerializeField, Min(0)] private int _perfectStreak = 4;
        [SerializeField, Range(0f, 1f)] private float _successScore = 0.6f;

        [Tooltip("광석 등급 0, 1, 2 순서. 활 깎기는 참나무만 쓰므로 재료 티어가 없어 첫 항목을 쓴다. Logs는 만점에 필요한 통나무 수다.")]
        [SerializeField] private BowSawDifficulty[] _difficulties =
        {
            new BowSawDifficulty { Strokes = 8, ZoneLo = 6f, ZoneHi = 10f, Logs = 8, TimeLimit = 40f },
            new BowSawDifficulty { Strokes = 9, ZoneLo = 6.5f, ZoneHi = 9.5f, Logs = 10, TimeLimit = 40f },
            new BowSawDifficulty { Strokes = 10, ZoneLo = 7f, ZoneHi = 9f, Logs = 12, TimeLimit = 40f },
        };

        public BowSawRules ToRules()
        {
            return new BowSawRules
            {
                IntroTime = _introTime,
                EndDelay = _endDelay,
                StrokeMin = _strokeMin,
                Hysteresis = _hysteresis,
                BindCooldown = _bindCooldown,
                BindFactor = _bindFactor,
                SlowFactor = _slowFactor,
                EdgeQuality = _edgeQuality,
                SlowQuality = _slowQuality,
                PerfectStreak = _perfectStreak,
                SuccessScore = _successScore,
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
                BowSawDifficulty difficulty = _difficulties[i];
                if (difficulty.Strokes <= 0 || difficulty.Logs <= 0 || difficulty.ZoneLo <= 0f || difficulty.ZoneHi <= difficulty.ZoneLo || difficulty.TimeLimit <= 0f)
                {
                    Debug.LogWarning($"난이도 {i}: 톱질 횟수와 시간은 0보다 크고 힘 구간의 위쪽 한계는 아래쪽 한계보다 커야 한다.", this);
                }
            }
        }
    }
}
