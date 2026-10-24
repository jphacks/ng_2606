using UnityEditor;
using UnityEngine;

namespace DeskStage.Stage.EditorTools
{
    /// <summary>
    /// B の Step 3：初期値の BoxSettings アセットを決まった場所に作る。
    /// すでにあるときは作り直さず、そのアセットを選ぶだけにする（調整した値を消さないため）。
    /// </summary>
    public static class CreateBoxSettings
    {
        const string Folder = "Assets/_Project/Stage/Settings";
        const string AssetPath = Folder + "/BoxSettings.asset";

        [MenuItem("DeskStage/B/1. 箱の設定アセットを作る")]
        public static void Run()
        {
            var existing = AssetDatabase.LoadAssetAtPath<BoxSettings>(AssetPath);
            if (existing != null)
            {
                Debug.Log($"[Stage] {AssetPath} はすでにあります。作り直していません");
                Selection.activeObject = existing;
                return;
            }

            var settings = ScriptableObject.CreateInstance<BoxSettings>();
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = settings;
            Debug.Log($"[Stage] {AssetPath} を作りました");
        }
    }
}
