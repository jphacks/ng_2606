using System;
using DeskStage.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.UI
{
    /// <summary>
    /// スタートとゴールの目印（C の CourseMarkers）の上に、「START」「GOAL」の札を出す（Step 17 の、目印の見た目の差し替え）。
    /// 札は画面の上の部品で、3D の目印の位置に毎フレーム合わせる。遠くから見ても読めるよう、大きさは変えない。
    /// 目印は「StageRoot / CourseMarkers / Start・Goal」の名前で探す（C のコードは直接呼ばない）。
    /// </summary>
    public class CourseLabelsView : MonoBehaviour
    {
        public const string StartText = "START";
        public const string GoalText = "GOAL";

        static readonly Color StartColor = new Color(0.13f, 0.71f, 0.45f);
        static readonly Color GoalColor = new Color(0.94f, 0.29f, 0.42f);

        const float PillWidth = 250f;
        const float PillHeight = 92f;
        const float StemHeight = 34f;
        const float FontSize = 52f;

        // 札を指す点の、目印の足元からの高さ（m）。ゴールは旗（高さ 3cm）の上
        const float StartLift = 0.012f;
        const float GoalLift = 0.034f;

        [SerializeField] TMP_FontAsset font;
        [SerializeField] Sprite rounded;

        Func<bool> showStart;
        Func<bool> showGoal;

        RectTransform start;
        RectTransform goal;

        /// <summary>
        /// 文字のフォント、札の丸い板の絵、それぞれの札を出すかを渡す。
        /// 出すかが null なら、目印があればいつも出す（あそぶ画面）。たしかめ画面では、まだ置いていない目印の札を出さないために使う。
        /// </summary>
        public void Set(TMP_FontAsset fontAsset, Sprite roundedSprite, Func<bool> startPlaced = null, Func<bool> goalPlaced = null)
        {
            font = fontAsset;
            rounded = roundedSprite;
            showStart = startPlaced;
            showGoal = goalPlaced;
        }

        void LateUpdate()
        {
            if (start == null)
            {
                start = Create("StartLabel", StartText, StartColor);
                goal = Create("GoalLabel", GoalText, GoalColor);
            }
            var markers = StageContext.StageRoot != null ? StageContext.StageRoot.Find("CourseMarkers") : null;
            Follow(start, markers != null ? markers.Find("Start") : null, StartLift, showStart);
            Follow(goal, markers != null ? markers.Find("Goal") : null, GoalLift, showGoal);
        }

        void Follow(RectTransform label, Transform marker, float lift, Func<bool> show)
        {
            var cam = Camera.main;
            var visible = marker != null && cam != null && (show == null || show());
            if (visible)
            {
                var screen = cam.WorldToScreenPoint(marker.position + marker.up * lift);
                visible = screen.z > 0f;
                if (visible)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screen, null, out var local);
                    label.anchoredPosition = local;
                }
            }
            label.gameObject.SetActive(visible);
        }

        // 下向きの細い棒の先が、目印を指す。札は、その上に乗る
        RectTransform Create(string name, string text, Color color)
        {
            var root = NewRect(name, transform);
            root.sizeDelta = new Vector2(PillWidth, PillHeight + StemHeight);
            root.pivot = new Vector2(0.5f, 0f);

            var stem = NewRect("Stem", root);
            stem.anchorMin = stem.anchorMax = new Vector2(0.5f, 0f);
            stem.pivot = new Vector2(0.5f, 0f);
            stem.sizeDelta = new Vector2(10f, StemHeight + 10f);
            var stemImage = stem.gameObject.AddComponent<Image>();
            stemImage.color = color;
            stemImage.raycastTarget = false;

            var pill = NewRect("Pill", root);
            pill.anchorMin = new Vector2(0f, 1f);
            pill.anchorMax = new Vector2(1f, 1f);
            pill.pivot = new Vector2(0.5f, 1f);
            pill.sizeDelta = new Vector2(0f, PillHeight);
            var back = pill.gameObject.AddComponent<Image>();
            if (rounded != null)
            {
                back.sprite = rounded;
                back.type = Image.Type.Sliced;
            }
            back.color = color;
            back.raycastTarget = false;
            var outline = pill.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(4f, -4f);

            var label = NewRect("Text", pill);
            label.anchorMin = Vector2.zero;
            label.anchorMax = Vector2.one;
            label.offsetMin = label.offsetMax = Vector2.zero;
            var tmp = label.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                tmp.font = font;
            }
            tmp.text = text;
            tmp.fontSize = FontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.characterSpacing = 4f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            root.gameObject.SetActive(false);
            return root;
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
    }
}
