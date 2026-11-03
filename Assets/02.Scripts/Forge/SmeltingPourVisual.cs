using System;
using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // ③ 붓기 단계의 크레인에 매달린 도가니, 오가는 틀, 쇳물 줄기, 흘린 쇳물을 맡는다.
    // 도가니 위치는 모든 단계에서 여기서 정한다. 풀무질 동안은 화덕 위에 있고 붓기가 시작되면 크레인으로 옮겨 간다.
    public class SmeltingPourVisual
    {
        private const int StreamSegments = 16;
        private const float MaxTiltDegrees = 62f;

        private readonly SmeltingGreybox _kit;
        private readonly Transform _stage;
        private readonly SmeltingFurnaceVisual _furnace;
        private readonly Transform _mold;
        private readonly Transform _moldFill;
        private readonly Renderer _moldFillRenderer;
        private readonly Transform _moldCap;
        private readonly Renderer _moldCapRenderer;
        private readonly Transform _ribLeft;
        private readonly Transform _ribRight;
        private readonly Transform _ribFront;
        private readonly Renderer _ribLeftRenderer;
        private readonly Renderer _ribRightRenderer;
        private readonly Renderer _ribFrontRenderer;
        private readonly Transform _spillPool;
        private readonly Renderer _spillPoolRenderer;
        private readonly Transform _crane;
        private readonly Transform _chainLeft;
        private readonly Transform _chainRight;
        private readonly Transform[] _segments = new Transform[StreamSegments];
        private readonly Renderer[] _segmentRenderers = new Renderer[StreamSegments];
        private readonly Transform _splash;
        private readonly Renderer _splashRenderer;
        private SmeltingGame _game;
        private float _moldIn;
        private float _cart;
        private float _crucibleBlend;
        private float _tiltShown;
        private float _fillShown;
        private float _poolX;
        private float _poolHeat;
        private float _coolTime = -1f;

        // 쇳물 줄기가 닿는 곳(로컬)과 세기(0~1), 틀 밖으로 튀는 정도. 입자와 소리가 따라 쓴다.
        public bool Flowing { get; private set; }
        public Vector3 LandPoint { get; private set; }
        public bool LandsInMold { get; private set; }
        public float Spilling { get; private set; }
        public Vector3 MoldCenter => new Vector3(_game != null ? _game.MoldX : 0f, SmeltingLayout.MoldY + 0.7f, SmeltingLayout.RailZ);

        public SmeltingPourVisual(SmeltingGreybox kit, Transform stage, SmeltingFurnaceVisual furnace)
        {
            _kit = kit;
            _stage = stage;
            _furnace = furnace;

            float railZ = SmeltingLayout.RailZ;
            Color rail = new Color(0.22f, 0.22f, 0.25f);
            kit.Box(stage, "RailBack", new Vector3(0f, 0.06f, railZ + 0.72f), new Vector3(7.4f, 0.12f, 0.16f), rail);
            kit.Box(stage, "RailFront", new Vector3(0f, 0.06f, railZ - 0.72f), new Vector3(7.4f, 0.12f, 0.16f), rail);

            var moldObject = new GameObject("MoldRoot");
            moldObject.transform.SetParent(stage, false);
            _mold = moldObject.transform;
            Color iron = new Color(0.2f, 0.2f, 0.23f);
            Color stone = new Color(0.34f, 0.32f, 0.33f);
            // 깊고 앞이 트인 틀: 옆에서 쇳물 높이가 오르는 게 그대로 보인다.
            kit.Box(_mold, "Cart", Vector3.zero, new Vector3(1.6f, 0.2f, 1.5f), iron);
            kit.Box(_mold, "WallBack", new Vector3(0f, 0.62f, 0.5f), new Vector3(1.3f, 0.95f, 0.14f), stone);
            kit.Box(_mold, "WallLeft", new Vector3(-0.6f, 0.58f, 0f), new Vector3(0.14f, 0.9f, 1f), stone);
            kit.Box(_mold, "WallRight", new Vector3(0.6f, 0.58f, 0f), new Vector3(0.14f, 0.9f, 1f), stone);
            kit.Box(_mold, "FrontFoot", new Vector3(0f, 0.17f, -0.5f), new Vector3(1.3f, 0.1f, 0.12f), stone);
            kit.Box(_mold, "Floor", new Vector3(0f, 0.15f, 0f), new Vector3(1.06f, 0.06f, 0.9f), new Color(0.12f, 0.11f, 0.12f));

            GameObject fill = kit.Box(_mold, "Fill", new Vector3(0f, 0.18f, 0f), new Vector3(1.06f, 0.02f, 0.88f), new Color(1f, 0.6f, 0.2f));
            _moldFill = fill.transform;
            _moldFillRenderer = fill.GetComponent<Renderer>();
            GameObject cap = kit.Box(_mold, "FillCap", new Vector3(0f, 0.2f, 0f), new Vector3(1.0f, 0.025f, 0.82f), new Color(1f, 0.7f, 0.3f));
            _moldCap = cap.transform;
            _moldCapRenderer = cap.GetComponent<Renderer>();
            cap.SetActive(false);

            // 눈금: 양옆 벽 앞면에 붙은 푸른 표시와 앞쪽 가는 선
            Color cyan = new Color(0.2f, 0.9f, 1f);
            Color cyanGlow = new Color(0.1f, 0.85f, 1f) * 1.4f;
            float lineY = 0.18f + SmeltingLayout.FillHeight;
            Glow(kit.Box(_mold, "FillLine", new Vector3(0f, lineY, -0.5f), new Vector3(1.3f, 0.04f, 0.04f), cyan), cyan, cyanGlow);
            for (int side = -1; side <= 1; side += 2)
            {
                Glow(kit.Box(_mold, "FillMark" + side, new Vector3(0.6f * side, lineY, -0.52f), new Vector3(0.2f, 0.06f, 0.06f), cyan), cyan, cyanGlow);
            }

            // 눈금을 넘기면 틀 가장자리로 쇳물이 넘치는 모습
            Color orange = new Color(1f, 0.6f, 0.2f);
            GameObject ribLeft = kit.Box(_mold, "RibL", new Vector3(-0.68f, 0.4f, -0.1f), new Vector3(0.1f, 1.1f, 0.2f), orange);
            GameObject ribRight = kit.Box(_mold, "RibR", new Vector3(0.68f, 0.4f, -0.1f), new Vector3(0.1f, 1.1f, 0.2f), orange);
            GameObject ribFront = kit.Box(_mold, "RibF", new Vector3(0f, 0.4f, -0.58f), new Vector3(1.1f, 1.1f, 0.08f), orange);
            _ribLeft = ribLeft.transform;
            _ribRight = ribRight.transform;
            _ribFront = ribFront.transform;
            _ribLeftRenderer = ribLeft.GetComponent<Renderer>();
            _ribRightRenderer = ribRight.GetComponent<Renderer>();
            _ribFrontRenderer = ribFront.GetComponent<Renderer>();
            ribLeft.SetActive(false);
            ribRight.SetActive(false);
            ribFront.SetActive(false);

            GameObject pool = kit.Cylinder(stage, "SpillPool", new Vector3(0f, 0.02f, railZ - 0.5f), new Vector3(0.01f, 0.02f, 0.01f), new Color(1f, 0.5f, 0.1f));
            _spillPool = pool.transform;
            _spillPoolRenderer = pool.GetComponent<Renderer>();
            pool.SetActive(false);

            var craneObject = new GameObject("Crane");
            craneObject.transform.SetParent(stage, false);
            _crane = craneObject.transform;
            float postHeight = SmeltingLayout.CraneY;
            kit.Box(_crane, "CraneBeam", new Vector3(0f, SmeltingLayout.CraneY, SmeltingLayout.HangZ), new Vector3(8.4f, 0.22f, 0.3f), rail);
            kit.Box(_crane, "CranePostL", new Vector3(-4.1f, postHeight * 0.5f, SmeltingLayout.HangZ), new Vector3(0.24f, postHeight, 0.24f), new Color(0.2f, 0.2f, 0.23f));
            kit.Box(_crane, "CranePostR", new Vector3(4.1f, postHeight * 0.5f, SmeltingLayout.HangZ), new Vector3(0.24f, postHeight, 0.24f), new Color(0.2f, 0.2f, 0.23f));
            _chainLeft = kit.Box(_crane, "ChainL", Vector3.zero, new Vector3(0.05f, 1f, 0.05f), new Color(0.3f, 0.3f, 0.33f)).transform;
            _chainRight = kit.Box(_crane, "ChainR", Vector3.zero, new Vector3(0.05f, 1f, 0.05f), new Color(0.3f, 0.3f, 0.33f)).transform;
            _crane.gameObject.SetActive(false);

            var stream = new GameObject("Stream");
            stream.transform.SetParent(stage, false);
            for (int i = 0; i < StreamSegments; i++)
            {
                GameObject segment = kit.Cylinder(stream.transform, "Seg" + i, Vector3.zero, new Vector3(0.1f, 0.25f, 0.1f), new Color(1f, 0.6f, 0.2f));
                _segments[i] = segment.transform;
                _segmentRenderers[i] = segment.GetComponent<Renderer>();
                segment.SetActive(false);
            }

            GameObject splash = kit.Cylinder(stream.transform, "Splash", Vector3.zero, new Vector3(0.3f, 0.02f, 0.3f), new Color(1f, 0.6f, 0.2f));
            _splash = splash.transform;
            _splashRenderer = splash.GetComponent<Renderer>();
            splash.SetActive(false);
        }

        public void Bind(SmeltingGame game)
        {
            _game = game;
        }

        public void ResetVisual()
        {
            _moldIn = 0f;
            _cart = 0f;
            _crucibleBlend = 0f;
            _tiltShown = 0f;
            _fillShown = 0f;
            _poolHeat = 0f;
            _coolTime = -1f;
            _crane.gameObject.SetActive(false);
            _spillPool.gameObject.SetActive(false);
            Flowing = false;
        }

        // 붓기가 끝나 주괴가 굳기 시작한다.
        public void StartCooling()
        {
            _coolTime = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (_game == null)
            {
                return;
            }

            UpdateMold(deltaTime);
            UpdateCrucible(deltaTime);
            UpdateStream(deltaTime);
        }

        private bool Pouring => _game.Act == SmeltingGame.ActKind.Pour || _game.Act == SmeltingGame.ActKind.Done;

        private void UpdateMold(float deltaTime)
        {
            _moldIn = Mathf.MoveTowards(_moldIn, Pouring ? 1f : 0f, deltaTime * 1.6f);
            float ease = _moldIn * _moldIn * (3f - 2f * _moldIn);
            _cart = Mathf.Lerp(_cart, Pouring ? _game.MoldX : 0f, 1f - Mathf.Exp(-30f * deltaTime));
            _mold.localPosition = new Vector3(_cart, Mathf.Lerp(SmeltingLayout.MoldY - 1.5f, SmeltingLayout.MoldY, ease), SmeltingLayout.RailZ);
            // 어려울수록 입구가 좁다.
            _mold.localScale = new Vector3(_game.MoldHalfWidth / 0.5f, 1f, 1f);

            // 채움: 부드럽게 오른다. 다 부은 뒤에는 식어서 주괴 색이 된다.
            _fillShown = Mathf.MoveTowards(_fillShown, _game.Fill, deltaTime * 1.2f);
            float height = 0.02f + SmeltingLayout.FillHeight * Mathf.Min(_fillShown, 1.3f);
            _moldFill.localScale = new Vector3(1.06f, height, 0.88f);
            _moldFill.localPosition = new Vector3(0f, 0.18f + height * 0.5f, 0f);
            if (_game.Act == SmeltingGame.ActKind.Done && _coolTime >= 0f)
            {
                _coolTime += deltaTime;
            }
            float heat = _game.Act == SmeltingGame.ActKind.Done
                ? Mathf.Clamp01(1f - _coolTime * 0.7f) * 0.55f
                : 0.85f * Mathf.Clamp01(0.4f + _game.MetalHeat);
            _kit.PaintHot(_moldFillRenderer, heat, 0.4f);

            // 쇳물 위쪽 면은 더 밝게 빛나서 액체처럼 보인다.
            _moldCap.localPosition = new Vector3(0f, 0.18f + height + 0.012f, 0f);
            _moldCap.gameObject.SetActive(_fillShown > 0.02f);
            _kit.PaintHot(_moldCapRenderer, Mathf.Clamp01(heat + 0.2f), 0.4f);
        }

        private void UpdateCrucible(float deltaTime)
        {
            _crucibleBlend = Mathf.MoveTowards(_crucibleBlend, Pouring ? 1f : 0f, deltaTime * 1.3f);
            float ease = _crucibleBlend * _crucibleBlend * (3f - 2f * _crucibleBlend);

            // 도가니는 주둥이를 축으로 기운다(주둥이 위치는 고정이라 줄기가 늘 같은 곳에서 떨어진다).
            float tiltWant = _game.Act == SmeltingGame.ActKind.Pour ? _game.Tilt : 0f;
            _tiltShown = Mathf.Lerp(_tiltShown, tiltWant, 1f - Mathf.Exp(-24f * deltaTime));
            float angle = MaxTiltDegrees * _tiltShown * ease;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            float spout = Pouring ? _game.Spout : _game.SpoutStart;
            Vector3 hang = new Vector3(spout, SmeltingLayout.HangY, SmeltingLayout.HangZ) - rotation * SmeltingLayout.LipLocal;
            Vector3 position = Vector3.Lerp(SmeltingLayout.PitPos, hang, ease) + Vector3.up * (Mathf.Sin(ease * Mathf.PI) * 0.9f);
            _furnace.Crucible.localPosition = position;
            _furnace.Crucible.localRotation = rotation;

            UpdateCrane(position, ease);
            UpdateMelt(position, angle);
        }

        // 쇳물 면과 찌꺼기는 도가니가 기울어도 수평을 유지한다.
        private void UpdateMelt(Vector3 position, float angle)
        {
            bool bellows = _game.Act == SmeltingGame.ActKind.Bellows;
            bool molten = Pouring || (bellows && _game.Molten);
            bool showMelt = molten || (bellows && _game.Heat > 0.6f);
            _furnace.Melt.gameObject.SetActive(showMelt);
            _furnace.Slag.gameObject.SetActive(molten);
            if (!showMelt)
            {
                return;
            }

            float level = Pouring
                ? -0.25f + 0.5f * Mathf.Clamp01(_game.Reserve / Mathf.Max(0.01f, _game.ReserveStart))
                : -0.25f + 0.45f * Mathf.Clamp01((_game.Heat - 0.55f) / 0.45f);
            float radius = 1.72f * Mathf.Sqrt(Mathf.Max(0.05f, Mathf.Cos(angle * Mathf.Deg2Rad)));
            Vector3 center = position + Vector3.up * level;
            bool empty = Pouring && _game.Reserve < 0.02f;
            _furnace.Melt.localPosition = center;
            _furnace.Melt.localRotation = Quaternion.identity;
            _furnace.Melt.localScale = new Vector3(empty ? 0.01f : radius, 0.02f, empty ? 0.01f : radius);
            float meltHeat = Pouring ? Mathf.Clamp01(0.4f + _game.MetalHeat * 0.7f) : Mathf.Clamp01(0.35f + _game.Heat * 0.5f);
            _kit.PaintHot(_furnace.MeltRenderer, meltHeat, 0.3f);

            if (!molten)
            {
                return;
            }

            float share = Mathf.Clamp01(_game.Dross / Mathf.Max(Mathf.Max(_game.Reserve, 0.0001f), _game.Dross));
            float slagRadius = Mathf.Min(radius * Mathf.Sqrt(share) * (Pouring ? 1f : 0.6f), radius * 0.99f);
            _furnace.Slag.localPosition = center + Vector3.up * 0.015f;
            _furnace.Slag.localRotation = Quaternion.identity;
            _furnace.Slag.localScale = new Vector3(empty ? 0.01f : slagRadius, 0.022f, empty ? 0.01f : slagRadius);
        }

        private void UpdateCrane(Vector3 cruciblePosition, float ease)
        {
            bool show = ease > 0.01f;
            _crane.gameObject.SetActive(show);
            if (!show)
            {
                return;
            }

            float bottom = cruciblePosition.y + 0.55f;
            float length = Mathf.Max(0.1f, SmeltingLayout.CraneY - bottom);
            float middle = (SmeltingLayout.CraneY + bottom) * 0.5f;
            _chainLeft.localPosition = new Vector3(cruciblePosition.x - 0.8f, middle, cruciblePosition.z);
            _chainRight.localPosition = new Vector3(cruciblePosition.x + 0.8f, middle, cruciblePosition.z);
            _chainLeft.localScale = new Vector3(0.05f, length, 0.05f);
            _chainRight.localScale = new Vector3(0.05f, length, 0.05f);
        }

        private void UpdateStream(float deltaTime)
        {
            bool pouring = _game.Act == SmeltingGame.ActKind.Pour && _game.Flow > 0.004f;
            float strength = Mathf.Clamp01(_game.Flow / _game.FlowMax);
            float width = 0.07f + 0.4f * Mathf.Pow(strength, 0.85f);
            Vector3 lip = _stage.InverseTransformPoint(_furnace.Crucible.TransformPoint(SmeltingLayout.LipLocal));

            LandsInMold = _game.LandsInMold;
            float endY = LandsInMold
                ? SmeltingLayout.MoldY + 0.18f + SmeltingLayout.FillHeight * Mathf.Min(_fillShown, 1.3f)
                : 0.04f;

            // 눈금을 넘기면 가장자리로 넘치고, 줄기가 틀에서 빗나가면(약하거나 너무 세면) 바닥으로 흐른다.
            float overflow = pouring ? Mathf.Clamp01((_game.Fill - _game.FillHigh) / 0.1f) : 0f;
            float miss = pouring ? Mathf.Clamp01(_game.MissAmount / _game.MoldEdge) : 0f;
            Spilling = Mathf.Max(overflow, miss);
            UpdateOverflowRibs(overflow);
            if (miss > 0.02f)
            {
                _poolX = _game.StreamX(1f);
            }
            UpdateSpillPool(deltaTime, Spilling > 0.02f || overflow > 0.02f);

            Vector3 previous = lip;
            float landX = _game.StreamX(1f);
            for (int i = 0; i < StreamSegments; i++)
            {
                float f = (i + 1f) / StreamSegments;
                // 주둥이에서 옆으로 나온 쇳물이 중력으로 휘며 떨어진다. 줄기는 떨어질수록 가늘어지고, 세면 출렁인다.
                float x = Mathf.Lerp(lip.x, landX, f) + Mathf.Sin(Time.time * 16f - f * 6f) * 0.03f * strength * f;
                float y = Mathf.Lerp(lip.y, endY, f * f);
                var point = new Vector3(x, y, Mathf.Lerp(lip.z, SmeltingLayout.RailZ, f));
                Transform segment = _segments[i];
                // 아주 약하면 끊겨 똑똑 떨어지는 물방울처럼 보인다.
                bool drip = strength < 0.08f && Mathf.Sin(Time.time * 14f - i * 1.7f) < -0.2f;
                if (!pouring || drip)
                {
                    segment.gameObject.SetActive(false);
                    previous = point;
                    continue;
                }

                segment.gameObject.SetActive(true);
                Vector3 delta = point - previous;
                float length = delta.magnitude;
                segment.localPosition = (point + previous) * 0.5f;
                if (length > 0.001f)
                {
                    segment.localRotation = Quaternion.LookRotation(delta, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
                }
                float taper = Mathf.Lerp(1f, 0.55f, f);
                segment.localScale = new Vector3(width * taper, length * 0.58f, width * taper);
                _kit.PaintHot(_segmentRenderers[i], Mathf.Clamp01(0.3f + 0.7f * _game.MetalHeat), 0.3f);
                previous = point;
            }

            // 닿는 곳에 쇳물이 퍼지는 자국
            _splash.gameObject.SetActive(pouring);
            if (pouring)
            {
                float spread = width * (2.4f + 0.5f * Mathf.Sin(Time.time * 20f));
                _splash.localPosition = new Vector3(landX, endY + 0.01f, SmeltingLayout.RailZ);
                _splash.localScale = new Vector3(spread, 0.02f, spread);
                _kit.PaintHot(_splashRenderer, Mathf.Clamp01(0.4f + 0.6f * _game.MetalHeat), 0.3f);
            }

            Flowing = pouring;
            LandPoint = new Vector3(previous.x, endY, previous.z);
        }

        private void UpdateOverflowRibs(float overflow)
        {
            bool over = overflow > 0.02f;
            _ribLeft.gameObject.SetActive(over);
            _ribRight.gameObject.SetActive(over);
            _ribFront.gameObject.SetActive(over);
            if (!over)
            {
                return;
            }

            float w = 0.05f + 0.16f * overflow;
            _ribLeft.localScale = new Vector3(w, 1.1f, 0.2f + 0.2f * overflow);
            _ribRight.localScale = _ribLeft.localScale;
            _ribFront.localScale = new Vector3(0.9f + 0.2f * overflow, 1.1f, 0.05f + 0.06f * overflow);
            _kit.PaintHot(_ribLeftRenderer, 0.75f, 0.3f);
            _kit.PaintHot(_ribRightRenderer, 0.75f, 0.3f);
            _kit.PaintHot(_ribFrontRenderer, 0.8f, 0.3f);
        }

        // 흘린 쇳물 웅덩이는 흘린 만큼 커지고, 식으면서 어두워진다.
        private void UpdateSpillPool(float deltaTime, bool spilling)
        {
            float spilled = Mathf.Max(0f, _game.Spilled);
            _spillPool.gameObject.SetActive(spilled > 0.01f);
            if (spilled <= 0.01f)
            {
                return;
            }

            float diameter = 0.8f + 3.4f * Mathf.Sqrt(Mathf.Clamp01(spilled / 0.7f));
            _spillPool.localScale = Vector3.Lerp(_spillPool.localScale, new Vector3(diameter, 0.02f, diameter * 0.8f), 1f - Mathf.Exp(-4f * deltaTime));
            _spillPool.localPosition = new Vector3(_poolX, 0.02f, SmeltingLayout.RailZ - 0.35f);
            _poolHeat = spilling ? Mathf.MoveTowards(_poolHeat, 0.85f, deltaTime * 2f) : Mathf.MoveTowards(_poolHeat, 0.15f, deltaTime * 0.12f);
            _kit.PaintHot(_spillPoolRenderer, _poolHeat, 0.15f);
        }

        private void Glow(GameObject box, Color color, Color emission)
        {
            _kit.Paint(box.GetComponent<Renderer>(), color, emission);
        }
    }
}
