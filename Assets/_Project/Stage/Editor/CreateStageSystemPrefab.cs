using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeskStage.Stage.EditorTools
{
    /// <summary>
    /// 合流用の Stage_System プレハブを作る（B_scan_data.md Step 14）。
    /// 答えから箱を作る（StageReader）、机の板と箱を出す（StageBuilder）、保存・一覧・読み込み（StageLibrary）、
    /// 読み取りの補助（BoxAssist）。デバッグ用のボタンやパネルは入れない。
    /// </summary>
    public static class CreateStageSystemPrefab
    {
        public const string PrefabPath = "Assets/_Project/Stage/Prefabs/Stage_System.prefab";
        const string SettingsPath = "Assets/_Project/Stage/Settings/BoxSettings.asset";

        [MenuItem("DeskStage/B/4. Stage_System プレハブを作る")]
        public static void Run()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            var settings = AssetDatabase.LoadAssetAtPath<BoxSettings>(SettingsPath);

            var root = new GameObject("Stage_System");
            try
            {
                var builder = root.AddComponent<StageBuilder>();
                var builderSo = new SerializedObject(builder);
                builderSo.FindProperty("settings").objectReferenceValue = settings;
                builderSo.FindProperty("followWorkingBoxes").boolValue = true;
                builderSo.ApplyModifiedPropertiesWithoutUndo();

                var reader = root.AddComponent<StageReader>();
                var readerSo = new SerializedObject(reader);
                readerSo.FindProperty("settings").objectReferenceValue = settings;
                readerSo.FindProperty("builder").objectReferenceValue = builder;
                readerSo.ApplyModifiedPropertiesWithoutUndo();

                var library = root.AddComponent<StageLibrary>();
                var librarySo = new SerializedObject(library);
                librarySo.FindProperty("builder").objectReferenceValue = builder;
                librarySo.ApplyModifiedPropertiesWithoutUndo();

                var assist = root.AddComponent<BoxAssist>();
                var assistSo = new SerializedObject(assist);
                assistSo.FindProperty("settings").objectReferenceValue = settings;
                assistSo.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            Debug.Log($"[Merge] 作りました：{PrefabPath}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
