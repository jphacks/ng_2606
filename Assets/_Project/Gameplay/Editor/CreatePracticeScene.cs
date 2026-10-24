using System.IO;
using DeskStage.Shared;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeskStage.Gameplay.EditorTools
{
    /// <summary>
    /// C の Step 3：AR なしで PC でキャラを試す練習用シーン Test_C.unity を作る。
    /// 床と、高さの決まり（Docs/StepRules.md）を確かめる箱を並べ、キャラを置く。
    /// </summary>
    public static class CreatePracticeScene
    {
        const string ScenePath = "Assets/_Project/Scenes/Test_C.unity";
        const string SampleStagePath = "Stages/sample-01/stage.json";

        // 練習用の箱の幅・奥行き（m）
        const float BoxWidth = 0.08f;

        // お手本ステージを練習用の床と重ならないように置く位置
        static readonly Vector3 SampleOffset = new Vector3(1f, 0f, 0f);

        [MenuItem("DeskStage/C/3. 練習用シーンを作る")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("練習用シーン", "Test_C.unity はすでにあります。作り直しますか？", "作り直す", "やめる"))
            {
                return;
            }

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreatePlayerPrefab.PrefabPath);
            if (playerPrefab == null)
            {
                playerPrefab = CreatePlayerPrefab.Build();
                if (playerPrefab == null)
                {
                    return;
                }
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 小さな世界なので、近くの物が切れないよう Near Clip Plane を小さくする
            var camera = Camera.main;
            camera.nearClipPlane = 0.01f;
            camera.transform.position = new Vector3(0f, 0.45f, -0.55f);
            camera.transform.LookAt(Vector3.zero);

            var stageRoot = new GameObject("StageRoot").transform;

            // 床：幅60cm・奥行き60cm・厚さ1cm。上面が高さ0
            CreateBox(stageRoot, "Floor", new Vector3(0f, -0.005f, 0f), new Vector3(0.6f, 0.01f, 0.6f));

            // 高さの決まりの境目を確かめる箱（奥の列）
            float[] heights = { 0.005f, 0.02f, 0.04f, 0.045f, 0.05f, 0.08f };
            for (int i = 0; i < heights.Length; i++)
            {
                float h = heights[i];
                float x = -0.225f + 0.09f * i;
                CreateBox(stageRoot, $"Box_{h * 100f:0.#}cm", new Vector3(x, h / 2f, 0.18f), new Vector3(BoxWidth, h, BoxWidth));
            }

            // 2cm を足場にして 6cm へ登る、くっつけた箱（真ん中の列）
            CreateBox(stageRoot, "Pair_2cm", new Vector3(-0.12f, 0.01f, 0f), new Vector3(BoxWidth, 0.02f, BoxWidth));
            CreateBox(stageRoot, "Pair_6cm", new Vector3(-0.04f, 0.03f, 0f), new Vector3(BoxWidth, 0.06f, BoxWidth));

            // すき間 1cm（渡れる）と 2cm（落ちる）の箱の組（手前の列）
            CreateGapPair(stageRoot, "Gap1cm", -0.15f, 0.01f);
            CreateGapPair(stageRoot, "Gap2cm", 0.12f, 0.02f);

            // 視点のカメラ（Step 6）：キャラを追う。マウスの右ドラッグで回り、Q・E で近づく・離れる
            var viewCamera = camera.gameObject.AddComponent<ViewCamera>();
            viewCamera.settings = AssetDatabase.LoadAssetAtPath<CharacterSettings>(CreateCharacterSettings.AssetPath);
            camera.gameObject.AddComponent<ViewDebugInput>();

            // カメラ基準の向き（Step 7）：スティックの上が画面の奥になる
            var relative = camera.gameObject.AddComponent<CameraRelativeInput>();
            relative.viewCamera = camera.transform;

            // ゲームの進行（Step 11）：右上の仮のボタンで「ステージかんせい」→「あそぶ」と押すと、キャラがスタートに出る
            var gameFlow = new GameObject("GameFlow").AddComponent<GameFlow>();
            gameFlow.playerPrefab = playerPrefab;
            gameFlow.viewCamera = viewCamera;
            gameFlow.cameraRelativeInput = relative;
            var panel = gameFlow.gameObject.AddComponent<GameFlowDebugPanel>();
            panel.gameFlow = gameFlow;
            panel.practiceRoot = stageRoot;

            // 瞬間移動の確かめ（Step 5）：ボタンで 6cm の箱の上へ移す
            var target = new GameObject("TeleportTarget").transform;
            target.SetParent(stageRoot, false);
            target.localPosition = new Vector3(-0.04f, 0.061f, 0f);
            var button = new GameObject("TeleportTestButton").AddComponent<TeleportTestButton>();
            button.gameFlow = gameFlow;
            button.target = target;

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Gameplay] {ScenePath} を作りました");
        }

        [MenuItem("DeskStage/C/4. お手本ステージを練習用シーンに置く")]
        public static void PlaceSampleStage()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                Debug.LogError($"[Gameplay] {ScenePath} を開いてから実行してください");
                return;
            }

            string jsonPath = Path.Combine(Application.streamingAssetsPath, SampleStagePath);
            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"[Gameplay] {jsonPath} がありません");
                return;
            }
            var stage = JsonUtility.FromJson<StageData>(File.ReadAllText(jsonPath));

            // 前に置いたお手本ステージは消して置き直す
            var old = GameObject.Find("SampleStage");
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }

            var sampleRoot = new GameObject("SampleStage").transform;
            sampleRoot.position = SampleOffset;

            // 机の板：厚さ1cm。上面が高さ0
            CreateBox(sampleRoot, "Desk", new Vector3(0f, -0.005f, 0f), new Vector3(stage.desk.width, 0.01f, stage.desk.depth));

            foreach (var box in stage.boxes)
            {
                var go = CreateBox(sampleRoot, box.id, Vec3.To(box.center), Vec3.To(box.size));
                go.transform.localRotation = Quaternion.Euler(0f, box.yawDegrees, 0f);
            }

            // 「ステージかんせい」でお手本ステージを使うようにする（キャラはお手本のスタートに出る）
            var panel = Object.FindFirstObjectByType<GameFlowDebugPanel>();
            if (panel != null)
            {
                panel.sampleRoot = sampleRoot;
                panel.sampleStageId = stage.id;
                EditorUtility.SetDirty(panel);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Gameplay] お手本ステージ {stage.id}（箱 {stage.boxes.Length} 個）を置きました。保存は Ctrl+S");
        }

        // すき間を空けて2つ並べた高さ2cmの箱
        static void CreateGapPair(Transform parent, string name, float centerX, float gap)
        {
            float offset = (BoxWidth + gap) / 2f;
            var size = new Vector3(BoxWidth, 0.02f, BoxWidth);
            CreateBox(parent, name + "_L", new Vector3(centerX - offset, 0.01f, -0.15f), size);
            CreateBox(parent, name + "_R", new Vector3(centerX + offset, 0.01f, -0.15f), size);
        }

        // 当たり判定つきの箱（Terrain）を作る
        static GameObject CreateBox(Transform parent, string name, Vector3 localCenter, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.layer = Layers.Terrain;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localCenter;
            go.transform.localScale = size;
            return go;
        }
    }
}
