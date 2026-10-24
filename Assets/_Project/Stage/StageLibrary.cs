using System;
using System.IO;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>
    /// 端末のステージの一覧・読み込み・保存・消す（B の Step 10・11）。ファイルの扱いは StageStorage。
    /// ・起動したら、同梱用のステージを保存用へコピーし、一覧を StageContext.StageList に入れる
    /// ・SelectStage：読み込んで中身を確かめ、StageLoaded を出して組み立てる（組み立て終わると StageBuilder が StageBuilt を出す）
    /// ・SaveStage：今のステージ（たしかめ画面の箱とコースの物）を保存し、SaveCompleted を出す
    /// ・DeleteStage：消して、一覧を作り直す
    /// </summary>
    public class StageLibrary : MonoBehaviour
    {
        [SerializeField] StageBuilder builder;

        // 一覧に出す絵（thumbnail.jpg）の大きさと画質
        const int ThumbnailMaxSide = 256;
        const int ThumbnailQuality = 80;

        StageStorage storage;

        void Awake()
        {
            storage = new StageStorage(
                Path.Combine(Application.streamingAssetsPath, "Stages"),
                Path.Combine(Application.persistentDataPath, "Stages"));
        }

        void OnEnable()
        {
            UiRequests.SelectStage += OnSelectStage;
            UiRequests.SaveStage += OnSaveStage;
            UiRequests.DeleteStage += OnDeleteStage;
        }

        void OnDisable()
        {
            UiRequests.SelectStage -= OnSelectStage;
            UiRequests.SaveStage -= OnSaveStage;
            UiRequests.DeleteStage -= OnDeleteStage;
        }

        void Start()
        {
            var copied = storage.CopyBundled();
            if (copied > 0)
            {
                Debug.Log($"[Stage] 同梱のステージを {copied} 個コピーしました");
            }
            Refresh();
        }

        /// <summary>一覧を作り直して、StageListChanged を出す。</summary>
        public void Refresh()
        {
            StageContext.SetStageList(storage.List());
        }

        void OnSelectStage(string id)
        {
            if (!storage.TryLoad(id, out var stage, out var reason))
            {
                Debug.LogWarning($"[Stage] {id} を読み込めません：{reason}");
                return;
            }
            StageContext.RaiseStageLoaded(stage);
            if (builder != null)
            {
                builder.Build(stage);
            }
        }

        void OnSaveStage(string displayName)
        {
            var stage = StageSavePrep.Prepare(StageContext.CurrentStage, ScanContext.WorkingBoxes, displayName, DateTimeOffset.Now);
            var ok = storage.Save(stage, out var reason);
            if (ok)
            {
                Debug.Log($"[Stage] {stage.id}（{stage.name}）を保存しました：箱 {stage.boxes.Length} 個");
                SaveThumbnail(stage.id);
                Refresh();
            }
            else
            {
                Debug.LogWarning($"[Stage] 保存できませんでした：{reason}");
            }
            StageContext.RaiseSaveCompleted(ok, ok ? "" : reason);
        }

        /// <summary>撮った 1 枚目（真上）の写真を小さくして、一覧に出す絵（thumbnail.jpg）として保存する。写真が無ければ何もしない。</summary>
        void SaveThumbnail(string stageId)
        {
            var jpeg = StagePhotoRegions.TopJpeg;
            if (jpeg == null)
            {
                return;
            }
            var thumbnail = MakeThumbnail(jpeg, ThumbnailMaxSide, ThumbnailQuality);
            if (thumbnail != null && storage.WriteThumbnail(stageId, thumbnail))
            {
                Debug.Log($"[Stage] {stageId} の一覧の絵を保存しました（{thumbnail.Length / 1024} KB）");
            }
        }

        /// <summary>写真（JPEG）を、長い辺が maxSide 以下の JPEG にする。読めなければ null。</summary>
        static byte[] MakeThumbnail(byte[] jpeg, int maxSide, int quality)
        {
            var source = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try
            {
                if (!source.LoadImage(jpeg))
                {
                    return null;
                }
                var size = FitLongSide(source.width, source.height, maxSide);
                var rt = RenderTexture.GetTemporary(size.x, size.y, 0);
                var before = RenderTexture.active;
                try
                {
                    Graphics.Blit(source, rt);
                    RenderTexture.active = rt;
                    var small = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                    small.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                    small.Apply();
                    var bytes = small.EncodeToJPG(quality);
                    Destroy(small);
                    return bytes;
                }
                finally
                {
                    RenderTexture.active = before;
                    RenderTexture.ReleaseTemporary(rt);
                }
            }
            finally
            {
                Destroy(source);
            }
        }

        /// <summary>長い辺が maxSide 以下になるように、縦横の比を保って縮めた大きさ。</summary>
        public static Vector2Int FitLongSide(int width, int height, int maxSide)
        {
            var longSide = Mathf.Max(width, height);
            if (longSide <= maxSide || longSide <= 0)
            {
                return new Vector2Int(Mathf.Max(1, width), Mathf.Max(1, height));
            }
            var scale = (float)maxSide / longSide;
            return new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(width * scale)), Mathf.Max(1, Mathf.RoundToInt(height * scale)));
        }

        void OnDeleteStage(string id)
        {
            if (storage.Delete(id))
            {
                Debug.Log($"[Stage] {id} を消しました");
                Refresh();
            }
            else
            {
                Debug.LogWarning($"[Stage] {id} を消せませんでした");
            }
        }
    }
}
