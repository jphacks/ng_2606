using UnityEditor;
using UnityEngine;

namespace DeskStage.Gameplay.EditorTools
{
    /// <summary>
    /// C の Step 2：初期値の CharacterSettings アセットを決まった場所に作る。
    /// すでにあるときは作り直さず、そのアセットを選ぶだけにする（調整した値を消さないため）。
    /// </summary>
    public static class CreateCharacterSettings
    {
        const string Folder = "Assets/_Project/Gameplay/Settings";
        public const string AssetPath = Folder + "/CharacterSettings.asset";

        [MenuItem("DeskStage/C/1. キャラの設定アセットを作る")]
        public static void Run()
        {
            var existing = AssetDatabase.LoadAssetAtPath<CharacterSettings>(AssetPath);
            if (existing != null)
            {
                Debug.Log($"[Gameplay] {AssetPath} はすでにあります。作り直していません");
                Selection.activeObject = existing;
                return;
            }

            var settings = ScriptableObject.CreateInstance<CharacterSettings>();
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = settings;
            Debug.Log($"[Gameplay] {AssetPath} を作りました");
        }
    }
}
