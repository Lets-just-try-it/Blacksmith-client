using Blacksmith.Core.Forge;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Forge
{
    // 판 성형의 쇠판 화면. 쇠판에 그려진 조각 외곽선(허용 띠)을 보여 주고 마우스를 게임 좌표로 바꾼다.
    // 녹색 점이 오린 끝이고, 오린 자국은 주황 선이다. 붉은 구간은 불순물이라 띠가 좁다.
    // 규칙은 건드리지 않고 PlateGame의 값만 읽는다. 글자는 PlateView가 맡는다.
    public class PlateBoardView : MonoBehaviour
    {
        private const int Margin = 6;
        private const float Unit = 44f;
        private const float TextureScale = 2f;

        private static readonly Color Frame = new Color(0.03f, 0.03f, 0.05f, 1f);
        private static readonly Color Hot = new Color(1f, 0.85f, 0.45f);
        private static readonly Color Done = new Color(0.5f, 1f, 0.6f);
        private static readonly Color Bad = new Color(1f, 0.4f, 0.35f);

        [SerializeField] private RawImage _image;

        private PixelCanvas _canvas;
        private PlateGame _game;
        private RectTransform _rect;
        private float _pulse;
        // 바뀌지 않는 바탕(쇠판, 허용 띠, 밑그림 점선)을 저장해 둔 조각과 상태. 조각이 바뀔 때만 다시 그린다.
        private PlateGame.Part _cachedPart;
        private bool _cachedDone;
        private bool _hasCache;

        private float BoardWidth => PlateGame.Width * Unit;
        private float BoardHeight => PlateGame.Height * Unit;

        private void Awake()
        {
            _rect = (RectTransform)_image.transform;
            _canvas = new PixelCanvas(Mathf.RoundToInt(BoardWidth) + Margin * 2, Mathf.RoundToInt(BoardHeight) + Margin * 2, TextureScale);
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

        public void Bind(PlateGame game)
        {
            _game = game;
            _hasCache = false;
        }

        public void Unbind()
        {
            _game = null;
            _hasCache = false;
            _cachedPart = null;
            Clear();
        }

        // 화면의 마우스 위치를 쇠판 위 게임 좌표(칸)로 바꾼다. 쇠판 밖이면 false다.
        public bool TryMap(Vector2 screenPoint, out float x, out float y)
        {
            x = -100f;
            y = -100f;
            if (_game == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screenPoint, null, out Vector2 local))
            {
                return false;
            }

            Vector2 offset = local - _rect.rect.center;
            if (Mathf.Abs(offset.x) > BoardWidth * 0.5f || Mathf.Abs(offset.y) > BoardHeight * 0.5f)
            {
                return false;
            }

            x = offset.x / Unit + PlateGame.Width * 0.5f;
            y = -offset.y / Unit + PlateGame.Height * 0.5f;
            return true;
        }

        // 게임 좌표(칸)를 보드 중심 기준 화면 위치(위쪽이 +)로 바꾼다. 말풍선 위치에 쓴다.
        public Vector2 ToLocal(float x, float y)
        {
            return new Vector2((x - PlateGame.Width * 0.5f) * Unit, -(y - PlateGame.Height * 0.5f) * Unit);
        }

        public void Tick(float deltaTime)
        {
            if (_game == null)
            {
                return;
            }

            _pulse += deltaTime;
            PlateGame.Part part = _game.Phase != PlateGame.PhaseKind.Done ? _game.CurrentPart : null;
            if (!_hasCache || part != _cachedPart || (part != null && part.Done != _cachedDone))
            {
                BuildBackground(part);
            }

            _canvas.RestoreSnapshot();
            if (part != null)
            {
                DrawProgress(part);
            }

            DrawCursor(part);
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
            return new Vector2(Margin + x * Unit, Margin + y * Unit);
        }

        // ------------------------------------------------------------------ 쇠판

        private void BuildBackground(PlateGame.Part part)
        {
            _canvas.Clear(Frame);
            DrawBoard();
            if (part != null)
            {
                DrawBand(part);
            }

            _canvas.SaveSnapshot();
            _cachedPart = part;
            _cachedDone = part != null && part.Done;
            _hasCache = true;
        }

        private void DrawBoard()
        {
            Rect board = new Rect(Margin, Margin, BoardWidth, BoardHeight);
            // 쇠판: 가로 결이 있는 철판
            const int rows = 20;
            for (int i = 0; i < rows; i++)
            {
                float k = 0.46f + 0.035f * Mathf.Sin(i * 1.9f) + 0.015f * (i % 2);
                _canvas.Fill(new Rect(board.x, board.y + board.height * i / rows, board.width, board.height / rows + 1f), new Color(k, k + 0.02f, k + 0.07f));
            }

            Outline(board, new Color(1f, 1f, 1f, 0.5f), 2f);
            DrawRivets(board);
        }

        private void DrawRivets(Rect board)
        {
            foreach (Vector2 corner in new[] { new Vector2(14f, 14f), new Vector2(board.width - 14f, 14f), new Vector2(14f, board.height - 14f), new Vector2(board.width - 14f, board.height - 14f) })
            {
                _canvas.Circle(board.position + corner, 5f, new Color(0.3f, 0.32f, 0.38f));
                _canvas.Circle(board.position + corner + new Vector2(-1f, -1f), 2f, new Color(0.7f, 0.72f, 0.8f));
            }
        }

        // 허용 띠(홈)와 밑그림 점선. 조각이 같으면 그대로라 한 번만 그려 저장한다.
        // 불순물 구간은 붉고 좁다. 점마다 원을 그리지 않고 굵은 선으로 잇는다.
        private void DrawBand(PlateGame.Part part)
        {
            for (int i = 0; i + 2 < part.N; i += 2)
            {
                Vector2 from = ToCanvas(part.X[i], part.Y[i]);
                Vector2 to = ToCanvas(part.X[i + 2], part.Y[i + 2]);
                float width = part.Tol[i] * Unit * 2f;
                Color outer = part.Narrow[i] ? new Color(0.42f, 0.2f, 0.17f) : new Color(0.33f, 0.36f, 0.44f);
                Color inner = part.Narrow[i] ? new Color(0.5f, 0.26f, 0.2f) : new Color(0.4f, 0.43f, 0.52f);
                if (part.Done)
                {
                    outer = new Color(0.25f, 0.45f, 0.3f);
                    inner = new Color(0.32f, 0.55f, 0.38f);
                }

                _canvas.Line(from, to, width, outer);
                _canvas.Line(from, to, Mathf.Max(4f, width - 6f), inner);
            }

            // 밑그림 점선. 오린 자국이 위를 덮으므로 처음부터 끝까지 그려 둔다.
            for (int i = 0; i < part.N; i += 3)
            {
                _canvas.Circle(ToCanvas(part.X[i], part.Y[i]), 1.6f, new Color(1f, 1f, 1f, 0.85f));
            }
        }

        // 매 프레임 바뀌는 부분: 길 안내 화살표, 오린 자국, 오린 끝.
        private void DrawProgress(PlateGame.Part part)
        {
            if (!part.Done)
            {
                for (int k = 1; k <= 3; k++)
                {
                    int index = part.Head + k * 28;
                    if (index + 3 >= part.N)
                    {
                        break;
                    }

                    DrawArrow(part, index);
                }
            }

            // 오린 자국: 달궈진 주황 선. 선에 가까울수록 밝다.
            for (int i = 1; i <= part.Head; i++)
            {
                Color color = part.Done ? Done : Color.Lerp(new Color(1f, 0.85f, 0.45f), new Color(0.95f, 0.45f, 0.25f), part.Err[i]);
                _canvas.Line(ToCanvas(part.X[i - 1], part.Y[i - 1]), ToCanvas(part.X[i], part.Y[i]), 4f, color);
            }

            if (!part.Done)
            {
                DrawHead(part);
            }
        }

        private void DrawArrow(PlateGame.Part part, int index)
        {
            Vector2 a = ToCanvas(part.X[index - 3], part.Y[index - 3]);
            Vector2 b = ToCanvas(part.X[index + 3], part.Y[index + 3]);
            Vector2 direction = (b - a).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x);
            Vector2 center = ToCanvas(part.X[index], part.Y[index]);
            Color color = new Color(0.7f, 1f, 0.75f, 0.9f);
            _canvas.Line(center - direction * 5f + normal * 6f, center + direction * 4f, 3f, color);
            _canvas.Line(center - direction * 5f - normal * 6f, center + direction * 4f, 3f, color);
        }

        // 녹색 점: 오린 끝. 쥐고 있지 않으면 맥박처럼 커졌다 작아진다.
        private void DrawHead(PlateGame.Part part)
        {
            Vector2 head = ToCanvas(part.X[part.Head], part.Y[part.Head]);
            if (!_game.Cutting)
            {
                float beat = 1f + 0.18f * Mathf.Sin(_pulse * 6f);
                _canvas.Circle(head, 13f * beat, new Color(0.35f, 1f, 0.5f, 0.35f));
                _canvas.Circle(head, 8f, new Color(0.4f, 1f, 0.55f));
                _canvas.Ring(head, 20f * beat, new Color(0.5f, 1f, 0.6f, 0.9f));
                return;
            }

            _canvas.Circle(head, 7f, _game.OffBand ? Bad : new Color(1f, 0.95f, 0.7f));
            for (int k = 0; k < 6; k++)
            {
                float phase = Mathf.Repeat(_pulse * 3f + k * 0.37f, 1f);
                float angle = k * 2.4f + Mathf.Floor(_pulse * 3f + k * 0.37f) * 1.7f;
                Vector2 position = head + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (6f + phase * 18f);
                _canvas.Circle(position, 3f * (1f - phase) + 1f, new Color(1f, 0.8f - 0.4f * phase, 0.3f, 1f - phase));
            }
        }

        // 띠를 벗어났으면 돌아갈 곳(녹색 점)을 가리키는 붉은 점선을 보여 준다.
        private void DrawCursor(PlateGame.Part part)
        {
            if (_game.PointerX < -50f)
            {
                return;
            }

            Vector2 pointer = ToCanvas(_game.PointerX, _game.PointerY);
            if (_game.Cutting && _game.OffBand && part != null)
            {
                Vector2 head = ToCanvas(part.X[part.Head], part.Y[part.Head]);
                Vector2 delta = head - pointer;
                float length = delta.magnitude;
                for (float t = 0f; t < length; t += 14f)
                {
                    _canvas.Circle(pointer + delta / length * t, 3f, new Color(1f, 0.4f, 0.35f, 0.8f));
                }

                _canvas.Ring(head, 22f + 4f * Mathf.Sin(_pulse * 10f), new Color(1f, 0.45f, 0.4f, 0.9f));
            }

            Color body = new Color(0.22f, 0.22f, 0.27f);
            _canvas.Line(pointer + new Vector2(6f, -6f), pointer + new Vector2(30f, -30f), 7f, body);
            _canvas.Line(pointer + new Vector2(6f, -6f), pointer + new Vector2(30f, -30f), 3f, new Color(0.55f, 0.55f, 0.62f));
            _canvas.Circle(pointer, 5f, _game.Cutting ? (_game.OffBand ? Bad : Hot) : new Color(0.85f, 0.88f, 0.95f));
        }

        private void Outline(Rect rect, Color color, float width)
        {
            _canvas.Fill(new Rect(rect.xMin, rect.yMin, rect.width, width), color);
            _canvas.Fill(new Rect(rect.xMin, rect.yMax - width, rect.width, width), color);
            _canvas.Fill(new Rect(rect.xMin, rect.yMin, width, rect.height), color);
            _canvas.Fill(new Rect(rect.xMax - width, rect.yMin, width, rect.height), color);
        }
    }
}
