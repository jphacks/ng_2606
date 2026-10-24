using DeskStage.Shared;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace DeskStage.AR
{
    /// <summary>
    /// D の依頼（StartScan、FinishScan、Mode が Play）を受けて AR を始め、止める（A_ar.md Step 10）。
    /// 【未決】遊ぶ画面の背景をカメラ映像にするかは決まっていない。
    /// 決まるまでは、Play になったら AR Session も止める（完全な3D画面）。
    /// </summary>
    public class ScanSession : MonoBehaviour
    {
        [SerializeField] ARSession m_Session;
        [SerializeField] ARPlaneManager m_PlaneManager;
        [SerializeField] ScanStatusReporter m_Reporter;

        [Tooltip("起動時に AR を止めておき、StartScan で動かす。Test_A は単体で動かすため false")]
        [SerializeField] bool m_DisableOnAwake = true;

        AppMode m_LastMode;

        void Awake()
        {
            if (m_Session == null) m_Session = FindFirstObjectByType<ARSession>();
            if (m_PlaneManager == null) m_PlaneManager = FindFirstObjectByType<ARPlaneManager>();
            if (m_Reporter == null) m_Reporter = GetComponent<ScanStatusReporter>();

            m_LastMode = StageContext.Mode;

            if (m_DisableOnAwake)
            {
                SetArRunning(false);
                StageContext.SetScanStatus(ScanStatus.NotStarted);
            }
        }

        void OnEnable()
        {
            UiRequests.StartScan += OnStartScan;
            UiRequests.FinishScan += OnFinishScan;
        }

        void OnDisable()
        {
            UiRequests.StartScan -= OnStartScan;
            UiRequests.FinishScan -= OnFinishScan;
        }

        void Update()
        {
            // Mode には変わった知らせがないので、毎フレーム見る
            var mode = StageContext.Mode;
            if (mode == m_LastMode)
            {
                return;
            }
            m_LastMode = mode;

            if (mode == AppMode.Play)
            {
                OnEnterPlay();
            }
        }

        void OnStartScan()
        {
            SetArRunning(true);
            if (m_Reporter != null) m_Reporter.Resume();
            StageContext.SetScanStatus(ScanStatus.Starting);
        }

        void OnFinishScan()
        {
            // 見つけた面は残す。たしかめ画面でカメラ映像を使うので AR Session は止めない
            if (m_PlaneManager != null) m_PlaneManager.enabled = false;
            if (m_Reporter != null) m_Reporter.Pause();
            StageContext.SetScanStatus(ScanStatus.Finished);
        }

        void OnEnterPlay()
        {
            SetArRunning(false);
            if (m_Reporter != null) m_Reporter.Pause();
            StageContext.SetScanStatus(ScanStatus.NotStarted);
        }

        void SetArRunning(bool running)
        {
            if (m_Session != null) m_Session.enabled = running;
            if (m_PlaneManager != null) m_PlaneManager.enabled = running;
        }
    }
}
