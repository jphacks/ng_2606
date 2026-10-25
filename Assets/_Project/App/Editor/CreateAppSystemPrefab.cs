using System.IO;
using DeskStage.Merge;
using DeskStage.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeskStage.App.EditorTools
{
    /// <summary>
    /// 新しい UI の App_System プレハブを作る。
    /// 画面（Canvas、ボタン、文字）は、起動したときに AppRoot がコードで作るので、プレハブに置かない。
    /// ここに置くのは、画面の外の部品：AppRoot、3D の見た目（StageLook）、デバッグ表示（DebugOverlay）、遊ぶ画面の光源（PlayLight）、
    /// 写真を貼る部品（StagePhotoTexturer）、キャラ・背景を撮る部品（CharacterCapture）、遊ぶときにキャラを立てる部品（CharacterAvatar）。
    /// D の部品（StageLook、DebugOverlay、PlayLight）は、D の Test_D.unity から、そのまま取り出す（Test_D は保存しない）。
    /// </summary>
    public static class CreateAppSystemPrefab
    {
        public const string PrefabPath = "Assets/_Project/App/Prefabs/App_System.prefab";
        const string SourceScenePath = "Assets/_Project/Scenes/Test_D.unity";
        const string FontPath = "Assets/_Project/UI/Fonts/NotoSansJP-Regular SDF.asset";
        const string ViewLookPath = "Assets/_Project/Rendering/ViewLook.asset";
        const string GestureSettingsPath = "Assets/_Project/UI/ViewGestureSettings.asset";
        const string GimmickMaterialPath = "Assets/_Project/App/Materials/GimmickUnlit.mat";
        const string TitleImagePath = "Assets/_Project/App/Art/Title.png";
        const string WordingPath = "Assets/_Project/UI/Wording.asset";
        const string PhotoMaterialPath = "Assets/_Project/Merge/Materials/PhotoUnlit.mat";
        const string CutoutMaterialPath = "Assets/_Project/Merge/Materials/CharacterCutout.mat";

        static readonly string[] KeepNames = { "StageLook", "DebugOverlay", "PlayLight" };

        [MenuItem("DeskStage/App/1. App_System プレハブを作る")]
        public static void Run()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var photoMaterial = AssetDatabase.LoadAssetAtPath<Material>(PhotoMaterialPath);
            var cutoutMaterial = AssetDatabase.LoadAssetAtPath<Material>(CutoutMaterialPath);
            if (photoMaterial == null || cutoutMaterial == null)
            {
                Debug.LogError("[App] 写真の素材がありません。先にメニュー「DeskStage/Merge/1. UI_System プレハブを作る」を一度実行してください");
                Finish(1);
                return;
            }

            var scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
            var app = new GameObject("App_System");
            foreach (var name in KeepNames)
            {
                Transform found = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.name == name) found = root.transform;
                }
                if (found == null)
                {
                    Debug.LogWarning($"[App] Test_D.unity に {name} がありません");
                    continue;
                }
                found.SetParent(app.transform, true);
            }

            var root2 = app.AddComponent<AppRoot>();
            var capture = app.AddComponent<CharacterCapture>();
            var texturer = app.AddComponent<StagePhotoTexturer>();
            var avatar = app.AddComponent<CharacterAvatar>();

            var rootSo = new SerializedObject(root2);
            rootSo.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            rootSo.FindProperty("viewLook").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ViewLook>(ViewLookPath);
            rootSo.FindProperty("viewGestureSettings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ViewGestureSettings>(GestureSettingsPath);
            rootSo.FindProperty("wording").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Wording>(WordingPath);
            rootSo.FindProperty("cameraCapture").objectReferenceValue = capture;
            rootSo.FindProperty("gimmickMaterial").objectReferenceValue = EnsureGimmickMaterial();
            AssetDatabase.ImportAsset(TitleImagePath, ImportAssetOptions.ForceUpdate);
            rootSo.FindProperty("titleImage").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(TitleImagePath);
            rootSo.ApplyModifiedPropertiesWithoutUndo();

            var texturerSo = new SerializedObject(texturer);
            texturerSo.FindProperty("photoMaterial").objectReferenceValue = photoMaterial;
            texturerSo.ApplyModifiedPropertiesWithoutUndo();
            var avatarSo = new SerializedObject(avatar);
            avatarSo.FindProperty("cutoutMaterial").objectReferenceValue = cutoutMaterial;
            avatarSo.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            PrefabUtility.SaveAsPrefabAsset(app, PrefabPath);
            Debug.Log($"[App] 作りました：{PrefabPath}");

            // Test_D は保存せずに閉じる
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Finish(0);
        }

        /// <summary>しかけの板に使う素材（URP の Unlit、半透明、両面）。無ければ作る。実行時の Shader.Find はビルドで削られるので、アセットにして、プレハブから参照する。</summary>
        static Material EnsureGimmickMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(GimmickMaterialPath);
            if (existing != null)
            {
                return existing;
            }
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            Directory.CreateDirectory(Path.GetDirectoryName(GimmickMaterialPath));
            var material = new Material(shader) { name = "GimmickUnlit" };
            material.SetFloat("_Surface", 1f); // Transparent
            material.SetFloat("_Blend", 0f);   // Alpha
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", 0f);    // 両面
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");
            AssetDatabase.CreateAsset(material, GimmickMaterialPath);
            AssetDatabase.SaveAssets();
            return material;
        }

        static void Finish(int code)
        {
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(code);
            }
        }
    }
}
