using UnityEngine;

namespace Blacksmith.Forge
{
    // 원, 선, 사각형을 직접 그려 텍스처 하나로 보여 주는 작은 2D 캔버스. 연마의 확대한 날 끝 화면처럼
    // 도형이 매 프레임 바뀌는 화면을 UI 요소 수십 개 없이 그릴 때 쓴다.
    // 좌표는 논리 크기 기준이고 왼쪽 위가 (0, 0)이다. scale배로 키운 텍스처에 그려 화면이 커져도 선명하다.
    public class PixelCanvas
    {
        private readonly int _width;
        private readonly int _height;
        private readonly float _scale;
        private readonly Color32[] _pixels;
        private Color32[] _tile;
        private int _tileRows;

        public Texture2D Texture { get; }
        public float Width { get; }
        public float Height { get; }

        public PixelCanvas(int logicalWidth, int logicalHeight, float scale)
        {
            Width = logicalWidth;
            Height = logicalHeight;
            _scale = scale;
            _width = Mathf.RoundToInt(logicalWidth * scale);
            _height = Mathf.RoundToInt(logicalHeight * scale);
            _pixels = new Color32[_width * _height];
            Texture = new Texture2D(_width, _height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "PixelCanvas",
            };
        }

        public void Clear(Color color)
        {
            Color32 fill = color;
            for (int i = 0; i < _pixels.Length; i++)
            {
                _pixels[i] = fill;
            }
        }

        // 위쪽 tileHeight(논리 크기)만큼 그린 무늬를 바탕으로 저장해 두고, 매 프레임 세로로 shift만큼 밀어 가며 전체에 반복해 채운다.
        // 도는 숫돌 표면처럼 위아래로 이어지는 무늬를 도형을 다시 그리지 않고 한 줄씩 복사만 해서 흘려 보낼 때 쓴다.
        public void SaveTile(float tileHeight)
        {
            _tileRows = Mathf.RoundToInt(tileHeight * _scale);
            _tile = new Color32[_tileRows * _width];
            for (int row = 0; row < _tileRows; row++)
            {
                System.Array.Copy(_pixels, (_height - 1 - row) * _width, _tile, row * _width, _width);
            }
        }

        // shift만큼 무늬가 위로 밀린 모습으로 채운다. 음수면 아래로 밀린다.
        public void RestoreTile(int shift)
        {
            for (int row = 0; row < _height; row++)
            {
                int source = ((row + shift) % _tileRows + _tileRows) % _tileRows;
                System.Array.Copy(_tile, source * _width, _pixels, (_height - 1 - row) * _width, _width);
            }
        }

        public void Apply()
        {
            Texture.SetPixelData(_pixels, 0);
            Texture.Apply(false);
        }

        public void Fill(Rect rect, Color color)
        {
            int x0 = Mathf.Max(0, Mathf.RoundToInt(rect.xMin * _scale));
            int x1 = Mathf.Min(_width, Mathf.RoundToInt(rect.xMax * _scale));
            int y0 = Mathf.Max(0, Mathf.RoundToInt(rect.yMin * _scale));
            int y1 = Mathf.Min(_height, Mathf.RoundToInt(rect.yMax * _scale));
            if (color.a >= 1f)
            {
                // 불투명하면 섞지 않고 그대로 덮는다.
                Color32 solid = color;
                for (int y = y0; y < y1; y++)
                {
                    int row = (_height - 1 - y) * _width;
                    for (int x = x0; x < x1; x++)
                    {
                        _pixels[row + x] = solid;
                    }
                }
                return;
            }

            Paint paint = new Paint(color);
            for (int y = y0; y < y1; y++)
            {
                int row = (_height - 1 - y) * _width;
                for (int x = x0; x < x1; x++)
                {
                    Blend(row + x, paint, 1f);
                }
            }
        }

        public void Circle(Vector2 center, float radius, Color color)
        {
            Vector2 c = center * _scale;
            float r = radius * _scale;
            Bounds(c.x - r - 1f, c.y - r - 1f, c.x + r + 1f, c.y + r + 1f, out int x0, out int y0, out int x1, out int y1);
            float outer = (r + 0.5f) * (r + 0.5f);
            float inner = Mathf.Max(0f, r - 0.5f);
            inner *= inner;
            Paint paint = new Paint(color);
            for (int y = y0; y <= y1; y++)
            {
                int row = (_height - 1 - y) * _width;
                float dy = y + 0.5f - c.y;
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - c.x;
                    float squared = dx * dx + dy * dy;
                    if (squared > outer)
                    {
                        continue;
                    }
                    Blend(row + x, paint, squared <= inner ? 1f : Mathf.Clamp01(r - Mathf.Sqrt(squared) + 0.5f));
                }
            }
        }

        public void Ring(Vector2 center, float radius, Color color, float thickness = 2f)
        {
            Vector2 c = center * _scale;
            float r = radius * _scale;
            float half = thickness * _scale * 0.5f;
            Bounds(c.x - r - half - 1f, c.y - r - half - 1f, c.x + r + half + 1f, c.y + r + half + 1f, out int x0, out int y0, out int x1, out int y1);
            float outer = (r + half + 0.5f) * (r + half + 0.5f);
            float inner = Mathf.Max(0f, r - half - 0.5f);
            inner *= inner;
            Paint paint = new Paint(color);
            for (int y = y0; y <= y1; y++)
            {
                int row = (_height - 1 - y) * _width;
                float dy = y + 0.5f - c.y;
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - c.x;
                    float squared = dx * dx + dy * dy;
                    if (squared > outer || squared < inner)
                    {
                        continue;
                    }
                    Blend(row + x, paint, Mathf.Clamp01(half + 0.5f - Mathf.Abs(Mathf.Sqrt(squared) - r)));
                }
            }
        }

        public void Line(Vector2 from, Vector2 to, float width, Color color)
        {
            Vector2 a = from * _scale;
            Vector2 b = to * _scale;
            float half = width * _scale * 0.5f;
            float abx = b.x - a.x;
            float aby = b.y - a.y;
            float lengthSquared = Mathf.Max(0.0001f, abx * abx + aby * aby);
            float reach = (half + 0.5f) * (half + 0.5f);
            Bounds(Mathf.Min(a.x, b.x) - half - 1f, Mathf.Min(a.y, b.y) - half - 1f, Mathf.Max(a.x, b.x) + half + 1f, Mathf.Max(a.y, b.y) + half + 1f, out int x0, out int y0, out int x1, out int y1);
            Paint paint = new Paint(color);
            for (int y = y0; y <= y1; y++)
            {
                int row = (_height - 1 - y) * _width;
                float py = y + 0.5f;
                for (int x = x0; x <= x1; x++)
                {
                    float px = x + 0.5f;
                    float t = Mathf.Clamp01(((px - a.x) * abx + (py - a.y) * aby) / lengthSquared);
                    float dx = px - (a.x + abx * t);
                    float dy = py - (a.y + aby * t);
                    float squared = dx * dx + dy * dy;
                    if (squared > reach)
                    {
                        continue;
                    }
                    Blend(row + x, paint, Mathf.Clamp01(half + 0.5f - Mathf.Sqrt(squared)));
                }
            }
        }

        private void Bounds(float minX, float minY, float maxX, float maxY, out int x0, out int y0, out int x1, out int y1)
        {
            x0 = Mathf.Max(0, Mathf.FloorToInt(minX));
            x1 = Mathf.Min(_width - 1, Mathf.CeilToInt(maxX));
            y0 = Mathf.Max(0, Mathf.FloorToInt(minY));
            y1 = Mathf.Min(_height - 1, Mathf.CeilToInt(maxY));
        }

        // 색을 정수로 미리 바꿔 두어 픽셀마다 변환하지 않는다.
        private readonly struct Paint
        {
            public readonly int R;
            public readonly int G;
            public readonly int B;
            public readonly float Alpha;

            public Paint(Color color)
            {
                R = (int)(color.r * 255f);
                G = (int)(color.g * 255f);
                B = (int)(color.b * 255f);
                Alpha = color.a;
            }
        }

        // index는 텍스처 배열 위치다. 텍스처의 y는 아래에서 위로 늘어나므로 호출하는 쪽이 위에서부터 센 y를 뒤집어 계산한다.
        private void Blend(int index, in Paint paint, float coverage)
        {
            float alpha = paint.Alpha * coverage;
            if (alpha <= 0f)
            {
                return;
            }

            Color32 below = _pixels[index];
            if (below.a == 255)
            {
                // 아래가 불투명하면 나눗셈 없이 정수로 섞는다. 대부분의 칸이 여기에 해당한다.
                int weight = (int)(alpha * 256f);
                _pixels[index] = new Color32(
                    (byte)(below.r + ((paint.R - below.r) * weight >> 8)),
                    (byte)(below.g + ((paint.G - below.g) * weight >> 8)),
                    (byte)(below.b + ((paint.B - below.b) * weight >> 8)),
                    255);
                return;
            }

            float inverse = 1f - alpha;
            float outAlpha = alpha + below.a / 255f * inverse;
            if (outAlpha <= 0f)
            {
                return;
            }

            float belowWeight = below.a / 255f * inverse;
            float r = (paint.R / 255f * alpha + below.r / 255f * belowWeight) / outAlpha;
            float g = (paint.G / 255f * alpha + below.g / 255f * belowWeight) / outAlpha;
            float b = (paint.B / 255f * alpha + below.b / 255f * belowWeight) / outAlpha;
            _pixels[index] = new Color32((byte)(r * 255f), (byte)(g * 255f), (byte)(b * 255f), (byte)(outAlpha * 255f));
        }
    }
}
