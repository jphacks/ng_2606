using System;
using System.Collections;
using System.Text;
using DeskStage.AR;
using DeskStage.Shared;
using UnityEngine;
using UnityEngine.Networking;

namespace DeskStage.App
{
    /// <summary>
    /// みんなの ステージ（Docs/specs/home-modes の REQ-9〜11）。この Mac のサーバーに、ステージを投稿する・一覧を読む・1 件を読む。
    /// 接続先と合言葉は、撮影と同じ ServerConfig。http は、開発用ビルドで同じ Wi-Fi の中だけ（ScanClient と同じ決まり）。
    /// 送るのは、形・コース・しばり・名前だけ。写真（一覧の絵、箱の写真）は送らない。
    /// </summary>
    public class StageShareClient : MonoBehaviour
    {
        [Serializable]
        public class Summary
        {
            public string id = "";
            public string name = "";
            public string createdAt = "";
            public int boxCount;
            public bool hasRules;
        }

        [Serializable]
        class SummaryList
        {
            public Summary[] stages = new Summary[0];
        }

        [Serializable]
        class PostResult
        {
            public string id = "";
        }

        const int TimeoutSeconds = 15;

        ServerConfig config;

        string BaseUrl => (config != null ? config.serverUrl : "http://127.0.0.1:8000").Trim().TrimEnd('/');
        string Token => config != null ? config.appToken : "";

        void Awake()
        {
            config = Resources.Load<ServerConfig>(ServerConfig.ResourceName);
        }

        /// <summary>投稿する。done(成功したか, 失敗の理由)。</summary>
        public void Post(StageData stage, Action<bool, string> done)
        {
            StartCoroutine(Send("POST", "/stages", ToPostJson(stage), (ok, text) =>
            {
                done(ok, ok ? "" : text);
            }));
        }

        /// <summary>一覧を読む。done(一覧。読めなければ null, 失敗の理由)。</summary>
        public void List(Action<Summary[], string> done)
        {
            StartCoroutine(Send("GET", "/stages", null, (ok, text) =>
            {
                if (!ok)
                {
                    done(null, text);
                    return;
                }
                var list = ScanClient.LooksLikeJsonObject(text) ? JsonUtility.FromJson<SummaryList>(text) : null;
                done(list?.stages ?? new Summary[0], "");
            }));
        }

        /// <summary>1 件を読む。done(ステージ。読めなければ null, 失敗の理由)。</summary>
        public void Get(string id, Action<StageData, string> done)
        {
            StartCoroutine(Send("GET", "/stages/" + UnityWebRequest.EscapeURL(id), null, (ok, text) =>
            {
                if (!ok)
                {
                    done(null, text);
                    return;
                }
                var stage = ScanClient.LooksLikeJsonObject(text) ? JsonUtility.FromJson<StageData>(text) : null;
                if (stage != null && stage.rules == null) stage.rules = new CourseRules();
                done(stage, stage == null ? "形が違います" : "");
            }));
        }

        /// <summary>投稿する中身：写真のファイル名（texture）を空にした写し。</summary>
        public static string ToPostJson(StageData stage)
        {
            var copy = JsonUtility.FromJson<StageData>(JsonUtility.ToJson(stage));
            foreach (var b in copy.boxes ?? new BoxData[0])
            {
                b.texture = "";
            }
            return JsonUtility.ToJson(copy);
        }

        IEnumerator Send(string method, string path, string body, Action<bool, string> done)
        {
            var url = BaseUrl + path;
            if (!ScanClient.IsAllowedUrl(url, Debug.isDebugBuild, out var refused))
            {
                done(false, refused);
                yield break;
            }
            using (var request = new UnityWebRequest(url, method))
            {
                if (body != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                    request.SetRequestHeader("Content-Type", "application/json");
                }
                request.downloadHandler = new DownloadHandlerBuffer();
                if (!string.IsNullOrEmpty(Token))
                {
                    request.SetRequestHeader("X-App-Token", Token);
                }
                request.timeout = TimeoutSeconds;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[Share] {method} {path}：{request.responseCode} {request.error}");
                    done(false, request.responseCode == 0 ? "network" : request.responseCode.ToString());
                    yield break;
                }
                done(true, request.downloadHandler.text);
            }
        }
    }
}
