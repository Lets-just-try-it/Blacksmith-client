using System.Collections.Generic;
using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // ② 풀무질 단계의 화덕, 풀무, 도가니 안의 쇳덩이를 맡는다. 도가니를 옮기고 기울이는 일은 SmeltingPourVisual이 한다.
    public class SmeltingFurnaceVisual
    {
        private const int NuggetCount = 8;
        private const int RimPieces = 18;
        private const int WallPieces = 24;
        private const float WallRadius = 0.93f;
        private const float RimRadius = 0.96f;
        private static readonly Vector3 NuggetSize = new Vector3(0.36f, 0.3f, 0.34f);

        private readonly SmeltingGreybox _kit;
        private readonly List<Renderer> _pot = new List<Renderer>();
        private readonly Renderer _hearthMouth;
        private readonly List<Transform> _nuggets = new List<Transform>();
        private readonly List<Renderer> _nuggetRenderers = new List<Renderer>();
        private readonly Transform _bellowsTop;
        private readonly Transform _bellowsLeather;
        private readonly Transform _bellowsHandle;
        private SmeltingGame _game;
        private float _bellowsKick;
        private int _arrived;

        public Transform Crucible { get; }
        public Transform Melt { get; }
        public Renderer MeltRenderer { get; }
        public Transform Slag { get; }
        // 지금 화덕이 달궈진 정도(0~1). 불꽃과 조명, 소리가 따라 쓴다.
        public float HeatShown { get; private set; }

        public SmeltingFurnaceVisual(SmeltingGreybox kit, Transform stage)
        {
            _kit = kit;
            Color stone = new Color(0.30f, 0.29f, 0.31f);
            Color dark = new Color(0.13f, 0.12f, 0.13f);
            Vector3 hearth = SmeltingLayout.HearthPos;
            float height = SmeltingLayout.HearthHeight;

            kit.Box(stage, "Hearth", hearth + new Vector3(0f, height * 0.5f, 0f), new Vector3(3f, height, 1.9f), stone);
            kit.Box(stage, "HearthTopL", hearth + new Vector3(-1.25f, height + 0.08f, 0f), new Vector3(0.5f, 0.16f, 1.95f), dark);
            kit.Box(stage, "HearthTopR", hearth + new Vector3(1.25f, height + 0.08f, 0f), new Vector3(0.5f, 0.16f, 1.95f), dark);
            _hearthMouth = kit.Box(stage, "HearthMouth", hearth + new Vector3(0f, 0.45f, -0.96f), new Vector3(1.7f, 0.55f, 0.06f), new Color(1f, 0.4f, 0.1f)).GetComponent<Renderer>();

            var crucibleObject = new GameObject("Crucible");
            crucibleObject.transform.SetParent(stage, false);
            crucibleObject.transform.localPosition = SmeltingLayout.PitPos;
            Crucible = crucibleObject.transform;
            // 도가니는 위가 뚫린 그릇이다. 쇳물 면이 그 안에 담겨 보이고 기울여도 그릇 안에서 수평을 유지한다.
            _pot.Add(kit.Cylinder(Crucible, "PotBottom", new Vector3(0f, -0.45f, 0f), new Vector3(1.9f, 0.05f, 1.9f), new Color(0.2f, 0.2f, 0.22f)).GetComponent<Renderer>());
            for (int i = 0; i < WallPieces; i++)
            {
                float wallAngle = i / (float)WallPieces * Mathf.PI * 2f;
                GameObject wall = kit.Box(Crucible, "PotWall" + i, new Vector3(Mathf.Cos(wallAngle) * WallRadius, 0f, Mathf.Sin(wallAngle) * WallRadius),
                    new Vector3(0.27f, 1f, 0.1f), new Color(0.2f, 0.2f, 0.22f));
                wall.transform.localRotation = Quaternion.Euler(0f, -wallAngle * Mathf.Rad2Deg - 90f, 0f);
                _pot.Add(wall.GetComponent<Renderer>());
            }
            // 도가니 가장자리: 안쪽이 비어 있어 쇳물 면이 그릇 안에 담긴 것처럼 보인다.
            for (int i = 0; i < RimPieces; i++)
            {
                float angle = i / (float)RimPieces * Mathf.PI * 2f;
                GameObject piece = kit.Box(Crucible, "RimPiece" + i, new Vector3(Mathf.Cos(angle) * RimRadius, 0.54f, Mathf.Sin(angle) * RimRadius),
                    new Vector3(0.42f, 0.12f, 0.12f), new Color(0.32f, 0.32f, 0.36f));
                piece.transform.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 90f, 0f);
            }
            kit.Box(Crucible, "Lip", SmeltingLayout.LipLocal + new Vector3(-0.1f, -0.02f, 0f), new Vector3(0.35f, 0.08f, 0.7f), new Color(0.3f, 0.3f, 0.34f));
            kit.Box(Crucible, "Handle1", new Vector3(-1.1f, 0.15f, 0f), new Vector3(0.4f, 0.1f, 0.14f), new Color(0.18f, 0.18f, 0.2f));
            kit.Box(Crucible, "Handle2", new Vector3(1.1f, 0.15f, 0f), new Vector3(0.4f, 0.1f, 0.14f), new Color(0.18f, 0.18f, 0.2f));

            GameObject melt = kit.Cylinder(stage, "Melt", Vector3.zero, new Vector3(1.7f, 0.02f, 1.7f), Color.gray);
            Melt = melt.transform;
            MeltRenderer = melt.GetComponent<Renderer>();
            Melt.gameObject.SetActive(false);
            GameObject slag = kit.Cylinder(stage, "Slag", Vector3.zero, new Vector3(0.5f, 0.022f, 0.5f), new Color(0.2f, 0.09f, 0.05f));
            kit.Paint(slag.GetComponent<Renderer>(), new Color(0.2f, 0.09f, 0.05f), new Color(0.35f, 0.1f, 0.02f));
            Slag = slag.transform;
            Slag.gameObject.SetActive(false);

            // 도가니 안의 쇳덩이는 처음엔 비어 있고, 원석이 쪼개지면 하나씩 들어온다.
            for (int i = 0; i < NuggetCount; i++)
            {
                float angle = i / (float)NuggetCount * Mathf.PI * 2f;
                float radius = 0.2f + 0.35f * ((i * 37) % 5) / 4f;
                GameObject nugget = kit.Box(Crucible, "Nugget" + i,
                    new Vector3(Mathf.Cos(angle) * radius, -0.25f + 0.04f * (i % 3), Mathf.Sin(angle) * radius), NuggetSize, new Color(0.55f, 0.5f, 0.45f));
                nugget.transform.localRotation = Quaternion.Euler(i * 31f, i * 53f, i * 17f);
                nugget.SetActive(false);
                _nuggets.Add(nugget.transform);
                _nuggetRenderers.Add(nugget.GetComponent<Renderer>());
            }

            Color wood = new Color(0.4f, 0.26f, 0.15f);
            Vector3 bellows = SmeltingLayout.BellowsPos;
            kit.Box(stage, "BellowsBase", bellows + new Vector3(0f, 0.12f, 0f), new Vector3(1.9f, 0.14f, 1.3f), wood);
            _bellowsLeather = kit.Box(stage, "BellowsLeather", bellows + new Vector3(0f, 0.55f, 0f), new Vector3(1.6f, 0.7f, 1.15f), new Color(0.32f, 0.18f, 0.1f)).transform;
            _bellowsTop = kit.Box(stage, "BellowsTop", bellows + new Vector3(0f, 0.95f, 0f), new Vector3(1.9f, 0.14f, 1.3f), wood).transform;
            _bellowsHandle = kit.Box(stage, "BellowsHandle", bellows + new Vector3(-1.1f, 1.3f, 0f), new Vector3(1.2f, 0.14f, 0.14f), wood).transform;
            kit.Box(stage, "Nozzle", bellows + new Vector3(1.4f, 0.5f, 0f), new Vector3(1.4f, 0.22f, 0.22f), new Color(0.2f, 0.2f, 0.22f));
        }

        public void Bind(SmeltingGame game)
        {
            _game = game;
        }

        public void ResetVisual()
        {
            foreach (Transform nugget in _nuggets)
            {
                nugget.gameObject.SetActive(false);
            }
            _arrived = 0;
            _bellowsKick = 0f;
            HeatShown = 0f;
        }

        // 쪼개진 쇳덩이 하나가 도가니에 떨어졌다.
        public void AddNugget()
        {
            _nuggets[Mathf.Min(_arrived, _nuggets.Count - 1)].gameObject.SetActive(true);
            _arrived++;
        }

        public void Tick(float deltaTime)
        {
            if (_game == null)
            {
                return;
            }

            UpdateHearth(deltaTime);
            UpdateBellows(deltaTime);
        }

        private float HeatTarget
        {
            get
            {
                if (_game.Act == SmeltingGame.ActKind.Crack)
                {
                    return 0.12f;
                }
                if (_game.Act == SmeltingGame.ActKind.Bellows)
                {
                    return _game.Heat;
                }
                return 0.3f + 0.7f * Mathf.Lerp(0.5f, 1f, _game.MetalHeat);
            }
        }

        private bool Pouring => _game.Act == SmeltingGame.ActKind.Pour || _game.Act == SmeltingGame.ActKind.Done;

        private void UpdateHearth(float deltaTime)
        {
            HeatShown = Mathf.MoveTowards(HeatShown, HeatTarget, deltaTime * 1.6f);
            _kit.PaintHot(_hearthMouth, Mathf.Clamp01(0.3f + HeatShown * 0.8f), 0.25f);

            // 도가니: 불이 세질수록 달아오른다.
            float potHeat = _game.Act == SmeltingGame.ActKind.Crack ? 0.05f : Mathf.Clamp01(HeatShown * (Pouring ? 0.35f : 0.5f));
            foreach (Renderer piece in _pot)
            {
                _kit.PaintHot(piece, potHeat, 0.2f);
            }

            // 녹는 중: 쇳덩이가 작아지고 쇳물이 차오른다.
            bool bellows = _game.Act == SmeltingGame.ActKind.Bellows;
            float melt = bellows ? Mathf.Clamp01((_game.Heat - 0.55f) / 0.45f) : (_game.Act == SmeltingGame.ActKind.Crack ? 0f : 1f);
            for (int i = 0; i < _nuggets.Count; i++)
            {
                if (!_nuggets[i].gameObject.activeSelf)
                {
                    continue;
                }

                _nuggets[i].localScale = NuggetSize * Mathf.Lerp(1f, 0.05f, melt);
                _kit.PaintHot(_nuggetRenderers[i], Mathf.Clamp01(HeatShown * 1.1f + 0.15f), 0.45f);
                if (melt >= 1f)
                {
                    _nuggets[i].gameObject.SetActive(false);
                }
            }
        }

        // 지시선이 오르는 동안 풀무가 눌려 있고, 가라앉는 동안 벌어진다.
        private void UpdateBellows(float deltaTime)
        {
            bool active = _game.Act == SmeltingGame.ActKind.Bellows && !_game.Molten;
            float want = active ? Mathf.Clamp01(0.15f + _game.CursorV * 1.1f) : 0f;
            _bellowsKick = Mathf.Lerp(_bellowsKick, want, 1f - Mathf.Exp(-10f * deltaTime));
            float squeeze = Mathf.Clamp01(_bellowsKick);
            Vector3 b = SmeltingLayout.BellowsPos;
            _bellowsTop.localPosition = b + new Vector3(0f, 0.95f - 0.38f * squeeze, 0f);
            _bellowsLeather.localPosition = b + new Vector3(0f, 0.55f - 0.19f * squeeze, 0f);
            _bellowsLeather.localScale = new Vector3(1.6f, 0.7f - 0.38f * squeeze, 1.15f);
            _bellowsHandle.localPosition = b + new Vector3(-1.1f, 1.3f - 0.38f * squeeze, 0f);
            _bellowsHandle.localRotation = Quaternion.Euler(0f, 0f, 18f * squeeze);
        }
    }
}
