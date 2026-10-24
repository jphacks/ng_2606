using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace DeskStage.AR
{
    /// <summary>
    /// 覚えておく面1枚（A_ar.md Step 6）。
    /// 座標はすべて世界座標。ステージ座標への変換は SurfaceStabilizer の仕事。
    /// </summary>
    public class WatchedPlane
    {
        /// <summary>ARKit が付ける番号。</summary>
        public TrackableId Id;

        /// <summary>面の中心（世界座標）。</summary>
        public Vector3 Center;

        /// <summary>面の高さ（世界座標の Y）。</summary>
        public float Height;

        /// <summary>上から見た境界の点の並び（世界座標）。</summary>
        public Vector3[] Boundary = Array.Empty<Vector3>();

        /// <summary>最後に変わった時刻（Time.realtimeSinceStartup）。</summary>
        public float UpdatedAt;
    }

    /// <summary>
    /// 面の見張り（A_ar.md Step 6）。
    /// ARKit が見つけた面のうち、上向きの水平な面だけを覚えておく。
    /// 覚えた面は DeskFrame（机を決める）と SurfaceStabilizer（整える）が読む。
    ///
    /// ここでは捨てる・まとめる・整えるを一切しない。それは Step 11 の仕事。
    /// この部品は「ARKit が言ってきたことを、使える形で保持する」だけに絞る。
    /// </summary>
    [RequireComponent(typeof(ARPlaneManager))]
    public class PlaneWatcher : MonoBehaviour
    {
        readonly Dictionary<TrackableId, WatchedPlane> m_Planes =
            new Dictionary<TrackableId, WatchedPlane>();

        readonly List<WatchedPlane> m_Snapshot = new List<WatchedPlane>();

        ARPlaneManager m_PlaneManager;
        bool m_SnapshotDirty = true;

        /// <summary>覚えている面の一覧。読み取り専用。</summary>
        public IReadOnlyList<WatchedPlane> Planes
        {
            get
            {
                if (m_SnapshotDirty)
                {
                    m_Snapshot.Clear();
                    m_Snapshot.AddRange(m_Planes.Values);
                    m_SnapshotDirty = false;
                }
                return m_Snapshot;
            }
        }

        /// <summary>覚えている面が増えた・変わった・消えたときに出す。</summary>
        public event Action Changed;

        void Awake()
        {
            m_PlaneManager = GetComponent<ARPlaneManager>();
        }

        void OnEnable()
        {
            m_PlaneManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            m_PlaneManager.trackablesChanged.AddListener(OnTrackablesChanged);
        }

        void OnDisable()
        {
            m_PlaneManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
        }

        void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            var changed = false;

            foreach (var plane in args.added)
            {
                changed |= Remember(plane);
            }

            foreach (var plane in args.updated)
            {
                changed |= Remember(plane);
            }

            // ARKit は、成長した面が別の面を飲み込んだとき、飲み込まれた方を
            // 「消えた」として知らせてくる。ここで一覧から外れる。
            foreach (var pair in args.removed)
            {
                changed |= Forget(pair.Key);
            }

            if (changed)
            {
                m_SnapshotDirty = true;
                Changed?.Invoke();
            }
        }

        /// <summary>覚える。使わない面なら、覚えていた分を外す。</summary>
        bool Remember(ARPlane plane)
        {
            if (!ShouldWatch(plane))
            {
                return Forget(plane.trackableId);
            }

            if (!m_Planes.TryGetValue(plane.trackableId, out var watched))
            {
                watched = new WatchedPlane { Id = plane.trackableId };
                m_Planes[plane.trackableId] = watched;
            }

            watched.Center = plane.transform.position;
            watched.Height = plane.transform.position.y;
            watched.Boundary = ToWorldBoundary(plane);
            watched.UpdatedAt = Time.realtimeSinceStartup;
            m_SnapshotDirty = true;
            return true;
        }

        bool Forget(TrackableId id)
        {
            if (!m_Planes.Remove(id))
            {
                return false;
            }
            m_SnapshotDirty = true;
            return true;
        }

        /// <summary>
        /// 上向きの水平な面だけを使う。
        /// 天井（下向き）と壁（垂直）は、机の上の物ではないので捨てる。
        /// 別の面に飲み込まれた面も、もう実体がないので捨てる。
        /// </summary>
        static bool ShouldWatch(ARPlane plane)
        {
            if (plane == null)
            {
                return false;
            }
            if (plane.subsumedBy != null)
            {
                return false;
            }
            return plane.alignment == PlaneAlignment.HorizontalUp;
        }

        /// <summary>
        /// 境界の点を世界座標にする。
        /// ARPlane.boundary は面から見た XY（面の上では XZ にあたる）なので、
        /// 面の向きをかけて世界座標に直す。
        /// </summary>
        static Vector3[] ToWorldBoundary(ARPlane plane)
        {
            var boundary = plane.boundary;
            if (!boundary.IsCreated || boundary.Length == 0)
            {
                return Array.Empty<Vector3>();
            }

            var points = new Vector3[boundary.Length];
            for (var i = 0; i < boundary.Length; i++)
            {
                var local = new Vector3(boundary[i].x, 0f, boundary[i].y);
                points[i] = plane.transform.TransformPoint(local);
            }
            return points;
        }

        /// <summary>もういちどスキャンのときに、覚えている面を全部消す。</summary>
        public void Clear()
        {
            if (m_Planes.Count == 0)
            {
                return;
            }
            m_Planes.Clear();
            m_SnapshotDirty = true;
            Changed?.Invoke();
        }
    }
}
