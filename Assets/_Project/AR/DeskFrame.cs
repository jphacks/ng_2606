using System.Collections.Generic;
using DeskStage.Shared;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace DeskStage.AR
{
    /// <summary>
    /// 机を決め、その上に StageRoot を置く（A_ar.md Step 8）。
    /// UiRequests.SetDesk（画面のタップ位置）を受け、タップした先の面を机にする。
    /// 机が決まった後は、机の面が広がるのに合わせて DeskSize だけを更新する（原点と向きは変えない）。
    ///
    /// スキャン状態は書かない。FindingDesk と Scanning は ScanStatusReporter が、
    /// AR の状態と DeskDecided から決める（Step 9）。
    /// </summary>
    public class DeskFrame : MonoBehaviour
    {
        const float SizeEpsilon = 1e-4f;

        [SerializeField] PlaneWatcher m_Watcher;
        [SerializeField] ARRaycastManager m_RaycastManager;
        [SerializeField] ARAnchorManager m_AnchorManager;
        [SerializeField] ScanSettings m_Settings;

        readonly List<ARRaycastHit> m_Hits = new List<ARRaycastHit>();

        bool m_Deciding;
        bool m_Decided;
        TrackableId m_DeskPlaneId;
        Transform m_Root;

        /// <summary>決めた机の StageRoot。まだなら null。</summary>
        public Transform Root => m_Root;

        void Awake()
        {
            if (m_Watcher == null) m_Watcher = FindFirstObjectByType<PlaneWatcher>();
            if (m_RaycastManager == null) m_RaycastManager = FindFirstObjectByType<ARRaycastManager>();
            if (m_AnchorManager == null) m_AnchorManager = FindFirstObjectByType<ARAnchorManager>();
        }

        void OnEnable()
        {
            UiRequests.SetDesk += OnSetDesk;
            if (m_Watcher != null) m_Watcher.Changed += OnPlanesChanged;
        }

        void OnDisable()
        {
            UiRequests.SetDesk -= OnSetDesk;
            if (m_Watcher != null) m_Watcher.Changed -= OnPlanesChanged;
        }

        void OnSetDesk(Vector2 screenPosition)
        {
            // 机が決まった後の SetDesk は無視する（決め直しは Rescan で）
            if (m_Decided || m_Deciding || m_Watcher == null || m_RaycastManager == null)
            {
                return;
            }

            _ = DecideAsync(screenPosition);
        }

        async Awaitable DecideAsync(Vector2 screenPosition)
        {
            m_Deciding = true;
            try
            {
                var plane = FindTappedPlane(screenPosition);
                if (plane == null)
                {
                    return; // 当たらなければ何もしない
                }

                var fit = DeskFrameMath.FitDesk(plane.Boundary, plane.Height);
                var pose = new Pose(fit.Position, Quaternion.Euler(0f, fit.YawDegrees, 0f));
                var root = await CreateRoot(pose);

                m_Root = root;
                m_DeskPlaneId = plane.Id;
                m_Decided = true;

                ScanContext.DeskSize = fit.Size;
                StageContext.StageRoot = root;
                StageContext.RaiseDeskDecided();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                m_Deciding = false;
            }
        }

        WatchedPlane FindTappedPlane(Vector2 screenPosition)
        {
            m_Hits.Clear();
            if (!m_RaycastManager.Raycast(screenPosition, m_Hits, TrackableType.PlaneWithinPolygon))
            {
                return null;
            }

            // m_Hits は近い順。PlaneWatcher が覚えている面（上向きの水平な面）のうち、最初に当たったもの
            foreach (var hit in m_Hits)
            {
                var plane = FindWatched(hit.trackableId);
                if (plane != null && plane.Boundary.Length >= DeskFrameMath.MinBoundaryPoints)
                {
                    return plane;
                }
            }

            return null;
        }

        WatchedPlane FindWatched(TrackableId id)
        {
            foreach (var plane in m_Watcher.Planes)
            {
                if (plane.Id == id)
                {
                    return plane;
                }
            }

            return null;
        }

        /// <summary>StageRoot を作る。AR Anchor を付けるので、位置の再推定があっても本物の机に付いてくる。</summary>
        async Awaitable<Transform> CreateRoot(Pose pose)
        {
            if (m_AnchorManager != null && m_AnchorManager.enabled)
            {
                var result = await m_AnchorManager.TryAddAnchorAsync(pose);
                if (result.status.IsSuccess() && result.value != null)
                {
                    result.value.gameObject.name = "StageRoot";
                    return result.value.transform;
                }

                Debug.LogWarning("[DeskFrame] AR Anchor を作れませんでした。固定の位置に置きます");
            }

            var go = new GameObject("StageRoot");
            go.transform.SetPositionAndRotation(pose.position, pose.rotation);
            return go.transform;
        }

        void OnPlanesChanged()
        {
            if (!m_Decided || m_Root == null)
            {
                return;
            }

            var plane = FindWatched(m_DeskPlaneId) ?? FindMergedDeskPlane();
            if (plane == null)
            {
                return;
            }

            var size = DeskFrameMath.SymmetricSize(plane.Boundary, new Pose(m_Root.position, m_Root.rotation));
            if ((size - ScanContext.DeskSize).sqrMagnitude > SizeEpsilon * SizeEpsilon)
            {
                ScanContext.DeskSize = size;
            }
        }

        /// <summary>
        /// ARKit は、成長した面が別の面を飲み込んだとき、飲み込まれた方の番号を消す。
        /// 机の面が消えたら、StageRoot の真下にある同じ高さの面を、机の面の続きとして追う。
        /// </summary>
        WatchedPlane FindMergedDeskPlane()
        {
            var tolerance = m_Settings != null ? m_Settings.mergeHeightTolerance : 0.01f;
            foreach (var plane in m_Watcher.Planes)
            {
                if (Mathf.Abs(plane.Height - m_Root.position.y) <= tolerance
                    && DeskFrameMath.ContainsXZ(plane.Boundary, m_Root.position))
                {
                    m_DeskPlaneId = plane.Id;
                    return plane;
                }
            }

            return null;
        }
    }
}
