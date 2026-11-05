using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 정제의 소리와 타격감(불꽃, 불씨, 돌 파편, 증기, 조명, 화면 흔들림·번쩍임)을 맡는다.
    // 규칙은 건드리지 않고 SmeltingStage가 넘겨 주는 사건에 반응만 한다. 위치는 모두 SmeltingStage 로컬이다.
    public class SmeltingFeedback
    {
        // 무대가 줄어든 비율만큼 입자와 흔들림도 줄인다.
        private readonly float _scale;
        private readonly Transform _stage;
        private readonly AudioSource _sfx;
        private readonly ForgeNoiseSynth _fireSound;
        private readonly ForgeNoiseSynth _pourSound;
        private readonly Light _glow;
        private readonly ParticleSystem _flames;
        private readonly ParticleSystem _sparks;
        private readonly ParticleSystem _embers;
        private readonly ParticleSystem _steam;
        private readonly ParticleSystem _pourSparks;
        private readonly ParticleSystem _chips;
        private ForgeCameraFocus _camera;
        private SmeltingView _view;
        private float _lastSpill;
        private float _lastWarn;
        private int _lastComboPopup;

        public SmeltingFeedback(ForgeGreybox kit, Transform stage, Transform fxRoot, float scale,
            AudioSource sfx, ForgeNoiseSynth fireSound, ForgeNoiseSynth pourSound, Light glow)
        {
            _scale = scale;
            _stage = stage;
            _sfx = sfx;
            _fireSound = fireSound;
            _pourSound = pourSound;
            _glow = glow;
            _glow.range *= scale;

            _flames = kit.Particles(fxRoot, "Flames", new Color(1f, 0.6f, 0.2f, 0.9f), 1.5f * scale, 0.35f * scale, 0.7f, -0.2f * scale, 22f);
            _sparks = kit.Particles(fxRoot, "Sparks", new Color(1f, 0.85f, 0.4f, 1f), 3.4f * scale, 0.08f * scale, 0.9f, 0.9f * scale, 40f);
            _embers = kit.Particles(fxRoot, "Embers", new Color(1f, 0.55f, 0.15f, 1f), 1f * scale, 0.06f * scale, 1f, -0.3f * scale, 60f);
            _steam = kit.Particles(fxRoot, "Steam", new Color(0.9f, 0.9f, 0.95f, 0.3f), 1f * scale, 0.5f * scale, 1.1f, -0.15f * scale, 25f);
            _pourSparks = kit.Particles(fxRoot, "PourSparks", new Color(1f, 0.8f, 0.35f, 1f), 3.2f * scale, 0.07f * scale, 0.6f, 1.6f * scale, 70f);
            _chips = kit.Particles(fxRoot, "Chips", new Color(0.9f, 0.8f, 0.6f, 1f), 4f * scale, 0.07f * scale, 0.55f, 1.8f * scale, 85f);

            _flames.transform.position = stage.TransformPoint(SmeltingLayout.HearthPos + new Vector3(0f, 1.3f, -0.9f));
            _embers.transform.position = stage.TransformPoint(SmeltingLayout.HearthPos + new Vector3(0f, 1.2f, -0.8f));
            _glow.transform.position = stage.TransformPoint(SmeltingLayout.HearthPos + new Vector3(0f, 1.7f, -1.2f));
            _fireSound.Brightness = 0.2f;
            _fireSound.Crackle = 1.2f;
            _pourSound.Brightness = 0.35f;
            _pourSound.Crackle = 0.8f;
        }

        public void Begin(ForgeCameraFocus camera, SmeltingView view)
        {
            _camera = camera;
            _view = view;
            _lastComboPopup = 0;
        }

        public void End()
        {
            _fireSound.Volume = 0f;
            _pourSound.Volume = 0f;
            ForgeGreybox.SetRate(_flames, 0f);
            ForgeGreybox.SetRate(_embers, 0f);
            ForgeGreybox.SetRate(_sparks, 0f);
            ForgeGreybox.SetRate(_steam, 0f);
            ForgeGreybox.SetRate(_pourSparks, 0f);
            _glow.intensity = 0f;
            _camera = null;
            _view = null;
        }

        // ---------------- ① 쪼개기

        // 곡괭이가 원석에 닿았다: 돌 파편이 튀고 금이 간다.
        public void PlayImpact(Vector3 local, float quality, float strength)
        {
            Vector3 point = _stage.TransformPoint(local + SmeltingLayout.ArenaNormal * 0.4f);
            _chips.transform.position = point;
            ForgeGreybox.Burst(_chips, Mathf.RoundToInt(8f + 10f * strength));
            _sparks.transform.position = point;
            ForgeGreybox.Burst(_sparks, Mathf.RoundToInt(5f + 8f * strength));

            Play(ForgeProceduralSfx.Rock(Random.Range(0, 3)), Mathf.Clamp01(0.5f + 0.4f * quality), 0.9f + Random.Range(-0.08f, 0.1f) + (1f - quality) * 0.2f);
            Play(ForgeProceduralSfx.Clang(Random.Range(0, 3)), 0.25f * quality, 1.2f);
            Shake(0.03f + 0.05f * strength, 0.16f);
            Punch(0.012f + 0.02f * strength);
            if (quality >= 1f)
            {
                Flash(0.04f, new Color(1f, 0.8f, 0.5f));
            }
        }

        // 맞힌 균열의 글자(완벽/좋음/약함과 콤보)
        public void ShowStrike(Vector3 local, float quality, int combo)
        {
            string label = quality >= 1f ? "완벽!" : (quality >= 0.75f ? "좋음" : "약함");
            if (combo >= 3 && quality >= 0.75f)
            {
                label += "  x" + combo;
            }
            Color color = quality >= 1f ? new Color(1f, 0.85f, 0.25f) : (quality >= 0.75f ? new Color(0.55f, 0.92f, 0.45f) : new Color(0.85f, 0.85f, 0.88f));
            PopupWorld(label, color, local);

            if (combo >= 5 && combo / 5 > _lastComboPopup)
            {
                _lastComboPopup = combo / 5;
                PopupScreen("콤보 x" + combo + "!", new Color(1f, 0.6f, 0.2f), 0.74f);
            }
            if (combo == 0)
            {
                _lastComboPopup = 0;
            }
        }

        public void PlayMiss(Vector3 local)
        {
            Play(ForgeProceduralSfx.Whoosh(), 0.25f, 1.6f);
            PopupWorld("놓침", new Color(0.8f, 0.45f, 0.4f), local);
        }

        public void PlayAvoid(Vector3 local)
        {
            Play(ForgeProceduralSfx.Ding(880f), 0.18f, 1f);
            PopupWorld("넘겼다", new Color(0.75f, 0.55f, 1f), local);
        }

        // 저주받은 균열을 눌렀다.
        public void PlayCurse(Vector3 local)
        {
            _pourSparks.transform.position = _stage.TransformPoint(local);
            ForgeGreybox.Burst(_pourSparks, 20);
            Play(ForgeProceduralSfx.Crack(), 0.8f, 0.8f);
            Play(ForgeProceduralSfx.Fail(), 0.5f, 0.9f);
            Shake(0.07f, 0.3f);
            Flash(0.12f, new Color(0.6f, 0.2f, 0.9f));
            PopupWorld("저주! 누르면 안 돼", new Color(0.8f, 0.4f, 1f), local);
        }

        // 아무 균열 근처도 아닌 곳을 눌렀다.
        public void PlayStray()
        {
            Play(ForgeProceduralSfx.Whoosh(), 0.3f, 1.5f);
            Play(ForgeProceduralSfx.Tick(), 0.2f, 0.7f);
        }

        public void PlayEarly(Vector3 local)
        {
            PopupWorld("아직!", new Color(0.8f, 0.8f, 0.85f), local);
        }

        // 마지막 균열까지 쳤다: 원석이 쪼개진다.
        public void PlaySplit(Vector3 boulderWorld, float yield)
        {
            _chips.transform.position = boulderWorld;
            ForgeGreybox.Burst(_chips, 50);
            _sparks.transform.position = boulderWorld;
            ForgeGreybox.Burst(_sparks, 40);
            Play(ForgeProceduralSfx.Boom(), 0.9f, 0.9f);
            Play(ForgeProceduralSfx.Rock(2), 0.9f, 0.7f);
            Play(ForgeProceduralSfx.Success(), 0.5f, 1.2f);
            Shake(0.1f, 0.4f);
            Punch(0.04f);
            Flash(0.15f, new Color(1f, 0.8f, 0.5f));
            PopupScreen("쪼개졌다!  수율 " + Mathf.RoundToInt(yield * 100f) + "%", new Color(1f, 0.85f, 0.4f), 0.8f);
        }

        // 쪼개진 쇳덩이가 도가니에 떨어졌다.
        public void PlayNuggetLanded(Vector3 local, int arrived)
        {
            _sparks.transform.position = _stage.TransformPoint(local);
            ForgeGreybox.Burst(_sparks, 10);
            Play(ForgeProceduralSfx.Clang(arrived % 3), 0.35f, 0.7f + 0.05f * arrived);
            Shake(0.015f, 0.1f);
        }

        // ---------------- ② 풀무질

        // 지시선이 불꽃 띠에 들어오거나 나갔다.
        public void PlayBeat(bool inZone)
        {
            if (!inZone)
            {
                return;
            }

            Play(ForgeProceduralSfx.Whoosh(), 0.4f, 1.2f);
            ForgeGreybox.Burst(_flames, 8);
            ForgeGreybox.Burst(_sparks, 2);
        }

        public void PlayMolten(float fluidity, bool timedOut)
        {
            ForgeGreybox.Burst(_sparks, 70);
            ForgeGreybox.Burst(_flames, 30);
            Play(ForgeProceduralSfx.Success(), 0.8f, 1f);
            Play(ForgeProceduralSfx.Boom(), 0.6f, 1.1f);
            Shake(0.08f, 0.35f);
            Punch(0.03f);
            Flash(0.15f, new Color(1f, 0.85f, 0.5f));
            PopupScreen(timedOut ? "덜 녹았다.." : "다 녹았다!  묽기 " + Mathf.RoundToInt(fluidity * 100f) + "%", new Color(1f, 0.85f, 0.4f), 0.78f);
        }

        // ---------------- ③ 붓기

        public void PlayPourStart()
        {
            Play(ForgeProceduralSfx.Whoosh(), 0.5f, 0.6f);
            Play(ForgeProceduralSfx.Clang(1), 0.4f, 0.6f);
        }

        public void PlaySpill()
        {
            if (Time.time - _lastSpill < 0.5f)
            {
                return;
            }

            _lastSpill = Time.time;
            Play(ForgeProceduralSfx.Gulp(), 0.3f, 1.4f);
        }

        public void PlayOverflow()
        {
            Play(ForgeProceduralSfx.Crack(), 0.6f, 1.1f);
            Shake(0.05f, 0.3f);
            Flash(0.12f, new Color(1f, 0.35f, 0.15f));
        }

        public void PlaySolidify()
        {
            Play(ForgeProceduralSfx.Crack(), 0.6f, 0.8f);
            PopupScreen("쇳물이 굳었다!", new Color(0.7f, 0.75f, 0.85f), 0.78f);
        }

        public void PlayWarn()
        {
            if (Time.time - _lastWarn < 2f)
            {
                return;
            }

            _lastWarn = Time.time;
            Play(ForgeProceduralSfx.Ding(300f), 0.6f, 1f);
            PopupScreen("찌꺼기가 나온다! 그만 붓자", new Color(1f, 0.55f, 0.3f), 0.78f);
        }

        // 붓기가 끝났다: 주괴가 굳는다.
        public void PlayIngot(Vector3 moldWorld)
        {
            Play(ForgeProceduralSfx.Success(), 0.8f, 1f);
            Play(ForgeProceduralSfx.Chime(880f), 0.5f, 1f);
            Flash(0.15f, new Color(1f, 0.85f, 0.55f));
            Shake(0.05f, 0.25f);
            _steam.transform.position = moldWorld;
            ForgeGreybox.Burst(_steam, 30);
            PopupWorld("주괴 완성!", new Color(1f, 0.85f, 0.3f), _stage.InverseTransformPoint(moldWorld));
        }

        public void PlayResult(float accuracy)
        {
            Play(accuracy >= 0.6f ? ForgeProceduralSfx.Success() : ForgeProceduralSfx.Fail(), 0.8f, 1f);
        }

        // ---------------- 매 프레임

        // 불이 셀수록 불꽃이 커지고 흰빛이 돌며, 타오르는 소리와 조명이 커진다. 쇳물 줄기에 따라 소리와 불똥도 낸다.
        public void Tick(SmeltingGame game, SmeltingFurnaceVisual furnace, SmeltingPourVisual pour)
        {
            float heat = furnace.HeatShown;
            ForgeGreybox.SetRate(_flames, game.Act == SmeltingGame.ActKind.Crack ? 6f : 8f + 50f * heat);
            ParticleSystem.MainModule flames = _flames.main;
            flames.startColor = Color.Lerp(new Color(1f, 0.55f, 0.15f, 0.9f), new Color(1f, 0.95f, 0.8f, 0.95f), Mathf.Clamp01((heat - 0.5f) * 2f));
            ForgeGreybox.SetRate(_embers, heat > 0.4f ? (heat - 0.4f) * 40f : 0f);

            _fireSound.Volume = 0.03f + 0.13f * heat;
            _fireSound.Brightness = 0.1f + 0.3f * heat;
            float flicker = 0.85f + 0.3f * Mathf.PerlinNoise(Time.time * 9f, 0f);
            _glow.intensity = (0.02f + 0.15f * heat) * flicker;
            _glow.color = Color.Lerp(new Color(1f, 0.35f, 0.1f), new Color(1f, 0.8f, 0.5f), heat);

            TickStream(game, pour);
        }

        private void TickStream(SmeltingGame game, SmeltingPourVisual pour)
        {
            if (!pour.Flowing)
            {
                _pourSound.Volume = 0f;
                ForgeGreybox.SetRate(_pourSparks, 0f);
                ForgeGreybox.SetRate(_steam, 0f);
                return;
            }

            _pourSound.Volume = 0.05f + 0.16f * Mathf.Sqrt(Mathf.Clamp01(game.Flow / game.FlowMax));
            _pourSound.Brightness = 0.2f + 0.3f * game.MetalHeat;

            Vector3 land = _stage.TransformPoint(pour.LandPoint);
            _pourSparks.transform.position = land;
            _steam.transform.position = land + Vector3.up * 0.1f * _scale;
            ForgeGreybox.SetRate(_pourSparks, 18f + 60f * pour.Spilling);
            ForgeGreybox.SetRate(_steam, pour.LandsInMold ? 4f : 14f);
            Shake(0.002f + 0.006f * game.Flow, 0.08f);
            if (pour.Spilling > 0.02f)
            {
                Shake(0.004f + 0.01f * pour.Spilling, 0.08f);
            }
        }

        private void Shake(float amplitude, float seconds)
        {
            if (_camera != null)
            {
                _camera.Shake(amplitude * _scale, seconds);
            }
        }

        private void Punch(float amount)
        {
            if (_camera != null)
            {
                _camera.Punch(amount);
            }
        }

        private void Flash(float alpha, Color color)
        {
            if (_view != null)
            {
                _view.Flash(alpha, color);
            }
        }

        private void PopupWorld(string text, Color color, Vector3 local)
        {
            if (_view != null)
            {
                _view.PopupWorld(text, color, _stage.TransformPoint(local + Vector3.up * 0.5f));
            }
        }

        private void PopupScreen(string text, Color color, float heightFraction)
        {
            if (_view != null)
            {
                _view.PopupScreen(text, color, heightFraction);
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
