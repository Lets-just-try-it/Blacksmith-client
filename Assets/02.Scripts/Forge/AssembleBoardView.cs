using Blacksmith.Core.Forge;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Forge
{
    // 조립의 방어구 화면. 가죽 작업 매트 위에 오려 낸 조각을 방어구 모양으로 늘어놓고,
    // 이음새의 구멍, 어긋난 채 끌려 다니는 덮는 판, 리벳과 줄어드는 링을 그린다. 마우스를 게임 좌표(칸, 방어구 가운데가 원점, 아래쪽이 +y)로 바꾼다.
    // 규칙은 건드리지 않고 AssembleGame의 값만 읽는다. 글자는 AssembleView가 맡는다.
    public class AssembleBoardView : MonoBehaviour
    {
        private const int Width = 716;
        private const int Height = 452;
        private const float Unit = 36f;
        private const float TextureScale = 2f;
        private const float HoleRadius = 0.3f;
        private const float RingStart = 1.5f;
        private const float RingTarget = 0.55f;

        private static readonly Color Mat = new Color(0.16f, 0.115f, 0.085f);
        private static readonly Color Stitch = new Color(0.78f, 0.62f, 0.38f, 0.8f);
        private static readonly Color Metal = new Color(0.43f, 0.47f, 0.55f);
        private static readonly Color MetalDark = new Color(0.3f, 0.33f, 0.4f);
        private static readonly Color Rim = new Color(0.66f, 0.7f, 0.78f);
        private static readonly Color Shadow = new Color(0.07f, 0.05f, 0.04f);
        private static readonly Color Good = new Color(0.5f, 1f, 0.6f);
        private static readonly Color Warm = new Color(1f, 0.85f, 0.45f);

        [SerializeField] private RawImage _image;

        private PixelCanvas _canvas;
        private AssembleGame _game;
        private RectTransform _rect;
        private float _pulse;

        private void Awake()
        {
            _rect = (RectTransform)_image.transform;
            _canvas = new PixelCanvas(Width, Height, TextureScale);
            _image.texture = _canvas.Texture;
            Clear();
        }

        private void OnDestroy()
        {
            if (_canvas != null)
            {
                Destroy(_canvas.Texture);
            }
        }

        public void Bind(AssembleGame game)
        {
            _game = game;
            BuildBackground();
        }

        public void Unbind()
        {
            _game = null;
            Clear();
        }

        // 화면의 마우스 위치를 방어구 위 게임 좌표(칸)로 바꾼다. 화면 밖이면 false다.
        public bool TryMap(Vector2 screenPoint, out float x, out float y)
        {
            x = -100f;
            y = -100f;
            if (_game == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screenPoint, null, out Vector2 local))
            {
                return false;
            }

            Vector2 offset = local - _rect.rect.center;
            if (Mathf.Abs(offset.x) > Width * 0.5f || Mathf.Abs(offset.y) > Height * 0.5f)
            {
                return false;
            }

            x = offset.x / Unit;
            y = -offset.y / Unit;
            return true;
        }

        // 게임 좌표(칸)를 보드 중심 기준 화면 위치(위쪽이 +)로 바꾼다. 말풍선 위치에 쓴다.
        public Vector2 ToLocal(float x, float y)
        {
            return new Vector2(x * Unit, -y * Unit);
        }

        public void Tick(float deltaTime)
        {
            if (_game == null)
            {
                return;
            }

            _pulse += deltaTime;
            _canvas.RestoreSnapshot();
            DrawSeams();
            DrawCursor();
            _canvas.Apply();
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

        private Vector2 ToCanvas(float x, float y)
        {
            return new Vector2(Width * 0.5f + x * Unit, Height * 0.5f + y * Unit);
        }

        private Rect ToRect(float minX, float minY, float maxX, float maxY)
        {
            Vector2 min = ToCanvas(minX, minY);
            Vector2 max = ToCanvas(maxX, maxY);
            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        // ------------------------------------------------------------------ 바탕과 조각

        // 바뀌지 않는 바탕(가죽 매트, 조각 모양)은 한 번만 그려 저장한다.
        private void BuildBackground()
        {
            _canvas.Clear(Shadow);
            DrawMat();
            foreach (AssembleGame.PartPos part in _game.Layout())
            {
                DrawPart(part);
            }

            _canvas.SaveSnapshot();
        }

        // 가죽 작업 매트: 세로 이음이 있는 갈색 가죽과 가장자리 박음질
        private void DrawMat()
        {
            Rect mat = new Rect(6f, 6f, Width - 12f, Height - 12f);
            RoundFill(mat, 14f, new Color(0.3f, 0.22f, 0.15f));
            RoundFill(new Rect(mat.x + 3f, mat.y + 3f, mat.width - 6f, mat.height - 6f), 12f, Mat);
            for (int i = 1; i < 6; i++)
            {
                float x = mat.x + mat.width * i / 6f;
                _canvas.Fill(new Rect(x, mat.y + 8f, 2f, mat.height - 16f), new Color(0.12f, 0.085f, 0.062f));
            }

            Rect stitch = new Rect(mat.x + 11f, mat.y + 11f, mat.width - 22f, mat.height - 22f);
            for (float x = stitch.xMin; x < stitch.xMax; x += 12f)
            {
                _canvas.Fill(new Rect(x, stitch.yMin, 6f, 2f), Stitch);
                _canvas.Fill(new Rect(x, stitch.yMax - 2f, 6f, 2f), Stitch);
            }

            for (float y = stitch.yMin; y < stitch.yMax; y += 12f)
            {
                _canvas.Fill(new Rect(stitch.xMin, y, 2f, 6f), Stitch);
                _canvas.Fill(new Rect(stitch.xMax - 2f, y, 2f, 6f), Stitch);
            }
        }

        private void DrawPart(AssembleGame.PartPos part)
        {
            float x = part.X;
            float y = part.Y;
            switch (part.Name)
            {
                case "투구 몸통":
                    Plate(x, y + 0.5f, 4.6f, 2.4f, 0.5f, Metal);
                    Dome(x, y - 0.3f, 2.3f);
                    Rib(x, y - 2.5f, x, y + 1.7f);
                    break;
                case "얼굴 가리개":
                    Plate(x, y, 4.4f, 3.2f, 0.8f, Metal);
                    Slot(x, y - 0.5f, 3.2f, 0.36f);
                    for (int i = 0; i < 4; i++)
                    {
                        Hole(x - 0.9f + i * 0.6f, y + 0.8f);
                    }

                    break;
                case "왼 볼가리개":
                case "오른 볼가리개":
                    Plate(x, y, 2.4f, 3.4f, 0.6f, Metal);
                    Rib(x, y - 1.3f, x, y + 1.3f);
                    break;
                case "이마 보강판":
                    Plate(x, y, 4.8f, 1.0f, 0.4f, MetalDark);
                    break;
                case "목 보호대":
                    Lames(x, y, 6.4f, 1.8f, 3);
                    break;
                case "등판":
                    Plate(x, y, 5.6f, 5.0f, 1.2f, MetalDark);
                    break;
                case "옆구리판":
                    Plate(x, y, 1.8f, 3.6f, 0.5f, Metal);
                    Rib(x, y - 1.4f, x, y + 1.4f);
                    break;
                case "허리 치마":
                    Lames(x, y, 6.2f, 2.4f, 3);
                    break;
                case "가슴판":
                    Plate(x, y, 4.8f, 4.4f, 1.3f, Metal);
                    Rib(x, y - 1.7f, x, y + 1.7f);
                    Rib(x - 1.4f, y - 0.4f, x + 1.4f, y - 0.4f);
                    break;
                case "목 가리개":
                    Plate(x, y, 3.4f, 1.2f, 0.5f, Metal);
                    break;
                case "왼 어깨판":
                case "오른 어깨판":
                    Dome(x, y, 1.6f);
                    Rib(x - 0.9f, y + 0.9f, x + 0.9f, y - 0.9f);
                    break;
                default:
                    Plate(x, y, 3.4f, 2.6f, 0.6f, Metal);
                    break;
            }
        }

        // 둥근 판: 그림자, 밝은 테두리, 몸통, 위쪽 빛 반사
        private void Plate(float cx, float cy, float w, float h, float radius, Color body)
        {
            Rect rect = ToRect(cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f);
            float rad = radius * Unit;
            RoundFill(new Rect(rect.x + 4f, rect.y + 5f, rect.width, rect.height), rad, Shadow);
            RoundFill(rect, rad, Rim);
            RoundFill(new Rect(rect.x + 2.5f, rect.y + 2.5f, rect.width - 5f, rect.height - 5f), Mathf.Max(2f, rad - 2.5f), body);
            RoundFill(new Rect(rect.x + 6f, rect.y + 6f, rect.width - 12f, rect.height * 0.28f), Mathf.Max(2f, rad - 6f), Color.Lerp(body, Color.white, 0.18f));
        }

        // 가로 비늘판을 겹겹이 쌓은 모양(목 보호대, 허리 치마)
        private void Lames(float cx, float cy, float w, float h, int count)
        {
            float step = h / count;
            for (int i = 0; i < count; i++)
            {
                float width = w * (1f - 0.08f * i);
                Plate(cx, cy - h * 0.5f + step * (i + 0.5f), width, step + 0.35f, 0.3f, i % 2 == 0 ? Metal : MetalDark);
            }
        }

        private void Dome(float cx, float cy, float radius)
        {
            Vector2 center = ToCanvas(cx, cy);
            float r = radius * Unit;
            _canvas.Circle(center + new Vector2(4f, 5f), r, Shadow);
            _canvas.Circle(center, r, Rim);
            _canvas.Circle(center, r - 3f, Metal);
            _canvas.Circle(center + new Vector2(-r * 0.25f, -r * 0.3f), r * 0.45f, Color.Lerp(Metal, Color.white, 0.2f));
        }

        // 판 위의 도드라진 줄(보강 능선)
        private void Rib(float x0, float y0, float x1, float y1)
        {
            _canvas.Line(ToCanvas(x0, y0) + Vector2.one * 1.5f, ToCanvas(x1, y1) + Vector2.one * 1.5f, 4f, MetalDark);
            _canvas.Line(ToCanvas(x0, y0), ToCanvas(x1, y1), 3f, Rim);
        }

        private void Slot(float cx, float cy, float w, float h)
        {
            Rect rect = ToRect(cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f);
            RoundFill(rect, rect.height * 0.5f, Shadow);
        }

        private void Hole(float cx, float cy)
        {
            _canvas.Circle(ToCanvas(cx, cy), 3f, Shadow);
        }

        // 모서리가 둥근 사각형(불투명). 위·아래 막대와 모서리 원으로 채운다.
        private void RoundFill(Rect rect, float radius, Color color)
        {
            float r = Mathf.Min(radius, rect.width * 0.5f, rect.height * 0.5f);
            _canvas.Fill(new Rect(rect.x + r, rect.y, rect.width - 2f * r, rect.height), color);
            _canvas.Fill(new Rect(rect.x, rect.y + r, rect.width, rect.height - 2f * r), color);
            _canvas.Circle(new Vector2(rect.xMin + r, rect.yMin + r), r, color);
            _canvas.Circle(new Vector2(rect.xMax - r, rect.yMin + r), r, color);
            _canvas.Circle(new Vector2(rect.xMin + r, rect.yMax - r), r, color);
            _canvas.Circle(new Vector2(rect.xMax - r, rect.yMax - r), r, color);
        }

        // ------------------------------------------------------------------ 이음새

        private void DrawSeams()
        {
            for (int i = 0; i < _game.Seams.Count; i++)
            {
                AssembleGame.Seam seam = _game.Seams[i];
                bool active = _game.Phase == AssembleGame.PhaseKind.Work && i == _game.Current;
                if (active)
                {
                    DrawActiveSeam(seam);
                }
                else
                {
                    bool finished = i < _game.Current || _game.Phase == AssembleGame.PhaseKind.Done
                        || (i == _game.Current && _game.Phase == AssembleGame.PhaseKind.SeamDone);
                    DrawIdleSeam(seam, finished);
                }
            }
        }

        // 아직 차례가 아니거나 끝난 이음새: 끝난 곳은 박은 리벳이 보이고, 남은 곳은 흐린 구멍만 보인다.
        private void DrawIdleSeam(AssembleGame.Seam seam, bool finished)
        {
            foreach (AssembleGame.Hole hole in seam.Holes)
            {
                DrawHole(hole, finished ? 1f : 0.45f);
            }
        }

        private void DrawActiveSeam(AssembleGame.Seam seam)
        {
            float pad = 0.15f;
            if (!_game.Locked)
            {
                // 놓아야 할 자리는 점선 테두리와 구멍 고리로, 끌려 다니는 덮는 판은 반투명한 쇠판으로 보여 준다.
                DrawTarget(seam, pad);
                Rect moving = ToRect(seam.MinX + _game.OffsetX, seam.MinY + _game.OffsetY, seam.MaxX + _game.OffsetX, seam.MaxY + _game.OffsetY);
                Color tint = _game.Aligned ? new Color(0.5f, 1f, 0.6f, 0.6f) : new Color(0.78f, 0.82f, 0.92f, 0.6f);
                RoundFill(new Rect(moving.x + 4f, moving.y + 5f, moving.width, moving.height), 10f, new Color(0f, 0f, 0f, 0.35f));
                _canvas.Fill(moving, tint);
                DrawOutline(moving, _game.Aligned ? Good : Rim, 2f);
                foreach (AssembleGame.Hole hole in seam.Holes)
                {
                    Vector2 center = ToCanvas(hole.X + _game.OffsetX, hole.Y + _game.OffsetY);
                    _canvas.Circle(center, HoleRadius * Unit + 2f, Shadow);
                    _canvas.Circle(center, HoleRadius * Unit, new Color(0.04f, 0.04f, 0.05f));
                }

                return;
            }

            // 핀으로 고정된 판: 제자리에 놓인 쇠판 위에 구멍을 박는다.
            Rect strip = ToRect(seam.MinX, seam.MinY, seam.MaxX, seam.MaxY);
            RoundFill(new Rect(strip.x + 4f, strip.y + 5f, strip.width, strip.height), 10f, Shadow);
            RoundFill(strip, 10f, Rim);
            RoundFill(new Rect(strip.x + 2.5f, strip.y + 2.5f, strip.width - 5f, strip.height - 5f), 8f, Metal);
            for (int i = 0; i < seam.Holes.Length; i++)
            {
                DrawHole(seam.Holes[i], 1f);
                if (i == _game.HammerHole && _game.Hammering)
                {
                    DrawRing(seam.Holes[i]);
                }
            }
        }

        // 판이 놓일 자리를 알려 주는 점선 테두리. 구멍마다 고리도 그린다.
        private void DrawTarget(AssembleGame.Seam seam, float pad)
        {
            Rect target = ToRect(seam.MinX - pad, seam.MinY - pad, seam.MaxX + pad, seam.MaxY + pad);
            Color color = _game.Aligned ? Good : new Color(1f, 0.85f, 0.45f, 0.9f);
            float dash = 9f;
            for (float x = target.xMin; x < target.xMax; x += dash * 2f)
            {
                _canvas.Fill(new Rect(x, target.yMin, Mathf.Min(dash, target.xMax - x), 2f), color);
                _canvas.Fill(new Rect(x, target.yMax - 2f, Mathf.Min(dash, target.xMax - x), 2f), color);
            }

            for (float y = target.yMin; y < target.yMax; y += dash * 2f)
            {
                _canvas.Fill(new Rect(target.xMin, y, 2f, Mathf.Min(dash, target.yMax - y)), color);
                _canvas.Fill(new Rect(target.xMax - 2f, y, 2f, Mathf.Min(dash, target.yMax - y)), color);
            }

            foreach (AssembleGame.Hole hole in seam.Holes)
            {
                _canvas.Ring(ToCanvas(hole.X, hole.Y), HoleRadius * Unit + 4f, color, 2f);
            }
        }

        // 빈 구멍은 어둡고, 리벳을 끼운 구멍은 밝은 머리가 보인다. 박은 구멍은 품질이 좋을수록 초록빛이 돈다.
        private void DrawHole(AssembleGame.Hole hole, float alpha)
        {
            Vector2 center = ToCanvas(hole.X, hole.Y);
            float radius = HoleRadius * Unit;
            if (hole.State == 2)
            {
                Color head = Color.Lerp(new Color(0.82f, 0.84f, 0.9f), Good, hole.Quality);
                _canvas.Circle(center + new Vector2(1.5f, 2f), radius + 2f, Shadow);
                _canvas.Circle(center, radius + 1.5f, MetalDark);
                _canvas.Circle(center, radius, head);
                _canvas.Circle(center + new Vector2(-radius * 0.3f, -radius * 0.3f), radius * 0.4f, new Color(1f, 1f, 1f, 0.85f));
                return;
            }

            if (hole.State == 1)
            {
                _canvas.Circle(center, radius + 2f, Shadow);
                _canvas.Circle(center, radius, Warm);
                _canvas.Circle(center + new Vector2(-radius * 0.3f, -radius * 0.3f), radius * 0.35f, new Color(1f, 1f, 0.9f));
                return;
            }

            _canvas.Circle(center, radius + 2f, new Color(Rim.r, Rim.g, Rim.b, alpha));
            _canvas.Circle(center, radius, new Color(0.04f, 0.04f, 0.05f, 1f));
        }

        // 안쪽 원은 목표, 바깥 링은 줄어든다. 링이 안쪽 원에 겹칠 때 누른다.
        private void DrawRing(AssembleGame.Hole hole)
        {
            Vector2 center = ToCanvas(hole.X, hole.Y);
            float target = RingTarget * Unit;
            _canvas.Ring(center, target, new Color(0.6f, 1f, 0.7f, 0.95f), 3f);
            if (_game.RingTime < 0f)
            {
                _canvas.Ring(center, RingStart * Unit, new Color(1f, 1f, 1f, 0.2f), 2f);
                return;
            }

            float t = Mathf.Clamp01(_game.RingTime / _game.RingDuration);
            float radius = Mathf.Max(0.1f, Mathf.Lerp(RingStart, RingTarget, _game.RingTime / _game.RingDuration)) * Unit;
            bool near = Mathf.Abs(_game.RingTime - _game.RingDuration) <= _game.GoodWindow;
            _canvas.Ring(center, radius, near ? Warm : new Color(1f, 1f, 1f, 0.5f + 0.4f * t), near ? 5f : 3f);
        }

        // 판을 맞추는 동안은 손 모양 점, 리벳을 박는 동안은 망치를 마우스 끝에 보여 준다.
        private void DrawCursor()
        {
            if (_game.PointerX < -50f)
            {
                return;
            }

            Vector2 pointer = ToCanvas(_game.PointerX, _game.PointerY);
            if (_game.Locked && _game.Phase == AssembleGame.PhaseKind.Work)
            {
                float swing = _game.Hammering && _game.RingTime >= 0f ? Mathf.Clamp01(1f - Mathf.Abs(_game.RingTime - _game.RingDuration) / 0.3f) : 0f;
                Vector2 head = pointer + new Vector2(8f, -8f);
                Vector2 tip = head + new Vector2(26f, -26f) * (1f + 0.15f * swing);
                _canvas.Line(head, tip, 6f, new Color(0.35f, 0.22f, 0.12f));
                _canvas.Line(head + new Vector2(-9f, -9f), head + new Vector2(9f, 9f), 12f, new Color(0.22f, 0.22f, 0.27f));
                _canvas.Line(head + new Vector2(-8f, -8f), head + new Vector2(8f, 8f), 5f, new Color(0.62f, 0.64f, 0.72f));
                _canvas.Circle(pointer, 3f, Warm);
                return;
            }

            Color color = _game.Dragging ? Warm : new Color(0.85f, 0.88f, 0.95f);
            _canvas.Circle(pointer, 7f + (_game.Dragging ? 1.5f * Mathf.Sin(_pulse * 12f) : 0f), new Color(0f, 0f, 0f, 0.35f));
            _canvas.Circle(pointer, 5f, color);
        }

        private void DrawOutline(Rect rect, Color color, float width)
        {
            _canvas.Fill(new Rect(rect.xMin, rect.yMin, rect.width, width), color);
            _canvas.Fill(new Rect(rect.xMin, rect.yMax - width, rect.width, width), color);
            _canvas.Fill(new Rect(rect.xMin, rect.yMin, width, rect.height), color);
            _canvas.Fill(new Rect(rect.xMax - width, rect.yMin, width, rect.height), color);
        }
    }
}
