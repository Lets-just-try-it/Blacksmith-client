using UnityEngine;

namespace Blacksmith.Forge
{
    // 작업대 임시 그레이박스(정제, 연마)를 만드는 도구. 상자·원통 하나에 머티리얼 하나를 쓰고 색은 MaterialPropertyBlock으로 칠한다.
    // 실제 모델이 들어오면 작업대 무대가 이 도구로 만드는 부분을 프리팹으로 바꾸면 된다.
    public class ForgeGreybox
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private readonly Material _body;
        private readonly Material _particle;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

        public ForgeGreybox(Material body, Material particle)
        {
            _body = body;
            _particle = particle;
        }

        public GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Color color)
        {
            return Primitive(PrimitiveType.Cube, parent, name, position, size, color);
        }

        // 지름 x·z, 높이 y*2인 원통이다(유니티 원통 기본 크기).
        public GameObject Cylinder(Transform parent, string name, Vector3 position, Vector3 size, Color color)
        {
            return Primitive(PrimitiveType.Cylinder, parent, name, position, size, color);
        }

        public void Paint(Renderer renderer, Color color, Color emission)
        {
            renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColor, color);
            _block.SetColor(EmissionColor, emission);
            renderer.SetPropertyBlock(_block);
        }

        // 달궈진 정도(0~1)에 따라 쇠 색에서 붉은색, 주황, 노랑으로 칠한다. 겉색은 어둡게 두고 빛은 발광으로 낸다.
        public void PaintHot(Renderer renderer, float heat, float baseGray = 0.35f)
        {
            Color hot = HeatColor(heat);
            Color metal = new Color(baseGray, baseGray, baseGray + 0.04f);
            Color body = Color.Lerp(metal, hot, Mathf.Clamp01(heat * 1.6f)) * 0.72f;
            Color glow = hot * (Mathf.Clamp01((heat - 0.15f) / 0.85f) * 0.9f);
            Paint(renderer, body, glow);
        }

        public static Color HeatColor(float t)
        {
            t = Mathf.Clamp01(t);
            Color c0 = new Color(0.22f, 0.22f, 0.25f);
            Color c1 = new Color(0.65f, 0.1f, 0.05f);
            Color c2 = new Color(1.0f, 0.45f, 0.08f);
            Color c3 = new Color(1.0f, 0.68f, 0.2f);
            if (t < 0.3f)
            {
                return Color.Lerp(c0, c1, t / 0.3f);
            }
            if (t < 0.65f)
            {
                return Color.Lerp(c1, c2, (t - 0.3f) / 0.35f);
            }
            return Color.Lerp(c2, c3, (t - 0.65f) / 0.35f);
        }

        // 크기 값은 월드 길이다. 부모가 줄여져 있어도 입자가 같은 크기로 나오도록 호출하는 쪽에서 배율을 곱해 넘긴다.
        public ParticleSystem Particles(Transform parent, string name, Color color, float speed, float size, float life, float gravity, float coneAngle)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = life;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.6f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 600;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = coneAngle;
            shape.radius = 0.05f * size * 3f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0.6f), new GradientColorKey(new Color(0.4f, 0.1f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = gradient;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.2f)));

            var particleRenderer = go.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = _particle;
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            return system;
        }

        public static void Burst(ParticleSystem system, int count)
        {
            system.Emit(count);
        }

        public static void SetRate(ParticleSystem system, float rate)
        {
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = rate;
        }

        private GameObject Primitive(PrimitiveType type, Transform parent, string name, Vector3 position, Vector3 size, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;

            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = _body;
            Paint(renderer, color, Color.black);
            return go;
        }
    }
}
