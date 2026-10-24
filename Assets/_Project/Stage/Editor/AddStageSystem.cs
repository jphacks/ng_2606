using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeskStage.Stage.EditorTools
{
    /// <summary>
    /// 今開いているシーンに、B の部品をまとめた Stage_System を足す。
    /// A の撮影・送受信と B の箱づくりを合わせて確かめるため（B の Step 7・8 の確認）。
    /// ・StageReader：Claude の答えから作業中の箱を作る（写真の方式、00_overview.md 9章）
    /// ・StageBuilder：作業中の箱が変わるたびに、その箱を StageRoot の子に出す
    /// 合流（Step 14）でプレハブにする前の、確認用の入口。
    /// </summary>
    public static class AddStageSystem
    {
        const string ObjectName = "Stage_System";
        const string SettingsPath = "Assets/_Project/Stage/Settings/BoxSettings.asset";

        [MenuItem("DeskStage/B/3. 今のシーンに Stage_System を足す")]
        public static void Run()
        {
            var existing = GameObject.Find(ObjectName);
            if (existing != null)
            {
                Debug.Log($"[Stage] {ObjectName} はすでにこのシーンにあります。足していません");
                Selection.activeGameObject = existing;
                return;
            }

            var settings = AssetDatabase.LoadAssetAtPath<BoxSettings>(SettingsPath);
            var system = new GameObject(ObjectName);
            Undo.RegisterCreatedObjectUndo(system, $"Add {ObjectName}");

            var reader = system.AddComponent<StageReader>();
            var boxSo = new SerializedObject(reader);
            boxSo.FindProperty("settings").objectReferenceValue = settings;
            boxSo.ApplyModifiedPropertiesWithoutUndo();

            var stageBuilder = system.AddComponent<StageBuilder>();
            var stageSo = new SerializedObject(stageBuilder);
            stageSo.FindProperty("settings").objectReferenceValue = settings;
            stageSo.FindProperty("followWorkingBoxes").boolValue = true;
            stageSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(system.scene);
            Selection.activeGameObject = system;
            Debug.Log($"[Stage] {ObjectName} を足しました。シーンを保存してからビルドしてください");
        }
    }
}
