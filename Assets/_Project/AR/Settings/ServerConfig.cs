using UnityEngine;

namespace DeskStage.AR
{
    /// <summary>
    /// 読み取りサーバーの場所と合言葉（A の Step 25）。
    /// 合言葉は秘密なので、このアセット（ServerConfig.asset）は Git に入れない（同じフォルダの .gitignore）。
    /// メニュー「DeskStage/A/5. サーバーの設定を作る」で、Server/.env の APP_TOKEN から作る。
    /// </summary>
    [CreateAssetMenu(fileName = "ServerConfig", menuName = "DeskStage/A/サーバーの設定")]
    public class ServerConfig : ScriptableObject
    {
        [Tooltip("読み取りサーバーの場所。PC で動かすときは http://127.0.0.1:8000。iPhone から同じ Wi-Fi で試すときは PC の IP アドレス")]
        public string serverUrl = "http://127.0.0.1:8000";

        [Tooltip("アプリとサーバーの合言葉（Server/.env の APP_TOKEN と同じ）。試しのモードのサーバーなら空でよい")]
        public string appToken = "";

        [Tooltip("答えを待つ長さ（秒）。過ぎたら Lost にする")]
        public int timeoutSeconds = 90;
    }
}
