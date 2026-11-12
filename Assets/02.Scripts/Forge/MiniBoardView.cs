using Blacksmith.Core.Forge;
using UnityEngine;
using UnityEngine.UI;

namespace Blacksmith.Forge
{
    // 미니게임 화면 가운데의 그림판. PixelCanvas 하나에 매 프레임 그리고, 마우스를 게임 좌표(칸, 가운데가 원점, 아래쪽이 +y)로 바꾼다.
    // 규칙은 건드리지 않고 게임의 값만 읽는다. 글자는 MiniPanelView가 맡는다.
    public abstract class MiniBoardView : MonoBehaviour
    {
        protected const int Width = 716;
        protected const int Height = 452;
        protected const float Unit = 36f;
        private const float TextureScale = 2f;

        [SerializeField] private RawImage _image;

        private RectTransform _rect;
        private bool _bound;

        protected PixelCanvas Canvas { get; private set; }
        protected float Pulse { get; private set; }
        // 판 밖에 있는 마우스도 좌표로 바꿀지. 마우스를 어디에 두어도 되는 미니게임이 끈다.
        protected virtual bool ClampToBoard => true;

        private void Awake()
        {
            _rect = (RectTransform)_image.transform;
            Canvas = new PixelCanvas(Width, Height, TextureScale);
            _image.texture = Canvas.Texture;
            ClearCanvas();
        }

        private void OnDestroy()
        {
            if (Canvas != null)
            {
                Destroy(Canvas.Texture);
            }
        }

        public void Bind(MiniGame game)
        {
            OnBind(game);
            _bound = true;
        }

        public void Unbind()
        {
            _bound = false;
            OnUnbind();
            ClearCanvas();
        }

        public void Tick(float deltaTime)
        {
            if (!_bound)
            {
                return;
            }

            Pulse += deltaTime;
            Draw();
            Canvas.Apply();
        }

        // 화면의 마우스 위치를 게임 좌표(칸)로 바꾼다. 그림판 밖이면 false다.
        public bool TryMap(Vector2 screenPoint, out float x, out float y)
        {
            x = -100f;
            y = -100f;
            if (!_bound || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screenPoint, null, out Vector2 local))
            {
                return false;
            }

            Vector2 offset = local - _rect.rect.center;
            if (ClampToBoard && (Mathf.Abs(offset.x) > Width * 0.5f || Mathf.Abs(offset.y) > Height * 0.5f))
            {
                return false;
            }

            x = offset.x / Unit;
            y = -offset.y / Unit;
            return true;
        }

        // 게임 좌표(칸)를 화면 픽셀 좌표(왼쪽 아래가 원점)로 바꾼다. 3D 장면의 물체를 그림판의 같은 자리에 놓을 때 쓴다.
        public Vector2 CellToScreen(float x, float y)
        {
            Vector2 local = ToLocal(x, y);
            Vector3 world = _rect.TransformPoint(new Vector3(_rect.rect.center.x + local.x, _rect.rect.center.y + local.y, 0f));
            return RectTransformUtility.WorldToScreenPoint(null, world);
        }

        // 게임 좌표(칸)를 그림판 중심 기준 화면 위치(위쪽이 +)로 바꾼다. 말풍선 위치에 쓴다.
        public Vector2 ToLocal(float x, float y)
        {
            return new Vector2(x * Unit, -y * Unit);
        }

        protected Vector2 ToCanvas(float x, float y)
        {
            return new Vector2(Width * 0.5f + x * Unit, Height * 0.5f + y * Unit);
        }

        protected abstract void OnBind(MiniGame game);

        protected virtual void OnUnbind()
        {
        }

        protected abstract void Draw();

        private void ClearCanvas()
        {
            if (Canvas == null)
            {
                return;
            }

            Canvas.Clear(Color.clear);
            Canvas.Apply();
        }
    }
}
