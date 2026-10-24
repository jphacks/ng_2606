using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.AR
{
    /// <summary>
    /// Step 10 の確認用の仮ボタン。D の画面がまだないときに、依頼を出して状態の変わり方を見る。
    /// D の UI ができたら、AR_System プレハブでは入れない（検証専用の使い捨て）。
    /// </summary>
    public class ScanSessionDebugPanel : MonoBehaviour
    {
        void OnGUI()
        {
            var scale = Screen.dpi > 0 ? Screen.dpi / 160f : 2f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            var w = Screen.width / scale;
            var h = Screen.height / scale;
            var y = h - 90f;

            GUI.Label(new Rect(8f, y - 24f, w - 16f, 22f),
                $"状態：{StageContext.ScanStatus}（{StageContext.ScanReason}）  場面：{StageContext.Mode}");

            var bw = (w - 32f) / 3f;
            if (GUI.Button(new Rect(8f, y, bw, 40f), "開始")) UiRequests.RequestStartScan();
            if (GUI.Button(new Rect(16f + bw, y, bw, 40f), "終了")) UiRequests.RequestFinishScan();
            if (GUI.Button(new Rect(24f + bw * 2f, y, bw, 40f), "遊ぶ")) StageContext.Mode = AppMode.Play;
        }
    }
}
