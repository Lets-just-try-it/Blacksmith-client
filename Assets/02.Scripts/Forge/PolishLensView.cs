using System.Collections.Generic;
using Blacksmith.Core.Forge;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Forge
{
    // 연마의 확대 화면. 도는 숫돌의 표면이 위아래로 흘러가고, 그 위에서 마우스 높이가 날의 각도가 된다.
    // 알맞은 각도는 초록 띠로 보이고 위아래로 흔들리므로 띠 안에 마우스를 맞춰 따라간다.
    // 아래쪽 막대는 날 끝 전체이며 칸마다 선 정도를 보여 준다. 숫돌이 닿은 칸에는 괄호가 붙는다.
    // 마우스 위치를 게임 좌표로 바꾸는 일(TryMap)도 여기서 한다. 규칙은 건드리지 않고 PolishGame의 값만 읽는다.
    public class PolishLensView : MonoBehaviour
    {
        private const int FrameMargin = 6;
        private const int LensWidth = 940;
        private const int LensHeight = 322;
        private const float TextureScale = 2f;
        // 게임 좌표 1.0이 화면에서 갖는 길이(렌즈 폭의 절반)
        private const float UnitSize = LensWidth * 0.5f;
        private const float BarHeight = 46f;
        private const float TileHeight = 96f;
        // 숫돌 표면이 흘러가는 속도(텍스처 줄 수/초)
        private const float WheelSpeed = 150f;

        private struct Mote
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Life;
            public float MaxLife;
            // 0 불똥, 1 김
            public int Kind;
            public Color Color;
        }

        private struct Ring
        {
            public Vector2 Position;
            public float Life;
            public float Radius;
            public Color Color;
        }

        private const float RingLife = 0.4f;

        [SerializeField] private RawImage _image;

        private readonly List<Mote> _motes = new List<Mote>();
        private readonly List<Ring> _rings = new List<Ring>();
        private PixelCanvas _canvas;
        private PolishGame _game;
        private RectTransform _rect;
        private readonly float[] _cellFlash = new float[PolishGame.SegmentCount];
        private float _pulse;
        private float _wheel;

        private float BarTop => FrameMargin + LensHeight - BarHeight - 4f;
        public float Pulse => _pulse;

        private void Awake()
        {
            _rect = (RectTransform)_image.transform;
            _canvas = new PixelCanvas(LensWidth + FrameMargin * 2, LensHeight + FrameMargin * 2, TextureScale);
            _image.texture = _canvas.Texture;
            BuildWheelTile();
            Clear();
        }

        private void OnDestroy()
        {
            if (_canvas != null)
            {
                Destroy(_canvas.Texture);
            }
        }

        public void Bind(PolishGame game)
        {
            _game = game;
            _motes.Clear();
            _rings.Clear();
            System.Array.Clear(_cellFlash, 0, _cellFlash.Length);
            _wheel = 0f;
        }

        public void Unbind()
        {
            _game = null;
            Clear();
        }

        // 열이 높을수록 불똥이 노랗다가 붉어지고, 각도가 어긋나면 옅어진다.
        public static Color SparkTone(PolishGame game)
        {
            if (game.Heat >= game.WarnHeat)
            {
                return new Color(1f, 0.32f, 0.2f);
            }
            return game.Quality < 0.5f ? new Color(0.8f, 0.86f, 0.95f) : Color.Lerp(new Color(1f, 0.95f, 0.65f), new Color(1f, 0.7f, 0.3f), game.Heat / Mathf.Max(0.01f, game.WarnHeat));
        }

        // 화면의 마우스 위치를 렌즈 안 게임 좌표로 바꾼다. 렌즈 밖이면 false다.
        public bool TryMap(Vector2 screenPoint, out float x, out float y)
        {
            x = -100f;
            y = -100f;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screenPoint, null, out Vector2 local))
            {
                return false;
            }

            Vector2 offset = local - _rect.rect.center;
            if (Mathf.Abs(offset.x) > LensWidth * 0.5f || Mathf.Abs(offset.y) > LensHeight * 0.5f)
            {
                return false;
            }

            x = offset.x / UnitSize;
            y = -offset.y / UnitSize;
            return true;
        }

        // 게임 좌표를 렌즈 중심 기준 화면 위치(위쪽이 +)로 바꾼다. 말풍선 위치에 쓴다.
        public Vector2 ToLocal(float x, float y)
        {
            return new Vector2(x * UnitSize, -y * UnitSize);
        }

        // 날 끝 막대 위 칸 가운데의 말풍선 위치
        public Vector2 SegmentLocal(int index)
        {
            return new Vector2(PolishGame.SegmentCenter(index) * UnitSize, -(BarTop - FrameMargin - LensHeight * 0.5f) - 10f);
        }

        public void Tick(float deltaTime)
        {
            if (_game == null)
            {
                return;
            }

            _pulse += deltaTime;
            _wheel += deltaTime * WheelSpeed;
            SpawnMotes();
            UpdateMotes(deltaTime);
            Draw();
        }

        // 칸이 다 갈렸다: 불똥이 튀고 파동이 번진다.
        public void BurstAt(int segment)
        {
            Vector2 p = new Vector2(ToCanvasX(PolishGame.SegmentCenter(segment)), BarTop);
            Color spark = new Color(1f, 0.9f, 0.55f);
            for (int i = 0; i < 12; i++)
            {
                AddMote(p, new Vector2(Random.Range(-90f, 90f), Random.Range(-170f, -40f)), 0.55f, 0, spark);
            }
            _rings.Add(new Ring { Position = p, Life = RingLife, Radius = 10f, Color = new Color(1f, 0.95f, 0.7f) });
            _cellFlash[segment] = 1f;
        }

        // 날이 탔다: 붉은 불똥이 사방으로 튄다.
        public void BurnAt()
        {
            if (_game.PointerX <= -50f)
            {
                return;
            }

            Vector2 p = ContactPoint();
            for (int i = 0; i < 24; i++)
            {
                AddMote(p, Random.insideUnitCircle * 220f, 0.7f, 0, new Color(1f, 0.3f, 0.18f));
            }
            _rings.Add(new Ring { Position = p, Life = RingLife, Radius = 12f, Color = new Color(1f, 0.35f, 0.25f) });
        }

        private void Clear()
        {
            if (_canvas == null)
            {
                return;
            }

            _canvas.Clear(Color.clear);
            _canvas.Apply();
        }

        private static float ToCanvasX(float x)
        {
            return FrameMargin + UnitSize + x * UnitSize;
        }

        private static float ToCanvasY(float y)
        {
            return FrameMargin + LensHeight * 0.5f + y * UnitSize;
        }

        private Vector2 ContactPoint()
        {
            return new Vector2(ToCanvasX(_game.PointerX), Mathf.Min(ToCanvasY(_game.PointerY), BarTop - 14f));
        }

        private void AddMote(Vector2 position, Vector2 velocity, float life, int kind, Color color)
        {
            _motes.Add(new Mote { Position = position, Velocity = velocity, Life = life, MaxLife = life, Kind = kind, Color = color });
        }

        private void SpawnMotes()
        {
            if (_game.PointerX <= -50f)
            {
                return;
            }

            Vector2 p = ContactPoint();
            if (_game.Grinding && Random.value < 0.35f + 0.55f * _game.Quality)
            {
                AddMote(p, new Vector2(Random.Range(40f, 190f), Random.Range(-110f, 40f)), 0.35f, 0, SparkTone(_game));
            }
            else if (_game.Dipping && Random.value < 0.5f)
            {
                AddMote(p, new Vector2(Random.Range(-25f, 25f), Random.Range(-90f, -40f)), 0.8f, 1, new Color(0.9f, 0.95f, 1f));
            }
        }

        private void UpdateMotes(float deltaTime)
        {
            for (int i = _motes.Count - 1; i >= 0; i--)
            {
                Mote mote = _motes[i];
                mote.Life -= deltaTime;
                mote.Position += mote.Velocity * deltaTime;
                mote.Velocity.y += (mote.Kind == 0 ? 280f : -20f) * deltaTime;
                mote.Velocity.x *= 1f - 1.2f * deltaTime;
                if (mote.Life <= 0f)
                {
                    _motes.RemoveAt(i);
                }
                else
                {
                    _motes[i] = mote;
                }
            }

            for (int i = 0; i < _cellFlash.Length; i++)
            {
                _cellFlash[i] = Mathf.MoveTowards(_cellFlash[i], 0f, deltaTime * 2.2f);
            }

            for (int i = _rings.Count - 1; i >= 0; i--)
            {
                Ring ring = _rings[i];
                ring.Life -= deltaTime;
                ring.Radius += 150f * deltaTime;
                if (ring.Life <= 0f)
                {
                    _rings.RemoveAt(i);
                }
                else
                {
                    _rings[i] = ring;
                }
            }
        }

        // ------------------------------------------------------------------ 그리기

        // 숫돌 표면 한 겹: 결 줄무늬와 알갱이. 위아래로 이어지게 그려 두고 흘려 보낸다.
        private void BuildWheelTile()
        {
            _canvas.Clear(new Color(0.02f, 0.02f, 0.03f, 1f));
            float width = _canvas.Width;
            for (int row = 0; row < (int)TileHeight; row++)
            {
                float t = row / TileHeight * Mathf.PI * 2f;
                float gray = 0.3f + 0.05f * Mathf.Sin(t * 8f) + 0.04f * Mathf.Sin(t * 21f + 1f) + 0.025f * Mathf.Sin(t * 3f);
                _canvas.Fill(new Rect(0f, row, width, 1.2f), new Color(gray * 1.02f, gray * 0.98f, gray * 0.94f));
            }

            var random = new System.Random(7);
            for (int i = 0; i < 320; i++)
            {
                var p = new Vector2((float)random.NextDouble() * width, 2f + (float)random.NextDouble() * (TileHeight - 4f));
                float size = 0.8f + (float)random.NextDouble() * 1.6f;
                bool light = random.NextDouble() < 0.5;
                _canvas.Circle(p, size, light ? new Color(0.62f, 0.56f, 0.5f, 0.55f) : new Color(0.08f, 0.07f, 0.07f, 0.5f));
            }

            // 양 끝으로 갈수록 어두워지는 둥근 숫돌의 느낌
            for (int i = 0; i < 70; i++)
            {
                float k = 1f - i / 70f;
                float alpha = k * k * 0.65f;
                _canvas.Fill(new Rect(i, 0f, 1f, TileHeight), new Color(0f, 0f, 0f, alpha));
                _canvas.Fill(new Rect(width - 1f - i, 0f, 1f, TileHeight), new Color(0f, 0f, 0f, alpha));
            }

            _canvas.SaveTile(TileHeight);
        }

        private void Draw()
        {
            _canvas.RestoreTile(Mathf.RoundToInt(_wheel));
            DrawMargins();
            DrawTargetLane();
            DrawBar();
            DrawContact();
            DrawMotes();
            DrawFrame();
            _canvas.Apply();
        }

        private void DrawMargins()
        {
            Color dark = new Color(0.02f, 0.02f, 0.03f);
            _canvas.Fill(new Rect(0f, 0f, _canvas.Width, FrameMargin), dark);
            _canvas.Fill(new Rect(0f, _canvas.Height - FrameMargin, _canvas.Width, FrameMargin), dark);
            _canvas.Fill(new Rect(0f, 0f, FrameMargin, _canvas.Height), dark);
            _canvas.Fill(new Rect(_canvas.Width - FrameMargin, 0f, FrameMargin, _canvas.Height), dark);
        }

        // 알맞은 각도의 띠: 위아래 경계선과 양 끝의 화살표. 띠 안에서 갈아야 한다.
        private void DrawTargetLane()
        {
            float center = ToCanvasY(_game.TargetAngle);
            float half = _game.AngleTolerance * UnitSize;
            bool good = _game.Grinding && _game.Quality >= 0.99f;
            Color line = good ? new Color(0.6f, 1f, 0.7f) : new Color(0.45f, 0.95f, 0.6f, 0.85f);
            float left = FrameMargin;
            float width = LensWidth;
            _canvas.Fill(new Rect(left, center - half - 1.5f, width, 3f), line);
            _canvas.Fill(new Rect(left, center + half - 1.5f, width, 3f), line);
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? left + 4f : left + width - 22f;
                _canvas.Fill(new Rect(x, center - half, 18f, half * 2f), new Color(0.45f, 0.95f, 0.6f, 0.28f));
                _canvas.Fill(new Rect(x + 6f, center - 3f, 6f, 6f), line);
            }
        }

        // 날 끝 막대: 칸마다 아래에서 차오르는 눈금. 가득 차 하얗게 빛나면 그 칸은 완성이다.
        // 탄 칸은 검붉고, 단단한 칸에는 윗면에 주황 표시가 있다. 막 완성한 칸은 금빛으로 번쩍인다.
        private void DrawBar()
        {
            float top = BarTop;
            float cell = LensWidth / (float)PolishGame.SegmentCount;
            _canvas.Fill(new Rect(FrameMargin - 2f, top - 3f, LensWidth + 4f, BarHeight + 6f), new Color(0.04f, 0.04f, 0.05f));
            for (int i = 0; i < PolishGame.SegmentCount; i++)
            {
                float ratio = Mathf.Clamp01(_game.Sharp[i] / _game.SegmentGoal);
                bool done = _game.Sharp[i] >= _game.SegmentGoal;
                float x = FrameMargin + i * cell;
                var back = new Rect(x + 1.5f, top, cell - 3f, BarHeight);
                bool burnt = _game.Burnt[i];
                _canvas.Fill(back, burnt ? new Color(0.28f, 0.07f, 0.05f) : new Color(0.13f, 0.13f, 0.17f));

                float height = done ? BarHeight : BarHeight * ratio;
                Color fill = done
                    ? Color.Lerp(new Color(0.92f, 0.97f, 1f), new Color(1f, 0.95f, 0.6f), _cellFlash[i])
                    : Color.Lerp(new Color(0.4f, 0.48f, 0.62f), new Color(0.68f, 0.76f, 0.9f), ratio);
                if (burnt && !done)
                {
                    fill = Color.Lerp(fill, new Color(0.45f, 0.12f, 0.08f), 0.7f);
                }

                if (height > 0.5f)
                {
                    _canvas.Fill(new Rect(back.x, top + BarHeight - height, back.width, height), fill);
                }
                if (done)
                {
                    _canvas.Fill(new Rect(back.x, top, back.width, 4f), Color.white);
                }
                else if (_game.Hard[i])
                {
                    _canvas.Fill(new Rect(x + cell * 0.5f - 5f, top - 1f, 10f, 4f), new Color(1f, 0.6f, 0.2f));
                }
            }
        }

        // 숫돌에 대고 있는 곳: 닿은 칸의 괄호, 막대와 이어 주는 선, 그리고 닿는 면과 고리
        private void DrawContact()
        {
            if (_game.PointerX <= -50f)
            {
                return;
            }

            Vector2 c = ContactPoint();
            float reach = (_game.ContactRadius + 1f / PolishGame.SegmentCount) * UnitSize;
            Color state = ContactColor();
            _canvas.Fill(new Rect(c.x - reach, BarTop - 8f, reach * 2f, 3f), state);
            _canvas.Fill(new Rect(c.x - reach, BarTop - 8f, 3f, 8f), state);
            _canvas.Fill(new Rect(c.x + reach - 3f, BarTop - 8f, 3f, 8f), state);
            _canvas.Fill(new Rect(c.x - 1f, c.y + 15f, 2f, Mathf.Max(0f, BarTop - 8f - c.y - 15f)), new Color(state.r, state.g, state.b, 0.45f));

            float width = _game.ContactRadius * UnitSize;
            _canvas.Fill(new Rect(c.x - width, c.y - 5f, width * 2f, 10f), new Color(0.78f, 0.82f, 0.9f));
            _canvas.Fill(new Rect(c.x - width, c.y - 5f, width * 2f, 3f), Color.white);
            _canvas.Ring(c, 17f, state);
            if (_game.Grinding)
            {
                _canvas.Ring(c, 24f + 4f * Mathf.Sin(_pulse * 32f), new Color(state.r, state.g, state.b, 0.45f), 2f);
            }
        }

        private Color ContactColor()
        {
            if (_game.Locked)
            {
                return new Color(1f, 0.3f, 0.25f);
            }
            if (_game.Dipping)
            {
                return new Color(0.5f, 0.8f, 1f);
            }
            if (_game.Grinding)
            {
                return Color.Lerp(new Color(1f, 0.45f, 0.35f), new Color(0.55f, 1f, 0.65f), _game.Quality);
            }
            return new Color(1f, 1f, 1f, 0.75f);
        }

        private void DrawMotes()
        {
            float limit = BarTop - 2f;
            foreach (Mote mote in _motes)
            {
                if (mote.Position.y > limit)
                {
                    continue;
                }

                float k = Mathf.Clamp01(mote.Life / mote.MaxLife);
                if (mote.Kind == 0)
                {
                    // 속도 방향으로 길게 늘여 튀는 불똥의 궤적처럼 그린다.
                    _canvas.Line(mote.Position, mote.Position - mote.Velocity * 0.04f, 2.6f, new Color(mote.Color.r, mote.Color.g, mote.Color.b, Mathf.Clamp01(k * 3f)));
                }
                else
                {
                    _canvas.Circle(mote.Position, 6f * (1.4f - k), new Color(mote.Color.r, mote.Color.g, mote.Color.b, k * 0.3f));
                }
            }

            foreach (Ring ring in _rings)
            {
                Color color = ring.Color;
                color.a = Mathf.Clamp01(ring.Life / RingLife) * 0.9f;
                _canvas.Ring(ring.Position, ring.Radius, color, 2.5f);
            }
        }

        // 테두리는 열이 오를수록 노랗다가 붉어지고, 경고 구간에서는 굵게 맥동한다.
        private void DrawFrame()
        {
            float heat = Mathf.Clamp01(_game.Heat / _game.BurnHeat);
            Color color = Color.Lerp(new Color(0.9f, 0.85f, 0.6f), new Color(1f, 0.25f, 0.2f), Mathf.Clamp01((heat - 0.4f) / 0.6f));
            float thickness = heat >= _game.WarnHeat / _game.BurnHeat ? 5f + 2f * Mathf.Sin(_pulse * 14f) : 3f;
            var rect = new Rect(0f, 0f, _canvas.Width, _canvas.Height);
            _canvas.Fill(new Rect(rect.x, rect.y, rect.width, thickness), color);
            _canvas.Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            _canvas.Fill(new Rect(rect.x, rect.y, thickness, rect.height), color);
            _canvas.Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }
    }
}
