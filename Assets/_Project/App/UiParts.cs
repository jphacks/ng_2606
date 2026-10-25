using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeskStage.App
{
    /// <summary>画面の場所を指で押したとき、その画面上の位置を渡す透明な板（コースを置く場所を選ぶときなど）。</summary>
    public class TapCatcher : MonoBehaviour, IPointerClickHandler
    {
        public Action<Vector2> Tapped;

        public void OnPointerClick(PointerEventData e)
        {
            if (!e.dragging)
            {
                Tapped?.Invoke(e.position);
            }
        }
    }

    /// <summary>文字（Label）が出ている間だけ、後ろの板（Image）を出す。文字は、別の部品（AssistController など）が出し入れする。</summary>
    [RequireComponent(typeof(Image))]
    public class PillFollow : MonoBehaviour
    {
        public TMP_Text Label;
        Image image;

        void Update()
        {
            if (image == null) image = GetComponent<Image>();
            if (Label != null) image.enabled = Label.gameObject.activeSelf;
        }
    }

    /// <summary>RawImage の絵を、枠いっぱいに（はみ出す分は切って）出す。枠の大きさが変わるたびに、uvRect を計算し直す。</summary>
    [RequireComponent(typeof(RawImage))]
    public class CoverUv : MonoBehaviour
    {
        RawImage raw;

        void OnEnable() => Apply();

        void OnRectTransformDimensionsChange() => Apply();

        // 最初のレイアウトの前や、画面を出した直後は、枠の大きさが決まっていないことがあるので、毎フレーム見て、変わっていたら計算し直す
        Vector2 lastSize;
        Texture lastTexture;

        void LateUpdate()
        {
            if (raw == null) raw = GetComponent<RawImage>();
            var size = ((RectTransform)transform).rect.size;
            if (size != lastSize || raw.texture != lastTexture)
            {
                Apply();
            }
        }

        public void Apply()
        {
            if (raw == null) raw = GetComponent<RawImage>();
            var texture = raw.texture;
            var rect = ((RectTransform)transform).rect;
            if (texture == null || texture.height <= 0 || rect.height <= 0f)
            {
                return;
            }
            lastSize = rect.size;
            lastTexture = texture;
            var frame = rect.width / rect.height;
            var photo = (float)texture.width / texture.height;
            if (photo > frame)
            {
                var w = frame / photo;
                raw.uvRect = new Rect((1f - w) * 0.5f, 0f, w, 1f);
            }
            else
            {
                var h = photo / frame;
                raw.uvRect = new Rect(0f, (1f - h) * 0.5f, 1f, h);
            }
        }
    }

    /// <summary>ハートの列（体力）。Set で、いくつ満ちているかを決める。ハートは、2 つの丸と、ひし形で描く。</summary>
    public class HeartRow
    {
        static readonly Color Full = Theme.Hex("EF4B6C");
        static readonly Color Empty = Theme.Hex("6B7596"); // 透けない色（重なる 3 つの図形の境目が、見えないように）

        public RectTransform Rect;
        readonly System.Collections.Generic.List<Image[]> hearts = new System.Collections.Generic.List<Image[]>();

        public static HeartRow Create(Transform parent, string name, float size, int count = PlayerHealth.Max)
        {
            var row = new HeartRow();
            row.Rect = UiKit.NewRect(name, parent);
            row.Rect.sizeDelta = new Vector2(count * size * 1.2f, size);
            for (var i = 0; i < count; i++)
            {
                var heart = UiKit.NewRect("Heart" + i, row.Rect);
                UiKit.Place(heart, new Vector2(0f, 0.5f), new Vector2(i * size * 1.2f + size * 0.1f, 0f), new Vector2(size, size));
                heart.pivot = new Vector2(0f, 0.5f);
                var parts = new Image[3];
                // ひし形（下のとがり）
                var diamond = UiKit.Panel(heart, "Diamond", Full, size * 0.05f);
                UiKit.Place(diamond.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -size * 0.1f), new Vector2(size * 0.56f, size * 0.56f));
                diamond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                diamond.raycastTarget = false;
                parts[0] = diamond;
                // 左右の丸（ふくらみ）
                for (var k = 0; k < 2; k++)
                {
                    var disc = UiKit.Disc(heart, "Lobe" + k, Full, size * 0.56f);
                    UiKit.Place(disc.rectTransform, new Vector2(0.5f, 0.5f), new Vector2((k == 0 ? -1f : 1f) * size * 0.2f, size * 0.18f), disc.rectTransform.sizeDelta);
                    parts[k + 1] = disc;
                }
                row.hearts.Add(parts);
            }
            return row;
        }

        /// <summary>満ちているハートの数を決める（左から）。</summary>
        public void Set(int filled)
        {
            for (var i = 0; i < hearts.Count; i++)
            {
                var color = i < filled ? Full : Empty;
                foreach (var part in hearts[i])
                {
                    part.color = color;
                }
            }
        }
    }

    /// <summary>画面の上の、小さな部品（シャッター、手順の札、帯など）。複数の画面で同じ見た目にするため、ここに置く。</summary>
    public static class UiParts
    {
        /// <summary>画面いっぱいの透明な板。raycast が true なら指を受け止める。</summary>
        public static Image Clear(Transform parent, string name, bool raycast)
        {
            var rect = UiKit.Fill(UiKit.NewRect(name, parent));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = UnityEngine.Color.clear;
            image.raycastTarget = raycast;
            return image;
        }

        /// <summary>画面の上か下に敷く、濃い紺の帯（3D の上でも、文字とボタンが読める）。</summary>
        public static Image Bar(Transform parent, string name, bool top, float height, float alpha = 0.82f)
        {
            var rect = UiKit.NewRect(name, parent);
            rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, top ? 1f : 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = UiKit.GradientSprite(
                Theme.WithAlpha(Theme.Navy, top ? alpha : 0f), Theme.WithAlpha(Theme.Navy, top ? 0f : alpha));
            image.raycastTarget = false;
            return image;
        }

        /// <summary>シャッターのボタン（白い輪の中に、オレンジの丸）。</summary>
        public static Button Shutter(Transform parent, UnityEngine.Events.UnityAction onClick, float diameter = 260f)
        {
            var outer = UiKit.Disc(parent, "Shutter", Color.white, diameter);
            outer.raycastTarget = true;
            var ring = UiKit.Disc(outer.rectTransform, "Ring", Theme.WithAlpha(Theme.Navy, 0.12f), diameter * 0.86f);
            UiKit.Place(ring.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, ring.rectTransform.sizeDelta);
            var core = UiKit.Disc(outer.rectTransform, "Core", Theme.Orange, diameter * 0.74f);
            UiKit.Place(core.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, core.rectTransform.sizeDelta);
            var button = outer.gameObject.AddComponent<Button>();
            button.targetGraphic = outer;
            button.transition = Selectable.Transition.None;
            outer.gameObject.AddComponent<PressScale>();
            if (onClick != null) button.onClick.AddListener(onClick);
            return button;
        }

        /// <summary>手順の札（「1 まうえから」）。0 は、まだ。1 は、いま。2 は、おわった。</summary>
        public class StepChip
        {
            public RectTransform Rect;
            Image back;
            Image numberBack;
            TMP_Text label;

            public static StepChip Create(Transform parent, string name, int step, string text, Vector2 size)
            {
                var chip = new StepChip();
                var panel = UiKit.Panel(parent, name, Color.white, size.y * 0.5f);
                chip.back = panel;
                chip.Rect = panel.rectTransform;
                chip.Rect.sizeDelta = size;
                chip.numberBack = UiKit.Disc(chip.Rect, "Number", Theme.Gray, size.y * 0.72f);
                UiKit.Place(chip.numberBack.rectTransform, new Vector2(0f, 0.5f), new Vector2(size.y * 0.14f, 0f), chip.numberBack.rectTransform.sizeDelta);
                var number = UiKit.Label(chip.numberBack.rectTransform, step.ToString(), size.y * 0.46f, Color.white, TextAlignmentOptions.Center, true, "N");
                UiKit.Fill(number.rectTransform);
                chip.label = UiKit.FitLabel(chip.Rect, text, size.y * 0.4f, Theme.Ink, TextAlignmentOptions.Left, true, "Text");
                UiKit.Fill(chip.label.rectTransform);
                chip.label.rectTransform.offsetMin = new Vector2(size.y * 0.95f, 4f);
                chip.label.rectTransform.offsetMax = new Vector2(-size.y * 0.3f, -4f);
                chip.SetState(0);
                return chip;
            }

            public void SetState(int state)
            {
                switch (state)
                {
                    case 1:
                        back.color = Theme.Orange;
                        numberBack.color = Theme.Ink;
                        label.color = Theme.Ink;
                        break;
                    case 2:
                        back.color = Color.white;
                        numberBack.color = Theme.Green;
                        label.color = Theme.Ink;
                        break;
                    default:
                        back.color = Theme.WithAlpha(Color.white, 0.55f);
                        numberBack.color = Theme.Gray;
                        label.color = Theme.InkSoft;
                        break;
                }
            }
        }

        /// <summary>カメラの映像を、枠の中に、全体が見えるように出す（切り取らない）。</summary>
        public class PreviewFrame
        {
            public RectTransform Rect;
            public RawImage Image;
            public AspectRatioFitter Fitter;

            public static PreviewFrame Create(Transform parent, string name)
            {
                var frame = new PreviewFrame();
                frame.Rect = UiKit.NewRect(name, parent);
                frame.Image = frame.Rect.gameObject.AddComponent<RawImage>();
                frame.Image.raycastTarget = false;
                frame.Image.enabled = false;
                frame.Fitter = frame.Rect.gameObject.AddComponent<AspectRatioFitter>();
                frame.Fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                frame.Fitter.aspectRatio = 4f / 3f;
                return frame;
            }

            public void Show(Texture texture)
            {
                Image.texture = texture;
                Image.enabled = texture != null;
                if (texture != null && texture.height > 0)
                {
                    Fitter.aspectRatio = (float)texture.width / texture.height;
                }
            }
        }

        /// <summary>三分割の線（真上から撮るとき、机をまっすぐに合わせる目安）。</summary>
        public static void Grid(Transform parent, float alpha = 0.35f)
        {
            var holder = UiKit.Fill(UiKit.NewRect("Grid", parent));
            for (var i = 1; i <= 2; i++)
            {
                var v = UiKit.NewRect("V" + i, holder);
                v.anchorMin = new Vector2(i / 3f, 0f);
                v.anchorMax = new Vector2(i / 3f, 1f);
                v.sizeDelta = new Vector2(3f, 0f);
                var vi = v.gameObject.AddComponent<Image>();
                vi.color = Theme.WithAlpha(Color.white, alpha);
                vi.raycastTarget = false;

                var h = UiKit.NewRect("H" + i, holder);
                h.anchorMin = new Vector2(0f, i / 3f);
                h.anchorMax = new Vector2(1f, i / 3f);
                h.sizeDelta = new Vector2(0f, 3f);
                var hi = h.gameObject.AddComponent<Image>();
                hi.color = Theme.WithAlpha(Color.white, alpha);
                hi.raycastTarget = false;
            }
        }
    }
}
