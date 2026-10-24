using UnityEditor;
using UnityEngine;

namespace DeskStage.Gameplay.EditorTools
{
    /// <summary>
    /// C の Step 13：合流用の Game_System.prefab を作る。Main.unity にはこのプレハブを置くだけにする（00_overview.md 6章）。
    /// 中身：GameFlow、CourseMarkers、CoursePlacer、SafetyGuard、RecordStore と Player プレハブへの参照、
    /// 子の PlayCamera（ViewCamera、CameraRelativeInput。遊ぶ画面のときだけ出る）。作り直すと同じファイルに上書きする。
    /// </summary>
    public static class CreateGameSystemPrefab
    {
        const string Folder = "Assets/_Project/Gameplay/Prefabs";
        public const string PrefabPath = Folder + "/Game_System.prefab";

        [MenuItem("DeskStage/C/5. Game_System プレハブを作る")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Gameplay] ▶（Play）を止めてから実行してください");
                return;
            }
            var settings = AssetDatabase.LoadAssetAtPath<CharacterSettings>(CreateCharacterSettings.AssetPath);
            if (settings == null)
            {
                Debug.LogError($"[Gameplay] {CreateCharacterSettings.AssetPath} がありません。先に「DeskStage/C/1. キャラの設定アセットを作る」を実行してください");
                return;
            }
            // Player プレハブは、いまの CharacterSettings の値で作り直してから参照する
            var playerPrefab = CreatePlayerPrefab.Build();
            if (playerPrefab == null)
            {
                return;
            }

            var root = new GameObject("Game_System");

            // 遊ぶ画面のカメラ。A の AR のカメラと取り合わないよう、MainCamera のタグと AudioListener は付けない
            var cameraObject = new GameObject("PlayCamera");
            cameraObject.transform.SetParent(root.transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.01f;
            // ほかのカメラが残っていても、遊ぶ画面はこのカメラが上に映るようにする
            camera.depth = 10f;
            var viewCamera = cameraObject.AddComponent<ViewCamera>();
            viewCamera.settings = settings;
            var relative = cameraObject.AddComponent<CameraRelativeInput>();
            relative.viewCamera = cameraObject.transform;

            var gameFlow = root.AddComponent<GameFlow>();
            gameFlow.playerPrefab = playerPrefab;
            gameFlow.viewCamera = viewCamera;
            gameFlow.cameraRelativeInput = relative;
            gameFlow.playCamera = cameraObject;
            gameFlow.recordStore = root.AddComponent<RecordStore>();

            var markers = root.AddComponent<CourseMarkers>();
            markers.gameFlow = gameFlow;
            markers.settings = settings;

            var guard = root.AddComponent<SafetyGuard>();
            guard.gameFlow = gameFlow;
            guard.settings = settings;

            // たしかめ画面で置くときは、A の AR のカメラ（Camera.main）を使う
            var placer = root.AddComponent<CoursePlacer>();
            placer.courseMarkers = markers;
            placer.settings = settings;

            // 遊ぶ画面になるまでは隠しておく（Play で GameFlow が出す）
            cameraObject.SetActive(false);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Selection.activeObject = prefab;
            Debug.Log($"[Gameplay] {PrefabPath} を作りました");
        }
    }
}
