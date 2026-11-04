using Blacksmith.Core;
using Blacksmith.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.UI
{
    // 화면 왼쪽 위에 낮의 게임 속 시각을 아날로그 시계와 디지털 시계로 보여 준다.
    // 아날로그: 12시간 시계판에서 문 여는 시각(10시)부터 지금 시각까지를 채운 부채꼴과 두 바늘로 흐른 시간을 보여 준다.
    // 디지털: 시계 아래에 "10:10"처럼 24시간제로 보여 준다. 매 분이 아니라 10분 단위로만 바꾼다.
    // 시각은 매 프레임 흐르는 표시라 Update에서 고친다.
    public class DayCycleHud : MonoBehaviour
    {
        private const int MinutesPerDial = 12 * 60;
        private const int DigitalStepMinutes = 10;

        [SerializeField] private GameState _gameState;
        [Tooltip("Filled·Radial360·시작점 위·시계 방향으로 둔 부채꼴 이미지. 흐른 시간만큼 채운다")]
        [SerializeField] private Image _elapsedFill;
        [Tooltip("문 여는 시각을 가리키는 바늘. 피벗이 시계 가운데에 오게 둔다")]
        [SerializeField] private RectTransform _openHand;
        [Tooltip("지금 시각을 가리키는 바늘. 피벗이 시계 가운데에 오게 둔다")]
        [SerializeField] private RectTransform _currentHand;
        [SerializeField] private TMP_Text _timeText;
        [Tooltip("며칠째인지와 영업 마감·밤을 적는다")]
        [SerializeField] private TMP_Text _dayText;

        private int _shownMinutes = -1;
        private int _shownDigital = -1;
        private int _shownDay = -1;
        private DayPhase _shownPhase;

        private void Start()
        {
            int openMinutes = _gameState.OpenClockMinutes;
            SetHandAngle(_openHand, openMinutes);
            // 부채꼴의 시작점(위)을 문 여는 시각 쪽으로 돌린다.
            _elapsedFill.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -GetDialAngle(openMinutes));
        }

        private void Update()
        {
            if (!_gameState.IsReady)
            {
                _shownDay = -1;
                _dayText.text = string.Empty;
                _timeText.text = string.Empty;
                return;
            }

            ShowAnalog(_gameState.ClockMinutes);
            ShowDigital(_gameState.ClockMinutes);
            ShowDay();
        }

        private void ShowAnalog(int minutes)
        {
            if (minutes == _shownMinutes)
            {
                return;
            }

            _shownMinutes = minutes;
            int elapsed = Mathf.Max(0, minutes - _gameState.OpenClockMinutes);
            _elapsedFill.fillAmount = Mathf.Clamp01(elapsed / (float)MinutesPerDial);
            SetHandAngle(_currentHand, minutes);
        }

        private void ShowDigital(int minutes)
        {
            int stepped = minutes / DigitalStepMinutes * DigitalStepMinutes;
            if (stepped == _shownDigital)
            {
                return;
            }

            _shownDigital = stepped;
            _timeText.text = $"{stepped / 60 % 24:00}:{stepped % 60:00}";
        }

        private void ShowDay()
        {
            if (_gameState.Day == _shownDay && _gameState.Phase == _shownPhase)
            {
                return;
            }

            _shownDay = _gameState.Day;
            _shownPhase = _gameState.Phase;
            _dayText.text = _shownPhase switch
            {
                DayPhase.Closing => $"{_shownDay}일째 · 영업 마감 중",
                DayPhase.Dusk or DayPhase.Night => $"{_shownDay}일째 · 밤",
                _ => $"{_shownDay}일째",
            };
        }

        private static void SetHandAngle(RectTransform hand, int minutes)
        {
            hand.localRotation = Quaternion.Euler(0f, 0f, -GetDialAngle(minutes));
        }

        // 12시 방향에서 시계 방향으로 잰 각도.
        private static float GetDialAngle(int minutes)
        {
            return minutes % MinutesPerDial / (float)MinutesPerDial * 360f;
        }
    }
}
