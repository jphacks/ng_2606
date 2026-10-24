using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace DeskStage.AR.EditorTools
{
    /// <summary>
    /// iOS のスクリプトシンボル UNITY_XR_ARKIT_LOADER_ENABLED を ProjectSettings に入れる。
    /// ARKit パッケージは、GUI で開いたときだけこれを自動で足す。バッチ実行では足されず、
    /// ビルド時の組み込み処理（ARKitBuildProcessor.PreprocessBuild）が丸ごと無効になって、
    /// libUnityARKit.a が組み込まれない（実機で「Failed to load session subsystem」になる）。
    /// ProjectSettings に入れておけば、GUI でもバッチでも同じビルドになる。
    /// </summary>
    public static class ArkitBuildSetup
    {
        public const string LoaderDefine = "UNITY_XR_ARKIT_LOADER_ENABLED";

        public static bool HasLoaderDefine()
        {
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.iOS);
            return defines.Split(';').Contains(LoaderDefine);
        }

        [MenuItem("DeskStage/A/2. ARKit のビルド設定をそろえる")]
        public static void Run()
        {
            if (HasLoaderDefine())
            {
                Debug.Log("[ArkitSetup] すでに入っています: " + LoaderDefine);
            }
            else
            {
                var current = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.iOS);
                var next = string.IsNullOrEmpty(current) ? LoaderDefine : current + ";" + LoaderDefine;
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.iOS, next);
                AssetDatabase.SaveAssets();
                Debug.Log("[ArkitSetup] 入れました: " + LoaderDefine);
            }

            // 入れた直後はスクリプトの再読み込みが走る。バッチ実行はここで終わる
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
