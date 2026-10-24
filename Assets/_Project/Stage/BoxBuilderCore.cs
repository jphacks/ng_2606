using System.Collections.Generic;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>箱の一覧に起きた変化1件。</summary>
    public struct BoxChangeEntry
    {
        public string boxId;
        public BoxChange change;
    }

    /// <summary>
    /// 上面の一覧から、上面1つにつき箱1つを作り・直し・消す計算（B の Step 6・7）。
    /// MonoBehaviour から切り離して、EditMode テストで確かめられるようにする。
    /// 自分が作った auto の箱だけを扱う。manual の箱（補助で足した・直した箱）には触らない。
    /// </summary>
    public class BoxBuilderCore
    {
        readonly float minBoxHeight;
        readonly float heightMergeThreshold;
        readonly float gapCloseThreshold;

        // 上面の番号 → 箱の id
        readonly Dictionary<int, string> surfaceToBox = new Dictionary<int, string>();
        int nextNumber = 1;

        /// <summary>手順書の初期値で作る（テスト用）。</summary>
        public BoxBuilderCore() : this(0.005f, 0.008f, 0.016f)
        {
        }

        public BoxBuilderCore(BoxSettings settings)
            : this(settings.minBoxHeight, settings.heightMergeThreshold, settings.gapCloseThreshold)
        {
        }

        public BoxBuilderCore(float minBoxHeight, float heightMergeThreshold, float gapCloseThreshold)
        {
            this.minBoxHeight = minBoxHeight;
            this.heightMergeThreshold = heightMergeThreshold;
            this.gapCloseThreshold = gapCloseThreshold;
        }

        /// <summary>
        /// 上面の一覧に合わせて boxes を書き換え、起きた変化を返す。
        /// 形が変わっていない箱は Modified にしない（D の演出が何度も出ないように）。
        /// </summary>
        public List<BoxChangeEntry> Apply(IReadOnlyList<SurfaceData> surfaces, List<BoxData> boxes)
        {
            // 1. 上面ごとに、整える前の箱を作る
            var raw = new List<BoxData>();
            var seen = new HashSet<int>();
            foreach (var surface in surfaces)
            {
                // 境界の点がない上面と、低すぎる上面からは箱を作らない
                if (surface.Outline == null || surface.Outline.Length == 0 || surface.Height < minBoxHeight)
                {
                    continue;
                }

                if (surfaceToBox.TryGetValue(surface.Id, out var mappedId))
                {
                    var existing = boxes.Find(b => b.id == mappedId);
                    if (existing != null && existing.source == BoxData.SourceManual)
                    {
                        // 補助で直された箱は、上面が変わっても自動で直さない
                        seen.Add(surface.Id);
                        continue;
                    }
                    if (existing == null)
                    {
                        // 一覧から箱が消されていたら、新しい id で作り直す
                        surfaceToBox.Remove(surface.Id);
                    }
                }

                seen.Add(surface.Id);
                if (!surfaceToBox.TryGetValue(surface.Id, out var boxId))
                {
                    boxId = NewBoxId(boxes, raw);
                    surfaceToBox[surface.Id] = boxId;
                }
                var made = MakeBox(surface);
                made.id = boxId;
                raw.Add(made);
            }

            // 2. 高さをそろえ、すき間をなくす
            var refined = BoxRefiner.Refine(raw, heightMergeThreshold, gapCloseThreshold);

            // 3. 作業中の一覧との違いを書き込む
            var changes = new List<BoxChangeEntry>();
            foreach (var made in refined)
            {
                var index = boxes.FindIndex(b => b.id == made.id);
                if (index < 0)
                {
                    boxes.Add(made);
                    changes.Add(new BoxChangeEntry { boxId = made.id, change = BoxChange.Added });
                }
                else if (!SameShape(boxes[index], made))
                {
                    boxes[index] = made;
                    changes.Add(new BoxChangeEntry { boxId = made.id, change = BoxChange.Modified });
                }
            }

            // 4. 上面が消えた箱を消す（manual になった箱は残す）
            var gone = new List<int>();
            foreach (var pair in surfaceToBox)
            {
                if (!seen.Contains(pair.Key))
                {
                    gone.Add(pair.Key);
                }
            }
            foreach (var surfaceId in gone)
            {
                var boxId = surfaceToBox[surfaceId];
                surfaceToBox.Remove(surfaceId);
                if (boxes.RemoveAll(b => b.id == boxId && b.source == BoxData.SourceAuto) > 0)
                {
                    changes.Add(new BoxChangeEntry { boxId = boxId, change = BoxChange.Removed });
                }
            }

            return changes;
        }

        /// <summary>上面と箱の対応と、連番を忘れる（もういちどスキャンのとき）。</summary>
        public void Reset()
        {
            surfaceToBox.Clear();
            nextNumber = 1;
        }

        static BoxData MakeBox(SurfaceData surface)
        {
            var f = BoxFootprint.Fit(surface.Outline);
            var height = surface.Height;
            return new BoxData
            {
                // 箱の高さ＝上面の高さ。中心の高さはその半分（箱は机の天面から立つ）。
                // 積んだ物は、上の物も下の物の見えている所も、それぞれ机まで届く箱になる（重なってよい）
                center = new[] { f.center.x, height / 2f, f.center.y },
                size = new[] { f.width, height, f.depth },
                yawDegrees = f.yawDegrees,
                source = BoxData.SourceAuto,
            };
        }

        /// <summary>「box-」＋2桁の連番。一覧にすでにある id は飛ばす。</summary>
        string NewBoxId(List<BoxData> boxes, List<BoxData> pending)
        {
            while (true)
            {
                var id = $"box-{nextNumber:00}";
                nextNumber++;
                if (!boxes.Exists(b => b.id == id) && !pending.Exists(b => b.id == id))
                {
                    return id;
                }
            }
        }

        const float ShapeTolerance = 0.0005f;

        static bool SameShape(BoxData a, BoxData b)
        {
            return Near(Vec3.To(a.center), Vec3.To(b.center))
                && Near(Vec3.To(a.size), Vec3.To(b.size))
                && Mathf.Abs(Mathf.DeltaAngle(a.yawDegrees, b.yawDegrees)) < 0.1f;
        }

        static bool Near(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < ShapeTolerance * ShapeTolerance;
    }
}
