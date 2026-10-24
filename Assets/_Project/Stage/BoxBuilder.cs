using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>
    /// A の上面の一覧（ScanContext.Surfaces）が変わるたびに、作業中の箱（ScanContext.WorkingBoxes）を作り直す（B の Step 6）。
    /// 計算は BoxBuilderCore。ここは知らせを受けて、結果を知らせるだけ。
    /// </summary>
    public class BoxBuilder : MonoBehaviour
    {
        readonly BoxBuilderCore core = new BoxBuilderCore();

        void OnEnable()
        {
            ScanContext.SurfacesChanged += OnSurfacesChanged;
        }

        void OnDisable()
        {
            ScanContext.SurfacesChanged -= OnSurfacesChanged;
        }

        void OnSurfacesChanged()
        {
            var changes = core.Apply(ScanContext.Surfaces, ScanContext.WorkingBoxes);
            foreach (var c in changes)
            {
                ScanContext.RaiseBoxesChanged(c.boxId, c.change, BoxData.SourceAuto);
            }
        }

        /// <summary>上面と箱の対応を忘れる（もういちどスキャンのとき。B の Step 8 で使う）。</summary>
        public void ResetMapping() => core.Reset();
    }
}
