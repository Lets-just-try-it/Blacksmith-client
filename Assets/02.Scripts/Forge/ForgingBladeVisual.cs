using Blacksmith.Core.Forge;
using UnityEngine;

namespace Blacksmith.Forge
{
    // 단조 연출. 부위로 나눈 칼날이 두드릴수록 얇아지고, 망치가 호를 그리며 내려친다.
    // 작은 박자는 가볍게 내려치고, 큰 박자는 누르는 동안 망치가 머리 위 뒤쪽까지 젖혀지며 달아오른다.
    // 값은 모두 ForgingGame 상태에서 읽고, 박자 판정 사건은 StartSwing과 Impact로 받는다.
    public class ForgingBladeVisual : MonoBehaviour
    {
        // 망치 각도(도). 0이면 머리가 바로 아래, 음수면 왼쪽이다.
        private const float AngleHit = -55f;
        private const float AngleRest = -80f;
        private const float AngleLight = -125f;
        private const float AngleHeavy = -215f;
        private const float AngleHeavyReady = -108f;
        private const float SlamTime = 0.10f;
        private const float RecoverTime = 0.28f;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] private GameObject _root;
        [SerializeField] private Transform[] _segments;
        [SerializeField] private Renderer[] _segmentRenderers;
        [SerializeField] private Transform[] _goalMarks;
        [SerializeField] private Transform _cursorMark;
        [SerializeField] private Transform _hammerPivot;
        [SerializeField] private Transform _hammerHead;
        [SerializeField] private Renderer _hammerHeadRenderer;
        [SerializeField, Min(0.01f)] private float _segmentWidth = 0.14f;
        [SerializeField, Min(0.01f)] private float _segmentDepth = 0.2f;
        [SerializeField, Min(0.01f)] private float _heightScale = 0.154f;
        [SerializeField, Min(0.01f)] private float _hammerLength = 0.53f;
        [SerializeField] private Color _coolColor = new Color(0.4f, 0.42f, 0.46f);
        [SerializeField] private Color _hotColor = new Color(1f, 0.45f, 0.1f);
        [SerializeField] private Color _headColor = new Color(0.25f, 0.25f, 0.28f);

        private MaterialPropertyBlock _block;
        private ForgingGame _game;
        private float[] _shown;
        private float[] _shownHoldUntil;
        private float[] _squash;
        private float[] _flash;
        private float[] _heat;
        private float _hammerX;
        private float _angle = AngleRest;
        private float _slamTime = -1f;
        private float _slamFrom;
        private float _power;
        private bool _impactPending;
        private float _impactTimer;
        private int _impactSegment;
        private float _impactQuality;
        private float _impactStrength;
        private float _jolt;
        private Vector3 _rootBase;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _rootBase = _root.transform.localPosition;
            int count = _segments.Length;
            _shown = new float[count];
            _shownHoldUntil = new float[count];
            _squash = new float[count];
            _flash = new float[count];
            _heat = new float[count];
        }

        // 콤보가 쌓일수록 오르는 기세(0~1)
        public float Power => _power;

        // 부위 위쪽 한가운데의 월드 위치. 불꽃과 충격파가 터지는 자리다.
        public Vector3 ImpactPoint(int segment)
        {
            return _root.transform.TransformPoint(new Vector3(SegmentX(segment), 0.02f, 0f));
        }

        public void SetVisible(bool visible)
        {
            _root.SetActive(visible);
        }

        public void Bind(ForgingGame game)
        {
            _game = game;
            _power = 0f;
            _impactPending = false;
            _slamTime = -1f;
            _angle = AngleRest;
            for (int i = 0; i < _segments.Length; i++)
            {
                _shown[i] = 1f;
                _shownHoldUntil[i] = 0f;
                _squash[i] = 0f;
                _flash[i] = 0f;
                _heat[i] = 1f;
                _goalMarks[i].localPosition = new Vector3(SegmentX(i), game.Target[i] * _heightScale, -_segmentDepth * 0.5f - 0.01f);
            }
            _hammerX = SegmentX(game.Cursor);
            PlaceHammer(_hammerX, ThicknessTop(game.Cursor), _angle);
        }

        // 눌렀거나 뗐으면 맞든 안 맞든 망치는 내려간다.
        public void StartSwing()
        {
            if (_slamTime >= 0f && _slamTime < SlamTime * 0.5f)
            {
                return;
            }

            _slamTime = 0f;
            _slamFrom = _angle;
        }

        // 박자를 쳤을 때. 망치가 실제로 닿는 순간에 칼날이 찌그러지게 잠깐 미룬다.
        public void Impact(int segment, float quality, float strength)
        {
            StartSwing();
            _impactStrength = strength;
            _impactPending = true;
            _impactTimer = SlamTime;
            _impactSegment = Mathf.Clamp(segment, 0, _segments.Length - 1);
            _impactQuality = quality;
            _shownHoldUntil[_impactSegment] = Time.time + SlamTime + 0.02f;
        }

        // 놓친 박자. 내려치는 중이던 동작은 멈춘다.
        public void CancelSwing()
        {
            _slamTime = -1f;
        }

        public void Tick(float deltaTime)
        {
            if (_game == null)
            {
                return;
            }

            float wantPower = Mathf.Clamp01(_game.Combo / 10f);
            _power = Mathf.MoveTowards(_power, wantPower, deltaTime * (wantPower > _power ? 1.2f : 2.5f));

            if (_impactPending)
            {
                _impactTimer -= deltaTime;
                if (_impactTimer <= 0f)
                {
                    _impactPending = false;
                    DoImpact();
                }
            }

            UpdateSegments(deltaTime);

            // 작업대가 눌렸다 올라온다.
            _jolt = Mathf.MoveTowards(_jolt, 0f, deltaTime * 6f);
            _root.transform.localPosition = _rootBase + Vector3.down * (0.045f * _jolt * _jolt);

            int cursor = _game.Cursor;
            float targetX = SegmentX(cursor);
            _hammerX = Mathf.Lerp(_hammerX, targetX, 1f - Mathf.Exp(-12f * deltaTime));
            _cursorMark.localPosition = new Vector3(targetX, ThicknessTop(cursor) + 0.1f + 0.012f * Mathf.Sin(Time.time * 6f), 0f);
            _cursorMark.gameObject.SetActive(!_game.Finished && _game.Holding == null);

            UpdateHammer(deltaTime, cursor);
        }

        private float SegmentX(int index)
        {
            return (index - (_segments.Length - 1) * 0.5f) * _segmentWidth;
        }

        private float ThicknessTop(int index)
        {
            return Mathf.Max(0.05f, _shown[index]) * _heightScale;
        }

        private void UpdateSegments(float deltaTime)
        {
            for (int i = 0; i < _segments.Length; i++)
            {
                if (Time.time >= _shownHoldUntil[i])
                {
                    _shown[i] += (_game.Thickness[i] - _shown[i]) * (1f - Mathf.Exp(-16f * deltaTime));
                }
                _squash[i] = Mathf.MoveTowards(_squash[i], 0f, deltaTime * 5f);
                _flash[i] = Mathf.MoveTowards(_flash[i], 0f, deltaTime * 3.5f);

                bool done = _game.Progress[i] >= _game.Need[i] - 0.01f;
                _heat[i] = Mathf.MoveTowards(_heat[i], done ? 0.3f : 1f, deltaTime * (done ? 0.3f : 1f));

                float squash = _squash[i];
                float thickness = ThicknessTop(i) * (1f - 0.32f * squash);
                _segments[i].localScale = new Vector3(_segmentWidth * 0.96f * (1f + 0.22f * squash), thickness, _segmentDepth * (1f + 0.09f * squash));
                _segments[i].localPosition = new Vector3(SegmentX(i), thickness * 0.5f, 0f);
                Paint(_segmentRenderers[i], Color.Lerp(_coolColor, _hotColor, Mathf.Clamp01(_heat[i] + _flash[i] * 0.45f)));
            }
        }

        private void UpdateHammer(float deltaTime, int cursor)
        {
            ForgingGame.Beat next = _game.NextBeat;
            float want = AngleRest;
            float tremble = 0f;
            bool charging = _game.Holding != null;

            if (charging)
            {
                // 꾹 누르는 동안: 망치가 머리 위 뒤쪽까지 젖혀진다(처음엔 빠르게, 끝에서 천천히).
                float progress = _game.HoldProgress;
                float eased = 1f - (1f - progress) * (1f - progress);
                want = Mathf.Lerp(AngleRest, AngleHeavy, eased);
                tremble = progress * progress * 2.2f;
            }
            else if (next != null)
            {
                if (next.IsHeavy && !next.Started)
                {
                    // 눌러야 할 때가 다가오면 살짝 들어 올려 "곧 누른다"를 알려 준다.
                    float a = 1f - Mathf.Clamp01((next.PressTime - _game.Clock) / 0.5f);
                    want = Mathf.Lerp(AngleRest, AngleHeavyReady, a * a * (3f - 2f * a));
                }
                else if (!next.IsHeavy)
                {
                    float a = 1f - Mathf.Clamp01((next.Time - _game.Clock) / 0.45f);
                    want = Mathf.Lerp(AngleRest, AngleLight, a * a * (3f - 2f * a));
                }
            }

            _angle = SlamAngle(deltaTime, want, Mathf.Lerp(_angle, want, 1f - Mathf.Exp(-(charging ? 18f : 14f) * deltaTime)));

            float shake = tremble > 0f ? (Mathf.PerlinNoise(Time.time * 40f, 1f) - 0.5f) * 2f * tremble : 0f;
            PlaceHammer(_hammerX, ThicknessTop(cursor), _angle + shake);

            // 힘을 모을수록 머리가 달아오르고, 콤보가 쌓이면 평소에도 주황빛이 돈다.
            float charge = charging ? _game.HoldProgress : 0f;
            float glow = Mathf.Clamp01(Mathf.Max(_power * 0.7f, charge));
            Paint(_hammerHeadRenderer, Color.Lerp(_headColor, _hotColor, glow));
        }

        // 내려치는 동안은 박자에 따른 각도 대신 내려치기와 튕겨 오르는 움직임을 쓴다.
        private float SlamAngle(float deltaTime, float want, float free)
        {
            if (_slamTime < 0f)
            {
                return free;
            }

            _slamTime += deltaTime;
            if (_slamTime < SlamTime)
            {
                // 갈수록 빨라진다.
                float k = _slamTime / SlamTime;
                return Mathf.Lerp(_slamFrom, AngleHit, k * k * k);
            }

            if (_slamTime < SlamTime + RecoverTime)
            {
                float k = (_slamTime - SlamTime) / RecoverTime;
                return Mathf.Lerp(AngleHit, want, 1f - (1f - k) * (1f - k));
            }

            _slamTime = -1f;
            return want;
        }

        // 내려친 순간(AngleHit) 머리가 (strikeX, strikeY)에 오도록 축을 정하고, 각도 angle로 망치를 놓는다.
        private void PlaceHammer(float strikeX, float strikeY, float angle)
        {
            float hit = AngleHit * Mathf.Deg2Rad;
            _hammerPivot.localPosition = new Vector3(strikeX - _hammerLength * Mathf.Sin(hit), strikeY + _hammerLength * Mathf.Cos(hit), 0f);
            _hammerPivot.localRotation = Quaternion.Euler(0f, 0f, angle);
            // 머리의 평평한 면이 내려치는 순간 바닥과 나란하도록, 손잡이가 기운 만큼 머리를 반대로 돌린다.
            _hammerHead.localRotation = Quaternion.Euler(0f, 0f, -angle * 0.85f);
        }

        private void DoImpact()
        {
            int segment = _impactSegment;
            _squash[segment] = 1f;
            _flash[segment] = Mathf.Lerp(0.4f, 1f, _impactQuality);
            _jolt = Mathf.Max(_jolt, Mathf.Clamp01(0.35f + 0.5f * _impactStrength));
            if (segment > 0)
            {
                _squash[segment - 1] = Mathf.Max(_squash[segment - 1], 0.45f);
            }
            if (segment < _segments.Length - 1)
            {
                _squash[segment + 1] = Mathf.Max(_squash[segment + 1], 0.45f);
            }
        }

        private void Paint(Renderer target, Color color)
        {
            target.GetPropertyBlock(_block);
            _block.SetColor(BaseColor, color);
            target.SetPropertyBlock(_block);
        }
    }
}
