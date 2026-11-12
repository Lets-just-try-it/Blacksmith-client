using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 활 깎기 작업대 위에 올라가는 줄인 3D 무대: 톱질 모탕 위에 가로로 누운 통나무와, 그 위에서 앞뒤로 밀고 당기는 톱.
    // 카메라는 앞에서 비스듬히 내려다보므로 화면의 마우스 위(위로 밀기)가 톱을 안쪽으로, 아래(당기기)가 바깥쪽으로 보낸다.
    // 켠 만큼 톱날이 내려가고 켜진 틈이 깊어지며, 다 켜면 통나무가 둘로 갈라져 떨어지고 새 통나무가 놓인다.
    // 힘은 톱밥(알맞게 켜면 곱게 쏟아짐), 톱날이 달아오른 색과 불똥(너무 셈), 톱 소리로 보여 준다.
    // 모양은 임시 그레이박스라 코드로 만든다. 진행과 입력은 BowSawStation이, 규칙은 BowSawGame이 맡는다.
    public class BowSawStage : MiniStage
    {
        private const float Radius = 0.6f;
        private const float CenterY = 1.7f;
        private const float HalfPieceLength = 1.9f;
        // 카메라가 앞에서 보면 톱날이 옆면(얇은 선)으로만 보이므로 소품 전체를 비스듬히 돌려 놓는다.
        private const float Yaw = -38f;
        private const float CoreWidth = 0.1f;
        private const int StripCount = 12;
        private const float SplitSeconds = 0.55f;
        private const float PopSeconds = 0.3f;

        private static readonly Color Bark = new Color(0.42f, 0.27f, 0.15f);
        private static readonly Color Pale = new Color(0.86f, 0.68f, 0.42f);
        private static readonly Color Steel = new Color(0.74f, 0.77f, 0.82f);
        private static readonly Color KerfColor = new Color(0.12f, 0.07f, 0.04f);

        private Transform _pivot;
        private Transform _left;
        private Transform _right;
        private Transform _core;
        private Transform _saw;
        private Renderer _blade;
        private Renderer _teeth;
        private ParticleSystem _dust;
        private ForgeSawSynth _synth;
        private readonly Transform[] _strips = new Transform[StripCount];
        private float _sawZ;
        private float _engage;
        private float _split = -1f;
        private float _pop = 1f;
        private int _logsCut;

        protected override float FocusHeight => 1.5f;
        protected override float ViewSize => 4.6f;
        protected override float ScreenShift => 0.05f;

        private float Top => CenterY + Radius;

        protected override void BuildProps()
        {
            var pivot = new GameObject("Props");
            pivot.transform.SetParent(StageRoot, false);
            pivot.transform.localRotation = Quaternion.Euler(0f, Yaw, 0f);
            _pivot = pivot.transform;

            for (int side = -1; side <= 1; side += 2)
            {
                Kit.Box(_pivot, "Horse" + side, new Vector3(side * 1.5f, 0.65f, 0f), new Vector3(0.22f, 0.95f, 1.6f), new Color(0.32f, 0.22f, 0.14f));
                Kit.Box(_pivot, "HorseTop" + side, new Vector3(side * 1.5f, 1.15f, 0f), new Vector3(0.5f, 0.1f, 1.3f), new Color(0.26f, 0.17f, 0.1f));
            }

            _left = BuildHalf("Left", -1);
            _right = BuildHalf("Right", 1);
            _core = Kit.Cylinder(_pivot, "Core", new Vector3(0f, CenterY, 0f), new Vector3(Radius * 2f, CoreWidth * 0.5f, Radius * 2f), Bark).transform;
            _core.localRotation = Quaternion.Euler(0f, 0f, 90f);

            for (int i = 0; i < StripCount; i++)
            {
                _strips[i] = Kit.Box(_pivot, "Kerf" + i, new Vector3(0f, Top, 0f), new Vector3(CoreWidth + 0.02f, 0.05f, 0.1f), KerfColor).transform;
            }

            BuildSaw();

            _dust = Kit.Particles(FxRoot, "Sawdust", new Color(0.92f, 0.78f, 0.52f), 1.1f * Scale, 0.05f * Scale, 0.7f, 1.2f * Scale, 38f);
            ParticleSystem.MainModule main = _dust.main;
            main.startColor = new Color(0.92f, 0.78f, 0.52f);
            ParticleSystem.ColorOverLifetimeModule colors = _dust.colorOverLifetime;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.95f, 0.82f, 0.56f), 0f), new GradientColorKey(new Color(0.7f, 0.55f, 0.34f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.6f), new GradientAlphaKey(0f, 1f) });
            colors.color = gradient;

            var soundObject = new GameObject("SawSound");
            soundObject.transform.SetParent(_pivot, false);
            _synth = soundObject.AddComponent<ForgeSawSynth>();
        }

        // 길이 방향이 x축인 통나무 반쪽. 바깥쪽 끝과 안쪽 켜진 면에 밝은 나무 단면이 있다.
        private Transform BuildHalf(string name, int side)
        {
            var group = new GameObject(name);
            group.transform.SetParent(_pivot, false);
            group.transform.localPosition = new Vector3(side * (CoreWidth * 0.5f + HalfPieceLength * 0.5f), CenterY, 0f);

            GameObject body = Kit.Cylinder(group.transform, "Body", Vector3.zero, new Vector3(Radius * 2f, HalfPieceLength * 0.5f, Radius * 2f), Bark);
            body.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            for (int end = -1; end <= 1; end += 2)
            {
                GameObject face = Kit.Cylinder(group.transform, "Face" + end, new Vector3(end * (HalfPieceLength * 0.5f + 0.004f), 0f, 0f), new Vector3(Radius * 1.9f, 0.006f, Radius * 1.9f), Pale);
                face.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                for (int ring = 1; ring <= 2; ring++)
                {
                    float size = Radius * 1.9f * ring / 3f;
                    GameObject line = Kit.Cylinder(group.transform, "Ring" + end + ring, new Vector3(end * (HalfPieceLength * 0.5f + 0.006f), 0f, 0f), new Vector3(size, 0.006f, size), ring == 1 ? new Color(0.7f, 0.5f, 0.28f) : new Color(0.78f, 0.58f, 0.34f));
                    line.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
            }

            return group.transform;
        }

        // 톱: 원점이 톱니 끝이다. 양끝에 손잡이가 있는 큰 켜는 톱.
        private void BuildSaw()
        {
            var group = new GameObject("Saw");
            group.transform.SetParent(_pivot, false);
            _saw = group.transform;
            _blade = Kit.Box(_saw, "Blade", new Vector3(0f, 0.3f, 0f), new Vector3(0.03f, 0.55f, 2.2f), Steel).GetComponent<Renderer>();
            _teeth = Kit.Box(_saw, "Teeth", new Vector3(0f, 0.025f, 0f), new Vector3(0.05f, 0.06f, 2.1f), new Color(0.5f, 0.52f, 0.56f)).GetComponent<Renderer>();
            Kit.Box(_saw, "Spine", new Vector3(0f, 0.55f, 0f), new Vector3(0.06f, 0.06f, 2.2f), new Color(0.3f, 0.3f, 0.34f));
            for (int end = -1; end <= 1; end += 2)
            {
                Kit.Box(_saw, "Handle" + end, new Vector3(0f, 0.42f, end * 1.15f), new Vector3(0.12f, 0.5f, 0.14f), new Color(0.36f, 0.22f, 0.12f));
                Kit.Box(_saw, "Grip" + end, new Vector3(0f, 0.7f, end * 1.15f), new Vector3(0.14f, 0.1f, 0.3f), new Color(0.3f, 0.18f, 0.1f));
            }
        }

        public override void Begin(ForgeCameraFocus camera)
        {
            base.Begin(camera);
            _sawZ = 0f;
            _engage = 0f;
            _split = -1f;
            _pop = 1f;
            _logsCut = 0;
            _core.gameObject.SetActive(true);
            ResetHalves(1f);
        }

        public override void End()
        {
            if (_synth != null)
            {
                _synth.Intensity = 0f;
                _synth.Strain = 0f;
            }

            ForgeGreybox.SetRate(_dust, 0f);
            Sparks(_dust.transform.position, 0f);
            base.End();
        }

        // 매 프레임 게임 값을 3D 장면에 옮긴다.
        public void Show(BowSawGame game, float deltaTime)
        {
            bool playing = game.Phase == BowSawGame.PhaseKind.Play;
            bool cutting = playing && game.Holding && game.BindLeft <= 0f && _split < 0f;
            float zone = game.ZoneHi;
            float over = Mathf.Clamp01((game.Speed - zone) / (zone * 0.4f));

            UpdateLogs(game, deltaTime);
            UpdateSaw(game, deltaTime, playing, cutting, over);
            UpdateEffects(game, cutting, over, zone);
        }

        private void UpdateLogs(BowSawGame game, float deltaTime)
        {
            if (game.LogsCut != _logsCut)
            {
                _logsCut = game.LogsCut;
                _split = 0f;
            }

            if (_split >= 0f)
            {
                _split += deltaTime;
                float t = _split;
                float fall = 4.5f * t * t;
                _left.localPosition = new Vector3(-(CoreWidth * 0.5f + HalfPieceLength * 0.5f) - 2.2f * t, CenterY - fall, 0.3f * t);
                _right.localPosition = new Vector3(CoreWidth * 0.5f + HalfPieceLength * 0.5f + 2.2f * t, CenterY - fall, -0.3f * t);
                _left.localRotation = Quaternion.Euler(0f, 0f, 280f * t * t);
                _right.localRotation = Quaternion.Euler(0f, 0f, -280f * t * t);
                _core.gameObject.SetActive(false);
                if (_split >= SplitSeconds)
                {
                    _split = -1f;
                    _pop = 0f;
                    ResetHalves(0.6f);
                    _core.gameObject.SetActive(true);
                }
            }
            else if (_pop < 1f)
            {
                _pop = Mathf.Min(1f, _pop + deltaTime / PopSeconds);
                ResetHalves(Mathf.Lerp(0.6f, 1f, _pop));
            }

            UpdateKerf(_split < 0f ? Mathf.Clamp01(game.Depth) * Radius * 2f : 0f);
        }

        private void ResetHalves(float scale)
        {
            _left.localPosition = new Vector3(-(CoreWidth * 0.5f + HalfPieceLength * 0.5f), CenterY, 0f);
            _right.localPosition = new Vector3(CoreWidth * 0.5f + HalfPieceLength * 0.5f, CenterY, 0f);
            _left.localRotation = Quaternion.identity;
            _right.localRotation = Quaternion.identity;
            _left.localScale = new Vector3(1f, scale, scale);
            _right.localScale = new Vector3(1f, scale, scale);
            _core.localScale = new Vector3(Radius * 2f * scale, CoreWidth * 0.5f, Radius * 2f * scale);
        }

        // 켜진 깊이만큼 위에서부터 둥근 단면을 따라 어두운 띠를 쌓아 켜진 틈을 보여 준다.
        private void UpdateKerf(float depth)
        {
            float strip = Radius * 2f / StripCount;
            for (int i = 0; i < StripCount; i++)
            {
                float from = i * strip;
                float to = Mathf.Min(depth, from + strip);
                bool visible = to - from > 0.002f;
                _strips[i].gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                float mid = (from + to) * 0.5f;
                float chord = 2f * Mathf.Sqrt(Mathf.Max(0f, mid * (2f * Radius - mid)));
                _strips[i].localScale = new Vector3(CoreWidth + 0.02f, to - from, Mathf.Max(0.05f, chord));
                _strips[i].localPosition = new Vector3(0f, Top - mid, 0f);
            }
        }

        private void UpdateSaw(BowSawGame game, float deltaTime, bool playing, bool cutting, float over)
        {
            bool engaged = playing && game.Holding && _split < 0f;
            _engage += ((engaged ? 1f : 0f) - _engage) * Mathf.Min(1f, deltaTime * 12f);

            if (cutting && game.PointerX > -50f)
            {
                float target = Mathf.Clamp(-game.RelativeY * 0.55f, -0.85f, 0.85f);
                _sawZ += (target - _sawZ) * Mathf.Min(1f, deltaTime * 28f);
            }

            float cutY = Top - Mathf.Clamp01(game.Depth) * Radius * 2f;
            float restY = Top + 0.55f;
            bool jam = game.BindLeft > 0f;
            _saw.localPosition = new Vector3(0f, Mathf.Lerp(restY, cutY, _engage), _sawZ);
            _saw.localRotation = Quaternion.Euler(0f, 0f, jam ? 7f + Mathf.Sin(Time.time * 70f) * 2f : 0f);

            Color bladeColor = Color.Lerp(Steel, new Color(1f, 0.5f, 0.25f), over);
            Color glow = new Color(2f, 0.55f, 0.12f) * over;
            if (jam)
            {
                bladeColor = new Color(0.95f, 0.35f, 0.3f);
                glow = new Color(1.4f, 0.2f, 0.1f);
            }

            Kit.Paint(_blade, bladeColor, glow);
            Kit.Paint(_teeth, Color.Lerp(new Color(0.5f, 0.52f, 0.56f), new Color(1f, 0.4f, 0.2f), over), glow);
        }

        private void UpdateEffects(BowSawGame game, bool cutting, float over, float zone)
        {
            float level = Mathf.Clamp(game.Speed / zone, 0f, 1.6f);
            Vector3 contact = _pivot.TransformPoint(new Vector3(0f, Top + 0.02f - Mathf.Clamp01(game.Depth) * Radius * 2f * 0.3f, Mathf.Clamp(_sawZ, -0.3f, 0.3f)));
            _dust.transform.position = contact;
            ForgeGreybox.SetRate(_dust, cutting ? 20f + 90f * level : 0f);
            Sparks(contact, cutting && over > 0.25f ? 50f * over : 0f);

            _synth.Intensity = cutting ? level : 0f;
            _synth.Strain = cutting ? over : 0f;
        }
    }
}
