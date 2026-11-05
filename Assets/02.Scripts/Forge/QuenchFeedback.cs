using UnityEngine;

namespace Blacksmith.Forge
{
    // 담금질의 소리와 연출을 맡는다. 달군 칼날이 타닥거리는 소리, 수조로 옮길 때의 바람 소리, 물에 닿는 소리와 김,
    // 담근 동안 이어지는 치익 소리, 담근 초를 세는 틱, 꺼낼 때의 소리, 결과 소리와 화면 흔들림이다.
    // 규칙은 건드리지 않고 QuenchStation이 넘겨 주는 사건에 반응만 한다.
    // 담근 동안의 치익 소리는 일정하게 둔다. 소리의 변화가 기준 시간의 힌트가 되면 안 된다.
    public class QuenchFeedback : MonoBehaviour
    {
        [SerializeField] private AudioSource _sfx;
        [SerializeField] private ForgeNoiseSynth _loopSound;
        [SerializeField] private ParticleSystem _steam;
        [SerializeField] private Light _glow;

        private ForgeCameraFocus _camera;
        private Vector3 _waterPoint;
        private bool _submerged;
        private float _crackleTimer;

        public void Begin(ForgeCameraFocus camera, Vector3 waterPoint)
        {
            _camera = camera;
            _waterPoint = waterPoint;
            _submerged = false;
            _steam.transform.position = waterPoint;
            ForgeGreybox.SetRate(_steam, 0f);
            // 달군 칼날이 식으며 타닥거리는 낮은 소리
            _loopSound.Volume = 0.03f;
            _loopSound.Brightness = 0.25f;
            _loopSound.Crackle = 0.5f;
            _glow.intensity = 0.8f;
        }

        public void End()
        {
            _loopSound.Volume = 0f;
            _loopSound.Crackle = 0f;
            ForgeGreybox.SetRate(_steam, 0f);
            _glow.intensity = 0f;
            _camera = null;
            _submerged = false;
        }

        // 행동 키를 눌러 칼날을 수조로 옮기기 시작했다.
        public void PlayCarry()
        {
            Play(ForgeProceduralSfx.Whoosh(), 0.5f, 0.8f);
        }

        // 칼날이 물에 닿았다: 풍덩 소리, 김이 터지고 화면이 흔들린다.
        public void PlaySplash()
        {
            _submerged = true;
            Play(ForgeProceduralSfx.Gulp(), 0.7f, 0.7f);
            Play(ForgeProceduralSfx.Boom(), 0.5f, 1.3f);
            ForgeGreybox.Burst(_steam, 60);
            ForgeGreybox.SetRate(_steam, 22f);
            Shake(0.012f, 0.25f);
            Punch(0.02f);

            // 담그는 동안은 치익 소리가 일정하게 이어지고, 붉은 빛은 사라진다.
            _loopSound.Volume = 0.13f;
            _loopSound.Brightness = 0.85f;
            _loopSound.Crackle = 0f;
            _glow.intensity = 0f;
        }

        // 담근 뒤 초를 보여 주는 동안의 틱 소리
        public void PlaySecond(int second)
        {
            Play(ForgeProceduralSfx.Tick(), 0.45f, 0.9f + 0.08f * second);
        }

        // 칼날을 꺼냈다: 물이 튀는 소리와 김이 크게 올라온다.
        public void PlayPull(bool timedOut)
        {
            _submerged = false;
            _loopSound.Volume = 0f;
            Play(ForgeProceduralSfx.Whoosh(), 0.6f, 1.4f);
            Play(ForgeProceduralSfx.Gulp(), 0.35f, 1.5f);
            ForgeGreybox.Burst(_steam, timedOut ? 40 : 90);
            ForgeGreybox.SetRate(_steam, 0f);
            Shake(timedOut ? 0.006f : 0.016f, 0.2f);
            Punch(timedOut ? 0.01f : 0.035f);
        }

        // 결과 소리. 기준 시간에 딱 맞으면 가장 화려하게 울린다.
        public void PlayResult(float accuracy, bool perfect, bool timedOut)
        {
            if (timedOut)
            {
                Play(ForgeProceduralSfx.Fail(), 0.7f, 0.9f);
            }
            else if (perfect)
            {
                Play(ForgeProceduralSfx.Chime(1318f), 0.7f, 1f);
                Play(ForgeProceduralSfx.Success(), 0.7f, 1f);
                Punch(0.03f);
            }
            else if (accuracy >= 0.6f)
            {
                Play(ForgeProceduralSfx.Ding(880f), 0.6f, 1f);
                Play(ForgeProceduralSfx.Success(), 0.5f, 0.95f);
            }
            else
            {
                Play(ForgeProceduralSfx.Fail(), 0.7f, 1f);
            }
        }

        // 물에 담긴 동안 김이 일정하게 오르고, 아직 담그기 전에는 칼날 쪽 붉은 빛이 은은하게 숨 쉰다.
        private void Update()
        {
            if (_camera == null)
            {
                return;
            }

            if (!_submerged && _glow.intensity > 0f)
            {
                _crackleTimer += Time.deltaTime;
                _glow.intensity = 0.7f + 0.2f * Mathf.Sin(_crackleTimer * 9f);
            }
        }

        private void Shake(float amplitude, float seconds)
        {
            if (_camera != null)
            {
                _camera.Shake(amplitude, seconds);
            }
        }

        private void Punch(float amount)
        {
            if (_camera != null)
            {
                _camera.Punch(amount);
            }
        }

        private void Play(AudioClip clip, float volume, float pitch)
        {
            if (!MinigameFeedbackSwitch.Enabled)
            {
                return;
            }

            _sfx.pitch = pitch;
            _sfx.PlayOneShot(clip, volume);
        }
    }
}
