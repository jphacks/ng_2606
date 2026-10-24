using System.Collections.Generic;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>
    /// A から届いた Claude の答え（ScanContext.ScanResultReceived）から、作業中の箱を作る（B の Step 7・8）。
    /// 計算は StageReaderCore。ここは知らせを受けて、結果を書き、知らせるだけ。
    /// ・新しい答えが来たら、前の auto の箱を消して作り直す。補助で直した manual の箱は残す
    /// ・Rescan（もういちど撮る）を受けたら、箱をすべて消し、コースの物も消す
    /// </summary>
    public class StageReader : MonoBehaviour
    {
        [SerializeField] BoxSettings settings;

        StageReaderCore core;

        void Awake()
        {
            core = settings != null ? new StageReaderCore(settings) : new StageReaderCore();
            if (settings == null)
            {
                Debug.LogWarning("[Stage] StageReader に BoxSettings が入っていません。手順書の初期値で動かします");
            }
        }

        void OnEnable()
        {
            ScanContext.ScanResultReceived += OnScanResult;
            UiRequests.Rescan += OnRescan;
        }

        void OnDisable()
        {
            ScanContext.ScanResultReceived -= OnScanResult;
            UiRequests.Rescan -= OnRescan;
        }

        void OnScanResult(string json)
        {
            var result = core.Read(json);
            if (!result.Ok)
            {
                Debug.LogWarning($"[Stage] 答えから箱を作れませんでした：{result.Reason}");
                return;
            }
            Debug.Log($"[Stage] 答えから箱を {result.Boxes.Count} 個作りました（入れなかった物 {result.Skipped} 個、"
                + $"紙 {(result.PaperFound ? "あり" : "なし")}、基準：{result.ScaleReference}）");

            // 前の auto の箱を消す（manual の箱は子どもが直した物なので残す）
            var removed = new List<string>();
            ScanContext.WorkingBoxes.RemoveAll(b =>
            {
                if (b.source != BoxData.SourceAuto)
                {
                    return false;
                }
                removed.Add(b.id);
                return true;
            });
            foreach (var id in removed)
            {
                ScanContext.RaiseBoxesChanged(id, BoxChange.Removed, BoxData.SourceAuto);
            }

            // 新しい箱を足す。manual の箱と id が重ならないようにする
            foreach (var box in result.Boxes)
            {
                box.id = NewId(box.id);
                ScanContext.WorkingBoxes.Add(box);
                ScanContext.RaiseBoxesChanged(box.id, BoxChange.Added, BoxData.SourceAuto);
            }

            ScanContext.DeskSize = result.DeskSize;
            UpdateCurrentStage(result.DeskSize);
        }

        /// <summary>もういちど撮る（B の Step 8）：箱をすべて消し（manual も）、コースの物も消す。</summary>
        void OnRescan()
        {
            var all = new List<BoxData>(ScanContext.WorkingBoxes);
            ScanContext.WorkingBoxes.Clear();
            foreach (var b in all)
            {
                ScanContext.RaiseBoxesChanged(b.id, BoxChange.Removed, b.source);
            }
            ScanContext.SelectedBoxId = "";
            StageCourse.Clear(StageContext.CurrentStage);
        }

        /// <summary>今のステージデータに、机と作業中の箱を入れる（保存とたしかめ画面で使う）。</summary>
        static void UpdateCurrentStage(Vector2 deskSize)
        {
            var stage = StageContext.CurrentStage ?? new StageData();
            stage.desk = new DeskData { width = deskSize.x, depth = deskSize.y };
            stage.boxes = ScanContext.WorkingBoxes.ToArray();
            StageContext.CurrentStage = stage;
        }

        static string NewId(string wanted)
        {
            if (!ScanContext.WorkingBoxes.Exists(b => b.id == wanted))
            {
                return wanted;
            }
            for (var n = 1; ; n++)
            {
                var id = $"box-{n:00}";
                if (!ScanContext.WorkingBoxes.Exists(b => b.id == id))
                {
                    return id;
                }
            }
        }
    }
}
