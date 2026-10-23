using System.Threading;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace DeskStage.EditorTools
{
    /// <summary>
    /// 全体 Step 1 の3：AR Foundation と Apple ARKit XR Plugin を入れる。
    /// 版を指定しないので、このエディタに合うものが選ばれる。
    /// </summary>
    public static class AddPackages
    {
        static readonly string[] Wanted =
        {
            "com.unity.xr.arfoundation",
            "com.unity.xr.arkit",
        };

        [MenuItem("DeskStage/A/1. AR パッケージを入れる")]
        public static void Run()
        {
            var request = Client.AddAndRemove(Wanted, null);
            while (!request.IsCompleted)
            {
                Thread.Sleep(200);
            }

            if (request.Status == StatusCode.Failure)
            {
                Debug.LogError("[Setup] パッケージの追加に失敗しました: " + request.Error.message);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
                return;
            }

            foreach (var package in request.Result)
            {
                Debug.Log("[Setup] 入りました: " + package.name + " " + package.version);
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
