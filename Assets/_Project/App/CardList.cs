using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.App
{
    /// <summary>横にスクロールするカードの列（ホームと同じ作り）。おだいの一覧で使う。</summary>
    public static class CardList
    {
        /// <summary>area の中に、横スクロールの列を作り、カードを入れる先（content）を返す。</summary>
        public static RectTransform Create(RectTransform area, float spacing)
        {
            var scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;
            area.gameObject.AddComponent<RectMask2D>().padding = new Vector4(-20f, -50f, -20f, -30f);
            area.gameObject.AddComponent<Image>().color = Color.clear;

            var content = UiKit.NewRect("Content", area);
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset((int)Theme.Margin, (int)Theme.Margin, 24, 40);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            content.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            return content;
        }
    }
}
