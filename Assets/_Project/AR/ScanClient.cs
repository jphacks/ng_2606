using System;
using System.Collections;
using System.Collections.Generic;
using DeskStage.Shared;
using UnityEngine;
using UnityEngine.Networking;

namespace DeskStage.AR
{
    /// <summary>
    /// 撮った2枚の写真を読み取りサーバー（Server/）に送り、Claude の答えを B に渡す（A の Step 25、00_overview.md 9.2）。
    /// ・PhotoCapture の PhotosReady を受けたら、POST /scan に送る。待つ間は Scanning のまま
    /// ・答えが届いたら ScanContext.RaiseScanResultReceived に渡し、Finished にする
    /// ・通信の失敗、サーバーの断り、答えの形が違うときは Lost にして、理由をログに残す
    /// ・Rescan を受けたら、送っている途中なら止める（撮り直しは PhotoCapture が行う）
    /// ・合言葉と写真を暗号化なしで外に出さないため、http:// は自分の PC か、開発用ビルドで家の Wi-Fi の中だけに送る
    /// </summary>
    public class ScanClient : MonoBehaviour
    {
        [SerializeField] PhotoCapture capture;

        [Tooltip("サーバーの場所と合言葉。空なら http://127.0.0.1:8000 に合言葉なしで送る（試しのモードのサーバー向け）")]
        [SerializeField] ServerConfig config;

        /// <summary>最後に送ったときの結果（ログと確認用）。</summary>
        public string LastMessage { get; private set; } = "";

        UnityWebRequest request;

        string ServerUrl => config != null ? config.serverUrl : "http://127.0.0.1:8000";
        string AppToken => config != null ? config.appToken : "";
        int TimeoutSeconds => config != null ? config.timeoutSeconds : 90;

        void Awake()
        {
            // 設定ファイルは Git に入らないので、プレハブやシーンから参照すると他の人の PC で切れる。
            // 参照が空なら、Resources（AR/Resources/ServerConfig.asset。メニュー「DeskStage/A/5. サーバーの設定を作る」で作る）から探す
            if (config == null)
            {
                config = Resources.Load<ServerConfig>(ServerConfig.ResourceName);
            }
        }

        void OnEnable()
        {
            if (capture != null)
            {
                capture.PhotosReady += OnPhotosReady;
            }
            UiRequests.Rescan += Abort;
        }

        void OnDisable()
        {
            if (capture != null)
            {
                capture.PhotosReady -= OnPhotosReady;
            }
            UiRequests.Rescan -= Abort;
            Abort();
        }

        void OnPhotosReady(byte[] top, byte[] side)
        {
            Abort();
            StartCoroutine(Send(top, side));
        }

        IEnumerator Send(byte[] top, byte[] side)
        {
            StageContext.SetScanStatus(ScanStatus.Scanning);
            LastMessage = "サーバーに送っています…";

            var form = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection("top", top, "top.jpg", "image/jpeg"),
                new MultipartFormFileSection("side", side, "side.jpg", "image/jpeg"),
            };
            var url = ScanUrl(ServerUrl);
            if (!IsAllowedUrl(url, Debug.isDebugBuild, out var refused))
            {
                Fail(refused);
                yield break;
            }
            var started = Time.realtimeSinceStartup;

            using (request = UnityWebRequest.Post(url, form))
            {
                if (!string.IsNullOrEmpty(AppToken))
                {
                    request.SetRequestHeader("X-App-Token", AppToken);
                }
                request.timeout = TimeoutSeconds;
                yield return request.SendWebRequest();

                var seconds = Time.realtimeSinceStartup - started;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    // サーバーの断りの理由は、本文の detail に入っている
                    Fail($"送れませんでした（{request.responseCode} {request.error}）{request.downloadHandler?.text}");
                    request = null;
                    yield break;
                }

                var json = request.downloadHandler.text;
                request = null;
                if (!LooksLikeJsonObject(json))
                {
                    Fail("答えが JSON の形になっていません");
                    yield break;
                }

                LastMessage = $"答えが届きました（{seconds:0.0} 秒）";
                Debug.Log($"[Scan] {LastMessage}");
                ScanContext.RaiseScanResultReceived(json);
                StageContext.SetScanStatus(ScanStatus.Finished);
            }
        }

        void Fail(string message)
        {
            LastMessage = message;
            Debug.LogWarning($"[Scan] {message}");
            StageContext.SetScanStatus(ScanStatus.Lost);
        }

        void Abort()
        {
            StopAllCoroutines();
            if (request != null)
            {
                // コルーチンを止めると using の後片付けが走らないので、ここで閉じる
                request.Abort();
                request.Dispose();
                request = null;
            }
        }

        /// <summary>サーバーの場所から、読み取りの受け口（/scan）の URL を作る。</summary>
        public static string ScanUrl(string serverUrl)
        {
            return (serverUrl ?? "").Trim().TrimEnd('/') + "/scan";
        }

        /// <summary>
        /// 送ってよい URL か。https はいつでもよい。http は、自分の PC（127.0.0.1、localhost）か、
        /// allowLanHttp のとき（開発用ビルド）に家の Wi-Fi の中のアドレス（10.x、172.16〜31.x、192.168.x）だけ。
        /// </summary>
        public static bool IsAllowedUrl(string url, bool allowLanHttp, out string reason)
        {
            reason = "";
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                reason = $"サーバーの場所が正しくありません（{url}）";
                return false;
            }
            if (uri.Scheme == Uri.UriSchemeHttps)
            {
                return true;
            }
            if (uri.Scheme != Uri.UriSchemeHttp)
            {
                reason = $"http か https のサーバーにしか送れません（{url}）";
                return false;
            }
            if (uri.IsLoopback || (allowLanHttp && IsPrivateLan(uri.Host)))
            {
                return true;
            }
            reason = "暗号化なし（http://）では、自分の PC か、開発用ビルドで家の Wi-Fi の中のサーバーにしか送れません。https:// のサーバーにしてください";
            return false;
        }

        static bool IsPrivateLan(string host)
        {
            var parts = host.Split('.');
            if (parts.Length != 4 || !int.TryParse(parts[0], out var a) || !int.TryParse(parts[1], out var b))
            {
                return false;
            }
            return a == 10 || (a == 172 && b >= 16 && b <= 31) || (a == 192 && b == 168);
        }

        /// <summary>中身を詳しく確かめるのは B（StageReader）。ここでは JSON のオブジェクトらしいかだけを見る。</summary>
        public static bool LooksLikeJsonObject(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }
            var t = text.Trim();
            return t.StartsWith("{") && t.EndsWith("}");
        }
    }
}
