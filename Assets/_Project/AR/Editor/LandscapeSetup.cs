using UnityEditor;
using UnityEngine;

namespace DeskStage.AR.EditorTools
{
    /// <summary>
    /// 画面の向きを横に固定する。このゲームは横画面で遊ぶ（2026-10-24 西垣さんの要望）。
    /// 縦は許可しない。横は左右どちらの持ち方でも使えるように、自動回転で両方を許可する。
    /// </summary>
    public static class LandscapeSetup
    {
        [MenuItem("DeskStage/A/2b. 画面を横向きに固定する")]
        public static void Run()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // バッチ実行では、PlayerSettings の変更が自動で書き出されないことがある。明示的に保存する
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            foreach (var asset in settings)
            {
                EditorUtility.SetDirty(asset);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Landscape] 画面の向きを横に固定しました");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
