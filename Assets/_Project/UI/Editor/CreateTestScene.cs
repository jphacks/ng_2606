using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DeskStage.UI.Editor
{
    /// <summary>
    /// メニュー「DeskStage/D/テスト用シーンを作る」。Scenes/Test_D.unity を作る（Step 1〜3）。
    /// Canvas、SafeArea、7つの画面、ScreenRouter、仮のボタン、プレイ画面のスティックとジャンプを組み立てる。
    /// 先に「日本語フォントを作る」と「文言の表を読み込む」を実行しておく。
    /// </summary>
    public static class CreateTestScene
    {
        const string ScenePath = "Assets/_Project/Scenes/Test_D.unity";

        // 基準の解像度（横2532・縦1170）。ボタンは直径160以上
        static readonly Vector2 ReferenceResolution = new Vector2(2532, 1170);
        static readonly Vector2 ButtonSize = new Vector2(520, 180);

        // Step 2：スティックの外周は直径約240、ジャンプは直径約200
        const float StickDiameter = 240f;
        const float JumpDiameter = 200f;

        static TMP_FontAsset font;
        static Wording wording;
        static ViewGestureSettings viewSettings;

        struct ScreenSpec
        {
            public string Name;
            public string DevTitle;
            public string NextKey;
            public ScreenId Next;
            public Color Color;
        }

        // 画面の名前（DevTitle）は流れを確かめるための開発用の表示。ボタンの文は Wording のキーで指定する。
        static readonly ScreenSpec[] Specs =
        {
            new ScreenSpec { Name = "Screen_Title", DevTitle = "タイトル", NextKey = "title.start", Next = ScreenId.List, Color = new Color(0.85f, 0.93f, 1f) },
            new ScreenSpec { Name = "Screen_List", DevTitle = "ステージ いちらん", NextKey = "list.new", Next = ScreenId.Tips, Color = new Color(0.88f, 1f, 0.88f) },
            new ScreenSpec { Name = "Screen_Tips", DevTitle = "ならべかたの コツ", NextKey = "tips.scan", Next = ScreenId.Scan, Color = new Color(1f, 0.96f, 0.82f) },
            new ScreenSpec { Name = "Screen_Scan", DevTitle = "スキャン", NextKey = "scan.done", Next = ScreenId.Check, Color = new Color(0.82f, 0.95f, 1f) },
            new ScreenSpec { Name = "Screen_Check", DevTitle = "たしかめ", NextKey = "check.play", Next = ScreenId.Play, Color = new Color(1f, 0.9f, 0.9f) },
            new ScreenSpec { Name = "Screen_Play", DevTitle = "プレイ", NextKey = null, Next = ScreenId.Play, Color = new Color(0.92f, 0.9f, 1f) },
            new ScreenSpec { Name = "Screen_Clear", DevTitle = "クリア", NextKey = "clear.toList", Next = ScreenId.List, Color = new Color(1f, 0.93f, 0.75f) },
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
            if (!LoadAssets())
            {
                return;
            }
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

            CreatePlayControls(panels[(int)ScreenId.Play].transform);
            var guidance = CreateGuidance(panels[(int)ScreenId.Scan].transform);
            CreateScanScreen(panels[(int)ScreenId.Scan].transform);
            CreateTipsScreen(panels[(int)ScreenId.Tips].transform);
            CreateDevPanel(safeArea, router, guidance);
            CreateDebugOverlay();
            CreateStageLook();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Test_D.unity を作りました: " + ScenePath);
        }

        static bool LoadAssets()
        {
            font = null;
            var guids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { SetupAssets.FontFolder });
            if (guids.Length > 0)
            {
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            if (font == null)
            {
                Debug.LogWarning("日本語フォントのアセットが " + SetupAssets.FontFolder + " にありません。文字が四角になります。" +
                                 "メニュー「DeskStage/D/日本語フォントを作る」を先に実行してください。");
            }

            wording = AssetDatabase.LoadAssetAtPath<Wording>(SetupAssets.WordingAssetPath);
            if (wording == null)
            {
                Debug.LogError("文言のアセットがありません。メニュー「DeskStage/D/文言の表を読み込む」を先に実行してください。");
                return false;
            }

            SetupAssets.CreateViewGestureSettings();
            viewSettings = AssetDatabase.LoadAssetAtPath<ViewGestureSettings>(SetupAssets.ViewGestureSettingsPath);
            return true;
        }

        static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            // 音を聞く役。ないと効果音が鳴らない
            go.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.95f, 0.95f, 0.95f);
            cam.nearClipPlane = 0.01f;
            // 机（原点）を斜め上から見る。仮の箱を確かめるため
            go.transform.position = new Vector3(0f, 0.45f, -0.6f);
            go.transform.LookAt(Vector3.zero);

            CreatePlayLight();
        }

        // Step 7 の3：遊ぶ画面の光源。斜め上から、柔らかい影。影のずれ（Bias）は小さめ
        static void CreatePlayLight()
        {
            var go = new GameObject("PlayLight");
            go.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.97f, 0.92f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.75f;
            // 描画設定の Bias（Mobile は 1）は机の大きさには大きすぎるので、この光源だけの値を使う
            var data = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
            data.usePipelineSettings = false;
            light.shadowBias = 0.05f;
            light.shadowNormalBias = 0.3f;
        }

        static void CreateStageLook()
        {
            var look = AssetDatabase.LoadAssetAtPath<ViewLook>(CreateLookAssets.ViewLookPath);
            if (look == null)
            {
                Debug.LogWarning("ViewLook がありません。メニュー「DeskStage/D/見た目の素材を作る」を先に実行してください。");
            }
            new GameObject("StageLook").AddComponent<StageLook>().SetLook(look);
        }

        static void CreateDebugOverlay()
        {
            // Step 6：デバッグ表示。当たり判定の枠は、ふだんはオフ
            var go = new GameObject("DebugOverlay");
            var outline = go.AddComponent<ColliderOutline>();
            outline.enabled = false;
            var overlay = go.AddComponent<DebugOverlay>();
            overlay.SetColliderOutline(outline);
            overlay.SetFont(AssetDatabase.LoadAssetAtPath<Font>(SetupAssets.FontSourcePath));
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
            // スキャン・たしかめ・プレイは、後ろのカメラ映像や3D画面が見えるように背景を透明にする
            var over3D = spec.Name == "Screen_Scan" || spec.Name == "Screen_Check" || spec.Name == "Screen_Play";
            bg.color = over3D ? Color.clear : spec.Color;
            bg.raycastTarget = false;

            // 画面の名前は開発用の表示（子どもには見せない）
            var title = CreateLabel("DEV_Title", go.transform, "[DEV] " + spec.DevTitle, 64);
            title.gameObject.AddComponent<DevMarker>();
            title.color = new Color(0f, 0f, 0f, 0.4f);
            Anchor((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(1600, 120));

            if (spec.NextKey != null)
            {
                var button = CreateButton("Button_Next", go.transform, spec.NextKey);
                Anchor((RectTransform)button.transform, new Vector2(1f, 0f), new Vector2(-320f, 140f), ButtonSize);
                UnityEventTools.AddIntPersistentListener(button.onClick, router.ShowByIndex, (int)spec.Next);
            }
            return go;
        }

        static void CreatePlayControls(Transform playScreen)
        {
            var circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            // Step 4：視点の操作。画面いっぱいの透明な板を、スティックとジャンプより奥（先の子）に置く
            var viewArea = new GameObject("ViewGesture", typeof(RectTransform));
            viewArea.transform.SetParent(playScreen, false);
            Stretch((RectTransform)viewArea.transform);
            viewArea.AddComponent<Image>().color = Color.clear;
            viewArea.AddComponent<ViewGesture>().SetSettings(viewSettings);

            var readout = CreateLabel("DEV_ViewReadout", playScreen, "", 40);
            readout.gameObject.AddComponent<DevMarker>();
            readout.gameObject.AddComponent<DevViewReadout>();
            readout.color = new Color(0f, 0f, 0f, 0.6f);
            Anchor((RectTransform)readout.transform, new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(1400, 80));

            // 左下：スティック。外周の中で取っ手を動かすと、仮想ゲームパッドの左スティックになる
            var stickBase = new GameObject("Stick", typeof(RectTransform));
            stickBase.transform.SetParent(playScreen, false);
            var baseImage = stickBase.AddComponent<Image>();
            baseImage.sprite = circle;
            baseImage.color = new Color(1f, 1f, 1f, 0.5f);
            // 外周の上で始まった指も受け止め、視点の操作に渡さない
            baseImage.raycastTarget = true;
            Anchor((RectTransform)stickBase.transform, Vector2.zero, new Vector2(240f, 240f), Vector2.one * StickDiameter);

            var knob = new GameObject("Knob", typeof(RectTransform));
            knob.transform.SetParent(stickBase.transform, false);
            var knobImage = knob.AddComponent<Image>();
            knobImage.sprite = circle;
            knobImage.color = Color.white;
            Anchor((RectTransform)knob.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * (StickDiameter / 2f));
            var stick = knob.AddComponent<OnScreenStick>();
            // controlPath の setter は仮想デバイスを作り直すので、保存する値だけを書く
            var stickSo = new SerializedObject(stick);
            stickSo.FindProperty("m_ControlPath").stringValue = "<Gamepad>/leftStick";
            stickSo.FindProperty("m_MovementRange").floatValue = StickDiameter / 2f;
            stickSo.ApplyModifiedPropertiesWithoutUndo();

            // 右下：ジャンプ。仮想ゲームパッドの下のボタン
            var jump = new GameObject("Jump", typeof(RectTransform));
            jump.transform.SetParent(playScreen, false);
            var jumpImage = jump.AddComponent<Image>();
            jumpImage.sprite = circle;
            jumpImage.color = new Color(1f, 0.85f, 0.4f);
            Anchor((RectTransform)jump.transform, new Vector2(1f, 0f), new Vector2(-240f, 220f), Vector2.one * JumpDiameter);
            var jumpButton = jump.AddComponent<OnScreenButton>();
            var jumpSo = new SerializedObject(jumpButton);
            jumpSo.FindProperty("m_ControlPath").stringValue = "<Gamepad>/buttonSouth";
            jumpSo.ApplyModifiedPropertiesWithoutUndo();

            var jumpLabel = CreateWordingLabel("Label", jump.transform, "play.jump", 44);
            Stretch((RectTransform)jumpLabel.transform);
        }

        static GuidanceView CreateGuidance(Transform scanScreen)
        {
            // Step 5：画面の上の中央に1行の案内。文は状態に合わせて Wording から選ぶ
            var label = CreateLabel("Guidance", scanScreen, "", 72);
            Anchor((RectTransform)label.transform, new Vector2(0.5f, 1f), new Vector2(0f, -220f), new Vector2(1800, 120));
            var guidance = label.gameObject.AddComponent<GuidanceView>();
            guidance.SetWording(wording);
            return guidance;
        }

        static void CreateScanScreen(Transform scanScreen)
        {
            // Step 8：画面いっぱいの透明な板（タップで机を決める）を、ボタンより奥（最初の子）に置く
            var tapArea = new GameObject("ScanTapArea", typeof(RectTransform));
            tapArea.transform.SetParent(scanScreen, false);
            tapArea.transform.SetAsFirstSibling();
            Stretch((RectTransform)tapArea.transform);
            tapArea.AddComponent<Image>().color = Color.clear;

            var count = CreateLabel("BoxCount", scanScreen, "", 96);
            Anchor((RectTransform)count.transform, new Vector2(0.5f, 1f), new Vector2(0f, -360f), new Vector2(1400, 140));

            // 3Dの演出と音は Canvas の外に置く。スキャン画面が出ている間だけ ScanScreen が有効にする
            var fxObject = new GameObject("ScanEffects");
            fxObject.AddComponent<AudioSource>();
            var sound = fxObject.AddComponent<SoundFx>();
            var effects = fxObject.AddComponent<ScanEffects>();
            effects.Set(AssetDatabase.LoadAssetAtPath<ViewLook>(CreateLookAssets.ViewLookPath), sound);
            effects.enabled = false;

            var done = scanScreen.Find("Button_Next").GetComponent<Button>();
            tapArea.AddComponent<ScanScreen>().Set(wording, count, done, effects);
        }

        static void CreateTipsScreen(Transform tipsScreen)
        {
            // Step 9：画面いっぱいの透明な板でなぞる操作を受け、その子に絵・文・印を置く
            var area = new GameObject("TipsArea", typeof(RectTransform));
            area.transform.SetParent(tipsScreen, false);
            area.transform.SetAsFirstSibling();
            Stretch((RectTransform)area.transform);
            area.AddComponent<Image>().color = Color.clear;

            var group = new GameObject("PageGroup", typeof(RectTransform));
            group.transform.SetParent(area.transform, false);
            Stretch((RectTransform)group.transform);

            var imageGo = new GameObject("PageImage", typeof(RectTransform));
            imageGo.transform.SetParent(group.transform, false);
            var image = imageGo.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            Anchor((RectTransform)imageGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(900, 562));

            var text = CreateLabel("PageText", group.transform, "", 64);
            Anchor((RectTransform)text.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -250f), new Vector2(1900, 110));
            var dots = CreateLabel("PageDots", group.transform, "", 44);
            Anchor((RectTransform)dots.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -330f), new Vector2(600, 70));

            // 「スキャンする」の上に、物を並べてから押すように書く
            var hint = CreateWordingLabel("BeforeScan", tipsScreen, "tips.beforeScan", 40);
            Anchor((RectTransform)hint.transform, new Vector2(1f, 0f), new Vector2(-320f, 270f), new Vector2(700, 70));

            var show = CreateButton("Button_ShowTips", tipsScreen, "tips.show");
            Anchor((RectTransform)show.transform, new Vector2(0f, 0f), new Vector2(260f, 140f), new Vector2(400, 160));

            var scan = tipsScreen.Find("Button_Next").GetComponent<Button>();
            var pages = AssetDatabase.LoadAssetAtPath<TipsPages>(CreateTipsAssets.TipsPagesPath);
            if (pages == null)
            {
                Debug.LogWarning("コツの絵の一覧がありません。メニュー「DeskStage/D/コツの絵を作る」を先に実行してください。");
            }
            area.AddComponent<TipsScreen>().Set(wording, pages, image, text, dots, scan, show, group);
        }

        static void CreateDevPanel(RectTransform parent, ScreenRouter router, GuidanceView guidance)
        {
            // 仮のボタン。画面の右上に縦に並べる。合流前に DevMarker の付いた物ごと消す。
            var go = new GameObject("DEV_Panel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<DevMarker>();
            Anchor((RectTransform)go.transform, new Vector2(1f, 1f), new Vector2(-190f, -400f), new Vector2(340, 760));
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            // ボタンの大きさを並びに合わせて決めさせる（決めさせないと既定の小さな四角のまま重なる）
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            var dev = go.AddComponent<DevEvents>();
            dev.SetRouter(router);
            dev.SetGuidance(guidance);

            AddDevButton(go.transform, "DEV: スキャン状態 →", dev.NextScanStatus);
            AddDevButton(go.transform, "DEV: 仮の箱", dev.ToggleSampleBoxes);
            AddDevButton(go.transform, "DEV: 箱が見つかった", dev.RaiseBoxFound);
            AddDevButton(go.transform, "DEV: コツを未読に", dev.ResetTipsSeen);
            AddDevButton(go.transform, "DEV: DeskDecided", dev.RaiseDeskDecided);
            AddDevButton(go.transform, "DEV: StageBuilt", dev.RaiseStageBuilt);
            AddDevButton(go.transform, "DEV: SaveOk", dev.RaiseSaveOk);
            AddDevButton(go.transform, "DEV: Goal → クリア", dev.RaiseGoal);
        }

        static void AddDevButton(Transform parent, string label, UnityAction action)
        {
            // 開発用なので Wording を通さない
            var go = CreateButtonBase(label, parent);
            go.AddComponent<DevMarker>();
            go.GetComponent<Image>().color = new Color(1f, 0.8f, 0.2f);
            var text = CreateLabel("Label", go.transform, label, 34);
            Stretch((RectTransform)text.transform);
            UnityEventTools.AddPersistentListener(go.GetComponent<Button>().onClick, action);
        }

        static Button CreateButton(string name, Transform parent, string wordingKey)
        {
            var go = CreateButtonBase(name, parent);
            var text = CreateWordingLabel("Label", go.transform, wordingKey, 56);
            Stretch((RectTransform)text.transform);
            return go.GetComponent<Button>();
        }

        static GameObject CreateButtonBase(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = Color.white;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            return go;
        }

        static TextMeshProUGUI CreateWordingLabel(string name, Transform parent, string key, float size)
        {
            if (!wording.Has(key))
            {
                Debug.LogError("文言の表にキーがありません: " + key);
            }
            var tmp = CreateLabel(name, parent, wording.Get(key), size);
            tmp.gameObject.AddComponent<WordingLabel>().Set(wording, key);
            return tmp;
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
