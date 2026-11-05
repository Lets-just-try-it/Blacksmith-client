using System;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 미니게임의 손맛 연출(효과음, 화면 흔들림·확대, 번쩍임, 타격 입자)을 켜고 끄는 설정이다.
    // 규칙·점수·판정 글자는 바뀌지 않고, 켜고 끄는 것은 내 컴퓨터에서만 적용된다.
    // 오디오 스레드(ForgeNoiseSynth)도 읽으므로 volatile이다.
    public static class MinigameFeedbackSwitch
    {
        private static volatile bool _enabled = true;

        public static bool Enabled => _enabled;

        public static event Action Changed;

        public static void SetEnabled(bool enabled)
        {
            if (_enabled == enabled)
            {
                return;
            }

            _enabled = enabled;
            Changed?.Invoke();
        }

        // 도메인 리로드 없이 플레이를 다시 시작해도 이전 세션의 값과 구독이 남지 않게 한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            _enabled = true;
            Changed = null;
        }
    }
}
