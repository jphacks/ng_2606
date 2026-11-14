#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;

namespace DeskStage.AR.EditorTools
{
    /// <summary>
    /// iOS のビルドのあとに、Info.plist へ「ローカルネットワーク」の許可を求める説明文（NSLocalNetworkUsageDescription）を書き足す（2026-11-14）。
    /// 同じ Wi-Fi の PC で動く読み取りサーバー（http://名前.local:8000 など）へ写真を送るには、iOS のローカルネットワークの許可が要る。
    /// 説明文が無いと、許可の確認がきちんと出ず、ケーブルを外すと（Wi-Fi の道しか無くなると）サーバーに届かなかった。
    /// ケーブルの道（開発用の直通のつながり）は、この許可が無くても通るので、ケーブルのときだけ動いていた。
    /// Unity の設定画面には、この項目が無いため、ビルドのたびにここで足す。
    /// </summary>
    public class IosLocalNetwork : IPostprocessBuildWithReport
    {
        public const string Key = "NSLocalNetworkUsageDescription";
        public const string Text = "同じ Wi-Fi のパソコンで動く読み取りサーバーに、撮った写真を送ってステージを作るために使います。";

        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS)
            {
                return;
            }
            var path = Path.Combine(report.summary.outputPath, "Info.plist");
            if (!File.Exists(path))
            {
                return;
            }
            var plist = new PlistDocument();
            plist.ReadFromFile(path);
            plist.root.SetString(Key, Text);
            plist.WriteToFile(path);
            UnityEngine.Debug.Log("[Build] Info.plist に、ローカルネットワークの説明文を入れました");
        }
    }
}
#endif
