using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DeskStage.AR.EditorTools
{
    /// <summary>
    /// A_ar.md Step 1 の2：iOS 向けにビルドして Xcode プロジェクトを書き出す。
    /// 書き出し先はプロジェクトの外（中に置くと Git に入ってしまう）。
    /// Development Build のチェック忘れを防ぐため、スクリプトから固定で入れる。
    /// </summary>
    public static class BuildIos
    {
        /// <summary>プロジェクトの外。~/Builds/DeskStageAR</summary>
        static string OutputPath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Builds", "DeskStageAR");

        [MenuItem("DeskStage/A/3. iOS 向けにビルドする")]
        public static void Run()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("[Build] ビルド対象のシーンがありません");
                Finish(1);
                return;
            }

            Directory.CreateDirectory(OutputPath);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                // Development Build。Profiler をつなげられるようにもしておく
                options = BuildOptions.Development | BuildOptions.AllowDebugging,
            };

            Debug.Log("[Build] シーン: " + string.Join(", ", scenes));
            Debug.Log("[Build] 書き出し先: " + OutputPath);

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Build] 成功。{summary.totalSize / (1024 * 1024)} MB / {summary.totalTime}");
                Debug.Log("[Build] 開くもの: " + Path.Combine(OutputPath, "Unity-iPhone.xcodeproj"));
                Finish(0);
                return;
            }

            Debug.LogError($"[Build] 失敗: {summary.result} / エラー {summary.totalErrors} 件");
            foreach (var step in report.steps)
            {
                foreach (var message in step.messages)
                {
                    if (message.type == LogType.Error || message.type == LogType.Exception)
                    {
                        Debug.LogError("[Build] " + message.content);
                    }
                }
            }
            Finish(1);
        }

        static void Finish(int code)
        {
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(code);
            }
        }
    }
}
