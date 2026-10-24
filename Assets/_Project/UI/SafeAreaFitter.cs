using UnityEngine;

namespace DeskStage.UI
{
    /// <summary>
    /// 端末の安全な範囲（角の丸み、切り欠き）に合わせて、このパネルの大きさを変える。
    /// 子どもが触る部品はすべてこのパネルの中に置く。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        RectTransform rect;
        Rect appliedArea;
        Vector2Int appliedSize;

        void OnEnable()
        {
            rect = (RectTransform)transform;
            Apply();
        }

        void Update()
        {
            // 向きや解像度が変わったときだけ計算し直す
            if (appliedArea != Screen.safeArea || appliedSize.x != Screen.width || appliedSize.y != Screen.height)
            {
                Apply();
            }
        }

        void Apply()
        {
            var area = Screen.safeArea;
            appliedArea = area;
            appliedSize = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }
            rect.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            rect.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
