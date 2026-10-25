using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeskStage.App
{
    /// <summary>
    /// 新しい UI の部品を、コードで作る道具（丸い板、文字、ボタン、輪）。
    /// 見た目の値は、Theme から取る。画面のコードは、ここの関数を呼んで組み立てる。
    /// </summary>
    public static class UiKit
    {
        /// <summary>物を消す。実行中は Destroy、編集中（確認用の画像を作るとき）は DestroyImmediate。</summary>
        public static void Kill(Object obj)
        {
            if (obj == null)
            {
                return;
            }
            if (Application.isPlaying) Object.Destroy(obj); else Object.DestroyImmediate(obj);
        }

        // ---- 位置 ----

        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>親いっぱいに広げる。</summary>
        public static RectTransform Fill(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
            return r;
        }

        /// <summary>親の割合（anchorMin、anchorMax）で場所を決め、内側に margin だけ寄せる。</summary>
        public static RectTransform Area(RectTransform r, Vector2 anchorMin, Vector2 anchorMax, float margin = 0f)
        {
            r.anchorMin = anchorMin;
            r.anchorMax = anchorMax;
            r.offsetMin = new Vector2(margin, margin);
            r.offsetMax = new Vector2(-margin, -margin);
            return r;
        }

        // 端（0、1）と真ん中（0.5）では、pivot を anchor と同じにする。それ以外の割合のときは、真ん中を基準にする
        static float PivotFor(float a) => a <= 0.01f || a >= 0.99f || Mathf.Abs(a - 0.5f) < 0.01f ? a : 0.5f;

        /// <summary>基準の点（anchor）から、位置と大きさ（画素）で決める。pivot は anchor と同じ（端と真ん中以外の割合は、真ん中）。</summary>
        public static RectTransform Place(RectTransform r, Vector2 anchor, Vector2 position, Vector2 size)
        {
            r.anchorMin = anchor;
            r.anchorMax = anchor;
            r.pivot = new Vector2(PivotFor(anchor.x), PivotFor(anchor.y));
            r.anchoredPosition = position;
            r.sizeDelta = size;
            return r;
        }

        // ---- 絵 ----

        static Sprite rounded;
        static Sprite circle;

        /// <summary>角の丸い四角（9 分割）。半径は、Image.pixelsPerUnitMultiplier で決める。</summary>
        public static Sprite RoundedSprite
        {
            get
            {
                if (rounded == null)
                {
                    const int size = 96;
                    const float radius = 40f;
                    var texture = NewTexture(size);
                    var pixels = new Color32[size * size];
                    for (var y = 0; y < size; y++)
                    {
                        for (var x = 0; x < size; x++)
                        {
                            // 四隅の円の中心までの距離で、ふちをなめらかにする（1 画素ぶん）
                            var cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                            var cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                            var d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                            var a = Mathf.Clamp01(radius - d + 0.5f);
                            pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                        }
                    }
                    texture.SetPixels32(pixels);
                    texture.Apply();
                    const float border = 44f;
                    rounded = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                        SpriteMeshType.FullRect, new Vector4(border, border, border, border));
                    rounded.name = "AppRounded";
                }
                return rounded;
            }
        }

        /// <summary>まるい絵（なめらかなふち）。</summary>
        public static Sprite CircleSprite
        {
            get
            {
                if (circle == null)
                {
                    const int size = 128;
                    var texture = NewTexture(size);
                    var pixels = new Color32[size * size];
                    var center = size / 2f;
                    var r = size / 2f - 1f;
                    for (var y = 0; y < size; y++)
                    {
                        for (var x = 0; x < size; x++)
                        {
                            var d = Mathf.Sqrt((x + 0.5f - center) * (x + 0.5f - center) + (y + 0.5f - center) * (y + 0.5f - center));
                            pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(r - d + 0.5f) * 255f));
                        }
                    }
                    texture.SetPixels32(pixels);
                    texture.Apply();
                    circle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                    circle.name = "AppCircle";
                }
                return circle;
            }
        }

        static Texture2D NewTexture(int size) => new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave,
        };

        /// <summary>縦のグラデーションの絵（上の色から下の色へ）。背景に使う。</summary>
        public static Sprite GradientSprite(Color top, Color bottom)
        {
            const int height = 64;
            var texture = new Texture2D(2, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var pixels = new Color32[2 * height];
            for (var y = 0; y < height; y++)
            {
                var c = Color.Lerp(bottom, top, y / (height - 1f)); // 配列の 0 行目が下
                pixels[y * 2] = c;
                pixels[y * 2 + 1] = c;
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 2, height), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>画面いっぱいの背景（上から下へのグラデーション）。</summary>
        public static Image Background(Transform parent, string name, Color top, Color bottom)
        {
            var rect = Fill(NewRect(name, parent));
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = GradientSprite(top, bottom);
            image.raycastTarget = true; // 後ろの画面のタップを受け止める
            return image;
        }

        // ---- 板 ----

        /// <summary>角の丸い板。radius は丸める半径（画素）。shadow が true なら、うすい影を下に敷く。</summary>
        public static Image Panel(Transform parent, string name, Color color, float radius = Theme.CardRadius, bool shadow = false)
        {
            var rect = NewRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            Round(image, radius);
            image.color = color;
            if (shadow)
            {
                // 影は、板の後ろに描かれる UI.Shadow（子の絵にすると、板の上に重なって、白が灰色に見える）
                var drop = rect.gameObject.AddComponent<Shadow>();
                drop.effectColor = new Color(0.1f, 0.15f, 0.3f, 0.18f);
                drop.effectDistance = new Vector2(0f, -12f);
                drop.useGraphicAlpha = false;
            }
            return image;
        }

        /// <summary>Image を、丸い板にする。</summary>
        public static void Round(Image image, float radius)
        {
            image.sprite = RoundedSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 44f / Mathf.Max(8f, radius);
        }

        /// <summary>まるい板（円）。</summary>
        public static Image Disc(Transform parent, string name, Color color, float diameter)
        {
            var rect = NewRect(name, parent);
            rect.sizeDelta = new Vector2(diameter, diameter);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = CircleSprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        // ---- 文字 ----

        public static TMP_Text Label(Transform parent, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false, string name = "Label")
        {
            var rect = NewRect(name, parent);
            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow; // フォントに「…」が無く、高さが足りないと全部消えるため
            if (bold) tmp.fontStyle = FontStyles.Bold;
            if (Theme.Font != null) tmp.font = Theme.Font;
            return tmp;
        }

        /// <summary>はみ出さないよう、大きさを自動で縮める文字。</summary>
        public static TMP_Text FitLabel(Transform parent, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false, string name = "Label")
        {
            var tmp = Label(parent, text, size, color, align, bold, name);
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = size;
            tmp.fontSizeMin = size * 0.55f;
            return tmp;
        }

        // ---- ボタン ----

        public enum ButtonStyle
        {
            Primary,    // いちばん大事（オレンジ）
            Secondary,  // ふつう（青）
            Soft,       // 白い板（文字は濃い紺）
            Danger,     // 消す（赤）
            Overlay,    // 3D の上に重ねる（濃い紺の半透明）
        }

        public static AppButton Button(Transform parent, string text, ButtonStyle style, Vector2 size, UnityAction onClick = null,
            string name = "Button", float fontSize = Theme.H2)
        {
            var panel = Panel(parent, name, Color.white, Theme.ButtonRadius, shadow: style != ButtonStyle.Overlay);
            var rect = panel.rectTransform;
            rect.sizeDelta = size;

            var button = panel.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = panel;
            button.transition = Selectable.Transition.None; // 色の変化は、AppButton が行う
            panel.gameObject.AddComponent<PressScale>();

            var label = FitLabel(rect, text, fontSize, Color.white, TextAlignmentOptions.Center, true);
            Fill(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(28f, 8f);
            label.rectTransform.offsetMax = new Vector2(-28f, -8f);

            var app = panel.gameObject.AddComponent<AppButton>();
            app.Init(button, panel, label, style);
            if (onClick != null) app.OnClick(onClick);
            return app;
        }

        // ---- 輪（待つとき）----

        static Sprite ring;

        /// <summary>しっぽが消えていく輪。回すと、クルクルになる。</summary>
        public static Sprite RingSprite
        {
            get
            {
                if (ring == null)
                {
                    const int size = 256;
                    var texture = NewTexture(size);
                    var pixels = new Color32[size * size];
                    var center = (size - 1) / 2f;
                    var outer = size * 0.5f - 2f;
                    var inner = outer * 0.8f;
                    for (var y = 0; y < size; y++)
                    {
                        for (var x = 0; x < size; x++)
                        {
                            float dx = x - center, dy = y - center;
                            var d = Mathf.Sqrt(dx * dx + dy * dy);
                            var ringAlpha = Mathf.Clamp01(outer - d + 0.5f) * Mathf.Clamp01(d - inner + 0.5f);
                            var angle = (Mathf.Atan2(dy, dx) + Mathf.PI) / (2f * Mathf.PI);
                            pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(ringAlpha * Mathf.Pow(angle, 1.4f) * 255f));
                        }
                    }
                    texture.SetPixels32(pixels);
                    texture.Apply();
                    ring = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                    ring.name = "AppRing";
                }
                return ring;
            }
        }

        /// <summary>回り続ける輪。</summary>
        public static Spinner Spinner(Transform parent, string name, Color color, float diameter, float turnsPerSecond = 0.9f)
        {
            var rect = NewRect(name, parent);
            rect.sizeDelta = new Vector2(diameter, diameter);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = RingSprite;
            image.color = color;
            image.raycastTarget = false;
            var spinner = rect.gameObject.AddComponent<Spinner>();
            spinner.TurnsPerSecond = turnsPerSecond;
            return spinner;
        }
    }

    /// <summary>押すと、少し縮む（押した感じ）。</summary>
    public class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        bool pressed;

        public void OnPointerDown(PointerEventData eventData) => pressed = true;

        public void OnPointerUp(PointerEventData eventData) => pressed = false;

        public void OnPointerExit(PointerEventData eventData) => pressed = false;

        void OnDisable()
        {
            pressed = false;
            transform.localScale = Vector3.one;
        }

        void Update()
        {
            var target = pressed ? Theme.PressScale : 1f;
            var s = Mathf.MoveTowards(transform.localScale.x, target, Time.unscaledDeltaTime * 6f);
            transform.localScale = new Vector3(s, s, 1f);
        }
    }

    /// <summary>回り続ける輪。</summary>
    public class Spinner : MonoBehaviour
    {
        public float TurnsPerSecond = 0.9f;

        void Update() => transform.localRotation = Quaternion.Euler(0f, 0f, -Time.unscaledTime * 360f * TurnsPerSecond);
    }

    /// <summary>ゆっくり上下にゆれる（飾り）。</summary>
    public class Bobbing : MonoBehaviour
    {
        public float Amplitude = 18f;
        public float Speed = 1.2f;
        public float Phase;

        Vector2 origin;
        bool hasOrigin;

        void OnEnable()
        {
            // 再び出るたびに、動いた途中の位置を基準にしてしまわないよう、最初の 1 回だけ覚える
            if (!hasOrigin)
            {
                origin = ((RectTransform)transform).anchoredPosition;
                hasOrigin = true;
            }
        }

        void Update()
        {
            var rect = (RectTransform)transform;
            rect.anchoredPosition = origin + new Vector2(0f, Mathf.Sin(Time.unscaledTime * Speed + Phase) * Amplitude);
        }
    }

    /// <summary>ボタン：色の決まり、押せる・押せない、押したときの音。</summary>
    public class AppButton : MonoBehaviour
    {
        public Button Button { get; private set; }
        public Image Background { get; private set; }
        public TMP_Text Label { get; private set; }
        public UiKit.ButtonStyle Style { get; private set; }

        bool interactable = true;

        public RectTransform Rect => (RectTransform)transform;

        public void Init(Button button, Image background, TMP_Text label, UiKit.ButtonStyle style)
        {
            Button = button;
            Background = background;
            Label = label;
            SetStyle(style);
            button.onClick.AddListener(() => UiSound.Play(UiSound.Cue.Click));
        }

        public void OnClick(UnityEngine.Events.UnityAction action) => Button.onClick.AddListener(action);

        public void SetText(string text) => Label.text = text;

        public void SetStyle(UiKit.ButtonStyle style)
        {
            Style = style;
            Apply();
        }

        public bool Interactable
        {
            get => interactable;
            set
            {
                interactable = value;
                Button.interactable = value;
                Apply();
            }
        }

        void Apply()
        {
            Color fill;
            Color text;
            switch (Style)
            {
                case UiKit.ButtonStyle.Primary: fill = Theme.Orange; text = Theme.Ink; break;
                case UiKit.ButtonStyle.Secondary: fill = Theme.Blue; text = Color.white; break;
                case UiKit.ButtonStyle.Danger: fill = Theme.Red; text = Color.white; break;
                case UiKit.ButtonStyle.Overlay: fill = Theme.WithAlpha(Theme.Navy, 0.78f); text = Color.white; break;
                default: fill = Color.white; text = Theme.Ink; break;
            }
            if (!interactable)
            {
                fill = Style == UiKit.ButtonStyle.Overlay ? Theme.WithAlpha(Theme.Navy, 0.45f) : Theme.Gray;
                text = Theme.WithAlpha(text == Color.white ? Color.white : Theme.Ink, 0.7f);
            }
            Background.color = fill;
            Label.color = text;
        }
    }
}
