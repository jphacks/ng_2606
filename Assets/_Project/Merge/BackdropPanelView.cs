using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.Merge
{
    /// <summary>
    /// ステージの背景の写真を撮る・消す画面（合流のつなぎ）。一覧の画面の「はいけいを とる」ボタンから開く、全画面の重ね画面。
    ///  ・いまの背景：保存してある背景の写真と、「あたらしく とる」「なくす」「とじる」
    ///  ・撮る：カメラの映像に、ステージの後ろに見せたい景色を映して「とる」
    ///  ・たしかめる：撮った写真を見て、「これにする」か「とりなおす」
    /// 撮った写真は、端末の中に 1 枚だけ保存される（BackdropStore）。遊ぶ画面とたしかめ画面の、ステージの後ろ（ドーム）に貼られる。
    /// 背景の写真が無いときは、ステージに背景は出ない。
    /// 部品はすべて、開いたときに作る。D の正式な画面ができるまでの代わり。
    /// </summary>
    public class BackdropPanelView : MonoBehaviour
    {
        // 保存する写真の、長い辺の上限（画素）と、JPEG の画質
        const int MaxSide = 1024;
        const int Quality = 80;

        [SerializeField] TMP_FontAsset font;
        [SerializeField] Button openButton;
        [SerializeField] CharacterCapture capture;

        static readonly Color Dim = new Color(0f, 0f, 0f, 0.8f);
        static readonly Color Box = new Color(0.1f, 0.12f, 0.18f, 1f);
        static readonly Color ButtonColor = new Color(0.2f, 0.28f, 0.45f, 1f);

        BackdropStore store;
        GameObject panel;
        GameObject statusView;
        GameObject captureView;
        GameObject reviewView;

        RawImage currentImage;
        AspectRatioFitter currentFitter;
        TMP_Text statusMessage;
        RawImage capturePreview;
        AspectRatioFitter captureFitter;
        TMP_Text captureHint;
        RawImage reviewImage;
        AspectRatioFitter reviewFitter;
        TMP_Text reviewMessage;

        Texture2D currentTexture;
        Texture2D reviewTexture;
        byte[] pendingJpeg;

        void Awake()
        {
            store = BackdropStore.AtDefaultPlace();
            if (openButton != null)
            {
                openButton.onClick.AddListener(Open);
            }
        }

        void OnDisable()
        {
            if (capture != null) capture.Close();
            ReleaseTextures();
        }

        public void Open()
        {
            EnsureBuilt();
            panel.transform.SetAsLastSibling();
            panel.SetActive(true);
            ShowStatus("");
        }

        public void Close()
        {
            if (capture != null) capture.Close();
            ReleaseTextures();
            if (panel != null) panel.SetActive(false);
        }

        // ---- 状態の切り替え ----

        void ShowStatus(string message)
        {
            if (capture != null) capture.Close();
            SetOnly(statusView);
            statusMessage.text = message;
            ReleaseCurrent();
            var jpeg = store.Load();
            if (jpeg != null)
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (texture.LoadImage(jpeg))
                {
                    currentTexture = texture;
                    currentImage.texture = texture;
                    currentImage.enabled = true;
                    currentFitter.aspectRatio = (float)texture.width / texture.height;
                    if (string.IsNullOrEmpty(message)) statusMessage.text = "いまの はいけいは これだよ";
                    return;
                }
                Destroy(texture);
            }
            currentImage.enabled = false;
            if (string.IsNullOrEmpty(message)) statusMessage.text = "はいけいは まだ ないよ。ステージの うしろは なにも でないよ";
        }

        void ShowCapture()
        {
            SetOnly(captureView);
            captureHint.text = "ステージの うしろに みせたい けしきを うつしてね（かべ、まど、へやの ようす など）";
            if (capture != null) capture.Open();
        }

        void ShowReview(byte[] jpeg)
        {
            SetOnly(reviewView);
            ReleaseReview();
            pendingJpeg = jpeg;
            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (jpeg != null && texture.LoadImage(jpeg))
            {
                reviewTexture = texture;
                reviewImage.texture = texture;
                reviewImage.enabled = true;
                reviewFitter.aspectRatio = (float)texture.width / texture.height;
                reviewMessage.text = "この けしきに する？";
            }
            else
            {
                Destroy(texture);
                reviewImage.enabled = false;
                reviewMessage.text = "うまく とれなかったよ。カメラの えいぞうが まだ きていないかも。もういちど やってみてね";
                pendingJpeg = null;
            }
        }

        void SetOnly(GameObject view)
        {
            statusView.SetActive(view == statusView);
            captureView.SetActive(view == captureView);
            reviewView.SetActive(view == reviewView);
        }

        void Update()
        {
            if (panel == null || !panel.activeSelf || !captureView.activeSelf || capture == null)
            {
                return;
            }
            var preview = capture.Preview;
            capturePreview.texture = preview;
            capturePreview.enabled = preview != null;
            if (preview != null && preview.height > 0)
            {
                captureFitter.aspectRatio = (float)preview.width / preview.height;
            }
            if (!string.IsNullOrEmpty(capture.Problem))
            {
                captureHint.text = capture.Problem;
            }
        }

        // ---- ボタンの動き ----

        void OnTake() => ShowReview(capture.CaptureFullJpeg(MaxSide, Quality));

        void OnConfirm()
        {
            if (pendingJpeg == null)
            {
                return;
            }
            var ok = store.Save(pendingJpeg);
            pendingJpeg = null;
            ShowStatus(ok ? "はいけいを ほぞんしたよ。つぎの ステージから うしろに でるよ" : "ほぞんできなかったよ");
        }

        void OnClear() => ShowStatus(store.Clear() ? "はいけいを なくしたよ" : "なくせなかったよ");

        // ---- 画面の部品を作る ----

        void EnsureBuilt()
        {
            if (panel != null)
            {
                return;
            }
            var canvas = openButton != null ? openButton.GetComponentInParent<Canvas>().rootCanvas : GetComponentInParent<Canvas>();
            panel = NewRect("BackdropPanel", canvas.transform).gameObject;
            Stretch((RectTransform)panel.transform, Vector2.zero, Vector2.one);
            panel.AddComponent<Image>().color = Dim; // 後ろの画面のボタンを押せないように、全面でタップを受ける

            var box = NewRect("Box", panel.transform);
            Stretch(box, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f));
            box.gameObject.AddComponent<Image>().color = Box;

            BuildStatusView(box);
            BuildCaptureView(box);
            BuildReviewView(box);
        }

        void BuildStatusView(RectTransform box)
        {
            statusView = NewRect("StatusView", box).gameObject;
            var root = (RectTransform)statusView.transform;
            Stretch(root, Vector2.zero, Vector2.one);

            var title = NewText("Title", root, "ステージの はいけい", 56f, TextAlignmentOptions.Center);
            Stretch(title.rectTransform, new Vector2(0f, 0.86f), new Vector2(1f, 0.98f));

            var imageRect = NewRect("Current", root);
            Stretch(imageRect, new Vector2(0.2f, 0.28f), new Vector2(0.8f, 0.84f));
            currentImage = imageRect.gameObject.AddComponent<RawImage>();
            currentImage.raycastTarget = false;
            currentFitter = imageRect.gameObject.AddComponent<AspectRatioFitter>();
            currentFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            currentFitter.aspectRatio = 4f / 3f;

            statusMessage = NewText("Message", root, "", 40f, TextAlignmentOptions.Center);
            Stretch(statusMessage.rectTransform, new Vector2(0.03f, 0.16f), new Vector2(0.97f, 0.27f));

            var take = NewButton("Button_New", root, "あたらしく とる", out _);
            Stretch((RectTransform)take.transform, new Vector2(0.03f, 0.03f), new Vector2(0.33f, 0.15f));
            take.onClick.AddListener(ShowCapture);
            var clear = NewButton("Button_Clear", root, "はいけいを なくす", out _);
            Stretch((RectTransform)clear.transform, new Vector2(0.35f, 0.03f), new Vector2(0.65f, 0.15f));
            clear.onClick.AddListener(OnClear);
            var close = NewButton("Button_Close", root, "とじる", out _);
            Stretch((RectTransform)close.transform, new Vector2(0.67f, 0.03f), new Vector2(0.97f, 0.15f));
            close.onClick.AddListener(Close);
        }

        void BuildCaptureView(RectTransform box)
        {
            captureView = NewRect("CaptureView", box).gameObject;
            var root = (RectTransform)captureView.transform;
            Stretch(root, Vector2.zero, Vector2.one);

            var previewRect = NewRect("Preview", root);
            Stretch(previewRect, new Vector2(0.03f, 0.2f), new Vector2(0.97f, 0.84f));
            capturePreview = previewRect.gameObject.AddComponent<RawImage>();
            capturePreview.raycastTarget = false;
            captureFitter = previewRect.gameObject.AddComponent<AspectRatioFitter>();
            captureFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            captureFitter.aspectRatio = 4f / 3f;

            captureHint = NewText("Hint", root, "", 40f, TextAlignmentOptions.Center);
            Stretch(captureHint.rectTransform, new Vector2(0.03f, 0.86f), new Vector2(0.97f, 0.98f));

            var take = NewButton("Button_Take", root, "とる", out _);
            Stretch((RectTransform)take.transform, new Vector2(0.2f, 0.03f), new Vector2(0.5f, 0.17f));
            take.onClick.AddListener(OnTake);
            var cancel = NewButton("Button_Cancel", root, "やめる", out _);
            Stretch((RectTransform)cancel.transform, new Vector2(0.55f, 0.03f), new Vector2(0.85f, 0.17f));
            cancel.onClick.AddListener(() => ShowStatus(""));
        }

        void BuildReviewView(RectTransform box)
        {
            reviewView = NewRect("ReviewView", box).gameObject;
            var root = (RectTransform)reviewView.transform;
            Stretch(root, Vector2.zero, Vector2.one);

            var imageRect = NewRect("Image", root);
            Stretch(imageRect, new Vector2(0.03f, 0.2f), new Vector2(0.97f, 0.84f));
            reviewImage = imageRect.gameObject.AddComponent<RawImage>();
            reviewImage.raycastTarget = false;
            reviewFitter = imageRect.gameObject.AddComponent<AspectRatioFitter>();
            reviewFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            reviewFitter.aspectRatio = 4f / 3f;

            reviewMessage = NewText("Message", root, "", 44f, TextAlignmentOptions.Center);
            Stretch(reviewMessage.rectTransform, new Vector2(0.03f, 0.86f), new Vector2(0.97f, 0.98f));

            var ok = NewButton("Button_Ok", root, "これに する", out _);
            Stretch((RectTransform)ok.transform, new Vector2(0.2f, 0.03f), new Vector2(0.5f, 0.17f));
            ok.onClick.AddListener(OnConfirm);
            var retry = NewButton("Button_Retry", root, "とりなおす", out _);
            Stretch((RectTransform)retry.transform, new Vector2(0.55f, 0.03f), new Vector2(0.85f, 0.17f));
            retry.onClick.AddListener(ShowCapture);
        }

        // ---- 小さな補助 ----

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
            if (font != null) tmp.font = font;
            return tmp;
        }

        Button NewButton(string name, Transform parent, string label, out TMP_Text labelText)
        {
            var rect = NewRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = ButtonColor;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            labelText = NewText("Label", rect, label, 44f, TextAlignmentOptions.Center);
            Stretch(labelText.rectTransform, Vector2.zero, Vector2.one);
            return button;
        }

        void ReleaseCurrent()
        {
            if (currentTexture != null)
            {
                Destroy(currentTexture);
                currentTexture = null;
            }
        }

        void ReleaseReview()
        {
            if (reviewTexture != null)
            {
                Destroy(reviewTexture);
                reviewTexture = null;
            }
        }

        void ReleaseTextures()
        {
            ReleaseCurrent();
            ReleaseReview();
            pendingJpeg = null;
        }
    }
}
