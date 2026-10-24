using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>
    /// ステージのファイルの読み書き（B の Step 10・11、置き場所は B_scan_data.md「ステージの置き場所の決まり」）。
    /// フォルダの場所を外から受け取るので、EditMode テストでは一時フォルダで確かめられる。
    /// ・同梱用（StreamingAssets/Stages）は読むだけ。初回に保存用（persistentDataPath/Stages）へコピーする
    /// ・以降の一覧・読み込み・保存・消すは、保存用だけを使う
    /// </summary>
    public class StageStorage
    {
        public const string ThumbnailFile = "thumbnail.jpg";
        const string StageFile = "stage.json";
        const string BackupFile = "stage.json.bak";
        const string TempFile = "stage.json.tmp";
        const string IndexFile = "index.json";
        // 同梱用からコピーしたステージの更新番号を覚えておくファイル（保存用のフォルダに置く）
        const string BundledRevisionsFile = "bundled-revisions.json";

        readonly string bundledDir;
        readonly string savedDir;

        public StageStorage(string bundledDir, string savedDir)
        {
            this.bundledDir = bundledDir;
            this.savedDir = savedDir;
        }

        [Serializable]
        class Index
        {
            public IndexEntry[] stages = Array.Empty<IndexEntry>();
        }

        [Serializable]
        class IndexEntry
        {
            public string id = "";
            public int revision;
        }

        /// <summary>
        /// 同梱用のステージを保存用へコピーする。まだコピーしていないか、更新番号が上がったものだけ。
        /// 子どもが消した同梱用のステージは、更新番号が上がるまでは戻さない。コピーした数を返す。
        /// </summary>
        public int CopyBundled()
        {
            Directory.CreateDirectory(savedDir);
            var indexPath = Path.Combine(bundledDir, IndexFile);
            if (!File.Exists(indexPath))
            {
                return 0;
            }

            var index = JsonUtility.FromJson<Index>(File.ReadAllText(indexPath)) ?? new Index();
            var revisionsPath = Path.Combine(savedDir, BundledRevisionsFile);
            var copied = File.Exists(revisionsPath)
                ? JsonUtility.FromJson<Index>(File.ReadAllText(revisionsPath)) ?? new Index()
                : new Index();
            var known = new Dictionary<string, int>();
            foreach (var e in copied.stages ?? Array.Empty<IndexEntry>())
            {
                known[e.id] = e.revision;
            }

            var count = 0;
            foreach (var e in index.stages ?? Array.Empty<IndexEntry>())
            {
                if (string.IsNullOrEmpty(e.id) || (known.TryGetValue(e.id, out var rev) && rev >= e.revision))
                {
                    continue;
                }
                var from = Path.Combine(bundledDir, e.id);
                if (!Directory.Exists(from))
                {
                    Debug.LogWarning($"[Stage] index.json にある {e.id} のフォルダが同梱用にありません");
                    continue;
                }
                CopyFolder(from, Path.Combine(savedDir, e.id));
                known[e.id] = e.revision;
                count++;
            }

            var list = new List<IndexEntry>();
            foreach (var pair in known)
            {
                list.Add(new IndexEntry { id = pair.Key, revision = pair.Value });
            }
            File.WriteAllText(revisionsPath, JsonUtility.ToJson(new Index { stages = list.ToArray() }, true));
            return count;
        }

        /// <summary>保存用のステージの一覧。新しい順。読み込めないステージも、理由を付けて入れる。</summary>
        public List<StageSummary> List()
        {
            var result = new List<StageSummary>();
            if (!Directory.Exists(savedDir))
            {
                return result;
            }
            foreach (var dir in Directory.GetDirectories(savedDir))
            {
                var id = Path.GetFileName(dir);
                if (!File.Exists(Path.Combine(dir, StageFile)))
                {
                    continue;
                }
                var ok = TryLoad(id, out var stage, out var reason);
                result.Add(new StageSummary
                {
                    id = id,
                    name = stage != null && !string.IsNullOrEmpty(stage.name) ? stage.name : id,
                    createdAt = stage?.createdAt ?? "",
                    loadable = ok,
                    reason = reason,
                });
            }
            result.Sort((a, b) => ParseTime(b.createdAt).CompareTo(ParseTime(a.createdAt)));
            return result;
        }

        /// <summary>保存用から読み込み、中身を確かめる。読み込めなければ false と理由。</summary>
        public bool TryLoad(string id, out StageData stage, out string reason)
        {
            stage = null;
            var path = Path.Combine(savedDir, id ?? "", StageFile);
            if (!IsSafeId(id) || !File.Exists(path))
            {
                reason = "ステージが見つかりません";
                return false;
            }
            try
            {
                stage = JsonUtility.FromJson<StageData>(File.ReadAllText(path));
            }
            catch (ArgumentException e)
            {
                reason = $"stage.json が壊れています（{e.Message}）";
                return false;
            }
            return StageValidator.Validate(stage, id, out reason);
        }

        /// <summary>
        /// 安全に保存する（Step 11 の4）：一時ファイルに書いてから、元の stage.json を .bak にし、一時ファイルを stage.json にする。
        /// 書き込み中にアプリが落ちても、元の stage.json か .bak が残る。
        /// </summary>
        public bool Save(StageData stage, out string reason)
        {
            if (!StageValidator.Validate(stage, stage?.id, out reason))
            {
                return false;
            }
            if (!IsSafeId(stage.id))
            {
                reason = $"ステージID が正しくありません（{stage.id}）";
                return false;
            }
            try
            {
                var dir = Path.Combine(savedDir, stage.id);
                Directory.CreateDirectory(dir);
                var temp = Path.Combine(dir, TempFile);
                var target = Path.Combine(dir, StageFile);
                var backup = Path.Combine(dir, BackupFile);
                File.WriteAllText(temp, JsonUtility.ToJson(stage, true));
                if (File.Exists(target))
                {
                    if (File.Exists(backup))
                    {
                        File.Delete(backup);
                    }
                    File.Move(target, backup);
                }
                File.Move(temp, target);
                return true;
            }
            catch (IOException e)
            {
                reason = $"保存できませんでした（{e.Message}）";
                return false;
            }
            catch (UnauthorizedAccessException e)
            {
                reason = $"保存できませんでした（{e.Message}）";
                return false;
            }
        }

        /// <summary>
        /// ステージの一覧に出す小さな絵（thumbnail.jpg）を、保存したステージのフォルダに書く。
        /// 一覧（D）は、ステージのフォルダの thumbnail.jpg があれば、それを出す（無ければ絵なし）。
        /// </summary>
        public bool WriteThumbnail(string id, byte[] jpeg)
        {
            if (!IsSafeId(id) || jpeg == null || jpeg.Length == 0)
            {
                return false;
            }
            try
            {
                var dir = Path.Combine(savedDir, id);
                if (!Directory.Exists(dir))
                {
                    return false;
                }
                File.WriteAllBytes(Path.Combine(dir, ThumbnailFile), jpeg);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>保存用からステージを消す。同梱用は消さない。</summary>
        public bool Delete(string id)
        {
            if (!IsSafeId(id))
            {
                return false;
            }
            var dir = Path.Combine(savedDir, id);
            if (!Directory.Exists(dir))
            {
                return false;
            }
            Directory.Delete(dir, true);
            return true;
        }

        /// <summary>フォルダの外を指す ID（「..」や区切りの文字）を受け付けない。</summary>
        static bool IsSafeId(string id)
        {
            return !string.IsNullOrEmpty(id)
                && id.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                && id != "." && id != ".."
                && !id.Contains("/") && !id.Contains("\\");
        }

        static void CopyFolder(string from, string to)
        {
            Directory.CreateDirectory(to);
            foreach (var file in Directory.GetFiles(from))
            {
                // Unity の .meta は保存用には要らない
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                File.Copy(file, Path.Combine(to, Path.GetFileName(file)), true);
            }
        }

        static DateTimeOffset ParseTime(string iso)
        {
            return DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t)
                ? t
                : DateTimeOffset.MinValue;
        }
    }
}
