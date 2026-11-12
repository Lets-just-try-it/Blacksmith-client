using UnityEngine;

namespace Blacksmith.Forge
{
    // 톱질 소리를 코드로 만들어 이어서 낸다. 임시 효과음이다.
    // 쇠 긁는 소리는 빼고 나무 소리 위주로 만든다: 톱니가 섬유에 걸릴 때마다 나는 낮고 둔탁한 "툭툭"(나무통 울림)과,
    // 나무를 문지르는 부드러운 "스으윽" 잡음이다. 높은 소리는 걸러 낸다.
    // 빠르게 움직일수록 소리가 촘촘하고 커지며, 움직임이 멈추는 순간에는 소리가 끊겨 "스윽 스윽" 박자가 생긴다.
    // Strain이 오르면(너무 세게 켤 때) 나무가 버티며 끙 하는 낮은 소리가 섞인다.
    // 오디오 스레드에서 읽는 값은 주 스레드가 쓰기만 한다. AudioSource가 켜져 있어야 OnAudioFilterRead가 불린다.
    [RequireComponent(typeof(AudioSource))]
    public class ForgeSawSynth : MonoBehaviour
    {
        private const float SampleRate = 44100f;

        // 0이면 조용하고 1이면 알맞은 힘으로 켜는 소리다. 더 크면 더 세고 요란하다.
        public float Intensity;
        // 0~1. 너무 센 정도
        public float Strain;

        private float _smoothIntensity;
        private float _bodyLow;
        private float _bodyBand;
        private float _midLow;
        private float _midBand;
        private float _rubLow;
        private float _rubSoft;
        private float _outLow;
        private float _groanPhase;
        private System.Random _random;

        private void Awake()
        {
            _random = new System.Random(11);
            AudioSource source = GetComponent<AudioSource>();
            source.clip = AudioClip.Create("silence", 4410, 1, 44100, false);
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 1f;
            source.Play();
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (_random == null)
            {
                return;
            }

            float intensity = MinigameFeedbackSwitch.Enabled ? Intensity : 0f;
            float strain = Strain;
            for (int i = 0; i < data.Length; i += channels)
            {
                _smoothIntensity += (intensity - _smoothIntensity) * 0.004f;
                float level = Mathf.Min(1.6f, _smoothIntensity);

                // 톱니가 섬유에 걸리는 충격: 빠를수록 촘촘하다.
                float impact = 0f;
                if (_random.NextDouble() < (90f + 650f * level) / SampleRate)
                {
                    impact = 0.4f + 0.6f * (float)_random.NextDouble();
                }

                // 나무통의 낮은 울림(약 190Hz)과 가운데 울림(약 520Hz)
                _bodyLow += 0.027f * _bodyBand;
                float bodyHigh = impact - _bodyLow - 0.16f * _bodyBand;
                _bodyBand += 0.027f * bodyHigh;
                _midLow += 0.074f * _midBand;
                float midHigh = impact * 0.5f - _midLow - 0.3f * _midBand;
                _midBand += 0.074f * midHigh;

                // 나무를 문지르는 부드러운 잡음(높은 소리를 걸러 둔탁하게)
                float noise = (float)(_random.NextDouble() * 2.0 - 1.0);
                _rubLow += (noise - _rubLow) * 0.07f;
                _rubSoft += (_rubLow - _rubSoft) * 0.2f;

                float sample = (_bodyBand * 5.5f + _midBand * 2.2f + _rubSoft * 1.1f) * level;
                if (strain > 0.01f)
                {
                    _groanPhase += (95f + 18f * Mathf.Sin(_groanPhase * 5f)) / SampleRate;
                    if (_groanPhase >= 1f)
                    {
                        _groanPhase -= 1f;
                    }

                    sample += Mathf.Sin(_groanPhase * 6.2832f) * 0.22f * strain * level;
                }

                sample = (float)System.Math.Tanh(sample) * 0.65f;
                _outLow += (sample - _outLow) * 0.3f;
                for (int channel = 0; channel < channels; channel++)
                {
                    data[i + channel] = _outLow;
                }
            }
        }
    }
}
