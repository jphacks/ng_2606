using System.Collections.Generic;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>
    /// A の上面の一覧（ScanContext.Surfaces）が変わるたびに、作業中の箱（ScanContext.WorkingBoxes）を作り直す（B の Step 6・7）。
    /// 計算は BoxBuilderCore。ここは知らせを受けて、結果を知らせるだけ。
    /// FinishScan を受けたら箱を確定し、それ以降の上面の変化では直さない。
    /// </summary>
    public class BoxBuilder : MonoBehaviour
    {
        [SerializeField] BoxSettings settings;

        BoxBuilderCore core;
        bool finished;

        void Awake()
        {
            core = settings != null ? new BoxBuilderCore(settings) : new BoxBuilderCore();
            if (settings == null)
            {
                Debug.LogWarning("[Stage] BoxBuilder に BoxSettings が入っていません。手順書の初期値で動かします");
            }
        }

        void OnEnable()
        {
            ScanContext.SurfacesChanged += OnSurfacesChanged;
            UiRequests.StartScan += OnStartScan;
            UiRequests.FinishScan += OnFinishScan;
            UiRequests.Rescan += OnRescan;
        }

        void OnDisable()
        {
            ScanContext.SurfacesChanged -= OnSurfacesChanged;
            UiRequests.StartScan -= OnStartScan;
            UiRequests.FinishScan -= OnFinishScan;
            UiRequests.Rescan -= OnRescan;
        }

        void OnStartScan()
        {
            finished = false;
        }

        void OnFinishScan()
        {
            // 最後の上面で一度そろえてから確定する
            OnSurfacesChanged();
            finished = true;
        }

        /// <summary>もういちどスキャン（B の Step 8）：箱とコースの物を消し、A の新しい上面から作り直す。</summary>
        void OnRescan()
        {
            Raise(core.ClearAll(ScanContext.WorkingBoxes));
            ScanContext.SelectedBoxId = "";
            StageCourse.Clear(StageContext.CurrentStage);
            finished = false;
        }

        void OnSurfacesChanged()
        {
            if (finished)
            {
                return;
            }
            Raise(core.Apply(ScanContext.Surfaces, ScanContext.WorkingBoxes));
        }

        static void Raise(List<BoxChangeEntry> changes)
        {
            foreach (var c in changes)
            {
                ScanContext.RaiseBoxesChanged(c.boxId, c.change, c.source);
            }
        }
    }
}
