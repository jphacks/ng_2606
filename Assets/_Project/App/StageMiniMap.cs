using DeskStage.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.App
{
    /// <summary>
    /// 写真の代わりに、ステージを上から見た図（Docs/specs/home-modes の REQ-13）。机の上に、箱の四角を描く。
    /// 高い箱ほど濃い色、しかけの箱はしかけの色（水は青、炎は赤、回復は緑）、坂は明るい色。スタートは緑の丸、ゴールは黄色の丸。
    /// </summary>
    public static class StageMiniMap
    {
        static readonly Color DeskColor = Theme.Hex("F3DDBB");
        static readonly Color LowBox = Theme.Hex("9CC3F2");
        static readonly Color HighBox = Theme.Hex("2F5FA8");

        public static void Draw(RectTransform area, StageData stage)
        {
            if (stage == null || stage.desk == null || stage.desk.width <= 0f || stage.desk.depth <= 0f)
            {
                return;
            }
            var size = area.rect.size;
            if (size.x <= 0f || size.y <= 0f)
            {
                size = area.sizeDelta;
            }
            // 机の縦横の比を保ったまま、枠に収める
            var scale = Mathf.Min(size.x / stage.desk.width, size.y / stage.desk.depth) * 0.92f;
            var desk = UiKit.Panel(area, "Desk", DeskColor, 18f);
            desk.raycastTarget = false;
            Center(desk.rectTransform, Vector2.zero, new Vector2(stage.desk.width, stage.desk.depth) * scale, 0f);

            foreach (var b in stage.boxes ?? new BoxData[0])
            {
                var color = Color.Lerp(LowBox, HighBox, Mathf.Clamp01(b.size[1] / 0.3f));
                switch (b.gimmick)
                {
                    case BoxData.GimmickWater: color = Theme.Blue; break;
                    case BoxData.GimmickFire: color = Theme.Red; break;
                    case BoxData.GimmickHeal: color = Theme.Green; break;
                }
                if (b.ramp)
                {
                    color = Color.Lerp(color, Color.white, 0.35f);
                }
                var rect = UiKit.Panel(desk.rectTransform, "Box", color, 6f);
                rect.raycastTarget = false;
                // ステージの +z（奥）は、図の上。Unity の Y 軸まわりの回転は、上から見て時計回り
                Center(rect.rectTransform, new Vector2(b.center[0], b.center[2]) * scale, new Vector2(b.size[0], b.size[2]) * scale, -b.yawDegrees);
            }
            Dot(desk.rectTransform, stage.spawn != null ? stage.spawn.position : null, Theme.Green, scale);
            Dot(desk.rectTransform, stage.goal != null ? stage.goal.position : null, Theme.Orange, scale);
        }

        static void Dot(RectTransform parent, float[] position, Color color, float scale)
        {
            if (position == null || position.Length < 3)
            {
                return;
            }
            var dot = UiKit.Disc(parent, "Dot", color, 34f);
            dot.raycastTarget = false;
            Center(dot.rectTransform, new Vector2(position[0], position[2]) * scale, new Vector2(34f, 34f), 0f);
        }

        static void Center(RectTransform r, Vector2 position, Vector2 size, float degrees)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = position;
            r.sizeDelta = size;
            r.localRotation = Quaternion.Euler(0f, 0f, degrees);
        }
    }
}
