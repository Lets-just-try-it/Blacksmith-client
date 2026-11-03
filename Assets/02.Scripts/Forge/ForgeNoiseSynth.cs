using UnityEngine;

namespace Blacksmith.Forge
{
    // 이어서 나는 소리(힘을 모을 때 타오르는 소리)를 코드로 만든다. 임시 효과음이다.
    // Brightness(0~1)가 높을수록 높은 소리가 나고, Volume으로 크기를, Crackle로 탁탁 튀는 정도를 정한다.
    // AudioSource가 켜져 있어야 OnAudioFilterRead가 불린다.
    [RequireComponent(typeof(AudioSource))]
    public class ForgeNoiseSynth : MonoBehaviour
    {
        public float Volume;
        public float Brightness = 0.5f;
        public float Crackle;

        private float _lowPass;
        private float _smoothVolume;
        private float _smoothBrightness = 0.5f;
        private System.Random _random;

        private void Awake()
        {
            _random = new System.Random(5);
            AudioSource source = GetComponent<AudioSource>();
            source.clip = AudioClip.Create("silence", 4410, 1, 44100, false);
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 1f;
            source.Play();
        }

        // 오디오 스레드에서 불리므로 Unity API를 쓰지 않는다.
        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (_random == null)
            {
                return;
            }

            float volume = Volume;
            float brightness = Brightness;
            float crackle = Crackle;
            for (int i = 0; i < data.Length; i += channels)
            {
                _smoothVolume += (volume - _smoothVolume) * 0.0008f;
                _smoothBrightness += (brightness - _smoothBrightness) * 0.0008f;
                float noise = (float)(_random.NextDouble() * 2.0 - 1.0);
                _lowPass += (noise - _lowPass) * (0.02f + _smoothBrightness * 0.7f);
                float sample = _lowPass;
                if (crackle > 0f && _random.NextDouble() < 0.0015 * crackle)
                {
                    sample += (float)(_random.NextDouble() * 2.0 - 1.0) * 1.8f;
                }

                sample *= _smoothVolume;
                for (int channel = 0; channel < channels; channel++)
                {
                    data[i + channel] = sample;
                }
            }
        }
    }
}
