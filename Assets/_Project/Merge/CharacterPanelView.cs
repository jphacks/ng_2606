using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.Merge
{
    /// <summary>
    /// キャラクターを写真で作る・選ぶ画面（合流のつなぎ）。一覧の画面の「キャラを えらぶ」ボタンから開く、全画面の重ね画面。
    ///  ・一覧：いつものキャラと、撮ったキャラが並ぶ。押すと選ぶ。「あたらしく とる」「えらんだ キャラを けす」「とじる」
    ///  ・撮る：カメラの映像に、無地の紙の上の人形を映して「とる」
    ///  ・たしかめる：切り抜いた人形を見て、「これにする」か「とりなおす」
    /// 部品はすべて、開いたときに作る（見た目の大きさは、画面の大きさから決める）。
    /// D の正式な画面ができるまでの代わり。
    /// </summary>
    public class CharacterPanelView : MonoBehaviour
    {
        [SerializeField] TMP_FontAsset font;
        [SerializeField] Button openButton;
        [SerializeField] CharacterCapture capture;

        static readonly Color Dim = new Color(0f, 0f, 0f, 0.8f);
        static readonly Color Box = new Color(0.1f, 0.12f, 0.18f, 1f);
        static readonly Color ButtonColor = new Color(0.2f, 0.28f, 0.45f, 1f);
        static readonly Color TileColor = new Color(0.22f, 0.24f, 0.3f, 1f);
        static readonly Color TileSelected = new Color(0.95f, 0.75f, 0.2f, 1f);
        static readonly Color Checker = new Color(0.55f, 0.55f, 0.58f, 1f);

        CharacterStore store;
        Canvas canvas;
        GameObject panel;
        GameObject listView;
        GameObject captureView;
        GameObject reviewView;

        RectTransform tileRow;
        TMP_Text listMessage;
        RawImage capturePreview;
        AspectRatioFitter captureFitter;
        TMP_Text captureHint;
        RawImage reviewImage;
        AspectRatioFitter reviewFitter;
        TMP_Text reviewMessage;
        Button reviewOk;

        readonly List<UnityEngine.Object> listObjects = new List<UnityEngine.Object>();
        CutoutResult pending;
        Texture2D pendingTexture;

        void Awake()
        {
            store = CharacterStore.AtDefaultPlace();
            if (openButton != null)
            {
                openButton.onClick.AddListener(Open);
            }
        }

        void OnDisable()
        {
            if (capture != null)
            {
                capture.Close();
            }
            ReleasePending();
            ReleaseList();
        }

        public void Open()
        {
            EnsureBuilt();
            panel.transform.SetAsLastSibling();
            panel.SetActive(true);
            ShowList("");
        }

        public void Close()
        {
            if (capture != null) capture.Close();
            ReleasePending();
            ReleaseList();
            if (panel != null) panel.SetActive(false);
        }

        // ---- 状態の切り替え ----

        void ShowList(string message)
        {
            if (capture != null) capture.Close();
            SetOnly(listView);
            listMessage.text = message;
            RebuildTiles();
        }

        void ShowCapture()
        {
            SetOnly(captureView);
            captureHint.text = "むじの かみの うえに にんぎょうを おいて、まんなかに うつしてね";
            if (capture != null) capture.Open();
        }

        void ShowReview(CutoutResult result)
        {
            SetOnly(reviewView);
            ReleasePending();
            pending = result;
            if (result.Ok)
            {
                pendingTexture = new Texture2D(result.Width, result.Height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
                pendingTexture.SetPixels32(result.Pixels);
                pendingTexture.Apply();
                reviewImage.texture = pendingTexture;
                reviewImage.enabled = true;
                reviewFitter.aspectRatio = (float)result.Width / result.Height;
                reviewMessage.text = "この こに する？";
                reviewOk.gameObject.SetActive(true);
            }
            else
            {
                reviewImage.enabled = false;
                reviewMessage.text = "うまく きりぬけなかったよ。" + result.Reason;
                reviewOk.gameObject.SetActive(false);
            }
        }

        void SetOnly(GameObject view)
        {
            listView.SetActive(view == listView);
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

        void OnTake() => ShowReview(capture.TakeCutout());

        void OnConfirm()
        {
            if (!pending.Ok || pendingTexture == null)
            {
                return;
            }
            var info = store.Add("", pendingTexture.EncodeToPNG(), (float)pending.Width / pending.Height, DateTimeOffset.Now);
            if (info == null)
            {
                ShowList("ほぞんできなかったよ");
                return;
            }
            store.SelectedId = info.id; // 撮ったキャラを、そのまま選ぶ
            ReleasePending();
            ShowList($"「{info.name}」を えらんだよ");
        }

        void OnDeleteSelected()
        {
            var id = store.SelectedId;
            if (string.IsNullOrEmpty(id))
            {
                ShowList("いつもの こは けせないよ");
                return;
            }
            ShowList(store.Delete(id) ? "けしたよ" : "けせなかったよ");
        }

        void Select(string id)
        {
            store.SelectedId = id;
            RebuildTiles();
            listMessage.text = string.IsNullOrEmpty(id) ? "いつもの こを えらんだよ" : "えらんだよ";
        }

        // ---- 一覧の絵 ----

        void RebuildTiles()
        {
            ReleaseList();
            foreach (Transform child in tileRow)
            {
                Destroy(child.gameObject);
            }
            var selected = store.SelectedId;
            AddTile(null, "いつもの こ", string.IsNullOrEmpty(selected));
            foreach (var info in store.List())
            {
                AddTile(info, info.name, info.id == selected);
            }
        }

        void AddTile(CharacterInfo info, string label, bool isSelected)
        {
            var height = ((RectTransform)panel.transform).rect.height;
            var size = Mathf.Max(120f, height * 0.36f);

            var tile = NewRect("Tile", tileRow);
            tile.sizeDelta = new Vector2(size * 0.8f, size);
            var layout = tile.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = size * 0.8f;
            layout.preferredHeight = size;
            var bg = tile.gameObject.AddComponent<Image>();
            bg.color = isSelected ? TileSelected : TileColor;
            var button = tile.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            var id = info != null ? info.id : "";
            button.onClick.AddListener(() => Select(id));

            // 絵（いつものキャラは、絵なしで「？」）
            var imageRect = NewRect("Image", tile);
            Stretch(imageRect, new Vector2(0.06f, 0.24f), new Vector2(0.94f, 0.96f));
            if (info != null)
            {
                var png = store.LoadPng(info.id);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
                if (png != null && texture.LoadImage(png))
                {
                    listObjects.Add(texture);
                    var raw = imageRect.gameObject.AddComponent<RawImage>();
                    raw.texture = texture;
                    raw.raycastTarget = false;
                    var fitter = imageRect.gameObject.AddComponent<AspectRatioFitter>();
                    fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    fitter.aspectRatio = info.aspect > 0f ? info.aspect : (float)texture.width / texture.height;
                }
            }
            else
            {
                var mark = NewText("Mark", imageRect, "カプセル", size * 0.16f, TextAlignmentOptions.Center);
                Stretch(mark.rectTransform, Vector2.zero, Vector2.one);
            }

            var text = NewText("Label", tile, label, size * 0.11f, TextAlignmentOptions.Center);
            text.color = isSelected ? Color.black : Color.white;
            Stretch(text.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.24f));
        }

        // ---- 画面の部品を作る ----

        void EnsureBuilt()
        {
            if (panel != null)
            {
                return;
            }
            canvas = openButton != null ? openButton.GetComponentInParent<Canvas>().rootCanvas : GetComponentInParent<Canvas>();
            panel = NewRect("CharacterPanel", canvas.transform).gameObject;
            var panelRect = (RectTransform)panel.transform;
            Stretch(panelRect, Vector2.zero, Vector2.one);
            var dim = panel.AddComponent<Image>();
            dim.color = Dim; // 後ろの画面のボタンを押せないように、全面でタップを受ける

            var box = NewRect("Box", panelRect);
            Stretch(box, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f));
            box.gameObject.AddComponent<Image>().color = Box;

            BuildListView(box);
            BuildCaptureView(box);
            BuildReviewView(box);
        }

        void BuildListView(RectTransform box)
        {
            listView = NewRect("ListView", box).gameObject;
            var root = (RectTransform)listView.transform;
            Stretch(root, Vector2.zero, Vector2.one);

            var title = NewText("Title", root, "キャラを えらぶ", 56f, TextAlignmentOptions.Center);
            Anchor(title.rectTransform, new Vector2(0f, 0.86f), new Vector2(1f, 0.98f));

            tileRow = NewRect("Tiles", root);
            Anchor(tileRow, new Vector2(0.03f, 0.26f), new Vector2(0.97f, 0.84f));
            var row = tileRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 24f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            listMessage = NewText("Message", root, "", 40f, TextAlignmentOptions.Center);
            Anchor(listMessage.rectTransform, new Vector2(0.03f, 0.16f), new Vector2(0.97f, 0.26f));

            var take = NewButton("Button_New", root, "あたらしく とる", out _);
            Anchor((RectTransform)take.transform, new Vector2(0.03f, 0.03f), new Vector2(0.33f, 0.15f));
            take.onClick.AddListener(ShowCapture);
            var delete = NewButton("Button_Delete", root, "えらんだ こを けす", out _);
            Anchor((RectTransform)delete.transform, new Vector2(0.35f, 0.03f), new Vector2(0.65f, 0.15f));
            delete.onClick.AddListener(OnDeleteSelected);
            var close = NewButton("Button_Close", root, "とじる", out _);
            Anchor((RectTransform)close.transform, new Vector2(0.67f, 0.03f), new Vector2(0.97f, 0.15f));
            close.onClick.AddListener(Close);
        }

        void BuildCaptureView(RectTransform box)
        {
            captureView = NewRect("CaptureView", box).gameObject;
            var root = (RectTransform)captureView.transform;
            Stretch(root, Vector2.zero, Vector2.one);

            var previewRect = NewRect("Preview", root);
            Anchor(previewRect, new Vector2(0.03f, 0.2f), new Vector2(0.97f, 0.84f));
            capturePreview = previewRect.gameObject.AddComponent<RawImage>();
            capturePreview.raycastTarget = false;
            captureFitter = previewRect.gameObject.AddComponent<AspectRatioFitter>();
            captureFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            captureFitter.aspectRatio = 4f / 3f;

            captureHint = NewText("Hint", root, "", 40f, TextAlignmentOptions.Center);
            Anchor(captureHint.rectTransform, new Vector2(0.03f, 0.86f), new Vector2(0.97f, 0.98f));

            var take = NewButton("Button_Take", root, "とる", out _);
            Anchor((RectTransform)take.transform, new Vector2(0.2f, 0.03f), new Vector2(0.5f, 0.17f));
            take.onClick.AddListener(OnTake);
            var cancel = NewButton("Button_Cancel", root, "やめる", out _);
            Anchor((RectTransform)cancel.transform, new Vector2(0.55f, 0.03f), new Vector2(0.85f, 0.17f));
            cancel.onClick.AddListener(() => ShowList(""));
        }

        void BuildReviewView(RectTransform box)
        {
            reviewView = NewRect("ReviewView", box).gameObject;
            var root = (RectTransform)reviewView.transform;
            Stretch(root, Vector2.zero, Vector2.one);

            // 切り抜きが分かるように、灰色の板の上に出す
            var back = NewRect("Back", root);
            Anchor(back, new Vector2(0.2f, 0.22f), new Vector2(0.8f, 0.82f));
            back.gameObject.AddComponent<Image>().color = Checker;
            var imageRect = NewRect("Image", back);
            Stretch(imageRect, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f));
            reviewImage = imageRect.gameObject.AddComponent<RawImage>();
            reviewImage.raycastTarget = false;
            reviewFitter = imageRect.gameObject.AddComponent<AspectRatioFitter>();
            reviewFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

            reviewMessage = NewText("Message", root, "", 44f, TextAlignmentOptions.Center);
            Anchor(reviewMessage.rectTransform, new Vector2(0.03f, 0.84f), new Vector2(0.97f, 0.98f));

            reviewOk = NewButton("Button_Ok", root, "これに する", out _);
            Anchor((RectTransform)reviewOk.transform, new Vector2(0.2f, 0.03f), new Vector2(0.5f, 0.17f));
            reviewOk.onClick.AddListener(OnConfirm);
            var retry = NewButton("Button_Retry", root, "とりなおす", out _);
            Anchor((RectTransform)retry.transform, new Vector2(0.55f, 0.03f), new Vector2(0.85f, 0.17f));
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

        static void Anchor(RectTransform rect, Vector2 min, Vector2 max) => Stretch(rect, min, max);

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

        void ReleasePending()
        {
            if (pendingTexture != null)
            {
                Destroy(pendingTexture);
                pendingTexture = null;
            }
            pending = default;
        }

        void ReleaseList()
        {
            foreach (var o in listObjects)
            {
                if (o != null) Destroy(o);
            }
            listObjects.Clear();
        }
    }
}
