using UnityEngine;

namespace Blacksmith.UI
{
    // 화면 페이드를 요청하는 에셋이다. Player 프리팹이나 Additive로 불러온 씬처럼 페이더를 인스펙터로 참조할 수 없는 쪽도 이 에셋을 거친다.
    // 페이더는 씬마다 하나를 두고 스스로 등록한다. 페이더가 없는 씬(테스트 씬)에서는 요청이 바로 끝난다.
    [CreateAssetMenu(fileName = "ScreenFadeChannel", menuName = "Blacksmith/Runtime/Screen Fade Channel")]
    public class ScreenFadeChannel : ScriptableObject
    {
        private ScreenFader _fader;

        public void Register(ScreenFader fader)
        {
            _fader = fader;
        }

        public void Unregister(ScreenFader fader)
        {
            if (_fader == fader)
            {
                _fader = null;
            }
        }

        // 화면이 완전히 검어지면 끝난다.
        public async Awaitable FadeOut()
        {
            if (_fader != null)
            {
                await _fader.FadeOut();
            }
        }

        public async Awaitable FadeIn()
        {
            if (_fader != null)
            {
                await _fader.FadeIn();
            }
        }
    }
}
