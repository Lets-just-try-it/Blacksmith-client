using UnityEngine;

namespace Blacksmith.Forge
{
    // PixelCanvas에 자주 쓰는 도형(둥근 사각형, 테두리, 점선 사각형)을 그리는 도구. 모두 논리 좌표를 쓴다.
    public static class PixelShapes
    {
        // 모서리가 둥근 사각형(불투명). 가로·세로 막대와 모서리 원으로 채운다.
        public static void RoundFill(PixelCanvas canvas, Rect rect, float radius, Color color)
        {
            float r = Mathf.Min(radius, rect.width * 0.5f, rect.height * 0.5f);
            canvas.Fill(new Rect(rect.x + r, rect.y, rect.width - 2f * r, rect.height), color);
            canvas.Fill(new Rect(rect.x, rect.y + r, rect.width, rect.height - 2f * r), color);
            canvas.Circle(new Vector2(rect.xMin + r, rect.yMin + r), r, color);
            canvas.Circle(new Vector2(rect.xMax - r, rect.yMin + r), r, color);
            canvas.Circle(new Vector2(rect.xMin + r, rect.yMax - r), r, color);
            canvas.Circle(new Vector2(rect.xMax - r, rect.yMax - r), r, color);
        }

        public static void Outline(PixelCanvas canvas, Rect rect, Color color, float width)
        {
            canvas.Fill(new Rect(rect.xMin, rect.yMin, rect.width, width), color);
            canvas.Fill(new Rect(rect.xMin, rect.yMax - width, rect.width, width), color);
            canvas.Fill(new Rect(rect.xMin, rect.yMin, width, rect.height), color);
            canvas.Fill(new Rect(rect.xMax - width, rect.yMin, width, rect.height), color);
        }

        public static void DashedOutline(PixelCanvas canvas, Rect rect, Color color, float dash, float width)
        {
            for (float x = rect.xMin; x < rect.xMax; x += dash * 2f)
            {
                canvas.Fill(new Rect(x, rect.yMin, Mathf.Min(dash, rect.xMax - x), width), color);
                canvas.Fill(new Rect(x, rect.yMax - width, Mathf.Min(dash, rect.xMax - x), width), color);
            }

            for (float y = rect.yMin; y < rect.yMax; y += dash * 2f)
            {
                canvas.Fill(new Rect(rect.xMin, y, width, Mathf.Min(dash, rect.yMax - y)), color);
                canvas.Fill(new Rect(rect.xMax - width, y, width, Mathf.Min(dash, rect.yMax - y)), color);
            }
        }

        // 판 모양: 그림자, 밝은 테두리, 몸통, 위쪽 빛 반사
        public static void Plate(PixelCanvas canvas, Rect rect, float radius, Color body, Color rim, Color shadow)
        {
            RoundFill(canvas, new Rect(rect.x + 4f, rect.y + 5f, rect.width, rect.height), radius, shadow);
            RoundFill(canvas, rect, radius, rim);
            RoundFill(canvas, new Rect(rect.x + 2.5f, rect.y + 2.5f, rect.width - 5f, rect.height - 5f), Mathf.Max(2f, radius - 2.5f), body);
        }
    }
}
