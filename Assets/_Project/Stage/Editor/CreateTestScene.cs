using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeskStage.Stage.EditorTools
{
    /// <summary>
    /// B の Step 5：Test_B.unity を作る。
    /// お手本ステージを組み立てるボタンと、机全体が見えるカメラを置く。
    /// </summary>
    public static class CreateTestScene
    {
        const string ScenePath = "Assets/_Project/Scenes/Test_B.unity";
        const string SettingsPath = "Assets/_Project/Stage/Settings/BoxSettings.asset";

        [MenuItem("DeskStage/B/2. テスト用シーンを作る")]
        public static void Run()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Test_B", $"{ScenePath} はすでにあります。作り直しますか？", "作り直す", "やめる"))
            {
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            // カメラと光だけの新しいシーン
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 机（90cm くらい）全体を手前斜め上から見る
            var camera = Camera.main;
            camera.transform.position = new Vector3(0f, 0.45f, -0.65f);
            camera.transform.LookAt(Vector3.zero);
            camera.nearClipPlane = 0.01f;

            var system = new GameObject("Stage_System");
            var builder = system.AddComponent<StageBuilder>();
            var button = system.AddComponent<SampleStageButton>();

            var builderSo = new SerializedObject(builder);
            builderSo.FindProperty("settings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<BoxSettings>(SettingsPath);
            builderSo.ApplyModifiedPropertiesWithoutUndo();

            var buttonSo = new SerializedObject(button);
            buttonSo.FindProperty("builder").objectReferenceValue = builder;
            buttonSo.ApplyModifiedPropertiesWithoutUndo();

            // 写真の答えから箱を作る部品と、保存した答えを読み込むボタン（Step 7）
            var reader = system.AddComponent<StageReader>();
            var readerSo = new SerializedObject(reader);
            readerSo.FindProperty("settings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<BoxSettings>(SettingsPath);
            readerSo.ApplyModifiedPropertiesWithoutUndo();

            var answerButton = system.AddComponent<AnswerFileButton>();
            var answerSo = new SerializedObject(answerButton);
            answerSo.FindProperty("builder").objectReferenceValue = builder;
            answerSo.ApplyModifiedPropertiesWithoutUndo();

            // ステージの一覧・読み込み・保存・消す（Step 10・11）と、その仮の画面
            var library = system.AddComponent<StageLibrary>();
            var librarySo = new SerializedObject(library);
            librarySo.FindProperty("builder").objectReferenceValue = builder;
            librarySo.ApplyModifiedPropertiesWithoutUndo();
            system.AddComponent<StageLibraryDebugPanel>();

            // 読み取りの補助（Step 9）と、その仮の操作。箱を直したら、その箱だけを出し直す
            builderSo.Update();
            builderSo.FindProperty("followWorkingBoxes").boolValue = true;
            builderSo.ApplyModifiedPropertiesWithoutUndo();
            var assist = system.AddComponent<BoxAssist>();
            var assistSo = new SerializedObject(assist);
            assistSo.FindProperty("settings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<BoxSettings>(SettingsPath);
            assistSo.ApplyModifiedPropertiesWithoutUndo();
            var assistPanel = system.AddComponent<BoxAssistDebugPanel>();
            var assistPanelSo = new SerializedObject(assistPanel);
            assistPanelSo.FindProperty("assist").objectReferenceValue = assist;
            assistPanelSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Stage] {ScenePath} を作りました。再生してボタンを押してください");
        }
    }
}
