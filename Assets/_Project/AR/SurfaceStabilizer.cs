using System.Collections.Generic;
using DeskStage.Shared;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace DeskStage.AR
{
    /// <summary>
    /// PlaneWatcher が覚えている面を整えて、ScanContext.Surfaces に書く（A_ar.md Step 11）。
    /// 計算は SurfaceStabilizerCore。ここは、世界座標の面をステージ座標に直して渡し、
    /// 結果を ScanContext に書いて SurfacesChanged を出すだけ。
    /// 机が決まるまで（StageRoot が無い間）は何もしない。遊ぶ場面では止まる（世界座標とステージ座標が合わなくなるため）。
    /// </summary>
    public class SurfaceStabilizer : MonoBehaviour
    {
        // 面を整える計算を回す間隔（秒）。毎フレームは要らない
        const float TickInterval = 0.1f;

        [SerializeField] PlaneWatcher m_Watcher;
        [SerializeField] ScanSettings m_Settings;

        readonly Dictionary<TrackableId, int> m_SourceIds = new Dictionary<TrackableId, int>();
        readonly List<RawSurface> m_Raw = new List<RawSurface>();

        SurfaceStabilizerCore m_Core;
        int m_NextSourceId = 1;
        float m_NextTick;

        public SurfaceStabilizerCore Core => m_Core;

        void Awake()
        {
            if (m_Watcher == null) m_Watcher = FindFirstObjectByType<PlaneWatcher>();
            m_Core = new SurfaceStabilizerCore(m_Settings != null ? m_Settings : ScriptableObject.CreateInstance<ScanSettings>());
        }

        void Update()
        {
            var now = Time.realtimeSinceStartup;
            if (now < m_NextTick || m_Watcher == null)
            {
                return;
            }

            m_NextTick = now + TickInterval;

            if (StageContext.Mode == AppMode.Play)
            {
                return;
            }

            var root = StageContext.StageRoot;
            if (root == null)
            {
                if (m_Core.Clear())
                {
                    Publish();
                }

                return;
            }

            m_Raw.Clear();
            foreach (var plane in m_Watcher.Planes)
            {
                if (plane.Boundary.Length == 0)
                {
                    continue;
                }

                if (!m_SourceIds.TryGetValue(plane.Id, out var sourceId))
                {
                    sourceId = m_NextSourceId++;
                    m_SourceIds[plane.Id] = sourceId;
                }

                var outline = new Vector2[plane.Boundary.Length];
                for (var i = 0; i < outline.Length; i++)
                {
                    var local = root.InverseTransformPoint(plane.Boundary[i]);
                    outline[i] = new Vector2(local.x, local.z);
                }

                m_Raw.Add(new RawSurface
                {
                    SourceId = sourceId,
                    Height = root.InverseTransformPoint(plane.Center).y,
                    Outline = outline,
                });
            }

            if (m_Core.Update(now, ScanContext.DeskSize, m_Raw))
            {
                Publish();
            }
        }

        /// <summary>覚えている面と一覧を空にする（もういちどスキャン、Step 12）。</summary>
        public void Clear()
        {
            m_SourceIds.Clear();
            if (m_Core.Clear())
            {
                Publish();
            }
        }

        void Publish()
        {
            ScanContext.Surfaces.Clear();
            ScanContext.Surfaces.AddRange(m_Core.Surfaces);
            ScanContext.RaiseSurfacesChanged();
        }
    }
}
