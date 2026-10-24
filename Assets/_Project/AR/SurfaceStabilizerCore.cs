using System;
using System.Collections.Generic;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.AR
{
    /// <summary>
    /// 安定化の入力。ステージ座標に直した、ARKit の面 1 枚（A_ar.md Step 11）。
    /// </summary>
    public struct RawSurface
    {
        /// <summary>面を区別する番号。同じ面には毎回同じ番号を渡す（TrackableId から割り当てる）。</summary>
        public int SourceId;

        /// <summary>机の天面からの高さ（m）。</summary>
        public float Height;

        /// <summary>上から見た境界の点の並び（ステージ座標の XZ）。</summary>
        public Vector2[] Outline;
    }

    /// <summary>
    /// ARKit の面を、B が箱を作りやすい「整えた上面の一覧」にする計算（A_ar.md Step 11）。
    /// AR に触れない普通のクラス。時刻は呼ぶ側が渡す（テストで進められるように）。
    ///
    /// 流れ：捨てる（机の外・低い・高い・小さい）→ 高さをなめらかにして 1mm に丸める
    ///       → 重なる・近い面をまとめる → ふちを広げる → 一覧にする。
    /// 面は、見つかってから settleTime の間、形が大きく変わらなかったものだけを使う。
    /// </summary>
    public class SurfaceStabilizerCore
    {
        const float ShapeEpsilon = 0.0005f; // 一覧の「変わった」とみなす差（m）。0.5mm
        const float HeightStep = 0.001f;    // 高さの丸め（m）。1mm

        class SourceState
        {
            public float SmoothedHeight;
            public float LastTime;
            public float StableSince;
            public Vector2 ReferenceCentroid;
            public float ReferenceArea;
            public bool Settled;
            public bool Seen;
        }

        class Candidate
        {
            public int SourceId;
            public float Height;
            public Vector2[] Outline;
            public float Area;
        }

        readonly ScanSettings m_Settings;
        readonly Dictionary<int, SourceState> m_Sources = new Dictionary<int, SourceState>();
        readonly Dictionary<string, int> m_GroupIds = new Dictionary<string, int>();
        readonly Dictionary<int, int[]> m_OriginalIds = new Dictionary<int, int[]>();
        readonly List<SurfaceData> m_Published = new List<SurfaceData>();

        int m_NextSurfaceId = 1;
        float m_LastPublishTime = float.NegativeInfinity;

        public SurfaceStabilizerCore(ScanSettings settings)
        {
            m_Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>今、出している整えた上面の一覧。</summary>
        public IReadOnlyList<SurfaceData> Surfaces => m_Published;

        /// <summary>まとめた面の、元の面の番号（RawSurface.SourceId）。まとめていない面は自分だけ。</summary>
        public IReadOnlyList<int> OriginalIds(int surfaceId)
            => m_OriginalIds.TryGetValue(surfaceId, out var ids) ? ids : Array.Empty<int>();

        /// <summary>
        /// 面を渡して、一覧を更新する。一覧が変わり、前に出してから notifyInterval 以上たっていたら true。
        /// （true のときに ScanContext.Surfaces を書き換えて SurfacesChanged を出す。）
        /// </summary>
        /// <param name="now">時刻（秒）。</param>
        /// <param name="deskSize">机の長方形の幅と奥行き（原点が中心、軸に平行）。</param>
        /// <param name="raw">ステージ座標に直した面。</param>
        public bool Update(float now, Vector2 deskSize, IReadOnlyList<RawSurface> raw)
        {
            Track(now, raw);
            var candidates = Filter(deskSize, raw);
            var next = Build(now, candidates);

            if (SameSurfaces(next, m_Published))
            {
                return false;
            }

            if (now - m_LastPublishTime < m_Settings.notifyInterval)
            {
                return false; // 次の呼び出しで、まだ違っていれば出す
            }

            Publish(next, now);
            return true;
        }

        /// <summary>すべての記録と一覧を消す（もういちどスキャン）。一覧が空でなかったら true。</summary>
        public bool Clear()
        {
            var hadSurfaces = m_Published.Count > 0;
            m_Sources.Clear();
            m_GroupIds.Clear();
            m_OriginalIds.Clear();
            m_Published.Clear();
            m_LastPublishTime = float.NegativeInfinity;
            return hadSurfaces;
        }

        /// <summary>面ごとに、高さをなめらかにし、形が落ち着いたかを数える。</summary>
        void Track(float now, IReadOnlyList<RawSurface> raw)
        {
            foreach (var state in m_Sources.Values) state.Seen = false;

            foreach (var surface in raw)
            {
                if (surface.Outline == null || surface.Outline.Length == 0)
                {
                    continue;
                }

                var area = SurfaceGeometry.Area(surface.Outline);
                var centroid = SurfaceGeometry.Centroid(surface.Outline);

                if (!m_Sources.TryGetValue(surface.SourceId, out var state))
                {
                    m_Sources[surface.SourceId] = new SourceState
                    {
                        SmoothedHeight = surface.Height,
                        LastTime = now,
                        StableSince = now,
                        ReferenceArea = area,
                        ReferenceCentroid = centroid,
                        Seen = true,
                    };
                    continue;
                }

                var dt = Mathf.Max(0f, now - state.LastTime);
                state.LastTime = now;
                var time = m_Settings.heightSmoothingTime;
                var alpha = time <= 0f ? 1f : 1f - Mathf.Exp(-dt / time);
                state.SmoothedHeight += (surface.Height - state.SmoothedHeight) * alpha;
                state.Seen = true;

                if (ShapeChangedALot(state, area, centroid))
                {
                    state.ReferenceArea = area;
                    state.ReferenceCentroid = centroid;
                    state.StableSince = now;
                }

                // 一度落ち着いた面は、その後に形が変わっても出し続ける（出たり消えたりして、箱がちらつかないように）
                if (!state.Settled && now - state.StableSince >= m_Settings.settleTime)
                {
                    state.Settled = true;
                }
            }

            var gone = new List<int>();
            foreach (var pair in m_Sources)
            {
                if (!pair.Value.Seen) gone.Add(pair.Key);
            }

            foreach (var id in gone) m_Sources.Remove(id);
        }

        bool ShapeChangedALot(SourceState state, float area, Vector2 centroid)
        {
            var ratio = m_Settings.settleChangeRatio;
            if (Mathf.Abs(area - state.ReferenceArea) > ratio * Mathf.Max(state.ReferenceArea, 1e-9f))
            {
                return true;
            }

            var scale = Mathf.Sqrt(Mathf.Max(state.ReferenceArea, 1e-9f));
            return (centroid - state.ReferenceCentroid).magnitude > ratio * scale;
        }

        /// <summary>捨てる。机の外、低い、高すぎる、小さすぎる、まだ落ち着いていない面。</summary>
        List<Candidate> Filter(Vector2 deskSize, IReadOnlyList<RawSurface> raw)
        {
            var candidates = new List<Candidate>();
            var halfX = deskSize.x / 2f + m_Settings.outsideDeskMargin;
            var halfZ = deskSize.y / 2f + m_Settings.outsideDeskMargin;

            foreach (var surface in raw)
            {
                if (surface.Outline == null || surface.Outline.Length == 0
                    || !m_Sources.TryGetValue(surface.SourceId, out var state) || !state.Settled)
                {
                    continue;
                }

                var height = Mathf.Round(state.SmoothedHeight / HeightStep) * HeightStep;
                // 机の天面より低い面と、机の天面と同じとみなす面（deskSurfaceHeight 未満）
                if (height < m_Settings.deskSurfaceHeight || height > m_Settings.maxSurfaceHeight)
                {
                    continue;
                }

                var area = SurfaceGeometry.Area(surface.Outline);
                if (area < m_Settings.minSurfaceArea)
                {
                    continue;
                }

                if (OutsideDesk(surface.Outline, halfX, halfZ))
                {
                    continue;
                }

                candidates.Add(new Candidate
                {
                    SourceId = surface.SourceId,
                    Height = height,
                    Outline = surface.Outline,
                    Area = area,
                });
            }

            return candidates;
        }

        /// <summary>面を囲む長方形が、広げた机の長方形と、まったく重ならなければ「外」。</summary>
        static bool OutsideDesk(Vector2[] outline, float halfX, float halfZ)
        {
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var p in outline)
            {
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minZ = Mathf.Min(minZ, p.y);
                maxZ = Mathf.Max(maxZ, p.y);
            }

            return maxX < -halfX || minX > halfX || maxZ < -halfZ || minZ > halfZ;
        }

        /// <summary>まとめて、ふちを広げて、番号を付ける。番号の小さい順に返す。</summary>
        List<SurfaceData> Build(float now, List<Candidate> candidates)
        {
            var groups = Group(candidates);
            var result = new List<SurfaceData>(groups.Count);
            var seenKeys = new HashSet<string>();
            var originals = new Dictionary<int, int[]>();

            foreach (var group in groups)
            {
                group.Sort((a, b) => a.SourceId.CompareTo(b.SourceId));
                var key = string.Join(",", group.ConvertAll(c => c.SourceId));
                seenKeys.Add(key);

                if (!m_GroupIds.TryGetValue(key, out var id))
                {
                    id = m_NextSurfaceId++;
                    m_GroupIds[key] = id;
                }

                var largest = group[0];
                foreach (var c in group)
                {
                    if (c.Area > largest.Area) largest = c;
                }

                Vector2[] outline;
                if (group.Count == 1)
                {
                    outline = group[0].Outline;
                }
                else
                {
                    var points = new List<Vector2>();
                    foreach (var c in group) points.AddRange(c.Outline);
                    outline = SurfaceGeometry.ConvexHull(points);
                }

                originals[id] = group.ConvertAll(c => c.SourceId).ToArray();
                result.Add(new SurfaceData
                {
                    Id = id,
                    Height = largest.Height,
                    Outline = SurfaceGeometry.Expand(outline, m_Settings.edgeExpansion),
                    UpdatedAt = now,
                });
            }

            // 今のフレームに無い組み合わせの番号は忘れる（組み合わせが戻ったら、新しい番号になる）
            var stale = new List<string>();
            foreach (var key in m_GroupIds.Keys)
            {
                if (!seenKeys.Contains(key)) stale.Add(key);
            }

            foreach (var key in stale) m_GroupIds.Remove(key);

            result.Sort((a, b) => a.Id.CompareTo(b.Id));
            m_PendingOriginals = originals;
            return result;
        }

        Dictionary<int, int[]> m_PendingOriginals;

        /// <summary>
        /// 高さの差が mergeHeightTolerance 以内で、上から見て重なるか、
        /// すき間が mergeGapTolerance 以内の面を、つなげてひとまとまりにする。
        /// </summary>
        List<List<Candidate>> Group(List<Candidate> candidates)
        {
            var parent = new int[candidates.Count];
            for (var i = 0; i < parent.Length; i++) parent[i] = i;

            int Find(int x)
            {
                while (parent[x] != x)
                {
                    parent[x] = parent[parent[x]];
                    x = parent[x];
                }

                return x;
            }

            for (var i = 0; i < candidates.Count; i++)
            {
                for (var j = i + 1; j < candidates.Count; j++)
                {
                    if (Mathf.Abs(candidates[i].Height - candidates[j].Height) > m_Settings.mergeHeightTolerance)
                    {
                        continue;
                    }

                    if (SurfaceGeometry.Distance(candidates[i].Outline, candidates[j].Outline)
                        <= m_Settings.mergeGapTolerance)
                    {
                        parent[Find(i)] = Find(j);
                    }
                }
            }

            var byRoot = new Dictionary<int, List<Candidate>>();
            for (var i = 0; i < candidates.Count; i++)
            {
                var root = Find(i);
                if (!byRoot.TryGetValue(root, out var list))
                {
                    list = new List<Candidate>();
                    byRoot[root] = list;
                }

                list.Add(candidates[i]);
            }

            return new List<List<Candidate>>(byRoot.Values);
        }

        void Publish(List<SurfaceData> next, float now)
        {
            // 前と同じ形の面は、最後に変わった時刻を引き継ぐ
            foreach (var surface in next)
            {
                foreach (var old in m_Published)
                {
                    if (old.Id == surface.Id && SameShape(old, surface))
                    {
                        surface.UpdatedAt = old.UpdatedAt;
                        break;
                    }
                }
            }

            m_Published.Clear();
            m_Published.AddRange(next);
            m_OriginalIds.Clear();
            foreach (var pair in m_PendingOriginals) m_OriginalIds[pair.Key] = pair.Value;
            m_LastPublishTime = now;
        }

        static bool SameSurfaces(List<SurfaceData> a, List<SurfaceData> b)
        {
            if (a.Count != b.Count) return false;
            for (var i = 0; i < a.Count; i++)
            {
                if (a[i].Id != b[i].Id || !SameShape(a[i], b[i])) return false;
            }

            return true;
        }

        static bool SameShape(SurfaceData a, SurfaceData b)
        {
            if (Mathf.Abs(a.Height - b.Height) > ShapeEpsilon || a.Outline.Length != b.Outline.Length)
            {
                return false;
            }

            for (var i = 0; i < a.Outline.Length; i++)
            {
                if ((a.Outline[i] - b.Outline[i]).magnitude > ShapeEpsilon) return false;
            }

            return true;
        }
    }
}
