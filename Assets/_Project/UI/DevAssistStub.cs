using System.Collections.Generic;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.UI
{
    /// <summary>
    /// 開発用：B の補助（BoxAssist、B の Step 9）の代わりに、補助の依頼を受けて箱を変える（Step 11 を PC で確かめるため）。
    /// 決まりは B_scan_data.md の Step 9 に合わせた最小限（5mm・15度にそろえる、範囲、もどすは20回まで）。
    /// テスト用シーンにだけ置き、B の BoxAssist ができたら使わない。合流前に DevMarker と一緒に消す。
    /// </summary>
    public class DevAssistStub : MonoBehaviour
    {
        const float Snap = 0.005f;
        const float SnapDegrees = 15f;
        const float AddSize = 0.05f;
        const int UndoMax = 20;

        readonly List<List<BoxData>> undo = new List<List<BoxData>>();

        void OnEnable()
        {
            UiRequests.AddBox += OnAdd;
            UiRequests.SelectBox += OnSelect;
            UiRequests.MoveBox += OnMove;
            UiRequests.ResizeBox += OnResize;
            UiRequests.RotateBox += OnRotate;
            UiRequests.DeleteBox += OnDelete;
            UiRequests.UndoBox += OnUndo;
        }

        void OnDisable()
        {
            UiRequests.AddBox -= OnAdd;
            UiRequests.SelectBox -= OnSelect;
            UiRequests.MoveBox -= OnMove;
            UiRequests.ResizeBox -= OnResize;
            UiRequests.RotateBox -= OnRotate;
            UiRequests.DeleteBox -= OnDelete;
            UiRequests.UndoBox -= OnUndo;
        }

        static bool Raycast(Vector2 screen, out RaycastHit hit)
        {
            hit = default;
            var cam = Camera.main;
            return cam != null && Physics.Raycast(cam.ScreenPointToRay(screen), out hit, 10f, Layers.TerrainMask);
        }

        void OnAdd(Vector2 screen)
        {
            var root = StageContext.StageRoot;
            if (root == null || !Raycast(screen, out var hit))
            {
                return;
            }
            Remember();
            var p = root.InverseTransformPoint(hit.point);
            var id = NextId();
            // 箱の上なら積む：高さは当たった面の高さ＋一辺
            var height = Mathf.Max(0f, p.y) + AddSize;
            ScanContext.WorkingBoxes.Add(new BoxData
            {
                id = id,
                center = new[] { Round(p.x), height * 0.5f, Round(p.z) },
                size = new[] { AddSize, height, AddSize },
                source = BoxData.SourceManual,
            });
            ScanContext.SelectedBoxId = id;
            Sync();
            ScanContext.RaiseBoxesChanged(id, BoxChange.Added, BoxData.SourceManual);
        }

        void OnSelect(Vector2 screen)
        {
            ScanContext.SelectedBoxId = Raycast(screen, out var hit) && Find(hit.collider.name) != null ? hit.collider.name : "";
        }

        void OnMove(Vector2 delta) => Change(b =>
        {
            b.center[0] = Round(b.center[0] + delta.x);
            b.center[2] = Round(b.center[2] + delta.y);
        });

        void OnResize(Vector3 delta) => Change(b =>
        {
            b.size[0] = Mathf.Clamp(Round(b.size[0] + delta.x), 0.01f, 0.60f);
            b.size[1] = Mathf.Clamp(Round(b.size[1] + delta.y), 0.005f, 0.40f);
            b.size[2] = Mathf.Clamp(Round(b.size[2] + delta.z), 0.01f, 0.60f);
            b.center[1] = b.size[1] * 0.5f;
        });

        void OnRotate(float degrees) => Change(b =>
            b.yawDegrees = Mathf.Repeat(Mathf.Round((b.yawDegrees + degrees) / SnapDegrees) * SnapDegrees, 360f));

        void OnDelete()
        {
            var box = Find(ScanContext.SelectedBoxId);
            if (box == null)
            {
                return;
            }
            Remember();
            ScanContext.WorkingBoxes.Remove(box);
            ScanContext.SelectedBoxId = "";
            Sync();
            ScanContext.RaiseBoxesChanged(box.id, BoxChange.Removed, BoxData.SourceManual);
        }

        void OnUndo()
        {
            if (undo.Count == 0)
            {
                return;
            }
            var last = undo[undo.Count - 1];
            undo.RemoveAt(undo.Count - 1);
            ScanContext.WorkingBoxes.Clear();
            ScanContext.WorkingBoxes.AddRange(last);
            if (Find(ScanContext.SelectedBoxId) == null)
            {
                ScanContext.SelectedBoxId = "";
            }
            Sync();
            ScanContext.RaiseBoxesChanged("", BoxChange.Modified, BoxData.SourceManual);
        }

        void Change(System.Action<BoxData> change)
        {
            var box = Find(ScanContext.SelectedBoxId);
            if (box == null)
            {
                return;
            }
            Remember();
            change(box);
            box.source = BoxData.SourceManual;
            Sync();
            ScanContext.RaiseBoxesChanged(box.id, BoxChange.Modified, BoxData.SourceManual);
        }

        // 操作の前の箱の一覧をまるごと写して積む
        void Remember()
        {
            var copy = new List<BoxData>();
            foreach (var b in ScanContext.WorkingBoxes)
            {
                copy.Add(new BoxData
                {
                    id = b.id,
                    center = (float[])b.center.Clone(),
                    size = (float[])b.size.Clone(),
                    yawDegrees = b.yawDegrees,
                    source = b.source,
                    texture = b.texture,
                });
            }
            undo.Add(copy);
            if (undo.Count > UndoMax)
            {
                undo.RemoveAt(0);
            }
        }

        // 箱のデータに合わせて、StageRoot の下の箱のオブジェクトを作る・直す・消す（B の StageBuilder の代わり）
        static void Sync()
        {
            var root = StageContext.StageRoot;
            if (root == null)
            {
                return;
            }
            var keep = new HashSet<string>();
            foreach (var b in ScanContext.WorkingBoxes)
            {
                keep.Add(b.id);
                var t = root.Find(b.id);
                if (t == null)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = b.id;
                    go.layer = Layers.Terrain;
                    t = go.transform;
                    t.SetParent(root, false);
                }
                t.localPosition = new Vector3(b.center[0], b.center[1], b.center[2]);
                t.localRotation = Quaternion.Euler(0f, b.yawDegrees, 0f);
                t.localScale = new Vector3(b.size[0], b.size[1], b.size[2]);
            }
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child.name.StartsWith("box-") && !keep.Contains(child.name))
                {
                    Destroy(child.gameObject);
                }
            }
            // 素材を場面に合わせ直してもらう
            StageContext.RaiseStageBuilt();
        }

        static BoxData Find(string id) => string.IsNullOrEmpty(id) ? null : ScanContext.WorkingBoxes.Find(b => b.id == id);

        static string NextId()
        {
            for (int n = 1; ; n++)
            {
                var id = "box-" + n.ToString("00");
                if (Find(id) == null)
                {
                    return id;
                }
            }
        }

        static float Round(float v) => Mathf.Round(v / Snap) * Snap;
    }
}
