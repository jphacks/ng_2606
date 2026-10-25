using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeskStage.AR.EditorTools
{
    /// <summary>
    /// A の Step 25：サーバーの設定（ServerConfig.asset）を作る。合言葉は Server/.env の APP_TOKEN から写す。
    /// このアセットは Git に入らない（AR/Resources/.gitignore）。実行時は ScanClient が Resources から探す。合言葉を変えたら、もう一度実行する。
    /// </summary>
    public static class CreateServerConfig
    {
        public const string AssetPath = "Assets/_Project/AR/Resources/ServerConfig.asset";

        [MenuItem("DeskStage/A/5. サーバーの設定を作る")]
        public static void Run()
        {
            var config = AssetDatabase.LoadAssetAtPath<ServerConfig>(AssetPath);
            if (config == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
                config = ScriptableObject.CreateInstance<ServerConfig>();
                AssetDatabase.CreateAsset(config, AssetPath);
            }

            var token = ReadAppToken();
            config.appToken = token ?? "";

            // バッチ実行では -serverUrl <url> で、サーバーの場所を渡せる（Wi-Fi が変わって IP が変わったときに、手で開かずに直せる）
            var url = CommandLineValue("-serverUrl");
            if (!string.IsNullOrEmpty(url))
            {
                config.serverUrl = url;
            }
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Selection.activeObject = config;

            // 合言葉は、ログに出さない
            Debug.Log(token == null
                ? $"[Scan] {AssetPath} を作りました。Server/.env が見つからないので、合言葉は空です（試しのモードのサーバー向け）。サーバーの場所：{config.serverUrl}"
                : $"[Scan] {AssetPath} を作り、Server/.env の合言葉を入れました。サーバーの場所：{config.serverUrl}。このファイルは Git に入りません");

            // バッチ実行では自分で終了しないと、エディタがプロジェクトのロックを握ったまま残り続ける
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        static string CommandLineValue(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        /// <summary>Server/.env の APP_TOKEN。ファイルや値がなければ null。</summary>
        static string ReadAppToken()
        {
            var env = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Server", ".env");
            if (!File.Exists(env))
            {
                return null;
            }
            foreach (var line in File.ReadAllLines(env))
            {
                var t = line.Trim();
                if (t.StartsWith("APP_TOKEN="))
                {
                    var value = t.Substring("APP_TOKEN=".Length).Trim();
                    return value.Length > 0 ? value : null;
                }
            }
            return null;
        }
    }
}
