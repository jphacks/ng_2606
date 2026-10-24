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
    /// A_ar.md Step 1 の1、Step 2、Step 3 の1、Step 6、9、10、8、11。
    /// Test_A シーンと ScanSettings アセットを作る。
    /// シーンを手で組むと人によって中身がずれるので、ここから作る（CLAUDE.md の決まり）。
    /// </summary>
    public static class CreateTestScene
    {
        const string ScenePath = "Assets/_Project/Scenes/Test_A.unity";
        const string SettingsPath = "Assets/_Project/AR/Settings/ScanSettings.asset";

        const string MaterialPath = "Assets/_Project/AR/Debug/PlaneVisual.mat";

        /// <summary>
        /// 面に貼る半透明のマテリアルをアセットとして作る。
        /// 実行時に Shader.Find で作ると、ビルドでシェーダーが削られて
        /// null になり落ちる。アセットにしてシーンから参照させれば残る。
        /// </summary>
        static Material CreatePlaneMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null)
            {
                return existing;
            }
            var material = PlaneProbe.CreateTransparentMaterial();
            if (material == null)
            {
                Debug.LogWarning("[Step3] 面のマテリアルを作れませんでした");
                return null;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
            AssetDatabase.CreateAsset(material, MaterialPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Step3] 作りました: " + MaterialPath);
            return material;
        }

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
            var raycastManager = originGo.AddComponent<ARRaycastManager>();

            // Step 8：StageRoot に付ける AR Anchor を作る管理役
            var anchorManager = originGo.AddComponent<ARAnchorManager>();

            // Step 6：上向きの水平な面だけを覚えておく
            var watcher = originGo.AddComponent<PlaneWatcher>();

            var probe = originGo.AddComponent<PlaneProbe>();
            var serialized = new SerializedObject(probe);
            serialized.FindProperty("m_Settings").objectReferenceValue = settings;
            serialized.FindProperty("m_PlaneMaterialAsset").objectReferenceValue = CreatePlaneMaterial();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Step 9・10：スキャン状態の報告と、開始・終了の受け付け。
            // Test_A は PlaneProbe を単体で動かすので、起動時に AR は止めない
            var scanGo = new GameObject("Scan");
            var reporter = scanGo.AddComponent<ScanStatusReporter>();
            var session = scanGo.AddComponent<ScanSession>();
            scanGo.AddComponent<ScanSessionDebugPanel>();

            var reporterSerialized = new SerializedObject(reporter);
            reporterSerialized.FindProperty("m_Settings").objectReferenceValue = settings;
            reporterSerialized.ApplyModifiedPropertiesWithoutUndo();

            var sessionSerialized = new SerializedObject(session);
            sessionSerialized.FindProperty("m_Session").objectReferenceValue = sessionGo.GetComponent<ARSession>();
            sessionSerialized.FindProperty("m_PlaneManager").objectReferenceValue = planeManager;
            sessionSerialized.FindProperty("m_Reporter").objectReferenceValue = reporter;
            sessionSerialized.FindProperty("m_DisableOnAwake").boolValue = false;
            sessionSerialized.ApplyModifiedPropertiesWithoutUndo();

            // Step 8：机を決めて StageRoot を置く。確認用に、タップと立方体の仮部品も付ける
            var deskFrame = scanGo.AddComponent<DeskFrame>();
            scanGo.AddComponent<DeskFrameDebug>();
            var deskSerialized = new SerializedObject(deskFrame);
            deskSerialized.FindProperty("m_Watcher").objectReferenceValue = watcher;
            deskSerialized.FindProperty("m_RaycastManager").objectReferenceValue = raycastManager;
            deskSerialized.FindProperty("m_AnchorManager").objectReferenceValue = anchorManager;
            deskSerialized.FindProperty("m_Settings").objectReferenceValue = settings;
            deskSerialized.ApplyModifiedPropertiesWithoutUndo();

            // Step 11：面を整えて ScanContext.Surfaces に書く
            var stabilizer = scanGo.AddComponent<SurfaceStabilizer>();
            var stabilizerSerialized = new SerializedObject(stabilizer);
            stabilizerSerialized.FindProperty("m_Watcher").objectReferenceValue = watcher;
            stabilizerSerialized.FindProperty("m_Settings").objectReferenceValue = settings;
            stabilizerSerialized.ApplyModifiedPropertiesWithoutUndo();
            scanGo.AddComponent<SurfaceDebugPanel>();

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
