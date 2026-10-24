using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.AR
{
    /// <summary>
    /// Test_A_Photo 用：撮影画面（D）の代わりの仮のボタンと、カメラの映像（A の Step 22）。
    /// D の撮影画面と同じく、共通コードの依頼だけを出す。
    /// </summary>
    public class PhotoCaptureDebugPanel : MonoBehaviour
    {
        [SerializeField] PhotoCapture capture;
        [SerializeField] ScanClient client;

        string message = "";

        void OnEnable()
        {
            if (capture != null)
            {
                capture.PhotosReady += OnPhotosReady;
            }
        }

        void OnDisable()
        {
            if (capture != null)
            {
                capture.PhotosReady -= OnPhotosReady;
            }
        }

        void OnPhotosReady(byte[] top, byte[] side)
        {
            message = $"2枚撮りました（真上 {top.Length / 1024} KB、斜め {side.Length / 1024} KB）\n{capture.LastSavedFolder}";
        }

        void OnGUI()
        {
            var scale = Screen.height / 720f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            if (GUI.Button(new Rect(20, 20, 260, 60), "撮影をはじめる"))
            {
                message = "";
                UiRequests.RequestStartScan();
            }
            if (GUI.Button(new Rect(20, 90, 260, 60), "とる"))
            {
                UiRequests.RequestTakePhoto();
            }
            if (GUI.Button(new Rect(20, 160, 260, 60), "もういちど とる（Rescan）"))
            {
                message = "";
                UiRequests.RequestRescan();
            }

            var shot = capture == null ? 0 : (capture.SideJpeg != null ? 2 : capture.TopJpeg != null ? 1 : 0);
            var next = shot == 0 ? "1枚目（真上）" : shot == 1 ? "2枚目（斜め）" : "おわり";
            GUI.Label(new Rect(20, 230, 600, 30), $"状態：{StageContext.ScanStatus}　次に撮るのは：{next}");
            if (StageContext.ScanStatus == ScanStatus.Limited)
            {
                var why = StageContext.ScanReason == ScanReason.Dark ? "くらい" : "ぶれている";
                GUI.Label(new Rect(20, 320, 600, 30), $"写真が{why}ので、もういちど とってね");
            }
            if (capture != null && (shot > 0 || StageContext.ScanStatus == ScanStatus.Limited))
            {
                var q = capture.LastQuality;
                GUI.Label(new Rect(20, 350, 600, 30), $"最後の写真：明るさ {q.brightness:0.000}、くっきりさ {q.sharpness:0.000000}");
            }
            GUI.Label(new Rect(20, 260, 900, 60), message);
            if (client != null && client.LastMessage.Length > 0)
            {
                GUI.Label(new Rect(20, 380, 900, 60), $"サーバー：{client.LastMessage}");
            }
            GUI.Label(new Rect(20, 440, 600, 30), $"箱：{ScanContext.WorkingBoxes.Count} 個");

            // カメラの映像（D の撮影画面では RawImage で映す）
            var preview = ScanContext.CameraPreview;
            if (preview != null)
            {
                var h = 600f;
                var w = h * preview.width / Mathf.Max(1, preview.height);
                GUI.DrawTexture(new Rect(300, 20, w, h), preview, ScaleMode.ScaleToFit);
            }
        }
    }
}
