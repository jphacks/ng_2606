using System.IO;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace DeskStage.AR.EditorTools
{
    /// <summary>
    /// A_ar.md Step 1 の1、Step 2、Step 3 の1。
    /// Test_A シーンと ScanSettings アセットを作る。
    /// シーンを手で組むと人によって中身がずれるので、ここから作る（CLAUDE.md の決まり）。
    /// </summary>
    public static class CreateTestScene
    {
        const string ScenePath = "Assets/_Project/Scenes/Test_A.unity";
        const string SettingsPath = "Assets/_Project/AR/Settings/ScanSettings.asset";

        /// <summary>
        /// Test_A をビルド対象の先頭に入れる。入れ忘れるとビルドしても
        /// 別のシーンが起動して、原因が分かりにくい。
        /// </summary>
        static void RegisterInBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenePath, true),
            };
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (s.path != ScenePath)
                {
                    scenes.Add(new EditorBuildSettingsScene(s.path, false));
                }
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[Step1] ビルド対象に入れました: " + ScenePath);
        }

        [MenuItem("DeskStage/A/スキャンの設定アセットを作る")]
        public static ScanSettings CreateSettings()
        {
            var existing = AssetDatabase.LoadAssetAtPath<ScanSettings>(SettingsPath);
            if (existing != null)
            {
                Debug.Log("[Step2] すでにあります: " + SettingsPath);
                return existing;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            var settings = ScriptableObject.CreateInstance<ScanSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Step2] 作りました: " + SettingsPath);
            return settings;
        }

        [MenuItem("DeskStage/A/テスト用シーンを作る")]
        public static void CreateScene()
        {
            var settings = CreateSettings();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 光は要らないが、立方体が見えるように1つ置く
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // AR Session
            var sessionGo = new GameObject("AR Session");
            sessionGo.AddComponent<ARSession>();
            sessionGo.AddComponent<ARInputManager>();

            // XR Origin (Mobile AR)
            var originGo = new GameObject("XR Origin");
            var origin = originGo.AddComponent<XROrigin>();

            var offsetGo = new GameObject("Camera Offset");
            offsetGo.transform.SetParent(originGo.transform, false);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(offsetGo.transform, false);
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.nearClipPlane = 0.01f;   // 3cm のキャラを見るので近くまで映す
            camera.farClipPlane = 20f;
            cameraGo.AddComponent<ARCameraManager>();
            cameraGo.AddComponent<ARCameraBackground>();
            cameraGo.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();

            origin.Camera = camera;
            origin.CameraFloorOffsetObject = offsetGo;

            // Step 3 の1：平らな面の検出とレイキャスト
            var planeManager = originGo.AddComponent<ARPlaneManager>();
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            originGo.AddComponent<ARRaycastManager>();

            // Step 6：上向きの水平な面だけを覚えておく
            originGo.AddComponent<PlaneWatcher>();

            var probe = originGo.AddComponent<PlaneProbe>();
            var serialized = new SerializedObject(probe);
            serialized.FindProperty("m_Settings").objectReferenceValue = settings;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // 動作確認用：前方50cm に一辺5cm の立方体
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "CheckCube";
            cube.transform.localScale = Vector3.one * 0.05f;
            cube.transform.position = new Vector3(0f, 0f, 0.5f);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            RegisterInBuildSettings();

            Debug.Log("[Step1] 作りました: " + ScenePath);

            // バッチ実行では自分で終了しないと、エディタがプロジェクトの
            // ロックを握ったまま残り続ける
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
