using UnityEngine;

namespace Blacksmith.Network
{
    // 하루 길이와 낮에 보여 줄 게임 속 시각 범위다. (docs/GAME_CONCEPT.md 2.3: 낮 10분 = 게임 속 10시~20시)
    [CreateAssetMenu(fileName = "DayCycle", menuName = "Blacksmith/Balance/Day Cycle")]
    public class DayCycleData : ScriptableObject
    {
        [Tooltip("[추정] 낮(영업) 길이(초)")]
        [SerializeField, Min(1f)] private float _daySeconds = 600f;
        [Tooltip("낮이 시작될 때의 게임 속 시각(시, 0~23)")]
        [SerializeField, Range(0, 23)] private int _openHour = 10;
        [Tooltip("낮이 끝날 때의 게임 속 시각(시). 시작 시각보다 커야 한다")]
        [SerializeField, Range(1, 24)] private int _closeHour = 20;
        [Tooltip("[추정] 낮이 끝나고 밤이 되기 전 \"밤이 되었습니다\"를 띄우고 조명을 어둡게 하는 연출 시간(초). 이 동안 시간이 멈춘다")]
        [SerializeField, Min(0f)] private float _duskSeconds = 3f;

        public float DaySeconds => _daySeconds;
        public int OpenHour => _openHour;
        public int CloseHour => _closeHour;
        public float DuskSeconds => _duskSeconds;
    }
}
