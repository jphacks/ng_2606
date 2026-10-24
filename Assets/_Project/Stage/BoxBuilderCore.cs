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
    /// 上面の一覧から、上面1つにつき箱1つを作り・直し・消す計算（B の Step 6）。
    /// MonoBehaviour から切り離して、EditMode テストで確かめられるようにする。
    /// 自分が作った auto の箱だけを扱い、補助で足した manual の箱には触らない。
    /// </summary>
    public class BoxBuilderCore
    {
        // 上面の番号 → 箱の id
        readonly Dictionary<int, string> surfaceToBox = new Dictionary<int, string>();
        int nextNumber = 1;

        /// <summary>
        /// 上面の一覧に合わせて boxes を書き換え、起きた変化を返す。
        /// 形が変わっていない箱は Modified にしない（D の演出が何度も出ないように）。
        /// </summary>
        public List<BoxChangeEntry> Apply(IReadOnlyList<SurfaceData> surfaces, List<BoxData> boxes)
        {
            var changes = new List<BoxChangeEntry>();
            var seen = new HashSet<int>();

            foreach (var surface in surfaces)
            {
                // 境界の点がない上面からは箱を作れない
                if (surface.Outline == null || surface.Outline.Length == 0)
                {
                    continue;
                }
                seen.Add(surface.Id);

                var made = MakeBox(surface);
                if (surfaceToBox.TryGetValue(surface.Id, out var boxId))
                {
                    var index = boxes.FindIndex(b => b.id == boxId);
                    if (index >= 0)
                    {
                        made.id = boxId;
                        if (!SameShape(boxes[index], made))
                        {
                            boxes[index] = made;
                            changes.Add(new BoxChangeEntry { boxId = boxId, change = BoxChange.Modified });
                        }
                        continue;
                    }
                    // 一覧から箱が消されていたら、作り直す
                }

                made.id = NewBoxId(boxes);
                surfaceToBox[surface.Id] = made.id;
                boxes.Add(made);
                changes.Add(new BoxChangeEntry { boxId = made.id, change = BoxChange.Added });
            }

            // 上面が消えたら箱も消す
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
                if (boxes.RemoveAll(b => b.id == boxId) > 0)
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
                // 箱の高さ＝上面の高さ。中心の高さはその半分（箱は机の天面から立つ）
                center = new[] { f.center.x, height / 2f, f.center.y },
                size = new[] { f.width, height, f.depth },
                yawDegrees = f.yawDegrees,
                source = BoxData.SourceAuto,
            };
        }

        /// <summary>「box-」＋2桁の連番。一覧にすでにある id は飛ばす。</summary>
        string NewBoxId(List<BoxData> boxes)
        {
            while (true)
            {
                var id = $"box-{nextNumber:00}";
                nextNumber++;
                if (!boxes.Exists(b => b.id == id))
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
