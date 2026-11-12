using System.Collections.Generic;
using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 시험 사격의 3D 장면: 사수의 눈높이에서 작은 대장간 사격실을 바라본다. 작업대 위 미니어처가 아니라 작업장 밖 멀리(아래쪽 허공)에 따로 지은 장면이다.
    // 돌바닥에 석벽과 목재 보, 횃불이 있고 안쪽 벽에는 용광로가 탄다. 과녁은 사람 키만 한 볏짚 허수아비(가슴과 머리)다.
    // 허수아비는 그림판(BowRangeBoardView)의 같은 화면 자리에 거리만큼 떨어뜨려 놓아서, 마우스 조준점과 3D 모습이 어긋나지 않는다.
    // 화면 아래에는 일인칭 활이 있고, 조준점을 향해 겨누고 시위를 당길수록 활이 휘고 화살이 뒤로 물러난다.
    // 놓으면 화살이 포물선으로 날아가 허수아비에 꽂힌다. 머리를 맞히면(치명타) 머리가 크게 흔들리고 짚이 튄다. 바람은 옆에 세운 깃발이 보여 준다.
    // 모양은 임시 그레이박스라 코드로 만든다. 진행과 입력은 BowRangeStation이, 규칙은 BowRangeGame이 맡는다.
    public class BowRangeStage : MiniStage
    {
        private const float EyeHeight = 1.6f;
        private const float ArrowLength = 1.3f;
        private const float DepthBelowRoom = -300f;

        private static readonly Color Straw = new Color(0.86f, 0.7f, 0.32f);
        private static readonly Color Burlap = new Color(0.72f, 0.6f, 0.38f);

        private float _headKick = -1f;
        private readonly List<Transform> _stuck = new List<Transform>();
        private readonly Transform[] _legs = new Transform[2];
        private Transform _range;
        private Transform _target;
        private Transform _boss;
        private Transform _head;
        private Transform _brim;
        private Transform _crown;
        private Transform _bottomStraw;
        private readonly Transform[] _eyes = new Transform[2];
        private readonly Transform[] _hands = new Transform[2];
        private Transform _flagPole;
        private Transform _flag;
        private Transform _bowRoot;
        private Transform _upperLimb;
        private Transform _lowerLimb;
        private Transform _stringUpper;
        private Transform _stringLower;
        private Transform _nocked;
        private Transform _handPull;
        private Transform _flying;
        private Vector3 _flyFrom;
        private Vector3 _tipWorld;
        private Vector3 _flyOffset;
        private float _flyClock;
        private bool _isFlying;
        private int _arrowIndex = -1;
        private int _shotsSeen;
        private float _flightTime = 0.7f;
        private float _distance = 30f;

        private Vector3 Eye => _range.position + new Vector3(0f, EyeHeight, 0f);

        protected override void BuildProps()
        {
            var range = new GameObject("Range");
            range.transform.SetParent(StageRoot, false);
            range.transform.position = StageRoot.position + new Vector3(0f, DepthBelowRoom, 0f);
            range.transform.rotation = Quaternion.identity;
            range.transform.localScale = Vector3.one / Scale;
            _range = range.transform;

            BuildGround();
            BuildTarget();
            BuildBow();
        }

        // 대장간 사격실: 작은 긴 방. 돌바닥 가운데에 널판 길, 양옆 석벽, 천장 보, 벽의 횃불, 안쪽 벽의 용광로.
        private void BuildGround()
        {
            Color stone = new Color(0.45f, 0.43f, 0.4f);
            Color darkStone = new Color(0.32f, 0.31f, 0.3f);
            Color timber = new Color(0.34f, 0.21f, 0.12f);
            Kit.Box(_range, "Floor", new Vector3(0f, -0.5f, 14f), new Vector3(10f, 1f, 36f), new Color(0.4f, 0.38f, 0.36f));
            Kit.Box(_range, "Lane", new Vector3(0f, 0.01f, 14f), new Vector3(3f, 0.02f, 30f), new Color(0.5f, 0.34f, 0.2f));
            Kit.Box(_range, "ShootingLine", new Vector3(0f, 0.03f, 1.2f), new Vector3(3f, 0.03f, 0.1f), Color.white);
            for (int meters = 5; meters <= 25; meters += 5)
            {
                Kit.Box(_range, "Mark" + meters, new Vector3(0f, 0.03f, meters), new Vector3(3f, 0.03f, 0.07f), meters % 10 == 0 ? new Color(0.85f, 0.3f, 0.25f) : new Color(0.9f, 0.9f, 0.85f));
            }

            // 벽과 천장(천장은 그림자를 드리우지 않아 안이 어두워지지 않는다)
            for (int side = -1; side <= 1; side += 2)
            {
                Kit.Box(_range, "Wall" + side, new Vector3(side * 5.4f, 2.5f, 14f), new Vector3(0.8f, 5f, 36f), stone);
                for (int i = 0; i < 5; i++)
                {
                    Kit.Box(_range, "Pillar" + side + i, new Vector3(side * 4.9f, 2.4f, 3f + i * 6f), new Vector3(0.5f, 4.8f, 0.5f), darkStone);
                }
            }

            Kit.Box(_range, "BackWall", new Vector3(0f, 2.5f, 32.4f), new Vector3(11f, 5f, 0.8f), darkStone);
            Kit.Box(_range, "FrontWall", new Vector3(0f, 2.5f, -4.4f), new Vector3(11f, 5f, 0.8f), darkStone);
            NoShadow(Kit.Box(_range, "Ceiling", new Vector3(0f, 5.2f, 14f), new Vector3(11f, 0.4f, 36f), new Color(0.27f, 0.18f, 0.11f)));
            for (int i = 0; i < 9; i++)
            {
                NoShadow(Kit.Box(_range, "Beam" + i, new Vector3(0f, 4.8f, i * 4f), new Vector3(10f, 0.35f, 0.35f), timber));
            }

            // 벽에 걸린 횃불: 불빛 상자와 따뜻한 점광원
            for (int i = 0; i < 4; i++)
            {
                float z = 5f + i * 8f;
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 position = new Vector3(side * 4.9f, 2.9f, z);
                    Kit.Box(_range, "Bracket" + side + i, position + new Vector3(0f, -0.3f, 0f), new Vector3(0.14f, 0.6f, 0.14f), timber);
                    GameObject flame = Kit.Box(_range, "Flame" + side + i, position + new Vector3(side * -0.08f, 0.2f, 0f), new Vector3(0.24f, 0.4f, 0.24f), new Color(1f, 0.6f, 0.2f));
                    Kit.Paint(flame.GetComponent<Renderer>(), new Color(1f, 0.6f, 0.2f), new Color(2.2f, 0.9f, 0.2f));
                    NoShadow(flame);
                    if (side < 0)
                    {
                        AddLight(position + new Vector3(1.2f, 0.2f, 0f), new Color(1f, 0.65f, 0.35f), 12f, 2f);
                    }
                }
            }

            // 안쪽 벽의 용광로: 타오르는 입구
            Kit.Box(_range, "Furnace", new Vector3(0f, 1.6f, 31.6f), new Vector3(5f, 3.2f, 1f), new Color(0.22f, 0.2f, 0.2f));
            GameObject mouth = Kit.Box(_range, "FurnaceMouth", new Vector3(0f, 1f, 31f), new Vector3(2.2f, 1.3f, 0.2f), new Color(1f, 0.5f, 0.15f));
            Kit.Paint(mouth.GetComponent<Renderer>(), new Color(1f, 0.5f, 0.15f), new Color(2.4f, 0.9f, 0.15f));
            NoShadow(mouth);
            AddLight(new Vector3(0f, 2f, 28f), new Color(1f, 0.55f, 0.25f), 16f, 2f);

            // 벽가 소품: 모루와 통
            for (int i = 0; i < 4; i++)
            {
                float z = 7f + i * 7f;
                float side = i % 2 == 0 ? -1f : 1f;
                Kit.Box(_range, "AnvilBase" + i, new Vector3(side * 4.2f, 0.3f, z), new Vector3(0.7f, 0.6f, 0.7f), new Color(0.35f, 0.24f, 0.14f));
                Kit.Box(_range, "Anvil" + i, new Vector3(side * 4.2f, 0.78f, z), new Vector3(0.85f, 0.36f, 0.4f), new Color(0.22f, 0.22f, 0.26f));
                Kit.Cylinder(_range, "Barrel" + i, new Vector3(-side * 4.3f, 0.45f, z + 2f), new Vector3(0.7f, 0.45f, 0.7f), new Color(0.4f, 0.26f, 0.15f));
            }
        }

        private static void NoShadow(GameObject gameObject)
        {
            gameObject.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void AddLight(Vector3 position, Color color, float range, float intensity)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(_range, false);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        // 과녁: 사람 키만 한 볏짚 허수아비. 볏짚 가슴(맞는 구역), 자루 머리(치명타 구역)와 모자, 가로대 팔, 말뚝, 옆의 깃발. 크기와 자리는 매 프레임 게임 값으로 정한다.
        private void BuildTarget()
        {
            var target = new GameObject("Target");
            target.transform.SetParent(_range, false);
            _target = target.transform;

            _boss = Kit.Box(_target, "Torso", Vector3.zero, Vector3.one, Straw).transform;

            _head = Kit.Cylinder(_target, "Head", Vector3.zero, Vector3.one, Burlap).transform;
            _brim = Kit.Cylinder(_target, "Brim", Vector3.zero, Vector3.one, new Color(0.35f, 0.22f, 0.12f)).transform;
            _crown = Kit.Cylinder(_target, "Crown", Vector3.zero, Vector3.one, new Color(0.4f, 0.26f, 0.15f)).transform;
            _bottomStraw = Kit.Box(_target, "BottomStraw", Vector3.zero, Vector3.one, Straw).transform;
            for (int i = 0; i < _eyes.Length; i++)
            {
                _eyes[i] = Kit.Box(_target, "Eye" + i, Vector3.zero, Vector3.one, new Color(0.12f, 0.08f, 0.05f)).transform;
                _hands[i] = Kit.Box(_target, "Hand" + i, Vector3.zero, Vector3.one, Straw).transform;
            }

            _legs[0] = Kit.Box(_range, "Pole", Vector3.zero, Vector3.one, new Color(0.4f, 0.27f, 0.15f)).transform;
            _legs[1] = Kit.Box(_range, "Arms", Vector3.zero, Vector3.one, new Color(0.4f, 0.27f, 0.15f)).transform;

            _flagPole = Kit.Box(_range, "FlagPole", Vector3.zero, Vector3.one, new Color(0.3f, 0.2f, 0.12f)).transform;
            _flag = Kit.Box(_range, "Flag", Vector3.zero, Vector3.one, new Color(0.9f, 0.3f, 0.25f)).transform;
        }

        // 일인칭 활: 원점이 손잡이다. 위·아래 활채는 당길수록 뒤로 휘고, 시위는 활 끝에서 화살 끝으로 이어진다.
        private void BuildBow()
        {
            var root = new GameObject("Bow");
            root.transform.SetParent(_range, false);
            _bowRoot = root.transform;
            Kit.Box(_bowRoot, "Grip", Vector3.zero, new Vector3(0.05f, 0.14f, 0.05f), new Color(0.3f, 0.2f, 0.12f));
            _upperLimb = Kit.Box(_bowRoot, "Upper", Vector3.zero, new Vector3(0.022f, 0.46f, 0.022f), new Color(0.5f, 0.52f, 0.58f)).transform;
            _lowerLimb = Kit.Box(_bowRoot, "Lower", Vector3.zero, new Vector3(0.022f, 0.46f, 0.022f), new Color(0.5f, 0.52f, 0.58f)).transform;
            _stringUpper = Kit.Box(_bowRoot, "StringUpper", Vector3.zero, Vector3.one, new Color(0.9f, 0.88f, 0.8f)).transform;
            _stringLower = Kit.Box(_bowRoot, "StringLower", Vector3.zero, Vector3.one, new Color(0.9f, 0.88f, 0.8f)).transform;
            _nocked = BuildArrow(_bowRoot, "NockedArrow");
            _handPull = Kit.Box(_bowRoot, "HandPull", Vector3.zero, new Vector3(0.1f, 0.09f, 0.12f), new Color(0.85f, 0.65f, 0.5f)).transform;
            Kit.Box(_bowRoot, "HandGrip", new Vector3(0f, -0.02f, -0.03f), new Vector3(0.11f, 0.1f, 0.12f), new Color(0.85f, 0.65f, 0.5f));
        }

        // 화살: 원점이 화살 끝(오늬)이고 앞(+z)으로 뻗는다. 깃과 촉이 있다.
        private Transform BuildArrow(Transform parent, string name)
        {
            var arrow = new GameObject(name).transform;
            arrow.SetParent(parent, false);
            Kit.Box(arrow, "Shaft", new Vector3(0f, 0f, ArrowLength * 0.5f), new Vector3(0.04f, 0.04f, ArrowLength), new Color(0.78f, 0.62f, 0.38f));
            Kit.Box(arrow, "Head", new Vector3(0f, 0f, ArrowLength + 0.03f), new Vector3(0.07f, 0.07f, 0.12f), new Color(0.6f, 0.62f, 0.68f));
            for (int side = -1; side <= 1; side += 2)
            {
                Kit.Box(arrow, "FletchA" + side, new Vector3(side * 0.05f, 0f, 0.1f), new Vector3(0.09f, 0.012f, 0.2f), new Color(0.9f, 0.3f, 0.25f));
                Kit.Box(arrow, "FletchB" + side, new Vector3(0f, side * 0.05f, 0.1f), new Vector3(0.012f, 0.09f, 0.2f), new Color(0.9f, 0.3f, 0.25f));
            }

            return arrow;
        }

        protected override void FocusCamera()
        {
            if (CameraFocus == null)
            {
                return;
            }

            CameraFocus.Focus(Eye, Eye + Vector3.forward * 10f);
        }

        public override void Begin(ForgeCameraFocus camera)
        {
            base.Begin(camera);
            ClearArrows();
            _arrowIndex = -1;
            _shotsSeen = 0;
            _isFlying = false;
        }

        public override void End()
        {
            ClearArrows();
            base.End();
        }

        private void ClearArrows()
        {
            foreach (Transform arrow in _stuck)
            {
                if (arrow != null)
                {
                    Destroy(arrow.gameObject);
                }
            }

            _stuck.Clear();
            if (_flying != null)
            {
                Destroy(_flying.gameObject);
                _flying = null;
            }

            _isFlying = false;
        }

        // 매 프레임 게임 값을 3D 장면에 옮긴다. 치명타로 흔들리는 머리의 시간도 여기서 흐른다. 그림판의 화면 좌표를 3D 방향으로 바꾸려고 board를 받는다.
        public void Show(BowRangeGame game, MiniBoardView board, float deltaTime)
        {
            if (CameraFocus == null || Camera.main == null)
            {
                return;
            }

            _flightTime = Mathf.Max(0.1f, game.FlightTime);
            if (_headKick >= 0f)
            {
                _headKick += deltaTime;
                if (_headKick > 2f)
                {
                    _headKick = -1f;
                }
            }

            float focal = Screen.height * 0.5f / Mathf.Tan(CameraFocus.FocusFov * 0.5f * Mathf.Deg2Rad);
            float pixelsPerCell = (board.CellToScreen(1f, 0f) - board.CellToScreen(0f, 0f)).magnitude;
            if (game.TargetNumber != _arrowIndex)
            {
                _arrowIndex = game.TargetNumber;
                ClearArrows();
            }

            _distance = game.Distance;
            UpdateTarget(game, board, focal, pixelsPerCell);
            UpdateBow(game, board, focal);
            UpdateFlight(game, deltaTime, focal, pixelsPerCell);
        }

        // 화면 좌표를 눈에서 깊이 1만큼 떨어진 방향으로 바꾼다(카메라는 +z를 곧장 바라본다).
        private static Vector3 Direction(Vector2 screen, float focal)
        {
            return new Vector3((screen.x - Screen.width * 0.5f) / focal, (screen.y - Screen.height * 0.5f) / focal, 1f);
        }

        private void UpdateTarget(BowRangeGame game, MiniBoardView board, float focal, float pixelsPerCell)
        {
            float depth = game.Distance;
            Vector3 center = Eye + Direction(board.CellToScreen(game.TargetX, game.TargetY), focal) * depth;
            float radius = game.TargetRadius * pixelsPerCell / focal * depth;
            _target.position = center;

            // 볏짚 가슴: 맞는 구역(반지름 radius)을 덮는 상자
            _boss.localPosition = new Vector3(0f, 0f, radius * 0.15f);
            _boss.localScale = new Vector3(radius * 1.55f, radius * 2.0f, radius * 0.6f);

            // 말뚝: 땅에서 가슴 뒤까지. 가로대 팔: 가슴 뒤 위쪽에 가로로 걸친다.
            float ground = _range.position.y;
            float bottom = center.y - radius * 1.1f;
            float height = Mathf.Max(0.1f, center.y - ground + radius * 0.4f);
            _legs[0].position = new Vector3(center.x, ground + height * 0.5f, center.z + radius * 0.35f);
            _legs[0].localScale = new Vector3(radius * 0.22f, height, radius * 0.22f);
            _legs[1].position = new Vector3(center.x, center.y + radius * 0.6f, center.z + radius * 0.45f);
            _legs[1].localScale = new Vector3(radius * 3.2f, radius * 0.14f, radius * 0.14f);
            for (int i = 0; i < _hands.Length; i++)
            {
                float side = i == 0 ? -1f : 1f;
                _hands[i].localPosition = new Vector3(side * radius * 1.65f, radius * 0.45f, radius * 0.45f);
                _hands[i].localScale = new Vector3(radius * 0.4f, radius * 0.7f, radius * 0.25f);
                _eyes[i].localScale = new Vector3(radius * 0.2f, radius * 0.09f, radius * 0.05f);
            }

            // 머리: 가슴 가운데에서 1.55 반지름 위. 치명타를 맞으면 한동안 흔들리며 들썩인다.
            float kick = _headKick >= 0f ? Mathf.Exp(-_headKick * 3.2f) : 0f;
            float wobble = kick * Mathf.Sin(_headKick * 34f);
            Vector3 headOffset = new Vector3(wobble * radius * 0.25f, kick * radius * 0.2f, 0f);
            Quaternion headTilt = Quaternion.Euler(0f, 0f, wobble * 28f);
            _head.localPosition = new Vector3(0f, radius * BowRangeGame.HeadOffset, radius * 0.2f) + headOffset;
            _head.localRotation = headTilt;
            _head.localScale = new Vector3(radius * BowRangeGame.HeadRatio * 2f, radius * BowRangeGame.HeadRatio, radius * BowRangeGame.HeadRatio * 2f);
            _brim.localPosition = new Vector3(0f, radius * (BowRangeGame.HeadOffset + BowRangeGame.HeadRatio), radius * 0.2f) + headOffset;
            _brim.localRotation = headTilt;
            _brim.localScale = new Vector3(radius * 1.4f, radius * 0.04f, radius * 1.4f);
            _crown.localPosition = new Vector3(0f, radius * (BowRangeGame.HeadOffset + 0.65f), radius * 0.2f) + headOffset;
            _crown.localRotation = headTilt;
            _crown.localScale = new Vector3(radius * 0.75f, radius * 0.25f, radius * 0.75f);
            for (int i = 0; i < _eyes.Length; i++)
            {
                float side = i == 0 ? -1f : 1f;
                _eyes[i].localPosition = new Vector3(side * radius * 0.17f, radius * (BowRangeGame.HeadOffset + 0.04f), -radius * 0.25f) + headOffset;
                _eyes[i].localRotation = headTilt;
                _eyes[i].localScale = new Vector3(radius * 0.15f, radius * 0.07f, radius * 0.05f);
            }

            _bottomStraw.localPosition = new Vector3(0f, -radius * 1.1f, radius * 0.1f);
            _bottomStraw.localScale = new Vector3(radius * 1.4f, radius * 0.35f, radius * 0.5f);

            // 깃발: 과녁 위 장대 끝에서 바람 방향으로 나부낀다. 바람이 셀수록 길고 팽팽하다.
            float poleHeight = radius * 0.9f;
            Vector3 poleBase = new Vector3(center.x + radius * 1.35f, center.y + radius * 0.4f, center.z + radius * 0.25f);
            _flagPole.position = poleBase + Vector3.up * poleHeight * 0.5f;
            _flagPole.localScale = new Vector3(radius * 0.06f, poleHeight, radius * 0.06f);
            float strength = Mathf.Clamp(game.Wind, -3f, 3f);
            float length = radius * (0.35f + 0.45f * Mathf.Abs(strength));
            float flutter = 1f + 0.12f * Mathf.Sin(Time.time * (6f + Mathf.Abs(strength) * 4f));
            _flag.position = poleBase + Vector3.up * poleHeight * 0.9f + Vector3.right * (Mathf.Sign(strength == 0f ? 1f : strength) * length * 0.5f);
            _flag.localScale = new Vector3(length * flutter, radius * 0.22f, radius * 0.02f);
            _flag.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 9f) * 4f * Mathf.Abs(strength));
        }

        // 활은 눈 앞 아래쪽에 있고 조준점을 향한다. 당길수록 활채가 뒤로 휘고 화살이 물러난다.
        private void UpdateBow(BowRangeGame game, MiniBoardView board, float focal)
        {
            Transform cameraTransform = Camera.main.transform;
            bool aiming = game.Phase == BowRangeGame.PhaseKind.Aim || game.Phase == BowRangeGame.PhaseKind.Draw;
            _bowRoot.gameObject.SetActive(game.Phase != BowRangeGame.PhaseKind.Intro && game.Phase != BowRangeGame.PhaseKind.Done);

            Vector3 direction = Vector3.forward;
            if (game.PointerX > -50f)
            {
                float swayX = game.Phase == BowRangeGame.PhaseKind.Draw ? game.SwayX : 0f;
                float swayY = game.Phase == BowRangeGame.PhaseKind.Draw ? game.SwayY : 0f;
                direction = Direction(board.CellToScreen(game.PointerX + swayX, game.PointerY + swayY), focal).normalized;
            }

            _bowRoot.position = cameraTransform.TransformPoint(new Vector3(-0.3f, -0.42f, 0.95f));
            _bowRoot.rotation = Quaternion.LookRotation(direction, Vector3.up);

            float power = game.Phase == BowRangeGame.PhaseKind.Draw ? game.Power : 0f;
            float angle = 10f + 22f * power;
            float limb = 0.46f;
            Vector3 upperTip = new Vector3(0f, Mathf.Cos(angle * Mathf.Deg2Rad), -Mathf.Sin(angle * Mathf.Deg2Rad)) * limb;
            Vector3 lowerTip = new Vector3(0f, -upperTip.y, upperTip.z);
            PlaceBox(_upperLimb, Vector3.zero, upperTip, 0.022f);
            PlaceBox(_lowerLimb, Vector3.zero, lowerTip, 0.022f);

            float pull = 0.06f + 0.34f * power;
            Vector3 nock = new Vector3(0f, 0f, upperTip.z - pull);
            PlaceBox(_stringUpper, upperTip, nock, 0.008f);
            PlaceBox(_stringLower, lowerTip, nock, 0.008f);
            _nocked.gameObject.SetActive(aiming);
            _nocked.localPosition = nock;
            _nocked.localRotation = Quaternion.identity;
            if (aiming)
            {
                _tipWorld = _bowRoot.TransformPoint(nock + new Vector3(0f, 0f, ArrowLength));
            }

            _handPull.gameObject.SetActive(aiming && power > 0f);
            _handPull.localPosition = nock + new Vector3(0f, -0.01f, -0.05f);
        }

        // 두 점 사이에 막대(상자)를 늘여 놓는다. 원점이 한쪽 끝이다.
        private static void PlaceBox(Transform box, Vector3 from, Vector3 to, float thickness)
        {
            Vector3 delta = to - from;
            box.localPosition = (from + to) * 0.5f;
            box.localRotation = delta.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(delta, Vector3.right) : Quaternion.identity;
            box.localScale = new Vector3(thickness, thickness, delta.magnitude);
        }

        // 쏜 직후 날아가는 화살을 만들고, 날아가다가 과녁에 닿으면 박아 둔다.
        private void UpdateFlight(BowRangeGame game, float deltaTime, float focal, float pixelsPerCell)
        {
            if (game.Shots > _shotsSeen)
            {
                _shotsSeen = game.Shots;
                StartFlight(game, focal, pixelsPerCell);
            }

            if (!_isFlying)
            {
                return;
            }

            _flyClock += deltaTime;
            float t = Mathf.Clamp01(_flyClock / _flightTime);
            Vector3 destination = _target.TransformPoint(_flyOffset);
            Vector3 straight = (destination - _flyFrom).normalized;
            Vector3 end = destination + straight * (ArrowLength * 0.3f);
            float arc = _distance * 0.012f;
            Vector3 tip = Vector3.Lerp(_flyFrom, end, t) + Vector3.up * (4f * arc * t * (1f - t));
            Vector3 velocity = (end - _flyFrom) + Vector3.up * (4f * arc * (1f - 2f * t));
            Quaternion rotation = Quaternion.LookRotation(velocity.sqrMagnitude > 0.0001f ? velocity : Vector3.forward, Vector3.up);
            _flying.rotation = rotation;
            _flying.position = tip - rotation * Vector3.forward * ArrowLength;

            if (t >= 1f)
            {
                _isFlying = false;
                _flying.SetParent(_target, true);
                _stuck.Add(_flying);
                if (game.LastHead)
                {
                    _headKick = 0.0001f;
                    Burst(30);
                }
                _flying = null;
            }
        }

        private void StartFlight(BowRangeGame game, float focal, float pixelsPerCell)
        {
            if (_flying != null)
            {
                Destroy(_flying.gameObject);
            }

            _flying = BuildArrow(_range, "Arrow" + _shotsSeen);
            _flyFrom = _tipWorld;
            _flyClock = 0f;
            _isFlying = true;

            // 꽂힌 자리는 쏜 순간의 과녁 가운데에서 얼마나 벗어났는지(칸)를 과녁 위 길이로 바꾼 것이다.
            float depth = game.Distance;
            float scale = pixelsPerCell / focal * depth;
            float radiusWorld = game.TargetRadius * scale;
            _flyOffset = new Vector3((game.ImpactX - game.TargetX) * scale, -(game.ImpactY - game.TargetY) * scale, game.LastHead ? -radiusWorld * 0.3f : -radiusWorld * 0.15f);
            _flying.rotation = _bowRoot.rotation;
            _flying.position = _flyFrom - _bowRoot.forward * ArrowLength;
        }
    }
}
