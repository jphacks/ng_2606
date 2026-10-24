using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeskStage.AR.EditorTools
{
    /// <summary>
    /// A の Step 22：写真の方式を試す Test_A_Photo.unity を作る。
    /// カメラ（またはエディタでは保存した写真）で2枚撮るための、仮のボタンと映像を置く。AR は使わない。
    /// </summary>
    public static class CreatePhotoTestScene
    {
        const string ScenePath = "Assets/_Project/Scenes/Test_A_Photo.unity";
        const string SettingsPath = "Assets/_Project/AR/Settings/ScanSettings.asset";

        [MenuItem("DeskStage/A/4. 写真のテスト用シーンを作る")]
        public static void Run()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Test_A_Photo", $"{ScenePath} はすでにあります。作り直しますか？", "作り直す", "やめる"))
            {
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var system = new GameObject("Photo_System");
            var capture = system.AddComponent<PhotoCapture>();
            var captureSo = new SerializedObject(capture);
            captureSo.FindProperty("settings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ScanSettings>(SettingsPath);
            captureSo.FindProperty("useTestPhotos").boolValue = true;
            captureSo.ApplyModifiedPropertiesWithoutUndo();

            var panel = system.AddComponent<PhotoCaptureDebugPanel>();
            var panelSo = new SerializedObject(panel);
            panelSo.FindProperty("capture").objectReferenceValue = capture;
            panelSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Photo] {ScenePath} を作りました。再生して「撮影をはじめる」→「とる」を2回押してください");
        }
    }
}
