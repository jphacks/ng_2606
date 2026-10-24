using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DeskStage.UI.Editor
{
    /// <summary>
    /// メニュー「DeskStage/D/テスト用シーンを作る」。Scenes/Test_D.unity を作る（Step 1）。
    /// Canvas、SafeArea、7つの画面、ScreenRouter、仮のボタンを組み立てる。
    /// </summary>
    public static class CreateTestScene
    {
        const string ScenePath = "Assets/_Project/Scenes/Test_D.unity";
        const string FontFolder = "Assets/_Project/UI/Fonts";

        // 基準の解像度（横2532・縦1170）。ボタンは直径160以上
        static readonly Vector2 ReferenceResolution = new Vector2(2532, 1170);
        static readonly Vector2 ButtonSize = new Vector2(520, 180);

        static TMP_FontAsset font;

        struct ScreenSpec
        {
            public ScreenId Id;
            public string Name;
            public string Title;
            public string NextLabel;
            public ScreenId Next;
            public Color Color;
        }

        // 画面の文は Step 3 で Wording に置き換える。ここは流れを確かめるための仮の文。
        static readonly ScreenSpec[] Specs =
        {
            new ScreenSpec { Id = ScreenId.Title, Name = "Screen_Title", Title = "タイトル", NextLabel = "はじめる", Next = ScreenId.List, Color = new Color(0.85f, 0.93f, 1f) },
            new ScreenSpec { Id = ScreenId.List, Name = "Screen_List", Title = "ステージ いちらん", NextLabel = "あたらしくつくる", Next = ScreenId.Tips, Color = new Color(0.88f, 1f, 0.88f) },
            new ScreenSpec { Id = ScreenId.Tips, Name = "Screen_Tips", Title = "ならべかたの コツ", NextLabel = "スキャンする", Next = ScreenId.Scan, Color = new Color(1f, 0.96f, 0.82f) },
            new ScreenSpec { Id = ScreenId.Scan, Name = "Screen_Scan", Title = "スキャン", NextLabel = "できた", Next = ScreenId.Check, Color = new Color(0.82f, 0.95f, 1f) },
            new ScreenSpec { Id = ScreenId.Check, Name = "Screen_Check", Title = "たしかめ", NextLabel = "あそぶ", Next = ScreenId.Play, Color = new Color(1f, 0.9f, 0.9f) },
            new ScreenSpec { Id = ScreenId.Play, Name = "Screen_Play", Title = "プレイ", NextLabel = null, Next = ScreenId.Play, Color = new Color(0.92f, 0.9f, 1f) },
            new ScreenSpec { Id = ScreenId.Clear, Name = "Screen_Clear", Title = "クリア", NextLabel = "ステージ いちらんへ", Next = ScreenId.List, Color = new Color(1f, 0.93f, 0.75f) },
        };

        [MenuItem("DeskStage/D/テスト用シーンを作る")]
        public static void Run()
        {
            if (!EditorUtility.DisplayDialog("テスト用シーンを作る",
                    ScenePath + " を新しく作ります（同じ名前があれば上書き）。いいですか？", "作る", "やめる"))
            {
                return;
            }
            Build();
        }

        /// <summary>確認ダイアログなしで作る。バッチモード（-executeMethod）から呼ぶ用。</summary>
        public static void RunBatch() => Build();

        static void Build()
        {
            FindFont();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateEventSystem();
            var canvas = CreateCanvas();
            var safeArea = CreateSafeArea(canvas.transform);

            var router = new GameObject("ScreenRouter").AddComponent<ScreenRouter>();
            var panels = new GameObject[Specs.Length];
            for (int i = 0; i < Specs.Length; i++)
            {
                panels[i] = CreateScreen(safeArea, Specs[i], router);
            }
            router.SetScreens(panels);

            CreateDevPanel(safeArea, router);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Test_D.unity を作りました: " + ScenePath);
        }

        static void FindFont()
        {
            font = null;
            var guids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { FontFolder });
            if (guids.Length > 0)
            {
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            if (font == null)
            {
                Debug.LogWarning("日本語フォントのアセットが " + FontFolder + " にありません。文字が四角になります。" +
                                 "フォントを作ってから、もう一度このメニューを実行してください。");
            }
        }

        static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.95f, 0.95f, 0.95f);
        }

        static void CreateEventSystem()
        {
            // 入力は新しい Input System だけを使う設定なので、専用のモジュールを付ける
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        static Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        static RectTransform CreateSafeArea(Transform parent)
        {
            var go = new GameObject("SafeArea", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            go.AddComponent<SafeAreaFitter>();
            return (RectTransform)go.transform;
        }

        static GameObject CreateScreen(RectTransform parent, ScreenSpec spec, ScreenRouter router)
        {
            var go = new GameObject(spec.Name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            var bg = go.AddComponent<Image>();
            bg.color = spec.Color;
            bg.raycastTarget = false;

            var title = CreateLabel("Title", go.transform, spec.Title, 96);
            Anchor((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1600, 160));

            if (spec.NextLabel != null)
            {
                var button = CreateButton("Button_Next", go.transform, spec.NextLabel);
                Anchor((RectTransform)button.transform, new Vector2(1f, 0f), new Vector2(-320f, 140f), ButtonSize);
                UnityEventTools.AddIntPersistentListener(button.onClick, router.ShowByIndex, (int)spec.Next);
            }
            return go;
        }

        static void CreateDevPanel(RectTransform parent, ScreenRouter router)
        {
            // 仮のボタン。画面の右上に縦に並べる。合流前に DevMarker の付いた物ごと消す。
            var go = new GameObject("DEV_Panel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<DevMarker>();
            Anchor((RectTransform)go.transform, new Vector2(1f, 1f), new Vector2(-190f, -300f), new Vector2(340, 520));
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            var dev = go.AddComponent<DevEvents>();
            dev.SetRouter(router);

            AddDevButton(go.transform, "DEV: DeskDecided", dev.RaiseDeskDecided);
            AddDevButton(go.transform, "DEV: StageBuilt", dev.RaiseStageBuilt);
            AddDevButton(go.transform, "DEV: SaveOk", dev.RaiseSaveOk);
            AddDevButton(go.transform, "DEV: Goal → クリア", dev.RaiseGoal);
        }

        static void AddDevButton(Transform parent, string label, UnityAction action)
        {
            var button = CreateButton(label, parent, label, 34);
            button.gameObject.AddComponent<DevMarker>();
            button.GetComponent<Image>().color = new Color(1f, 0.8f, 0.2f);
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        static Button CreateButton(string name, Transform parent, string label, float fontSize = 56)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = Color.white;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var text = CreateLabel("Label", go.transform, label, fontSize);
            Stretch((RectTransform)text.transform);
            return button;
        }

        static TextMeshProUGUI CreateLabel(string name, Transform parent, string text, float size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                tmp.font = font;
            }
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.black;
            tmp.raycastTarget = false;
            return tmp;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
