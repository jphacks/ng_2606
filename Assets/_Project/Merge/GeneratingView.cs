using DeskStage.AR;
using DeskStage.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.Merge
{
    /// <summary>
    /// 写真を送って、ステージができるのを待っている間の画面（合流のつなぎ）。
    /// 撮影の 2 枚がそろって、サーバーの答えを待っている間、全面を覆って、動いていることが分かるようにする：
    ///  ・回り続ける輪（クルクル）と、左右に動くバー
    ///  ・「つくっているよ…」の点が増えていく表示、経過の秒数
    ///  ・撮った 1 枚目の写真と、待っている間のひとこと
    /// 答えが届く（Finished）、失敗する（Lost）、撮り直す、のどれかで消える。失敗の案内は、撮影画面が出す。
    /// D の正式な「待ち画面」ができるまでの代わり。
    /// </summary>
    public class GeneratingView : MonoBehaviour
    {
        [SerializeField] TMP_FontAsset font;

        static readonly Color Dim = new Color(0.03f, 0.04f, 0.08f, 0.88f);
        static readonly Color Card = new Color(0.1f, 0.13f, 0.22f, 1f);
        static readonly Color Accent = new Color(1f, 0.78f, 0.25f, 1f);
        static readonly Color Track = new Color(1f, 1f, 1f, 0.15f);

        // 輪の回る速さ（1 秒に何周か）
        const float SpinnerTurnsPerSecond = 0.9f;

        // ひとこと（子ども向けに、ひらがな）。変える間隔（秒）
        static readonly string[] Tips =
        {
            "しゃしんから はこを みつけているよ",
            "はこの たかさを はかっているよ",
            "つくえの おおきさを しらべているよ",
            "ほんものの しゃしんを はっているよ",
        };
        const string LongWaitTip = "もうすこし まってね。ちょっと じかんが かかっているよ";
        const float TipSeconds = 4f;
        const float LongWaitSeconds = 45f;

        PhotoCapture capture;
        GameObject panel;
        RectTransform spinnerOuter;
        RectTransform spinnerInner;
        RectTransform barHighlight;
        RectTransform photoCard;
        RawImage photo;
        AspectRatioFitter photoFitter;
        TMP_Text title;
        TMP_Text elapsed;
        TMP_Text tip;
        Sprite ring;
        Texture2D photoTexture;
        byte[] shownJpeg;
        float shownSince;
        bool shown;

        void OnDisable()
        {
            Hide();
            ReleasePhoto();
        }

        void Update()
        {
            if (capture == null)
            {
                capture = FindFirstObjectByType<PhotoCapture>();
            }

            var waiting = capture != null
                && capture.SideJpeg != null
                && StageContext.Mode == AppMode.Scan
                && StageContext.ScanStatus == ScanStatus.Scanning;

            if (waiting && !shown)
            {
                Show();
            }
            else if (!waiting && shown)
            {
                Hide();
            }
            if (shown)
            {
                Animate();
            }
        }

        void Show()
        {
            EnsureBuilt();
            shown = true;
            shownSince = Time.unscaledTime;
            panel.transform.SetAsLastSibling();
            panel.SetActive(true);
            ShowPhoto(capture.TopJpeg);
        }

        void Hide()
        {
            shown = false;
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        // ---- 動き ----

        void Animate()
        {
            var t = Time.unscaledTime - shownSince;

            spinnerOuter.localRotation = Quaternion.Euler(0f, 0f, -t * 360f * SpinnerTurnsPerSecond);
            spinnerInner.localRotation = Quaternion.Euler(0f, 0f, t * 360f * SpinnerTurnsPerSecond * 1.4f);

            // 左右に行き来するバー（細い溝の中を、光る部分が動く）
            var wave = Mathf.PingPong(t * 0.55f, 1f);
            var eased = wave * wave * (3f - 2f * wave);
            const float segment = 0.28f;
            barHighlight.anchorMin = new Vector2(eased * (1f - segment), 0f);
            barHighlight.anchorMax = new Vector2(eased * (1f - segment) + segment, 1f);

            // 写真の枠を、ゆっくり息をするように動かす
            var pulse = 1f + 0.015f * Mathf.Sin(t * 2.2f);
            photoCard.localScale = new Vector3(pulse, pulse, 1f);

            // 「つくっているよ」の点は、見えない点も置いて、文字が左右にずれないようにする
            var dots = Mathf.FloorToInt(t / 0.4f) % 4;
            title.text = "ステージを つくっているよ" + new string('.', dots) + "<alpha=#00>" + new string('.', 3 - dots);

            var seconds = Mathf.FloorToInt(t);
            elapsed.text = $"{seconds} びょう　（いつもは 20びょうくらい）";

            tip.text = t >= LongWaitSeconds ? LongWaitTip : Tips[Mathf.FloorToInt(t / TipSeconds) % Tips.Length];
        }

        void ShowPhoto(byte[] jpeg)
        {
            if (jpeg == null || jpeg == shownJpeg)
            {
                photo.enabled = jpeg != null;
                return;
            }
            ReleasePhoto();
            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (texture.LoadImage(jpeg))
            {
                photoTexture = texture;
                shownJpeg = jpeg;
                photo.texture = texture;
                photo.enabled = true;
                photoFitter.aspectRatio = (float)texture.width / texture.height;
            }
            else
            {
                Destroy(texture);
                photo.enabled = false;
            }
        }

        void ReleasePhoto()
        {
            if (photoTexture != null)
            {
                Destroy(photoTexture);
                photoTexture = null;
            }
            shownJpeg = null;
        }

        // ---- 画面の部品を作る ----

        void EnsureBuilt()
        {
            if (panel != null)
            {
                return;
            }
            var canvas = GetComponentInChildren<Canvas>(true);
            canvas = canvas != null ? canvas.rootCanvas : FindFirstObjectByType<Canvas>();
            ring = MakeRingSprite(256);

            panel = NewRect("GeneratingPanel", canvas.transform).gameObject;
            Stretch((RectTransform)panel.transform, Vector2.zero, Vector2.one);
            panel.AddComponent<Image>().color = Dim; // 後ろの画面を押せないように、全面でタップを受ける

            var height = ((RectTransform)canvas.transform).rect.height;
            var unit = Mathf.Max(400f, height) / 1080f;

            // 中央のカード
            var card = NewRect("Card", panel.transform);
            Stretch(card, new Vector2(0.18f, 0.14f), new Vector2(0.82f, 0.86f));
            card.gameObject.AddComponent<Image>().color = Card;

            // 左：クルクル
            var spinnerArea = NewRect("SpinnerArea", card);
            Stretch(spinnerArea, new Vector2(0.04f, 0.2f), new Vector2(0.38f, 0.92f));
            spinnerOuter = NewRing("Outer", spinnerArea, 1f, Accent);
            spinnerInner = NewRing("Inner", spinnerArea, 0.62f, new Color(1f, 1f, 1f, 0.55f));

            // 左：撮った写真（輪の中に小さく）
            var photoHolder = NewRect("Photo", spinnerArea);
            Stretch(photoHolder, new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.7f));
            photoCard = photoHolder;
            var photoImage = NewRect("Image", photoHolder);
            Stretch(photoImage, Vector2.zero, Vector2.one);
            photo = photoImage.gameObject.AddComponent<RawImage>();
            photo.raycastTarget = false;
            photoFitter = photoImage.gameObject.AddComponent<AspectRatioFitter>();
            photoFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

            // 右：ことば
            title = NewText("Title", card, "", 64f * unit, TextAlignmentOptions.Left);
            Stretch(title.rectTransform, new Vector2(0.42f, 0.56f), new Vector2(0.97f, 0.86f));
            title.fontStyle = FontStyles.Bold;
            title.color = Color.white;

            tip = NewText("Tip", card, "", 44f * unit, TextAlignmentOptions.Left);
            Stretch(tip.rectTransform, new Vector2(0.42f, 0.36f), new Vector2(0.97f, 0.56f));
            tip.color = Accent;

            elapsed = NewText("Elapsed", card, "", 34f * unit, TextAlignmentOptions.Left);
            Stretch(elapsed.rectTransform, new Vector2(0.42f, 0.22f), new Vector2(0.97f, 0.34f));
            elapsed.color = new Color(1f, 1f, 1f, 0.65f);

            // 下：動くバー
            var track = NewRect("Track", card);
            Stretch(track, new Vector2(0.06f, 0.07f), new Vector2(0.94f, 0.11f));
            track.gameObject.AddComponent<Image>().color = Track;
            var highlight = NewRect("Highlight", track);
            highlight.gameObject.AddComponent<Image>().color = Accent;
            barHighlight = highlight;
            Stretch(highlight, new Vector2(0f, 0f), new Vector2(0.28f, 1f));
        }

        /// <summary>しっぽが消えていく、輪の形の絵。回すと、クルクルのように見える。</summary>
        RectTransform NewRing(string name, RectTransform parent, float scale, Color color)
        {
            var rect = NewRect(name, parent);
            var half = (1f - scale) / 2f;
            Stretch(rect, new Vector2(half, half), new Vector2(1f - half, 1f - half));
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = ring;
            image.color = color;
            image.raycastTarget = false;
            image.preserveAspect = true;
            return rect;
        }

        static Sprite MakeRingSprite(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
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
                    // 縁のぎざぎざを、1 画素ぶんぼかす
                    var ringAlpha = Mathf.Clamp01(outer - d + 0.5f) * Mathf.Clamp01(d - inner + 0.5f);
                    // 角度に応じて、しっぽが消える（ある角度で、先がくっきり切れる）
                    var angle = (Mathf.Atan2(dy, dx) + Mathf.PI) / (2f * Mathf.PI);
                    var alpha = ringAlpha * Mathf.Pow(angle, 1.4f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        TMP_Text NewText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
        {
            var rect = NewRect(name, parent);
            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            if (font != null) tmp.font = font;
            return tmp;
        }
    }
}
