using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.AR
{
    /// <summary>
    /// 合流の確認用の仮の操作盤（Main.unity）。D の UI（撮影画面、たしかめ画面、遊ぶ画面）が合流するまでの代わり。
    /// 撮影 → 2枚 → サーバー → 箱 → コース（スタートとゴール）→ 遊ぶ → もどる、を通す。
    /// 注意：StageContext.Mode は、約束では D だけが書く。ここは D の UI が無い間の仮として書く。
    /// D の UI_System を Main に置くときは、このオブジェクト（Integration_Panel）を外す。
    /// </summary>
    public class IntegrationPanel : MonoBehaviour
    {
        enum Placing
        {
            None,
            Start,
            Goal,
        }

        PhotoCapture capture;
        ScanClient client;
        Placing placing;
        string message = "";
        bool finishedHandled;

        void Awake()
        {
            capture = FindFirstObjectByType<PhotoCapture>();
            client = FindFirstObjectByType<ScanClient>();
        }

        void OnEnable() => StageContext.ScanStatusChanged += OnStatusChanged;

        void OnDisable() => StageContext.ScanStatusChanged -= OnStatusChanged;

        void OnStatusChanged(ScanStatus status, ScanReason reason)
        {
            // サーバーの答えが届いたら、たしかめ（3D）に進む
            if (status == ScanStatus.Finished && StageContext.Mode == AppMode.Scan && !finishedHandled)
            {
                finishedHandled = true;
                UiRequests.RequestFinishScan();
                StageContext.Mode = AppMode.Check;
                message = "箱ができたよ。スタートとゴールを置いてね";
            }
        }

        void OnGUI()
        {
            var scale = Screen.dpi > 0 ? Screen.dpi / 160f : 2f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var w = Screen.width / scale;
            var h = Screen.height / scale;

            var mode = StageContext.Mode;
            GUI.Box(new Rect(8f, 8f, 330f, 88f), "");
            GUI.Label(new Rect(16f, 10f, 320f, 22f), $"場面：{mode}　状態：{StageContext.ScanStatus}（{StageContext.ScanReason}）");
            GUI.Label(new Rect(16f, 30f, 320f, 22f), $"箱：{ScanContext.WorkingBoxes.Count} 個　机：{ScanContext.DeskSize.x:F2}×{ScanContext.DeskSize.y:F2} m");
            GUI.Label(new Rect(16f, 50f, 320f, 44f), message + (client != null && client.LastMessage.Length > 0 ? $"\nサーバー：{client.LastMessage}" : ""));

            // カメラの映像（撮影のとき）
            var preview = ScanContext.CameraPreview;
            if (mode == AppMode.Scan && preview != null)
            {
                var ph = h - 150f;
                var pw = ph * preview.width / Mathf.Max(1, preview.height);
                GUI.DrawTexture(new Rect((w - pw) / 2f, 100f, pw, ph), preview, ScaleMode.ScaleToFit);
            }

            var y = h - 54f;
            var bw = 150f;
            var x = 8f;
            bool Button(string label)
            {
                var pressed = GUI.Button(new Rect(x, y, bw, 44f), label);
                x += bw + 8f;
                return pressed;
            }

            switch (mode)
            {
                case AppMode.Menu:
                    if (Button("撮影をはじめる"))
                    {
                        finishedHandled = false;
                        placing = Placing.None;
                        StageContext.Mode = AppMode.Scan;
                        UiRequests.RequestStartScan();
                        message = "真上から撮ってね（1まいめ）";
                    }
                    break;

                case AppMode.Scan:
                    var next = capture != null && capture.TopJpeg == null ? "真上（1まいめ）" : "斜め（2まいめ）";
                    var waiting = StageContext.ScanStatus == ScanStatus.Scanning && capture != null && capture.SideJpeg != null;
                    if (!waiting && Button($"とる：{next}"))
                    {
                        UiRequests.RequestTakePhoto();
                        message = capture != null && capture.TopJpeg != null ? "斜めから撮ってね（2まいめ）" : "";
                    }
                    if (StageContext.ScanStatus == ScanStatus.Lost && Button("もういちど"))
                    {
                        finishedHandled = false;
                        UiRequests.RequestRescan();
                    }
                    if (StageContext.ScanStatus == ScanStatus.Limited)
                    {
                        message = StageContext.ScanReason == ScanReason.Dark ? "くらいよ。もういちど とってね" : "ぶれたよ。もういちど とってね";
                    }
                    break;

                case AppMode.Check:
                    if (Button("スタートを置く")) { placing = Placing.Start; message = "机の上をタップしてスタートを置く"; }
                    if (Button("ゴールを置く")) { placing = Placing.Goal; message = "机の上をタップしてゴールを置く"; }
                    if (Button("あそぶ"))
                    {
                        placing = Placing.None;
                        StageContext.Mode = AppMode.Play;
                        UiRequests.RequestPlay();
                        message = "";
                    }
                    if (Button("もういちど撮る"))
                    {
                        placing = Placing.None;
                        finishedHandled = false;
                        StageContext.Mode = AppMode.Scan;
                        UiRequests.RequestRescan();
                        message = "真上から撮ってね（1まいめ）";
                    }
                    break;

                case AppMode.Play:
                    if (Button("もどる"))
                    {
                        UiRequests.RequestBackToList();
                        StageContext.Mode = AppMode.Check;
                        message = "";
                    }
                    break;
            }

            // たしかめの画面で、机の上をタップしたら、コースの物を置く
            var e = Event.current;
            if (mode == AppMode.Check && placing != Placing.None && e.type == EventType.MouseDown && e.mousePosition.y < y)
            {
                var item = placing == Placing.Start ? CourseItem.Start : CourseItem.Goal;
                UiRequests.RequestPlaceCourseItem(item, new Vector2(e.mousePosition.x * scale, Screen.height - e.mousePosition.y * scale));
                message = placing == Placing.Start ? "スタートを置いたよ" : "ゴールを置いたよ";
                placing = Placing.None;
            }
        }
    }
}
