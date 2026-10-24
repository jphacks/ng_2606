using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.Merge.EditorTools
{
    /// <summary>
    /// 合流用の UI_System プレハブを作る（合流のつなぎ。D の正式な UI_System ができるまでの代わり）。
    /// D の Scenes/Test_D.unity を開き、開発用の仮の部品（名前が DEV_ で始まる物。DevEvents、DevMarker が付いている）を外し、
    /// 画面（Canvas）、ScreenRouter、ScanEffects、StageLook、EventSystem、DebugOverlay、PlayLight を UI_System にまとめて、プレハブにする。
    /// 写真方式の撮影画面（PhotoScanView）と、スタートとゴールを置くボタン（CoursePlaceView）を足す。
    /// Test_D.unity は、書き換えない（開くだけ。保存しない）。
    /// </summary>
    public static class CreateUiSystemPrefab
    {
        public const string PrefabPath = "Assets/_Project/Merge/Prefabs/UI_System.prefab";
        const string SourceScenePath = "Assets/_Project/Scenes/Test_D.unity";
        const string FontPath = "Assets/_Project/UI/Fonts/NotoSansJP-Regular SDF.asset";
        const string PhotoMaterialPath = "Assets/_Project/Merge/Materials/PhotoUnlit.mat";

        static readonly string[] RootNames =
        {
            "Canvas", "ScanEffects", "ScreenRouter", "StageLook", "EventSystem", "DebugOverlay", "PlayLight",
        };

        [MenuItem("DeskStage/Merge/1. UI_System プレハブを作る")]
        public static void Run()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
            var all = new List<Transform>();
            foreach (var root in scene.GetRootGameObjects())
            {
                all.AddRange(root.GetComponentsInChildren<Transform>(true));
            }

            Transform Find(string name)
            {
                foreach (var t in all)
                {
                    if (t != null && t.name == name) return t;
                }
                return null;
            }

            // 開発用の仮の部品を外す
            var removed = 0;
            foreach (var t in all.ToArray())
            {
                if (t != null && (t.name.StartsWith("DEV_") || t.GetComponent("DevMarker") != null))
                {
                    Object.DestroyImmediate(t.gameObject);
                    removed++;
                }
            }
            all.RemoveAll(t => t == null);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var screenScan = Find("Screen_Scan");
            var screenCheck = Find("Screen_Check");
            var router = Find("ScreenRouter");
            if (screenScan == null || screenCheck == null || router == null)
            {
                Debug.LogError("[Merge] Test_D.unity に Screen_Scan、Screen_Check、ScreenRouter が見つかりません");
                return;
            }

            BuildScanUi(screenScan, font);
            BuildCheckUi(screenCheck, font);

            // まとめる
            var uiRoot = new GameObject("UI_System");
            foreach (var name in RootNames)
            {
                var t = Find(name);
                if (t == null)
                {
                    Debug.LogWarning($"[Merge] Test_D.unity に {name} がありません");
                    continue;
                }
                t.SetParent(uiRoot.transform, true);
            }

            var flow = uiRoot.AddComponent<MergeFlow>();
            var flowSo = new SerializedObject(flow);
            flowSo.FindProperty("router").objectReferenceValue = router.GetComponent<DeskStage.UI.ScreenRouter>();
            flowSo.ApplyModifiedPropertiesWithoutUndo();

            // 3D のステージに本物の写真を貼る部品（素材は、ビルドで削られないよう、アセットとして持たせる）
            var texturer = uiRoot.AddComponent<StagePhotoTexturer>();
            var texturerSo = new SerializedObject(texturer);
            texturerSo.FindProperty("photoMaterial").objectReferenceValue = EnsurePhotoMaterial();
            texturerSo.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            PrefabUtility.SaveAsPrefabAsset(uiRoot, PrefabPath);
            Debug.Log($"[Merge] 作りました：{PrefabPath}（外した開発用の部品 {removed} 個）");

            // Test_D は保存せずに閉じる
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        /// <summary>写真を貼る素材（URP の Unlit）。無ければ作る。実行時の Shader.Find はビルドで削られるので、アセットにする。</summary>
        static Material EnsurePhotoMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(PhotoMaterialPath);
            if (existing != null)
            {
                return existing;
            }
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            Directory.CreateDirectory(Path.GetDirectoryName(PhotoMaterialPath));
            var material = new Material(shader) { name = "PhotoUnlit" };
            AssetDatabase.CreateAsset(material, PhotoMaterialPath);
            AssetDatabase.SaveAssets();
            return material;
        }

        // ---- 撮影画面 ----

        static void BuildScanUi(Transform screen, TMP_FontAsset font)
        {
            // ARKit の時代の案内（「つくえを うつしてね」など）と、写真を送らずにたしかめへ進む「できた」は使わない
            var guidance = screen.Find("Guidance");
            if (guidance != null) guidance.gameObject.SetActive(false);
            var next = screen.Find("Button_Next");
            if (next != null) next.gameObject.SetActive(false);

            var previewRect = NewRect("PhotoPreview", screen);
            Stretch(previewRect, new Vector2(0.08f, 0.2f), new Vector2(0.92f, 0.86f));
            var preview = previewRect.gameObject.AddComponent<RawImage>();
            preview.color = Color.white;
            preview.raycastTarget = false;
            var fitter = previewRect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 4f / 3f;
            previewRect.SetAsFirstSibling();

            var hint = NewText("PhotoHint", screen, "", 44f, font, TextAlignmentOptions.Center);
            Anchor(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1500f, 100f));

            var take = NewButton("Button_Take", screen, "とる（うえ）", font, out var takeLabel);
            Anchor((RectTransform)take.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(560f, 130f));

            var view = NewRect("PhotoScanView", screen).gameObject.AddComponent<PhotoScanView>();
            var so = new SerializedObject(view);
            so.FindProperty("preview").objectReferenceValue = preview;
            so.FindProperty("fitter").objectReferenceValue = fitter;
            so.FindProperty("take").objectReferenceValue = take;
            so.FindProperty("takeLabel").objectReferenceValue = takeLabel;
            so.FindProperty("hint").objectReferenceValue = hint;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- たしかめ画面：スタートとゴールを置く ----

        static void BuildCheckUi(Transform screen, TMP_FontAsset font)
        {
            var start = NewButton("Button_PlaceStart", screen, "スタートを おく", font, out _);
            Anchor((RectTransform)start.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 90f), new Vector2(420f, 110f));
            var goal = NewButton("Button_PlaceGoal", screen, "ゴールを おく", font, out _);
            Anchor((RectTransform)goal.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, -40f), new Vector2(420f, 110f));

            var hint = NewText("PlaceHint", screen, "", 38f, font, TextAlignmentOptions.Left);
            Anchor(hint.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 20f), new Vector2(1100f, 80f));

            var assist = screen.Find("AssistArea");
            var view = NewRect("CoursePlaceView", screen).gameObject.AddComponent<CoursePlaceView>();
            var so = new SerializedObject(view);
            so.FindProperty("startButton").objectReferenceValue = start;
            so.FindProperty("goalButton").objectReferenceValue = goal;
            so.FindProperty("hint").objectReferenceValue = hint;
            so.FindProperty("assistArea").objectReferenceValue = assist != null ? assist.gameObject : null;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- UI の部品を作る小さな補助 ----

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

        static void Anchor(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static TMP_Text NewText(string name, Transform parent, string text, float size, TMP_FontAsset font, TextAlignmentOptions align)
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

        static Button NewButton(string name, Transform parent, string label, TMP_FontAsset font, out TMP_Text labelText)
        {
            var rect = NewRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.12f, 0.15f, 0.25f, 0.9f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            labelText = NewText("Label", rect, label, 46f, font, TextAlignmentOptions.Center);
            Stretch(labelText.rectTransform, Vector2.zero, Vector2.one);
            return button;
        }
    }
}
