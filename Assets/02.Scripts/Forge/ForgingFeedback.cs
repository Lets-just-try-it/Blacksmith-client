using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 단조의 소리(망치 내려치는 소리, 힘 모으는 소리, 허공 휘두르는 소리, 결과 소리)와 타격감(불꽃, 충격파, 조명, 화면 흔들림·확대·번쩍임)을 맡는다.
    // 규칙은 건드리지 않고 ForgingStation이 넘겨 주는 사건에 반응만 한다.
    public class ForgingFeedback : MonoBehaviour
    {
        [SerializeField] private AudioSource _sfx;
        [SerializeField] private ForgeNoiseSynth _chargeSound;
        [SerializeField] private ParticleSystem _sparks;
        [SerializeField] private ParticleSystem _shock;
        [SerializeField] private Light _glow;

        private ForgeCameraFocus _camera;
        private ForgingView _view;
        private float _glowPulse;

        public void Begin(ForgeCameraFocus camera, ForgingView view)
        {
            _camera = camera;
            _view = view;
            _glowPulse = 0f;
        }

        public void End()
        {
            _chargeSound.Volume = 0f;
            _camera = null;
            _view = null;
            _glow.intensity = 0f;
        }

        // 정확도, 큰 박자 여부, 콤보로 쌓인 기세가 클수록 크게 터진다.
        public static float Strength(float quality, bool heavy, float power)
        {
            return quality * (heavy ? 1.7f : 1f) * (1f + power * 0.8f);
        }

        // 꾹 누르기 시작: 힘을 모은다.
        public void PlayCharge()
        {
            Play(ForgeProceduralSfx.Whoosh(), 0.5f, 0.6f);
        }

        // 박자가 없는 때 눌러서 허공을 쳤다.
        public void PlayAirSwing()
        {
            Play(ForgeProceduralSfx.Whoosh(), 0.35f, 1.5f);
        }

        // 판이 끝나고 결과가 나올 때. 정확도가 어느 정도 되면 성공 음, 아니면 실패 음이다.
        public void PlayEnd(float accuracy)
        {
            Play(accuracy >= 0.6f ? ForgeProceduralSfx.Success() : ForgeProceduralSfx.Fail(), 0.8f, 1f);
        }

        // 힘을 모으는 동안 타오르는 소리가 점점 커지고 높아지며, 화면이 아주 조금씩 다가온다.
        public void UpdateCharge(bool charging, float charge)
        {
            _chargeSound.Volume = charging ? 0.04f + 0.17f * charge : 0f;
            _chargeSound.Brightness = 0.15f + 0.7f * charge;
            if (charging && _camera != null)
            {
                _camera.Punch(0.003f + 0.007f * charge);
            }
        }

        // 망치가 닿은 순간의 소리와 연출. 규칙 시간을 잠깐 멈출 길이(초)를 돌려준다.
        public float PlayImpact(Vector3 point, int segment, float quality, bool heavy, float power)
        {
            float strength = Strength(quality, heavy, power);

            Play(ForgeProceduralSfx.Clang(segment % 3), Mathf.Clamp01(0.5f + 0.5f * quality),
                (heavy ? 0.78f : 1f) + Random.Range(-0.05f, 0.1f) + (1f - quality) * 0.25f);
            Play(ForgeProceduralSfx.Boom(), Mathf.Clamp01((heavy ? 0.6f : 0.25f) * (0.5f + quality) + power * 0.3f), heavy ? 0.95f : 1.2f);
            if (heavy)
            {
                Play(ForgeProceduralSfx.Crack(), 0.4f, 1.3f);
            }

            _sparks.transform.position = point + Vector3.up * 0.05f;
            _sparks.Emit(Mathf.RoundToInt(10f + 32f * strength));
            _shock.transform.position = point;
            _shock.Emit(Mathf.RoundToInt(6f + 16f * strength));
            _glowPulse = 1f;

            if (_camera != null)
            {
                _camera.Shake(0.008f + 0.02f * strength, 0.2f + 0.08f * strength);
                _camera.Punch(0.015f + 0.03f * strength);
            }
            if (quality >= 0.7f && _view != null)
            {
                _view.Flash(Mathf.Clamp(0.02f + 0.04f * strength, 0f, 0.12f), new Color(1f, 0.85f, 0.55f));
            }

            return 0.025f + 0.05f * strength;
        }

        public void Tick(float deltaTime, float power)
        {
            _glowPulse = Mathf.MoveTowards(_glowPulse, 0f, deltaTime * 4f);
            float flicker = 0.9f + 0.2f * Mathf.PerlinNoise(Time.time * 7f, 3f);
            _glow.intensity = (0.8f + 2.2f * power + 3f * _glowPulse) * flicker;
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
