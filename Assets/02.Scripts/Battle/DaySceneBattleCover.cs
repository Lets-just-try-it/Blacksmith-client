using Blacksmith.Network;
using Blacksmith.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Blacksmith.Battle
{
    // 전투 씬이 떠 있는 동안 낮 씬의 카메라·Screen Space UI·실시간 조명을 끄고 캐릭터 조작을 잠근다.
    // 두 씬이 함께 떠 있으면 서로 겹치기 때문이다 (docs/NETWORK_DESIGN.md 8절). 모든 참가자의 낮 씬에 둔다.
    // EventSystem과 AudioListener는 낮 씬 것을 그대로 쓰므로, 카메라는 오브젝트가 아니라 Camera 컴포넌트만 끈다.
    // 전투 씬을 불러오고 내리기 전에 BattleSceneLoader가 어둡게 한 화면을, 화면을 바꾼 뒤 다시 밝힌다.
    // 임시 구현: 낮·밤 사이클이 생기면 그 화면 전환과 합친다.
    public class DaySceneBattleCover : MonoBehaviour
    {
        [SerializeField] private BattleChannel _channel;
        [SerializeField] private PlayerNetworkInputLock _inputLock;
        [SerializeField] private ScreenFadeChannel _screenFade;
        [SerializeField] private Camera[] _cameras = new Camera[0];
        [SerializeField] private Canvas[] _canvases = new Canvas[0];
        [SerializeField] private Light[] _lights = new Light[0];

        private bool _isCovered;

        private void OnEnable()
        {
            _channel.BattleOpened += Cover;
            _channel.BattleClosed += Uncover;
            if (_channel.IsBattleOpen)
            {
                Cover();
            }
        }

        private void OnDisable()
        {
            _channel.BattleOpened -= Cover;
            _channel.BattleClosed -= Uncover;
            Uncover();
        }

        private void Cover()
        {
            if (_isCovered)
            {
                return;
            }

            _isCovered = true;
            SetDayViewEnabled(false);
            _inputLock.Lock(this);
            _ = _screenFade.FadeIn();
        }

        private void Uncover()
        {
            if (!_isCovered)
            {
                return;
            }

            _isCovered = false;
            // 씬을 내릴 때는 낮 씬 오브젝트가 먼저 파괴되었을 수 있다.
            SetDayViewEnabled(true);
            _inputLock.Release(this);
            if (gameObject.scene.isLoaded)
            {
                SceneManager.SetActiveScene(gameObject.scene);
            }

            _ = _screenFade.FadeIn();
        }

        private void SetDayViewEnabled(bool isEnabled)
        {
            foreach (Camera dayCamera in _cameras)
            {
                if (dayCamera != null)
                {
                    dayCamera.enabled = isEnabled;
                }
            }

            foreach (Canvas canvas in _canvases)
            {
                if (canvas != null)
                {
                    canvas.enabled = isEnabled;
                }
            }

            foreach (Light dayLight in _lights)
            {
                if (dayLight != null)
                {
                    dayLight.enabled = isEnabled;
                }
            }
        }
    }
}
