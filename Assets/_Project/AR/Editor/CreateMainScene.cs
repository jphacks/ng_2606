using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeskStage.AR.EditorTools
{
    /// <summary>
    /// 合流用の Main.unity を作る（00_overview.md 6章「合流（M3）の決まり」）。
    /// Main には、各担当のプレハブ（AR_System、Stage_System、Game_System）を置くだけにする。
    /// UI_System は、D の正式な物ができるまで、Test_D から作ったつなぎの物（Merge/Prefabs）を置く。
    /// Main を変えるときは、チームのチャットで「Main触ります」「Main終わりました」と伝える。
    /// </summary>
    public static class CreateMainScene
    {
        public const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        const string StagePrefabPath = "Assets/_Project/Stage/Prefabs/Stage_System.prefab";
        const string UiPrefabPath = "Assets/_Project/Merge/Prefabs/UI_System.prefab";
        const string GamePrefabPath = "Assets/_Project/Gameplay/Prefabs/Game_System.prefab";

        [MenuItem("DeskStage/A/7. Main シーンを作る")]
        public static void Run()
        {
            if (!Application.isBatchMode)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                    !EditorUtility.DisplayDialog("Main", $"{ScenePath} はすでにあります。作り直しますか？（Main を触るときは、先にチームに伝えてください）", "作り直す", "やめる"))
                {
                    return;
                }
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // カメラは1つだけ。たしかめ画面で、机の上を手前斜め上から見る（遊ぶときは Game_System の PlayCamera が上に描く）
            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.13f, 0.16f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 20f;
            cameraGo.AddComponent<AudioListener>();
            cameraGo.transform.position = new Vector3(0f, 0.55f, -0.75f);
            cameraGo.transform.LookAt(Vector3.zero);

            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Place(CreateArSystemPrefab.PrefabPath);
            Place(StagePrefabPath);
            Place(GamePrefabPath);

            // D の UI（Test_D から仮の部品を除いた UI_System。メニュー「DeskStage/Merge/1. UI_System プレハブを作る」で作る）
            Place(UiPrefabPath);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[Merge] 作りました：{ScenePath}");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        static void Place(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[Merge] プレハブがありません：{prefabPath}。先にプレハブを作ってください");
                return;
            }
            PrefabUtility.InstantiatePrefab(prefab);
        }
    }
}
