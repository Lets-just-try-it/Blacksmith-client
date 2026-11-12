using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 소리 파일 없이 코드로 만드는 임시 효과음. 망치로 쇠를 내려치는 소리(쇠 소리, 쿵, 균열), 바람 가르는 소리, 결과 소리(성공·실패 음)다.
    // 실제 효과음 파일이 들어오면 이 클래스를 쓰는 곳(ForgingFeedback)의 클립만 바꾸면 된다.
    // 클립은 처음 쓸 때 한 번 만들어 재사용한다.
    public static class ForgeProceduralSfx
    {
        private const int SampleRate = 44100;
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        // 쇠 두드리는 소리. variant가 다르면 음높이가 조금 다르다.
        public static AudioClip Clang(int variant)
        {
            float frequency = 780f + variant * 90f;
            return Make("clang" + variant, 0.5f, t =>
            {
                float envelope = Mathf.Exp(-t * 9f);
                float tone = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.5f
                    + Mathf.Sin(2f * Mathf.PI * frequency * 2.41f * t) * 0.25f * Mathf.Exp(-t * 14f)
                    + Mathf.Sin(2f * Mathf.PI * frequency * 3.9f * t) * 0.15f * Mathf.Exp(-t * 20f);
                float click = Noise() * Mathf.Exp(-t * 250f) * 0.8f;
                return (tone * envelope + click) * 0.8f;
            });
        }

        // 식은 쇠가 갈라지는 둔탁한 소리
        public static AudioClip Crack()
        {
            return Make("crack", 0.35f, t =>
            {
                float low = Mathf.Sin(2f * Mathf.PI * 110f * t) * Mathf.Exp(-t * 14f);
                float rattle = Noise() * Mathf.Exp(-t * 30f) * 0.6f;
                return (low * 0.7f + rattle) * 0.9f;
            });
        }

        // 낮게 울리는 "쿵" 소리. 내려치는 무게감을 만든다.
        public static AudioClip Boom()
        {
            return Make("boom", 0.7f, t =>
            {
                // 주파수가 빠르게 내려가는 낮은 소리(위상은 주파수를 적분한 값)
                float phase = 38f * t + 62f * (1f - Mathf.Exp(-4f * t)) / 4f;
                float body = Mathf.Sin(2f * Mathf.PI * phase) * Mathf.Exp(-t * 5.5f);
                float thump = Noise() * Mathf.Exp(-t * 60f) * 0.5f;
                return (body * 0.95f + thump) * 0.95f;
            });
        }

        // 바람 가르는 소리
        public static AudioClip Whoosh()
        {
            float lowPass = 0f;
            return Make("whoosh", 0.7f, t =>
            {
                float envelope = Mathf.Sin(Mathf.Clamp01(t / 0.7f) * Mathf.PI);
                lowPass += (Noise() - lowPass) * 0.08f;
                return lowPass * envelope * 1.6f;
            });
        }

        // 톱이 낄 때 나무가 삐걱대는 소리
        public static AudioClip Creak()
        {
            float phase = 0f;
            float lowPass = 0f;
            return Make("creak", 0.55f, t =>
            {
                float frequency = 150f + 70f * Mathf.Sin(t * 22f) + 60f * t;
                phase += 2f * Mathf.PI * frequency / SampleRate;
                lowPass += (Noise() - lowPass) * 0.15f;
                float saw = (phase / Mathf.PI) % 2f - 1f;
                float envelope = Mathf.Clamp01(t * 25f) * Mathf.Exp(-t * 4f);
                return (saw * 0.45f + lowPass * 0.5f) * envelope * 0.9f;
            });
        }

        // 통나무가 켜져 둘로 갈라져 떨어지는 소리: 나무 갈라지는 딱 소리와 둔탁한 쿵
        public static AudioClip LogSplit()
        {
            float lowPass = 0f;
            return Make("logsplit", 0.5f, t =>
            {
                lowPass += (Noise() - lowPass) * 0.35f;
                float crack = Noise() * Mathf.Exp(-t * 90f) * 0.9f;
                float thudTime = Mathf.Max(0f, t - 0.12f);
                float thud = Mathf.Sin(2f * Mathf.PI * 70f * thudTime) * Mathf.Exp(-thudTime * 14f) * (t > 0.12f ? 1f : 0f);
                float wood = lowPass * Mathf.Exp(-t * 20f) * 0.8f;
                return (crack + thud * 1.1f + wood) * 0.9f;
            });
        }

        // 시위를 놓는 "팅" 소리: 낮은 현의 울림이 금방 잦아든다.
        public static AudioClip Twang()
        {
            return Make("twang", 0.45f, t =>
            {
                float envelope = Mathf.Exp(-t * 11f);
                float vibrato = 1f + 0.03f * Mathf.Sin(t * 60f) * Mathf.Exp(-t * 6f);
                float tone = Mathf.Sin(2f * Mathf.PI * 210f * vibrato * t) * 0.55f + Mathf.Sin(2f * Mathf.PI * 420f * t) * 0.25f * Mathf.Exp(-t * 8f);
                float pluck = Noise() * Mathf.Exp(-t * 180f) * 0.5f;
                return (tone * envelope + pluck) * 0.9f;
            });
        }

        // 화살이 과녁에 박히는 "퍽" 소리: 둔탁한 짚단 소리와 짧은 떨림
        public static AudioClip Thud()
        {
            float lowPass = 0f;
            return Make("thud", 0.4f, t =>
            {
                lowPass += (Noise() - lowPass) * 0.12f;
                float body = Mathf.Sin(2f * Mathf.PI * 95f * t) * Mathf.Exp(-t * 26f);
                float straw = lowPass * Mathf.Exp(-t * 30f) * 1.4f;
                float shaft = Mathf.Sin(2f * Mathf.PI * 340f * t) * Mathf.Exp(-t * 18f) * 0.25f * (t > 0.015f ? 1f : 0f);
                return (body * 0.9f + straw + shaft) * 0.9f;
            });
        }

        // 돌이 깨지는 둔탁한 소리
        public static AudioClip Rock(int variant)
        {
            float lowPass = 0f;
            float frequency = 95f + variant * 22f;
            return Make("rock" + variant, 0.32f, t =>
            {
                lowPass += (Noise() - lowPass) * 0.28f;
                float body = Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-t * 22f);
                float crumble = lowPass * Mathf.Exp(-t * 16f) * 1.3f;
                float click = Noise() * Mathf.Exp(-t * 400f) * 0.7f;
                return (body * 0.9f + crumble * 0.7f + click) * 0.9f;
            });
        }

        // 쇳물이 흘러나오는 "꿀꺽" 소리
        public static AudioClip Gulp()
        {
            float lowPass = 0f;
            return Make("gulp", 0.3f, t =>
            {
                lowPass += (Noise() - lowPass) * 0.18f;
                return lowPass * Mathf.Sin(Mathf.Clamp01(t / 0.3f) * Mathf.PI) * 0.8f;
            });
        }

        public static AudioClip Ding(float frequency)
        {
            return Make("ding" + Mathf.RoundToInt(frequency), 0.8f, t =>
                Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-t * 5f) * 0.5f
                + Mathf.Sin(2f * Mathf.PI * frequency * 2f * t) * Mathf.Exp(-t * 8f) * 0.2f);
        }

        public static AudioClip Chime(float frequency)
        {
            return Make("chime" + Mathf.RoundToInt(frequency), 1.1f, t =>
                (Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-t * 3.2f) * 0.5f
                + Mathf.Sin(2f * Mathf.PI * frequency * 2.76f * t) * Mathf.Exp(-t * 6f) * 0.22f
                + Mathf.Sin(2f * Mathf.PI * frequency * 5.4f * t) * Mathf.Exp(-t * 11f) * 0.1f) * Mathf.Clamp01(t * 400f));
        }

        public static AudioClip Tick()
        {
            return Make("tick", 0.08f, t => Mathf.Sin(2f * Mathf.PI * 1400f * t) * Mathf.Exp(-t * 60f) * 0.4f);
        }

        // 날 끝을 갈아 흠집을 지웠을 때 나는 가는 쇠 소리
        public static AudioClip Etch(int variant)
        {
            float frequency = 1100f + variant * 140f;
            return Make("etch" + variant, 0.22f, t =>
            {
                float tone = Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-t * 26f) * 0.5f
                    + Mathf.Sin(2f * Mathf.PI * frequency * 1.51f * t) * Mathf.Exp(-t * 40f) * 0.25f;
                return tone + Noise() * Mathf.Exp(-t * 300f) * 0.6f;
            });
        }

        public static AudioClip Success()
        {
            float[] notes = { 523.25f, 659.25f, 783.99f };
            return Make("success", 0.7f, t =>
            {
                int index = Mathf.Min(2, (int)(t / 0.12f));
                float local = t - index * 0.12f;
                return Mathf.Sin(2f * Mathf.PI * notes[index] * t) * Mathf.Exp(-local * 6f) * 0.5f;
            });
        }

        public static AudioClip Fail()
        {
            float[] notes = { 392f, 311f };
            return Make("fail", 0.6f, t =>
            {
                int index = Mathf.Min(1, (int)(t / 0.2f));
                float local = t - index * 0.2f;
                return Mathf.Sin(2f * Mathf.PI * notes[index] * t) * Mathf.Exp(-local * 5f) * 0.5f;
            });
        }

        private static AudioClip Make(string key, float seconds, Func<float, float> sample)
        {
            if (Cache.TryGetValue(key, out AudioClip cached) && cached != null)
            {
                return cached;
            }

            int count = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                data[i] = Mathf.Clamp(sample(i / (float)SampleRate), -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(key, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            Cache[key] = clip;
            return clip;
        }

        private static float Noise()
        {
            return UnityEngine.Random.value * 2f - 1f;
        }
    }
}
