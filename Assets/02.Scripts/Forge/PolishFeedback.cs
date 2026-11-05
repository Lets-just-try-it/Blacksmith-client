using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 연마의 소리와 타격감(불똥, 김, 갈리는 소리, 화면 흔들림·번쩍임, 글자)을 맡는다.
    // 규칙은 건드리지 않고 PolishStage가 넘겨 주는 사건에 반응한다.
    public class PolishFeedback
    {
        // 칸을 다 갈 때마다 콤보에 따라 오르는 음계(반음 수)
        private static readonly int[] ComboSemitones = { 0, 2, 4, 7, 9, 12, 14 };

        private readonly float _scale;
        private readonly AudioSource _sfx;
        private readonly ForgeNoiseSynth _grind;
        private readonly Light _glow;
        private readonly ParticleSystem _sparks;
        private readonly ParticleSystem _glint;
        private readonly ParticleSystem _mist;
        private ForgeCameraFocus _camera;
        private PolishView _view;
        private PolishLensView _lens;
        private float _shakeClock;
        private float _lastDonePopup = -10f;

        public PolishFeedback(ForgeGreybox kit, Transform stage, Transform fxRoot, float scale, AudioSource sfx, ForgeNoiseSynth grind, Light glow)
        {
            _scale = scale;
            _sfx = sfx;
            _grind = grind;
            _glow = glow;
            _glow.range *= scale;

            _sparks = kit.Particles(fxRoot, "Sparks", new Color(1f, 0.8f, 0.4f), 3.5f * scale, 0.06f * scale, 0.5f, 1.6f * scale, 50f);
            _glint = kit.Particles(fxRoot, "Glint", Color.white, 1.6f * scale, 0.1f * scale, 0.8f, -0.2f * scale, 90f);
            _mist = kit.Particles(fxRoot, "Mist", new Color(0.85f, 0.92f, 1f, 0.3f), 0.9f * scale, 0.3f * scale, 1f, -0.05f * scale, 40f);
            _grind.Volume = 0f;
        }

        public void Begin(ForgeCameraFocus camera, PolishView view, PolishLensView lens)
        {
            _camera = camera;
            _view = view;
            _lens = lens;
        }

        public void End()
        {
            _grind.Volume = 0f;
            ForgeGreybox.SetRate(_sparks, 0f);
            ForgeGreybox.SetRate(_mist, 0f);
            _glow.intensity = 0f;
            _camera = null;
            _view = null;
            _lens = null;
        }

        // 매 프레임: 갈 때 불똥과 갈리는 소리, 식힐 때 김, 열에 따른 빛
        public void Tick(PolishGame game, Vector3 contact, Vector3 bucket)
        {
            bool hot = game.Heat >= game.WarnHeat;
            Color tone = PolishLensView.SparkTone(game);

            _sparks.transform.position = contact;
            ParticleSystem.MainModule main = _sparks.main;
            main.startColor = tone;
            ForgeGreybox.SetRate(_sparks, game.Grinding ? 70f * (0.3f + game.Quality) * (hot ? 1.4f : 1f) : 0f);

            _mist.transform.position = bucket;
            ForgeGreybox.SetRate(_mist, game.Dipping ? 60f : 0f);

            _glow.transform.position = contact + Vector3.up * 0.3f * _scale;
            _glow.color = tone;
            _glow.intensity = game.Grinding ? 0.1f + 0.25f * game.Heat : 0.02f;

            // 도는 숫돌의 낮은 소리가 깔리고, 대면 갈리는 소리가 얹힌다. 뜨거울수록 높고 거칠다.
            bool running = game.Phase == PolishGame.PhaseKind.Grind;
            _grind.Volume = game.Grinding ? 0.16f * (0.5f + 0.5f * game.Quality) : (running ? 0.035f : 0f);
            _grind.Brightness = 0.3f + 0.2f * (game.Grinding ? game.Quality : 0f) + 0.3f * Mathf.Clamp01(game.Heat);
            _grind.Crackle = game.Grinding ? (hot ? 0.55f : 0.2f) : 0f;

            // 갈고 있는 동안 화면이 미세하게 떨려 손으로 쥔 듯한 느낌을 준다. 각도가 어긋날수록 더 떤다.
            _shakeClock -= Time.deltaTime;
            if (game.Grinding && _shakeClock <= 0f)
            {
                _shakeClock = 0.07f;
                Shake(0.003f + 0.004f * (1f - game.Quality), 0.07f);
            }
        }

        // 닿는 칸이 바뀔 때마다 나는 짧은 소리. 칸이 많이 섰을수록 높아서 갈아 나가는 리듬이 된다.
        public void PlayStep(float ratio)
        {
            Play(ForgeProceduralSfx.Tick(), 0.22f, 0.9f + 0.7f * ratio);
        }

        public void PlayPhase()
        {
            Play(ForgeProceduralSfx.Tick(), 0.5f, 0.9f);
            Popup("숫돌이 돈다! 초록 띠에 맞춰 갈고, 뜨거워지면 E 또는 스페이스바로 식히자", new Color(1f, 0.85f, 0.5f), new Vector2(0f, 128f));
        }

        // 칸 하나를 다 갈았다.
        public void PlayDone(int segment, int combo, Vector3 contact)
        {
            string text = combo >= 2 ? "깨끗!  콤보 x" + combo : "깨끗!";
            Color color = combo >= 3 ? new Color(1f, 0.9f, 0.4f) : new Color(0.7f, 1f, 0.8f);
            if (_lens != null)
            {
                // 여러 칸이 거의 동시에 끝나면 글자가 겹치므로 잠시 간격을 둔다.
                if (Time.time - _lastDonePopup > 0.45f)
                {
                    _lastDonePopup = Time.time;
                    Popup(text, color, _lens.SegmentLocal(segment) + Vector2.up * 30f);
                }
                _lens.BurstAt(segment);
            }

            int step = ComboSemitones[Mathf.Clamp(combo - 1, 0, ComboSemitones.Length - 1)];
            Play(ForgeProceduralSfx.Chime(740f * Mathf.Pow(2f, step / 12f)), 0.5f, 1f);
            Play(ForgeProceduralSfx.Etch(segment), 0.3f, 1.2f);
            _glint.transform.position = contact + Vector3.up * 0.3f * _scale;
            ForgeGreybox.Burst(_glint, 10 + 3 * Mathf.Min(combo, 6));
            Shake(0.009f, 0.07f);
            if (_camera != null)
            {
                _camera.Punch(0.012f + 0.004f * Mathf.Min(combo, 5));
            }
            if (_view != null)
            {
                _view.Flash(0.05f, new Color(1f, 0.95f, 0.7f));
            }
        }

        // 물에 담갔다: 치익 소리와 김
        public void PlayDip(float heat)
        {
            Play(ForgeProceduralSfx.Whoosh(), 0.35f + 0.3f * heat, 1.5f);
            ForgeGreybox.Burst(_mist, 14 + (int)(24f * heat));
        }

        // 뜨거워졌다는 경고
        public void PlayWarn()
        {
            Play(ForgeProceduralSfx.Tick(), 0.6f, 1.6f);
            Popup("뜨겁다! E·Space로 식히자", new Color(1f, 0.6f, 0.35f), new Vector2(0f, 100f));
        }

        // 날이 탔다.
        public void PlayBurn()
        {
            Play(ForgeProceduralSfx.Crack(), 0.6f, 1f);
            Play(ForgeProceduralSfx.Fail(), 0.25f, 1.3f);
            Shake(0.02f, 0.14f);
            Popup("탔다! 식을 때까지 갈 수 없다", new Color(1f, 0.45f, 0.4f), new Vector2(0f, 128f));
            if (_lens != null)
            {
                _lens.BurnAt();
            }
            if (_view != null)
            {
                _view.Flash(0.12f, new Color(1f, 0.2f, 0.15f));
            }
        }

        public void PlayComplete(float score, float successScore)
        {
            if (score >= successScore)
            {
                Play(ForgeProceduralSfx.Chime(1318f), 0.7f, 1f);
                Play(ForgeProceduralSfx.Success(), 0.7f, 1f);
                ForgeGreybox.Burst(_glint, 50);
            }
            else
            {
                Play(ForgeProceduralSfx.Fail(), 0.8f, 1f);
            }
        }

        private void Shake(float amplitude, float seconds)
        {
            if (_camera != null)
            {
                _camera.Shake(amplitude * _scale, seconds);
            }
        }

        private void Popup(string text, Color color, Vector2 lensLocal)
        {
            if (_view != null)
            {
                _view.Popup(text, color, lensLocal);
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
