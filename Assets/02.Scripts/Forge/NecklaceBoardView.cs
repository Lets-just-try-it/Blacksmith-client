using System.Collections.Generic;
using Blacksmith.Core.Forge;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Forge
{
    // 장신구 제작의 작업판 화면. 단계마다 체인 판과 조각 상자, 숫돌과 보석, 펜던트 틀, 광내기 화면을 그리고,
    // 마우스를 체인 단계의 대상(조각·칸·상자)이나 보석 가운데 기준 칸 좌표로 바꾼다.
    // 도형은 뒤 캔버스(작업판)와 앞 캔버스(표시)에 PixelCanvas로 그리고, 보석은 모양이 매 프레임 바뀌므로 작은 텍스처로 만들어
    // 그 사이에 둔다(깎은 모양과 면의 윤을 그림으로 만든다). 규칙은 건드리지 않고 NecklaceGame의 값만 읽는다.
    // 글자는 NecklaceView가 맡는다.
    public class NecklaceBoardView : MonoBehaviour
    {
        public const int CanvasWidth = 1000;
        public const int CanvasHeight = 640;
        private const float TextureScale = 1.5f;

        // 체인 판 위의 1칸(px)과 조각 크기
        private const float Unit = 50f;
        private const float SegW = 52f;
        private const float SegH = 32f;
        private static readonly Vector2 BoardOrigin = new Vector2(170f, 12f);
        private static readonly Rect Crate = new Rect(180f, 384f, 640f, 146f);
        private static readonly Rect CrateInner = new Rect(194f, 396f, 612f, 106f);

        // 보석 화면의 1칸(px)과 보석 그림의 크기
        private const float GemUnit = 58f;
        private const int GemTexSize = 240;
        private const float Span = 1.2f;
        private static readonly Vector2 GemCenter = new Vector2(500f, 300f);
        private static readonly Color Gold = new Color(0.9f, 0.74f, 0.3f);
        private static readonly Color Frame = new Color(0.03f, 0.03f, 0.05f, 1f);

        private static readonly Color[] SymbolColors =
        {
            new Color(0.95f, 0.42f, 0.38f), new Color(0.4f, 0.68f, 1f), new Color(0.5f, 0.92f, 0.5f), new Color(0.78f, 0.5f, 0.95f),
        };

        private struct Spark
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Life;
            public Color Color;
        }

        [SerializeField] private RawImage _back;
        [Tooltip("보석 틀의 구멍을 그리는 그림. 보석 그림과 같은 텍스처를 쓴다")]
        [SerializeField] private RawImage _socket;
        [SerializeField] private RawImage _gem;
        [SerializeField] private RawImage _front;

        private readonly List<Spark> _sparks = new List<Spark>();
        private PixelCanvas _backCanvas;
        private PixelCanvas _frontCanvas;
        private RectTransform _rect;
        private NecklaceGame _game;
        private Vector2[][] _slotPositions;
        private float[][] _slotAngles;
        private Vector2 _pointer;
        private bool _hasPointer;
        private float _heldAngle;
        private float _pulse;
        private float _wheelAngle;
        private Color _gemColor = new Color(0.9f, 0.3f, 0.35f);

        // 보석 그림
        private Texture2D _gemTexture;
        private Color32[] _pixels;
        private int[] _pixelAngle;
        private float[] _pixelRho;
        private float[] _pixelNx;
        private float[] _pixelNy;
        private float[] _shade;
        private int[] _facetOfAngle;
        private float[] _litOfAngle;
        private int _facetCount;
        private float _gemKey = -1f;
        private int _gemMode = -1;
        private float _nextRebuild;

        private static float GemRadiusPx => NecklaceGame.GemR * GemUnit;

        private void Awake()
        {
            _rect = (RectTransform)_back.transform;
            _backCanvas = new PixelCanvas(CanvasWidth, CanvasHeight, TextureScale);
            _frontCanvas = new PixelCanvas(CanvasWidth, CanvasHeight, TextureScale);
            _back.texture = _backCanvas.Texture;
            _front.texture = _frontCanvas.Texture;
            BuildGemTexture();
            Clear();
        }

        private void OnDestroy()
        {
            if (_backCanvas != null)
            {
                Destroy(_backCanvas.Texture);
            }

            if (_frontCanvas != null)
            {
                Destroy(_frontCanvas.Texture);
            }

            if (_gemTexture != null)
            {
                Destroy(_gemTexture);
            }
        }

        public void Bind(NecklaceGame game, Color gemColor)
        {
            _game = game;
            _gemColor = gemColor;
            _slotPositions = null;
            _slotAngles = null;
            _hasPointer = false;
            _heldAngle = 0f;
            _gemKey = -1f;
            _gemMode = -1;
            _facetCount = 0;
            _sparks.Clear();
        }

        public void Unbind()
        {
            _game = null;
            Clear();
        }

        private void Clear()
        {
            if (_backCanvas == null)
            {
                return;
            }

            _backCanvas.Clear(Color.clear);
            _backCanvas.Apply();
            _frontCanvas.Clear(Color.clear);
            _frontCanvas.Apply();
            _gem.enabled = false;
            _socket.enabled = false;
        }

        // ------------------------------------------------------------------ 마우스

        // 화면의 마우스 위치를 게임 좌표로 바꾼다. 체인 단계는 가리킨 대상의 번호, 장착·광내기는 보석 가운데 기준 칸 좌표다.
        public bool TryMap(Vector2 screenPoint, out float x, out float y)
        {
            x = NecklaceGame.NoTarget;
            y = 0f;
            _hasPointer = false;
            if (_game == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screenPoint, null, out Vector2 local))
            {
                return false;
            }

            Vector2 offset = local - _rect.rect.center;
            _pointer = new Vector2(offset.x + CanvasWidth * 0.5f, CanvasHeight * 0.5f - offset.y);
            _hasPointer = _pointer.x >= 0f && _pointer.x <= CanvasWidth && _pointer.y >= 0f && _pointer.y <= CanvasHeight;
            if (!_hasPointer)
            {
                return false;
            }

            switch (_game.Phase)
            {
                case NecklaceGame.PhaseKind.Chain:
                    return MapChain(out x);
                case NecklaceGame.PhaseKind.Set:
                    x = (_pointer.x - GemCenter.x) / GemUnit;
                    y = (_pointer.y - GemCenter.y) / GemUnit;
                    return true;
                case NecklaceGame.PhaseKind.Polish:
                {
                    var area = new Rect(GemCenter.x - GemUnit * 3.3f, GemCenter.y - GemUnit * 3.3f, GemUnit * 6.6f, GemUnit * 6.6f);
                    if (!area.Contains(_pointer))
                    {
                        return false;
                    }

                    x = (_pointer.x - GemCenter.x) / GemUnit;
                    y = (_pointer.y - GemCenter.y) / GemUnit;
                    return true;
                }
            }

            return false;
        }

        private bool MapChain(out float code)
        {
            EnsureLayout();
            for (int r = 0; r < _game.Rows.Count; r++)
            {
                for (int j = 0; j < _game.Rows[r].Len; j++)
                {
                    if (InSlot(r, j, _pointer))
                    {
                        code = NecklaceGame.SlotCode(r, j);
                        return true;
                    }
                }
            }

            for (int i = _game.Segs.Count - 1; i >= 0; i--)
            {
                NecklaceGame.Seg seg = _game.Segs[i];
                if (!seg.Placed && _game.Held != seg.Id && InCratePiece(i, _pointer))
                {
                    code = NecklaceGame.PieceCode(seg.Id);
                    return true;
                }
            }

            if (_game.Held >= 0 && Crate.Contains(_pointer))
            {
                code = NecklaceGame.CrateCode;
                return true;
            }

            code = NecklaceGame.NoTarget;
            return false;
        }

        // ------------------------------------------------------------------ 체인 판 배치

        private void EnsureLayout()
        {
            if (_slotPositions != null || _game.Rows.Count == 0)
            {
                return;
            }

            _slotPositions = new Vector2[_game.Rows.Count][];
            _slotAngles = new float[_game.Rows.Count][];
            for (int r = 0; r < _game.Rows.Count; r++)
            {
                int length = _game.Rows[r].Len;
                _slotPositions[r] = new Vector2[length];
                _slotAngles[r] = new float[length];
                for (int j = 0; j < length; j++)
                {
                    NecklaceShape.Slot(_game.Level, r, j, length, out float x, out float y, out float angle);
                    _slotPositions[r][j] = new Vector2(x, y);
                    _slotAngles[r][j] = angle;
                }
            }
        }

        private static Vector2 BoardToCanvas(float x, float y)
        {
            return BoardOrigin + new Vector2(x * Unit, y * Unit);
        }

        private Vector2 SlotCanvas(int row, int slot)
        {
            return BoardToCanvas(_slotPositions[row][slot].x, _slotPositions[row][slot].y);
        }

        private bool InSlot(int row, int slot, Vector2 point)
        {
            return InRotatedBox(point, SlotCanvas(row, slot), _slotAngles[row][slot], SegW * 0.5f + 4f, SegH * 0.5f + 6f);
        }

        private static bool InRotatedBox(Vector2 point, Vector2 center, float angleDeg, float halfWidth, float halfHeight)
        {
            Vector2 d = point - center;
            float a = -angleDeg * Mathf.Deg2Rad;
            float lx = d.x * Mathf.Cos(a) - d.y * Mathf.Sin(a);
            float ly = d.x * Mathf.Sin(a) + d.y * Mathf.Cos(a);
            return Mathf.Abs(lx) <= halfWidth && Mathf.Abs(ly) <= halfHeight;
        }

        private static float Jitter(int i, int salt)
        {
            float v = Mathf.Sin(i * 12.9898f + salt * 78.233f) * 43758.5453f;
            return v - Mathf.Floor(v);
        }

        // 상자 안에서 i번째 조각이 놓인 자리와 기울기. 조각마다 정해진 자리라 꺼냈다 도로 넣으면 그 자리로 돌아온다.
        private Vector2 CratePosition(int i)
        {
            int columns = 8;
            int rows = Mathf.Max(2, (_game.Segs.Count + columns - 1) / columns);
            float cellW = CrateInner.width / columns;
            float cellH = CrateInner.height / rows;
            return new Vector2(
                CrateInner.x + (i % columns + 0.5f) * cellW + (Jitter(i, 1) - 0.5f) * 14f,
                CrateInner.y + (i / columns + 0.5f) * cellH + (Jitter(i, 2) - 0.5f) * 8f);
        }

        private static float CrateAngle(int i)
        {
            return (Jitter(i, 3) - 0.5f) * 50f;
        }

        private bool InCratePiece(int i, Vector2 point)
        {
            return InRotatedBox(point, CratePosition(i), CrateAngle(i), SegW * 0.5f, SegH * 0.5f);
        }

        // ------------------------------------------------------------------ 매 프레임

        public void Tick(float deltaTime)
        {
            if (_game == null)
            {
                return;
            }

            _pulse += deltaTime;
            bool grinding = _game.Phase == NecklaceGame.PhaseKind.Cut && _game.Grinding;
            _wheelAngle += deltaTime * (grinding ? (_game.Tool == 0 ? 900f : 500f) : 40f);
            TickSparks(deltaTime, grinding);

            // 뒤 방이 비쳐 보이지 않도록 작업판 전체를 어둡게 깐다.
            _backCanvas.Clear(new Color(0.03f, 0.03f, 0.05f, 0.92f));
            _frontCanvas.Clear(Color.clear);
            int step = DisplayStep();
            switch (step)
            {
                case 0:
                    HideGem();
                    DrawChain();
                    break;
                case 1:
                    DrawCut();
                    break;
                case 2:
                    DrawSet();
                    break;
                case 3:
                    DrawPolish();
                    break;
                default:
                    HideGem();
                    break;
            }

            foreach (Spark spark in _sparks)
            {
                _frontCanvas.Circle(spark.Position, 4f, new Color(spark.Color.r, spark.Color.g, spark.Color.b, Mathf.Clamp01(spark.Life * 3f)));
            }

            DrawCursor(step);
            _backCanvas.Apply();
            _frontCanvas.Apply();
        }

        // 지금 보여 줄 단계(0 체인, 1 깎기, 2 장착, 3 광내기). 시작 전에는 -1이다.
        private int DisplayStep()
        {
            switch (_game.Phase)
            {
                case NecklaceGame.PhaseKind.Intro:
                    return -1;
                case NecklaceGame.PhaseKind.Chain:
                    return 0;
                case NecklaceGame.PhaseKind.Cut:
                    return 1;
                case NecklaceGame.PhaseKind.Set:
                    return 2;
                case NecklaceGame.PhaseKind.Polish:
                case NecklaceGame.PhaseKind.Done:
                    return 3;
                default:
                    return _game.Step;
            }
        }

        private void TickSparks(float deltaTime, bool grinding)
        {
            for (int i = _sparks.Count - 1; i >= 0; i--)
            {
                Spark spark = _sparks[i];
                spark.Life -= deltaTime;
                spark.Position += spark.Velocity * deltaTime;
                spark.Velocity.y += 520f * deltaTime;
                if (spark.Life <= 0f)
                {
                    _sparks.RemoveAt(i);
                }
                else
                {
                    _sparks[i] = spark;
                }
            }

            // 갈 때 튀는 불꽃
            if (grinding)
            {
                Vector2 contact = new Vector2(GemCenter.x + Random.Range(-30f, 30f), GemCenter.y + _game.FacingExtent * GemRadiusPx);
                int count = _game.Tool == 0 ? 3 : 1;
                for (int i = 0; i < count; i++)
                {
                    _sparks.Add(new Spark
                    {
                        Position = contact,
                        Velocity = new Vector2(Random.Range(-160f, 160f), Random.Range(-200f, -40f)),
                        Life = 0.35f,
                        Color = _game.Tool == 0 ? new Color(1f, 0.8f, 0.4f) : new Color(0.9f, 0.95f, 1f),
                    });
                }
            }
        }

        public void Burst(Vector2 at, int count, Color color, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                _sparks.Add(new Spark { Position = at, Velocity = Random.insideUnitCircle * speed + Vector2.up * -80f, Life = 0.55f, Color = color });
            }
        }

        // 단계를 마쳤을 때 터뜨리는 곳: 체인은 판 가운데, 나머지는 보석 가운데
        public Vector2 BurstPoint(int step)
        {
            return step == 0 ? BoardToCanvas(NecklaceShape.BoardW * 0.5f, NecklaceShape.BoardH * 0.5f) : GemCenter;
        }

        public Vector2 FacetCenter(int i)
        {
            int phi = _game.TargetFacets[i].Phi;
            return OnProfile(_game.Profile, phi, _game.PolishRot, 0.6f);
        }

        // 캔버스 좌표를 이 화면 중심 기준 위치(위쪽이 +)로 바꾼다. 말풍선 위치에 쓴다.
        public static Vector2 ToLocal(Vector2 canvasPoint)
        {
            return new Vector2(canvasPoint.x - CanvasWidth * 0.5f, CanvasHeight * 0.5f - canvasPoint.y);
        }

        // ------------------------------------------------------------------ ① 체인

        private void DrawChain()
        {
            EnsureLayout();
            var board = new Rect(BoardOrigin.x, BoardOrigin.y, NecklaceShape.BoardW * Unit, NecklaceShape.BoardH * Unit);
            _backCanvas.Fill(new Rect(board.x - 6f, board.y - 6f, board.width + 12f, board.height + 12f), Frame);
            _backCanvas.Fill(board, new Color(0.1f, 0.09f, 0.12f, 0.95f));
            Outline(_backCanvas, board, new Color(0.85f, 0.6f, 0.25f, 0.6f), 2f);

            NecklaceShape.Strand[] strands = NecklaceShape.Of(_game.Level);
            for (int r = 0; r < strands.Length; r++)
            {
                DrawStrand(strands[r], r);
            }

            DrawPendants();
            if (_slotPositions == null)
            {
                return;
            }

            DrawSlots();
            DrawJoints(strands);
            DrawCrate();
        }

        private void DrawStrand(NecklaceShape.Strand strand, int row)
        {
            NecklaceShape.Point(strand, 0f, out float px, out float py);
            Vector2 previous = BoardToCanvas(px, py);
            for (int i = 1; i <= 24; i++)
            {
                NecklaceShape.Point(strand, i / 24f, out float x, out float y);
                Vector2 current = BoardToCanvas(x, y);
                _backCanvas.Line(previous, current, 4f, new Color(0.7f, 0.58f, 0.25f, 0.55f));
                previous = current;
            }

            if (_game.Rows.Count > row)
            {
                Vector2 a = BoardToCanvas(strand.X0, strand.Y0);
                Vector2 b = BoardToCanvas(strand.X2, strand.Y2);
                foreach (Vector2 end in new[] { a, b })
                {
                    _backCanvas.Circle(end, 15f, new Color(0.08f, 0.07f, 0.1f, 0.95f));
                    _backCanvas.Ring(end, 15f, Gold, 3f);
                }

                DrawSymbol(_backCanvas, a, _game.Rows[row].LeftSym, 12f);
                DrawSymbol(_backCanvas, b, _game.Rows[row].RightSym, 12f);
            }
        }

        private void DrawPendants()
        {
            float[][] gems = NecklaceShape.Gems[Mathf.Min(_game.Level, NecklaceShape.Gems.Length - 1)];
            for (int i = 0; i < gems.Length; i++)
            {
                Vector2 p = BoardToCanvas(gems[i][0], gems[i][1]);
                float radius = gems[i][2] * Unit * 0.62f;
                Color color = i == 0 ? _gemColor : Color.Lerp(_gemColor, Color.white, 0.25f);
                _backCanvas.Circle(p + new Vector2(2f, 4f), radius, new Color(0f, 0f, 0f, 0.4f));
                _backCanvas.Circle(p, radius, color);
                _backCanvas.Circle(p + new Vector2(-4f, -4f), gems[i][2] * Unit * 0.2f, new Color(1f, 1f, 1f, 0.55f));
                _backCanvas.Ring(p, radius, Gold, 2f);
            }
        }

        private void DrawSlots()
        {
            for (int r = 0; r < _game.Rows.Count; r++)
            {
                NecklaceGame.Row row = _game.Rows[r];
                for (int j = 0; j < row.Len; j++)
                {
                    Vector2 center = SlotCanvas(r, j);
                    int segId = row.Slots[j];
                    if (segId < 0)
                    {
                        Color outline = new Color(1f, 1f, 1f, _game.Held >= 0 ? 0.6f : 0.35f);
                        DrawPill(_backCanvas, center, _slotAngles[r][j], SegW, SegH + 4f, outline);
                        DrawPill(_backCanvas, center, _slotAngles[r][j], SegW - 3f, SegH, new Color(0.04f, 0.04f, 0.07f, 0.95f));
                    }
                    else
                    {
                        DrawSegment(_backCanvas, center, _slotAngles[r][j], _game.SegById(segId), 1f);
                    }
                }
            }
        }

        // 이음새: 맞물렸으면 초록, 양쪽이 놓였는데 안 맞으면 빨강
        private void DrawJoints(NecklaceShape.Strand[] strands)
        {
            for (int r = 0; r < _game.Rows.Count; r++)
            {
                NecklaceGame.Row row = _game.Rows[r];
                for (int j = 0; j <= row.Len; j++)
                {
                    Vector2 a = j == 0 ? BoardToCanvas(strands[r].X0, strands[r].Y0) : SlotCanvas(r, j - 1);
                    Vector2 b = j == row.Len ? BoardToCanvas(strands[r].X2, strands[r].Y2) : SlotCanvas(r, j);
                    Vector2 joint = (a + b) * 0.5f;
                    bool left = j == 0 || row.Slots[j - 1] >= 0;
                    bool right = j == row.Len || row.Slots[j] >= 0;
                    bool ok = _game.JointOk(r, j);
                    if (!ok && !(left && right))
                    {
                        continue;
                    }

                    _backCanvas.Circle(joint, 8f, new Color(0.05f, 0.05f, 0.08f, 0.95f));
                    _backCanvas.Circle(joint, 5.5f, ok ? new Color(0.5f, 1f, 0.6f) : new Color(1f, 0.45f, 0.4f));
                }
            }
        }

        private void DrawCrate()
        {
            Color wood = new Color(0.42f, 0.27f, 0.14f);
            Color woodDark = new Color(0.27f, 0.17f, 0.09f);
            Color woodLight = new Color(0.55f, 0.37f, 0.2f);
            _backCanvas.Fill(new Rect(Crate.x + 4f, Crate.y + 6f, Crate.width, Crate.height), new Color(0f, 0f, 0f, 0.4f));
            _backCanvas.Fill(Crate, wood);
            for (int i = 1; i < 4; i++)
            {
                _backCanvas.Fill(new Rect(Crate.x, Crate.y + Crate.height * i / 4f, Crate.width, 2f), woodDark);
            }

            _backCanvas.Fill(CrateInner, new Color(0.13f, 0.08f, 0.05f, 0.97f));
            Outline(_backCanvas, CrateInner, woodDark, 3f);

            int hover = -1;
            if (_hasPointer && _game.Held < 0)
            {
                for (int i = _game.Segs.Count - 1; i >= 0; i--)
                {
                    NecklaceGame.Seg seg = _game.Segs[i];
                    if (!seg.Placed && InCratePiece(i, _pointer))
                    {
                        hover = i;
                        break;
                    }
                }
            }

            for (int i = 0; i < _game.Segs.Count; i++)
            {
                NecklaceGame.Seg seg = _game.Segs[i];
                if (seg.Placed || _game.Held == seg.Id)
                {
                    continue;
                }

                Vector2 position = CratePosition(i) + (i == hover ? new Vector2(0f, -6f) : Vector2.zero);
                if (i == hover)
                {
                    DrawPill(_backCanvas, position, CrateAngle(i), SegW + 8f, SegH + 8f, new Color(1f, 1f, 1f, 0.85f));
                }

                DrawSegment(_backCanvas, position, CrateAngle(i), seg, 1f);
            }

            // 상자 앞판: 조각의 아래쪽을 가린다.
            var lip = new Rect(Crate.x, Crate.yMax - 28f, Crate.width, 28f);
            _backCanvas.Fill(lip, woodLight);
            _backCanvas.Fill(new Rect(lip.x, lip.y, lip.width, 3f), new Color(1f, 0.85f, 0.6f, 0.35f));
            _backCanvas.Fill(new Rect(lip.x, lip.yMax - 3f, lip.width, 3f), woodDark);
            _backCanvas.Fill(new Rect(Crate.x, Crate.y, 10f, Crate.height), wood);
            _backCanvas.Fill(new Rect(Crate.xMax - 10f, Crate.y, 10f, Crate.height), wood);
        }

        // ------------------------------------------------------------------ ② 깎기

        private void DrawCut()
        {
            Vector2 c = GemCenter;
            float radius = GemRadiusPx;
            float rotation = _game.RotDeg;

            // 숫돌: 보석의 아래쪽 끝에 닿아 있다.
            float contactY = c.y + _game.FacingExtent * radius + (_game.Grinding ? 0f : 6f);
            Vector2 wheel = new Vector2(c.x, contactY + 96f);
            bool coarse = _game.Tool == 0;
            _backCanvas.Circle(wheel + new Vector2(0f, 6f), 98f, new Color(0f, 0f, 0f, 0.35f));
            _backCanvas.Circle(wheel, 96f, coarse ? new Color(0.36f, 0.3f, 0.26f) : new Color(0.6f, 0.62f, 0.68f));
            _backCanvas.Ring(wheel, 88f, coarse ? new Color(0.5f, 0.42f, 0.34f) : new Color(0.8f, 0.82f, 0.9f), 3f);
            for (int i = 0; i < 8; i++)
            {
                float a = (_wheelAngle + i * 45f) * Mathf.Deg2Rad;
                Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                _backCanvas.Line(wheel + d * 18f, wheel + d * 86f, 4f, coarse ? new Color(0.25f, 0.2f, 0.17f) : new Color(0.45f, 0.47f, 0.55f));
                if (coarse)
                {
                    _backCanvas.Circle(wheel + d * 60f, 4f, new Color(0.7f, 0.6f, 0.45f));
                }
            }

            _backCanvas.Circle(wheel, 16f, new Color(0.15f, 0.15f, 0.18f));
            _backCanvas.Circle(c, radius + 42f, new Color(0.05f, 0.05f, 0.08f, 0.5f));

            ShowGem(rotation, Color.white, true);
            EnsureGemTexture();

            // 목표 모양(점선): 보석과 함께 돌아간다.
            for (int k = 0; k < 360; k += 3)
            {
                _frontCanvas.Circle(OnProfile(_game.Target, k, rotation, 1f), 3f, new Color(1f, 0.85f, 0.35f, 0.95f));
            }

            // 인덱스 눈금과 깎을 면의 표시
            for (int m = 0; m < NecklaceGame.IndexCount; m++)
            {
                float a = (m * NecklaceGame.IndexDeg + rotation) * Mathf.Deg2Rad;
                Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                bool facing = m * NecklaceGame.IndexDeg == _game.FacingDeg;
                _frontCanvas.Line(c + d * (radius * 1.2f), c + d * (radius * (facing ? 1.32f : 1.26f)), facing ? 4f : 2f, facing ? Color.white : new Color(1f, 1f, 1f, 0.35f));
            }

            foreach (NecklaceGame.TFacet facet in _game.TargetFacets)
            {
                float a = (facet.Phi + rotation) * Mathf.Deg2Rad;
                Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                bool facing = facet.Phi == _game.FacingDeg;
                _frontCanvas.Circle(c + d * (radius * 1.42f), facing ? 8f : 5f, facing ? new Color(1f, 0.9f, 0.4f) : new Color(1f, 0.75f, 0.25f, 0.9f));
            }

            DrawThicknessGauge();
        }

        // 남은 두께 계: 눈금은 일정한 선형이다(게이지가 갑자기 빨라지지 않는다).
        private void DrawThicknessGauge()
        {
            var gauge = new Rect(60f, 150f, 44f, 260f);
            _backCanvas.Fill(new Rect(gauge.x - 8f, gauge.y - 34f, 220f, gauge.height + 52f), new Color(0.05f, 0.05f, 0.08f, 0.82f));
            _backCanvas.Fill(gauge, new Color(0.1f, 0.1f, 0.13f));
            Outline(_backCanvas, gauge, new Color(1f, 1f, 1f, 0.45f), 1.5f);
            const float top = 0.10f;
            const float bottom = -0.03f;
            float YOf(float value) => gauge.y + gauge.height * (1f - Mathf.Clamp01((value - bottom) / (top - bottom)));
            float zoneTop = YOf(_game.Tol);
            float zoneBottom = YOf(-_game.Tol);
            _backCanvas.Fill(new Rect(gauge.x + 1f, zoneTop, gauge.width - 2f, zoneBottom - zoneTop), new Color(0.4f, 1f, 0.55f, 0.6f));
            _backCanvas.Fill(new Rect(gauge.x + 1f, zoneBottom, gauge.width - 2f, gauge.yMax - zoneBottom - 1f), new Color(1f, 0.3f, 0.25f, 0.3f));
            float remaining = _game.Remaining;
            bool inZone = Mathf.Abs(remaining) <= _game.Tol;
            _backCanvas.Fill(new Rect(gauge.x - 6f, YOf(remaining) - 2f, gauge.width + 12f, 4f), inZone ? new Color(0.6f, 1f, 0.7f) : Color.white);
        }

        // ------------------------------------------------------------------ ③ 장착

        private void DrawSet()
        {
            Vector2 c = GemCenter;
            float radius = GemRadiusPx;
            bool done = _game.Phase != NecklaceGame.PhaseKind.Set;

            // 펜던트 틀과 체인
            float chainY = -(NecklaceGame.GemR + 1.0f) * GemUnit;
            _backCanvas.Line(c + new Vector2(-30f, chainY), c + new Vector2(-210f, chainY - 50f), 5f, Gold * 0.8f);
            _backCanvas.Line(c + new Vector2(30f, chainY), c + new Vector2(210f, chainY - 50f), 5f, Gold * 0.8f);
            _backCanvas.Ring(c + new Vector2(0f, -(NecklaceGame.GemR + 1.15f) * GemUnit), 16f, Gold, 5f);
            _backCanvas.Circle(c + new Vector2(5f, 9f), radius + 64f, new Color(0f, 0f, 0f, 0.35f));
            _backCanvas.Circle(c, radius + 64f, Gold * 0.75f);
            _backCanvas.Circle(c, radius + 50f, Gold);
            _backCanvas.Ring(c, radius + 36f, Gold * 0.7f, 3f);

            // 구멍: 깎아 낸 보석과 같은 모양
            int socketDeg = _game.SocketIdx * NecklaceGame.IndexDeg;
            ShowSocket(socketDeg);
            ShowGem(_game.GemIdx * NecklaceGame.IndexDeg, Color.white, true, _game.GemX, _game.GemY);
            EnsureGemTexture();

            Color outline = done || _game.Aligned ? new Color(0.5f, 1f, 0.6f) : (_game.Near ? new Color(1f, 0.9f, 0.4f) : new Color(1f, 1f, 1f, 0.85f));
            Vector2 previous = OnProfile(_game.Profile, 0, socketDeg, 1f);
            for (int k = 4; k <= 360; k += 4)
            {
                Vector2 current = OnProfile(_game.Profile, k % 360, socketDeg, 1f);
                _frontCanvas.Line(previous, current, 4f, outline);
                previous = current;
            }

            Vector2 socketMark = c + new Vector2(Mathf.Cos(socketDeg * Mathf.Deg2Rad), Mathf.Sin(socketDeg * Mathf.Deg2Rad)) * (radius * 0.55f);
            _frontCanvas.Circle(socketMark, 9f, new Color(0f, 0f, 0f, 0.6f));
            _frontCanvas.Ring(socketMark, 9f, outline, 2f);

            Vector2 gemPosition = c + new Vector2(_game.GemX, _game.GemY) * GemUnit;
            if (_game.Dragging)
            {
                _frontCanvas.Circle(gemPosition, radius * 1.05f, new Color(1f, 1f, 1f, 0.07f));
            }

            float gemDeg = _game.GemIdx * NecklaceGame.IndexDeg;
            Vector2 gemMark = gemPosition + new Vector2(Mathf.Cos(gemDeg * Mathf.Deg2Rad), Mathf.Sin(gemDeg * Mathf.Deg2Rad)) * (radius * 0.55f);
            _frontCanvas.Circle(gemMark, 10f, new Color(0.1f, 0.08f, 0.02f, 0.9f));
            _frontCanvas.Circle(gemMark, 7f, new Color(1f, 0.85f, 0.35f));

            if (_game.SeatProgress > 0.01f)
            {
                float fraction = _game.SeatProgress;
                float arc = radius + 24f;
                Vector2 last = c + new Vector2(0f, -arc);
                int segments = Mathf.Max(2, Mathf.RoundToInt(fraction * 60f));
                for (int i = 1; i <= segments; i++)
                {
                    float a = (-90f + 360f * fraction * i / segments) * Mathf.Deg2Rad;
                    Vector2 point = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * arc;
                    _frontCanvas.Line(last, point, 7f, new Color(0.5f, 1f, 0.6f));
                    last = point;
                }
            }
        }

        // ------------------------------------------------------------------ ④ 광내기

        private void DrawPolish()
        {
            Vector2 c = GemCenter;
            float radius = GemRadiusPx;
            bool done = _game.Phase == NecklaceGame.PhaseKind.Done;

            _backCanvas.Circle(c, radius + 70f, new Color(0.05f, 0.05f, 0.08f, 0.55f));
            _backCanvas.Circle(c + new Vector2(4f, 8f), radius + 34f, new Color(0f, 0f, 0f, 0.4f));
            _backCanvas.Circle(c, radius + 34f, Gold * 0.7f);
            _backCanvas.Circle(c, radius + 24f, Gold);
            _backCanvas.Ring(c, radius + 16f, Gold * 0.6f, 3f);

            ShowGem(_game.PolishRot, Color.white, false);
            EnsureGemTexture();

            for (int i = 0; i < _game.TargetFacets.Count; i++)
            {
                bool hover = i == _game.Hover && !done;
                Vector2 center = FacetCenter(i);
                if (_game.Shine[i] >= 0.999f)
                {
                    float twinkle = 0.5f + 0.5f * Mathf.Sin(_pulse * 4f + i * 1.7f);
                    _frontCanvas.Circle(center + new Vector2(-6f, -6f), 3f + 3f * twinkle, new Color(1f, 1f, 0.95f, 0.95f));
                    _frontCanvas.Circle(center + new Vector2(8f, 4f), 2f + 2f * twinkle, new Color(1f, 1f, 0.95f, 0.7f));
                }
                else if (hover)
                {
                    _frontCanvas.Ring(center, 22f, _game.Scrubbing ? new Color(1f, 0.9f, 0.5f) : new Color(1f, 1f, 1f, 0.8f), 2f);
                }

                // 진행 표시: 면 가운데 아래 작은 막대
                if (!done && _game.Shine[i] < 0.999f)
                {
                    var bar = new Rect(center.x - 18f, center.y + 20f, 36f, 5f);
                    _frontCanvas.Fill(bar, new Color(0f, 0f, 0f, 0.5f));
                    _frontCanvas.Fill(new Rect(bar.x, bar.y, bar.width * _game.Shine[i], bar.height), new Color(0.6f, 1f, 0.7f));
                }
            }
        }

        // ------------------------------------------------------------------ 커서

        private void DrawCursor(int step)
        {
            if (!_hasPointer)
            {
                return;
            }

            switch (step)
            {
                case 0:
                    if (_game.Held >= 0)
                    {
                        float want = 0f;
                        if (_slotPositions != null)
                        {
                            for (int r = 0; r < _game.Rows.Count; r++)
                            {
                                for (int j = 0; j < _game.Rows[r].Len; j++)
                                {
                                    if (_game.Rows[r].Slots[j] < 0 && InSlot(r, j, _pointer))
                                    {
                                        want = _slotAngles[r][j];
                                    }
                                }
                            }
                        }

                        _heldAngle = Mathf.MoveTowardsAngle(_heldAngle, want, 600f * Time.unscaledDeltaTime);
                        DrawSegment(_frontCanvas, _pointer, _heldAngle, _game.SegById(_game.Held), 0.95f);
                    }
                    else
                    {
                        _heldAngle = 0f;
                    }

                    _frontCanvas.Circle(_pointer, 4f, new Color(1f, 1f, 1f, 0.9f));
                    return;
                case 2:
                    _frontCanvas.Circle(_pointer, _game.Dragging ? 11f : 8f, _game.Dragging ? new Color(1f, 0.9f, 0.6f, 0.9f) : new Color(1f, 1f, 1f, 0.6f));
                    _frontCanvas.Ring(_pointer, 16f, new Color(1f, 1f, 1f, 0.4f), 2f);
                    return;
                case 3:
                {
                    // 천: 문지르는 동안 노랗게 테두리가 켜진다.
                    var cloth = new Rect(_pointer.x - 18f, _pointer.y - 13f, 36f, 26f);
                    _frontCanvas.Fill(new Rect(cloth.x + 2f, cloth.y + 3f, cloth.width, cloth.height), new Color(0f, 0f, 0f, 0.3f));
                    _frontCanvas.Fill(cloth, new Color(0.92f, 0.92f, 0.94f));
                    Outline(_frontCanvas, cloth, _game.Scrubbing ? new Color(1f, 0.9f, 0.5f) : new Color(0.4f, 0.4f, 0.5f), 2f);
                    return;
                }
                default:
                    _frontCanvas.Circle(_pointer, 4f, new Color(1f, 1f, 1f, 0.85f));
                    return;
            }
        }

        // ------------------------------------------------------------------ 조각과 도형

        private static void DrawPill(PixelCanvas canvas, Vector2 center, float angleDeg, float length, float width, Color color)
        {
            float a = angleDeg * Mathf.Deg2Rad;
            Vector2 half = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (Mathf.Max(0f, length - width) * 0.5f);
            canvas.Line(center - half, center + half, width, color);
        }

        // 조각: 금빛 몸통 양 끝에 연결 모양(색과 모양 번호)이 있다.
        private static void DrawSegment(PixelCanvas canvas, Vector2 center, float angleDeg, NecklaceGame.Seg seg, float alpha)
        {
            Color gold = new Color(Gold.r, Gold.g, Gold.b, alpha);
            DrawPill(canvas, center + new Vector2(2f, 4f), angleDeg, SegW, SegH, new Color(0f, 0f, 0f, 0.35f * alpha));
            DrawPill(canvas, center, angleDeg, SegW, SegH, new Color(gold.r * 0.55f, gold.g * 0.55f, gold.b * 0.55f, alpha));
            DrawPill(canvas, center, angleDeg, SegW - 4f, SegH - 4f, gold);
            float a = angleDeg * Mathf.Deg2Rad;
            Vector2 axis = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            float end = SegW * 0.5f - 12f;
            DrawSymbol(canvas, center - axis * end, seg.Left, 12f);
            DrawSymbol(canvas, center + axis * end, seg.Right, 12f);
        }

        // 연결 모양: 색은 번호 / 4, 모양은 번호 % 4(원, 네모, 십자, 고리)
        private static void DrawSymbol(PixelCanvas canvas, Vector2 p, int symbol, float size)
        {
            Color color = SymbolColors[(symbol / 4) % 4];
            Color dark = new Color(0.08f, 0.08f, 0.1f, 0.95f);
            switch (symbol % 4)
            {
                case 0:
                    canvas.Circle(p, size * 0.62f + 2f, dark);
                    canvas.Circle(p, size * 0.62f, color);
                    break;
                case 1:
                    canvas.Fill(new Rect(p.x - size * 0.55f - 2f, p.y - size * 0.55f - 2f, size * 1.1f + 4f, size * 1.1f + 4f), dark);
                    canvas.Fill(new Rect(p.x - size * 0.55f, p.y - size * 0.55f, size * 1.1f, size * 1.1f), color);
                    break;
                case 2:
                {
                    float a = size * 0.62f;
                    float b = size * 0.22f;
                    canvas.Fill(new Rect(p.x - a - 2f, p.y - b - 2f, a * 2f + 4f, b * 2f + 4f), dark);
                    canvas.Fill(new Rect(p.x - b - 2f, p.y - a - 2f, b * 2f + 4f, a * 2f + 4f), dark);
                    canvas.Fill(new Rect(p.x - a, p.y - b, a * 2f, b * 2f), color);
                    canvas.Fill(new Rect(p.x - b, p.y - a, b * 2f, a * 2f), color);
                    break;
                }
                default:
                    canvas.Circle(p, size * 0.62f + 2f, dark);
                    canvas.Ring(p, size * 0.42f, color, size * 0.32f);
                    break;
            }
        }

        private static void Outline(PixelCanvas canvas, Rect rect, Color color, float width)
        {
            canvas.Fill(new Rect(rect.xMin, rect.yMin, rect.width, width), color);
            canvas.Fill(new Rect(rect.xMin, rect.yMax - width, rect.width, width), color);
            canvas.Fill(new Rect(rect.xMin, rect.yMin, width, rect.height), color);
            canvas.Fill(new Rect(rect.xMax - width, rect.yMin, width, rect.height), color);
        }

        // 보석 윤곽 위의 점: 각도 k(도, 보석 기준), 보석을 rotationDeg만큼 돌려 놓았을 때의 캔버스 위치
        private Vector2 OnProfile(float[] profile, int k, float rotationDeg, float scale)
        {
            return OnProfile(GemCenter, profile, k, rotationDeg, scale);
        }

        private static Vector2 OnProfile(Vector2 center, float[] profile, int k, float rotationDeg, float scale)
        {
            float a = (k + rotationDeg) * Mathf.Deg2Rad;
            return center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (profile[((k % 360) + 360) % 360] * scale * GemRadiusPx);
        }

        // ------------------------------------------------------------------ 보석 그림

        private void HideGem()
        {
            _gem.enabled = false;
            _socket.enabled = false;
        }

        private void ShowSocket(float rotationDeg)
        {
            PlaceImage(_socket, GemCenter, rotationDeg, new Color(0.03f, 0.03f, 0.05f, 0.96f));
        }

        private void ShowGem(float rotationDeg, Color tint, bool showShadow)
        {
            ShowGem(rotationDeg, tint, showShadow, 0f, 0f);
            _socket.enabled = false;
        }

        private void ShowGem(float rotationDeg, Color tint, bool showShadow, float cellX, float cellY)
        {
            PlaceImage(_gem, GemCenter + new Vector2(cellX, cellY) * GemUnit, rotationDeg, tint);
        }

        private void PlaceImage(RawImage image, Vector2 canvasCenter, float rotationDeg, Color tint)
        {
            float side = 2f * Span * GemRadiusPx;
            image.enabled = _gemTexture != null && _game.TargetFacets.Count > 0;
            var rect = (RectTransform)image.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(side, side);
            rect.anchoredPosition = ToLocal(canvasCenter);
            // 화면은 아래쪽이 +y라서 도는 방향이 UI와 반대다.
            rect.localRotation = Quaternion.Euler(0f, 0f, -rotationDeg);
            image.color = tint;
        }

        private void BuildGemTexture()
        {
            _gemTexture = new Texture2D(GemTexSize, GemTexSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            _gem.texture = _gemTexture;
            _socket.texture = _gemTexture;
            int count = GemTexSize * GemTexSize;
            _pixels = new Color32[count];
            _pixelAngle = new int[count];
            _pixelRho = new float[count];
            _pixelNx = new float[count];
            _pixelNy = new float[count];
            _shade = new float[count];
            for (int y = 0; y < GemTexSize; y++)
            {
                for (int x = 0; x < GemTexSize; x++)
                {
                    float nx = (x + 0.5f) / GemTexSize * 2f * Span - Span;
                    float ny = (y + 0.5f) / GemTexSize * 2f * Span - Span;
                    int i = (GemTexSize - 1 - y) * GemTexSize + x;
                    _pixelNx[i] = nx;
                    _pixelNy[i] = ny;
                    _pixelRho[i] = Mathf.Sqrt(nx * nx + ny * ny);
                    int k = Mathf.RoundToInt(Mathf.Atan2(ny, nx) * Mathf.Rad2Deg);
                    _pixelAngle[i] = ((k % 360) + 360) % 360;
                    _shade[i] = 0.72f + 0.28f * Mathf.Clamp01(0.5f - ny * 0.5f - nx * 0.25f);
                }
            }
        }

        // 보석 모양이나 윤이 바뀌었을 때만 다시 그린다. 갈리는 동안은 초당 약 20번만 그린다.
        private void EnsureGemTexture()
        {
            if (_game.TargetFacets.Count == 0)
            {
                return;
            }

            int mode = _game.Phase == NecklaceGame.PhaseKind.Polish || _game.Phase == NecklaceGame.PhaseKind.Done
                || (_game.Phase == NecklaceGame.PhaseKind.StepDone && _game.Step >= 3) ? 1 : 0;
            float key = mode * 100000f + (int)_game.Phase * 7919f + _game.Match * 1000f
                + (mode == 1 ? _game.AvgShine * 100000f + _game.ShinedCount : 0f)
                + (_game.Phase == NecklaceGame.PhaseKind.Cut ? _game.Index : 0f);
            bool grindingNow = mode == 0 && _game.Grinding;
            if (mode == _gemMode && Mathf.Approximately(key, _gemKey) && !grindingNow)
            {
                return;
            }

            if (mode == _gemMode && grindingNow && Time.unscaledTime < _nextRebuild)
            {
                return;
            }

            _nextRebuild = Time.unscaledTime + 0.05f;
            _gemMode = mode;
            _gemKey = key;
            RebuildGem(mode);
        }

        private static float CosDeg(float deg)
        {
            return Mathf.Cos(deg * Mathf.Deg2Rad);
        }

        private void RebuildGem(int mode)
        {
            Color baseColor = _gemColor;
            Color matte = new Color(0.55f, 0.55f, 0.56f);
            int facets = _game.TargetFacets.Count;
            if (_facetOfAngle == null || _facetCount != facets)
            {
                // 면 배치는 깎는 동안 변하지 않으니 한 번만 계산한다.
                _facetCount = facets;
                _facetOfAngle = new int[360];
                _litOfAngle = new float[360];
                for (int k = 0; k < 360; k++)
                {
                    int best = 0;
                    float bestDistance = 999f;
                    for (int i = 0; i < facets; i++)
                    {
                        float d = Mathf.Abs(NecklaceGame.Wrap(k - _game.TargetFacets[i].Phi));
                        if (d < bestDistance)
                        {
                            bestDistance = d;
                            best = i;
                        }
                    }

                    _facetOfAngle[k] = best;
                    _litOfAngle[k] = 0.82f + 0.22f * CosDeg(_game.TargetFacets[best].Phi - 225f);
                }
            }

            float[] profile = _game.Profile;
            float[] target = _game.Target;
            float[] rough = _game.Rough;
            float tolerance = _game.Tol;
            bool cutting = _game.Phase == NecklaceGame.PhaseKind.Cut;
            float br = baseColor.r, bg = baseColor.g, bb = baseColor.b;
            float dr = matte.r - br, dg = matte.g - bg, db = matte.b - bb;
            for (int i = 0; i < _pixels.Length; i++)
            {
                int k = _pixelAngle[i];
                float edge = profile[k];
                float rho = _pixelRho[i];
                float deficit = target[k] - edge;
                if (rho > edge)
                {
                    // 너무 깎은 자리는 있어야 할 보석이 파여 나간 것처럼 어둡게 움푹 보인다.
                    if (deficit > tolerance && rho <= target[k] + 0.004f)
                    {
                        float depth = Mathf.Clamp01((rho - edge) / Mathf.Max(0.001f, deficit));
                        Color hole = Color.Lerp(new Color(0.16f, 0.05f, 0.06f), new Color(0.02f, 0.01f, 0.02f), depth);
                        if (rho > target[k] - 0.012f)
                        {
                            hole = new Color(0.9f, 0.45f, 0.4f);
                        }

                        hole.a = 1f;
                        _pixels[i] = hole;
                    }
                    else
                    {
                        _pixels[i] = new Color32(0, 0, 0, 0);
                    }

                    continue;
                }

                float shade = _shade[i];
                Color color;
                if (mode == 0)
                {
                    // 거칠게 간 쪽은 뿌옇다.
                    float matteAmount = (rho > edge - 0.22f ? 0.55f : 0.2f) * rough[(((k + 7) / 15) % NecklaceGame.IndexCount)];
                    float mul = shade * (cutting ? 1f : _litOfAngle[k]);
                    if (rho > edge - 0.03f)
                    {
                        mul *= 0.7f;
                    }

                    if (deficit > tolerance)
                    {
                        mul *= 1f - 0.45f * Mathf.Clamp01((rho - (edge - 0.08f)) / 0.08f) * Mathf.Clamp01(deficit / 0.05f);
                    }

                    _pixels[i] = new Color32(
                        (byte)(Mathf.Min(1f, (br + dr * matteAmount) * mul) * 255f),
                        (byte)(Mathf.Min(1f, (bg + dg * matteAmount) * mul) * 255f),
                        (byte)(Mathf.Min(1f, (bb + db * matteAmount) * mul) * 255f),
                        255);
                    continue;
                }

                int facet = _facetOfAngle[k];
                float lit = _litOfAngle[k];
                float shine = _game.Shine[facet];
                Color dull = Color.Lerp(baseColor * 0.55f, matte, 0.6f) * shade;
                Color glossy = baseColor * shade * lit * 1.12f;
                color = Color.Lerp(dull, glossy, Mathf.Pow(shine, 0.8f));
                float distance = Mathf.Abs(NecklaceGame.Wrap(k - _game.TargetFacets[facet].Phi));
                if (distance < 14f)
                {
                    color += Color.white * (0.4f * shine * (1f - distance / 14f) * (1f - rho * 0.5f));
                }

                if (_facetOfAngle[k] != _facetOfAngle[(k + 1) % 360])
                {
                    color *= 0.55f;
                }

                if (rho > edge - 0.03f)
                {
                    color *= 0.7f;
                }

                if (deficit > tolerance)
                {
                    color *= 1f - 0.45f * Mathf.Clamp01((rho - (edge - 0.08f)) / 0.08f) * Mathf.Clamp01(deficit / 0.05f);
                }

                color.a = 1f;
                _pixels[i] = color;
            }

            _gemTexture.SetPixels32(_pixels);
            _gemTexture.Apply();
        }
    }
}
