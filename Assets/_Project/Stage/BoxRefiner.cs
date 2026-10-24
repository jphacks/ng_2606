using System.Collections.Generic;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>
    /// 上面から作った箱を、遊びやすい形に整える計算（B の Step 7）。
    /// 1. 隣り合う箱の上面の高さの差が小さければ、高い方にそろえる
    /// 2. 箱と箱のすき間が小さければ、低い方の箱を広げてすき間をなくす
    /// 渡された箱は変えず、整えた写しを返す。
    /// </summary>
    public static class BoxRefiner
    {
        /// <summary>回転の差がこれ以内なら「同じ向き」としてすき間を測る（度）。</summary>
        const float SameYawTolerance = 1f;

        public static List<BoxData> Refine(IReadOnlyList<BoxData> boxes, float heightMergeThreshold, float gapCloseThreshold)
        {
            var result = new List<BoxData>(boxes.Count);
            foreach (var b in boxes)
            {
                result.Add(Clone(b));
            }

            SnapHeights(result, heightMergeThreshold, gapCloseThreshold);
            CloseGaps(result, gapCloseThreshold);
            return result;
        }

        /// <summary>隣り合う（すき間が gapClose 未満の）箱どうしで、高さの差が threshold 未満なら高い方にそろえる。</summary>
        static void SnapHeights(List<BoxData> boxes, float threshold, float gapClose)
        {
            // そろえた結果、別の箱とまたそろう場合があるので、変わらなくなるまでくり返す
            for (var pass = 0; pass < boxes.Count; pass++)
            {
                var changed = false;
                for (var i = 0; i < boxes.Count; i++)
                {
                    for (var j = i + 1; j < boxes.Count; j++)
                    {
                        var a = boxes[i];
                        var b = boxes[j];
                        var diff = a.size[1] - b.size[1];
                        if (diff == 0f || Mathf.Abs(diff) >= threshold || !Near(a, b, gapClose))
                        {
                            continue;
                        }
                        SetHeight(diff < 0f ? a : b, Mathf.Max(a.size[1], b.size[1]));
                        changed = true;
                    }
                }
                if (!changed)
                {
                    break;
                }
            }
        }

        /// <summary>同じ向きの2つの箱の、向かい合う辺のすき間が threshold 未満なら、低い方の箱を広げてふさぐ。</summary>
        static void CloseGaps(List<BoxData> boxes, float threshold)
        {
            for (var i = 0; i < boxes.Count; i++)
            {
                for (var j = i + 1; j < boxes.Count; j++)
                {
                    var a = boxes[i];
                    var b = boxes[j];
                    if (Mathf.Abs(Mathf.DeltaAngle(a.yawDegrees, b.yawDegrees)) > SameYawTolerance)
                    {
                        // 【仮】向きの違う箱どうしは扱わない。RectFit ができて回った箱が出てきたら見直す
                        continue;
                    }

                    // a の向きにそろえた座標で、b の範囲を測る
                    var toLocal = Quaternion.Euler(0f, -a.yawDegrees, 0f);
                    var ac = Vec3.To(a.center);
                    var bc = toLocal * (Vec3.To(b.center) - ac);
                    var aHalf = new Vector2(a.size[0], a.size[2]) / 2f;
                    var bHalf = new Vector2(b.size[0], b.size[2]) / 2f;

                    var gapX = Mathf.Abs(bc.x) - aHalf.x - bHalf.x;
                    var gapZ = Mathf.Abs(bc.z) - aHalf.y - bHalf.y;
                    var overlapZ = aHalf.y + bHalf.y - Mathf.Abs(bc.z);
                    var overlapX = aHalf.x + bHalf.x - Mathf.Abs(bc.x);

                    // 低い方を広げる（高い方は本物の物の形のまま残す）
                    var lower = a.size[1] <= b.size[1] ? a : b;
                    var towardB = lower == a ? 1f : -1f;

                    if (gapX > 0f && gapX < threshold && overlapZ > 0f)
                    {
                        Widen(lower, a.yawDegrees, new Vector3(Mathf.Sign(bc.x) * towardB, 0f, 0f), gapX, 0);
                    }
                    else if (gapZ > 0f && gapZ < threshold && overlapX > 0f)
                    {
                        Widen(lower, a.yawDegrees, new Vector3(0f, 0f, Mathf.Sign(bc.z) * towardB), gapZ, 2);
                    }
                }
            }
        }

        /// <summary>箱を、localDir（向きをそろえた座標の X か Z）の方へ amount だけ広げる。</summary>
        static void Widen(BoxData box, float frameYaw, Vector3 localDir, float amount, int sizeAxis)
        {
            box.size[sizeAxis] += amount;
            var shift = Quaternion.Euler(0f, frameYaw, 0f) * localDir * (amount / 2f);
            box.center[0] += shift.x;
            box.center[2] += shift.z;
        }

        static void SetHeight(BoxData box, float height)
        {
            box.size[1] = height;
            box.center[1] = height / 2f;
        }

        /// <summary>上から見た外側の四角どうしが、gap 未満まで近いか（重なっている場合も含む）。</summary>
        static bool Near(BoxData a, BoxData b, float gap)
        {
            var ra = Bounds2D(a);
            var rb = Bounds2D(b);
            var dx = Mathf.Max(0f, Mathf.Max(ra.xMin - rb.xMax, rb.xMin - ra.xMax));
            var dz = Mathf.Max(0f, Mathf.Max(ra.yMin - rb.yMax, rb.yMin - ra.yMax));
            return dx < gap && dz < gap;
        }

        /// <summary>回した箱を上から見たとき、それを囲む XZ の軸にそろった四角。</summary>
        static Rect Bounds2D(BoxData box)
        {
            var rad = box.yawDegrees * Mathf.Deg2Rad;
            var cos = Mathf.Abs(Mathf.Cos(rad));
            var sin = Mathf.Abs(Mathf.Sin(rad));
            var hx = (box.size[0] * cos + box.size[2] * sin) / 2f;
            var hz = (box.size[0] * sin + box.size[2] * cos) / 2f;
            return Rect.MinMaxRect(box.center[0] - hx, box.center[2] - hz, box.center[0] + hx, box.center[2] + hz);
        }

        static BoxData Clone(BoxData b)
        {
            return new BoxData
            {
                id = b.id,
                center = (float[])b.center.Clone(),
                size = (float[])b.size.Clone(),
                yawDegrees = b.yawDegrees,
                source = b.source,
                texture = b.texture,
            };
        }
    }
}
