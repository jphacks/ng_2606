using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeskStage.AR.EditorTools
{
    /// <summary>
    /// A の Step 25：サーバーの設定（ServerConfig.asset）を作る。合言葉は Server/.env の APP_TOKEN から写す。
    /// このアセットは Git に入らない（AR/Settings/.gitignore）。合言葉を変えたら、もう一度実行する。
    /// </summary>
    public static class CreateServerConfig
    {
        public const string AssetPath = "Assets/_Project/AR/Settings/ServerConfig.asset";

        [MenuItem("DeskStage/A/5. サーバーの設定を作る")]
        public static void Run()
        {
            var config = AssetDatabase.LoadAssetAtPath<ServerConfig>(AssetPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ServerConfig>();
                AssetDatabase.CreateAsset(config, AssetPath);
            }

            var token = ReadAppToken();
            config.appToken = token ?? "";
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Selection.activeObject = config;

            Debug.Log(token == null
                ? $"[Scan] {AssetPath} を作りました。Server/.env が見つからないので、合言葉は空です（試しのモードのサーバー向け）"
                : $"[Scan] {AssetPath} を作り、Server/.env の合言葉を入れました（{token.Length} 文字）。このファイルは Git に入りません");
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
