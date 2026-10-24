using DeskStage.Stage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeskStage.AR.EditorTools
{
    /// <summary>
    /// A の Step 22・25：写真の方式を試す Test_A_Photo.unity を作る。AR は使わない。
    /// カメラ（またはエディタでは保存した写真）で2枚撮り、サーバーに送り、B の箱づくりで箱を出すまでを1つのシーンで通す。
    /// </summary>
    public static class CreatePhotoTestScene
    {
        const string ScenePath = "Assets/_Project/Scenes/Test_A_Photo.unity";
        const string SettingsPath = "Assets/_Project/AR/Settings/ScanSettings.asset";
        const string BoxSettingsPath = "Assets/_Project/Stage/Settings/BoxSettings.asset";

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

            // 机（50〜90cm くらい）の箱が見えるように、手前斜め上から見る
            var camera = Camera.main;
            camera.transform.position = new Vector3(0f, 0.6f, -0.8f);
            camera.transform.LookAt(Vector3.zero);
            camera.nearClipPlane = 0.01f;

            var system = new GameObject("Photo_System");
            var capture = system.AddComponent<PhotoCapture>();
            var captureSo = new SerializedObject(capture);
            captureSo.FindProperty("settings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ScanSettings>(SettingsPath);
            captureSo.FindProperty("useTestPhotos").boolValue = true;
            captureSo.ApplyModifiedPropertiesWithoutUndo();

            // サーバーへの送受信（Step 25）。設定（合言葉）は Git に入らないので、なければ空のまま
            var client = system.AddComponent<ScanClient>();
            var clientSo = new SerializedObject(client);
            clientSo.FindProperty("capture").objectReferenceValue = capture;
            clientSo.FindProperty("config").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ServerConfig>(CreateServerConfig.AssetPath);
            clientSo.ApplyModifiedPropertiesWithoutUndo();

            var panel = system.AddComponent<PhotoCaptureDebugPanel>();
            var panelSo = new SerializedObject(panel);
            panelSo.FindProperty("capture").objectReferenceValue = capture;
            panelSo.FindProperty("client").objectReferenceValue = client;
            panelSo.ApplyModifiedPropertiesWithoutUndo();

            // B の部品：答えから箱を作り（StageReader）、箱が変わるたびに出す（StageBuilder）
            var stage = new GameObject("Stage_System");
            var boxSettings = AssetDatabase.LoadAssetAtPath<BoxSettings>(BoxSettingsPath);
            var reader = stage.AddComponent<StageReader>();
            var readerSo = new SerializedObject(reader);
            readerSo.FindProperty("settings").objectReferenceValue = boxSettings;
            readerSo.ApplyModifiedPropertiesWithoutUndo();
            var builder = stage.AddComponent<StageBuilder>();
            var builderSo = new SerializedObject(builder);
            builderSo.FindProperty("settings").objectReferenceValue = boxSettings;
            builderSo.FindProperty("followWorkingBoxes").boolValue = true;
            builderSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Photo] {ScenePath} を作りました。サーバーを動かしてから、再生して「撮影をはじめる」→「とる」を2回押してください");
        }
    }
}
