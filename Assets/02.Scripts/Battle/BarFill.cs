using UnityEngine;

namespace Blacksmith.Battle
{
    // 바의 Fill 오른쪽 앵커를 비율만큼 줄인다.
    // Image Filled는 둥근 스프라이트를 늘려 끝이 번져 보이므로 쓰지 않는다.
    public static class BarFill
    {
        public static void SetRatio(RectTransform fill, float ratio)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), fill.anchorMax.y);
        }
    }
}
