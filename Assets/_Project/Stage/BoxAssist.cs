using System.Collections.Generic;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>
    /// 読み取りの補助（B の Step 9）：たしかめ画面で、見つからなかった物や違う箱を子どもが直す。補助だけで、ステージは現実の物で作る。
    /// D の依頼を受けて ScanContext.WorkingBoxes を変え、BoxesChanged を出す（指の動きの読み取りは D）。計算は BoxAssistCore。
    /// </summary>
    public class BoxAssist : MonoBehaviour
    {
        [SerializeField] BoxSettings settings;

        [Tooltip("タップ先を調べるカメラ。空なら Camera.main")]
        [SerializeField] Camera viewCamera;

        BoxAssistCore core;

        /// <summary>もどせる回数（ボタンを灰色にするかの目安）。</summary>
        public int UndoCount => core?.UndoCount ?? 0;

        void Awake()
        {
            core = new BoxAssistCore(settings != null ? BoxAssistCore.Settings.From(settings) : BoxAssistCore.Settings.Default);
        }

        void OnEnable()
        {
            UiRequests.AddBox += OnAddBox;
            UiRequests.SelectBox += OnSelectBox;
            UiRequests.MoveBox += OnMoveBox;
            UiRequests.ResizeBox += OnResizeBox;
            UiRequests.RotateBox += OnRotateBox;
            UiRequests.DeleteBox += OnDeleteBox;
            UiRequests.UndoBox += OnUndoBox;
            UiRequests.Rescan += OnRescan;
        }

        void OnDisable()
        {
            UiRequests.AddBox -= OnAddBox;
            UiRequests.SelectBox -= OnSelectBox;
            UiRequests.MoveBox -= OnMoveBox;
            UiRequests.ResizeBox -= OnResizeBox;
            UiRequests.RotateBox -= OnRotateBox;
            UiRequests.DeleteBox -= OnDeleteBox;
            UiRequests.UndoBox -= OnUndoBox;
            UiRequests.Rescan -= OnRescan;
        }

        void OnAddBox(Vector2 screenPosition)
        {
            var root = StageContext.StageRoot;
            if (root == null || !Raycast(screenPosition, out var hit))
            {
                return;
            }
            var local = root.InverseTransformPoint(hit.point);
            // 箱の上面に当たったら積む。机や箱の側面なら机の上に置く
            var onBoxTop = hit.collider.gameObject.name != "Desk" && hit.normal.y > 0.7f;
            var box = core.Add(ScanContext.WorkingBoxes, new Vector2(local.x, local.z), onBoxTop ? local.y : 0f, ScanContext.DeskSize);
            ScanContext.SelectedBoxId = box.id;
            Changed(box.id, BoxChange.Added);
        }

        void OnSelectBox(Vector2 screenPosition)
        {
            var id = "";
            if (Raycast(screenPosition, out var hit) && hit.collider.gameObject.name != "Desk"
                && ScanContext.WorkingBoxes.Exists(b => b.id == hit.collider.gameObject.name))
            {
                id = hit.collider.gameObject.name;
            }
            ScanContext.SelectedBoxId = id;
        }

        void OnMoveBox(Vector2 delta)
        {
            if (core.Move(ScanContext.WorkingBoxes, ScanContext.SelectedBoxId, delta, ScanContext.DeskSize) != null)
            {
                Changed(ScanContext.SelectedBoxId, BoxChange.Modified);
            }
        }

        void OnResizeBox(Vector3 delta)
        {
            if (core.Resize(ScanContext.WorkingBoxes, ScanContext.SelectedBoxId, delta) != null)
            {
                Changed(ScanContext.SelectedBoxId, BoxChange.Modified);
            }
        }

        void OnRotateBox(float degrees)
        {
            if (core.Rotate(ScanContext.WorkingBoxes, ScanContext.SelectedBoxId, degrees) != null)
            {
                Changed(ScanContext.SelectedBoxId, BoxChange.Modified);
            }
        }

        void OnDeleteBox()
        {
            var id = ScanContext.SelectedBoxId;
            if (core.Delete(ScanContext.WorkingBoxes, id))
            {
                ScanContext.SelectedBoxId = "";
                Changed(id, BoxChange.Removed);
            }
        }

        void OnUndoBox()
        {
            var before = new Dictionary<string, BoxData>();
            foreach (var b in ScanContext.WorkingBoxes)
            {
                before[b.id] = b;
            }
            if (!core.Undo(ScanContext.WorkingBoxes))
            {
                return;
            }

            // 戻した結果との違いを、箱ごとに知らせる
            foreach (var b in ScanContext.WorkingBoxes)
            {
                Changed(b.id, before.Remove(b.id) ? BoxChange.Modified : BoxChange.Added);
            }
            foreach (var id in before.Keys)
            {
                Changed(id, BoxChange.Removed);
            }
            if (!ScanContext.WorkingBoxes.Exists(b => b.id == ScanContext.SelectedBoxId))
            {
                ScanContext.SelectedBoxId = "";
            }
        }

        void OnRescan()
        {
            core.ClearHistory();
        }

        void Changed(string id, BoxChange change)
        {
            var box = ScanContext.WorkingBoxes.Find(b => b.id == id);
            ScanContext.RaiseBoxesChanged(id, change, box != null ? box.source : BoxData.SourceManual);
            // 保存とたしかめ画面で使う今のステージにも入れる
            if (StageContext.CurrentStage != null)
            {
                StageContext.CurrentStage.boxes = ScanContext.WorkingBoxes.ToArray();
            }
        }

        bool Raycast(Vector2 screenPosition, out RaycastHit hit)
        {
            var cam = viewCamera != null ? viewCamera : Camera.main;
            hit = default;
            if (cam == null)
            {
                return false;
            }
            var ray = cam.ScreenPointToRay(screenPosition);
            return Physics.Raycast(ray, out hit, 10f, Layers.TerrainMask);
        }
    }
}
