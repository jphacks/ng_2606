using DeskStage.Shared;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace DeskStage.AR
{
    /// <summary>
    /// AR の状態を毎フレーム調べ、スキャン状態として StageContext に書く（A_ar.md Step 9）。
    /// 変わったときだけ ScanStatusChanged が出る（StageContext.SetScanStatus の仕事）。
    /// 置き換えの表は ScanStatusResolver にある。
    /// </summary>
    public class ScanStatusReporter : MonoBehaviour
    {
        [SerializeField] ScanSettings m_Settings;

        bool m_DeskDecided;
        bool m_Paused;
        float m_RelocalizingSeconds;

        /// <summary>Finished や NotStarted を ScanSession が書いている間、上書きしないように止める。</summary>
        public void Pause() => m_Paused = true;

        public void Resume() => m_Paused = false;

        void OnEnable()
        {
            StageContext.DeskDecided += OnDeskDecided;
            UiRequests.Rescan += OnRescan;
        }

        void OnDisable()
        {
            StageContext.DeskDecided -= OnDeskDecided;
            UiRequests.Rescan -= OnRescan;
        }

        void OnDeskDecided() => m_DeskDecided = true;

        void OnRescan()
        {
            // 机の決め直しから（A_ar.md Step 12）
            m_DeskDecided = false;
            m_RelocalizingSeconds = 0f;
        }

        void Update()
        {
            if (m_Paused)
            {
                return;
            }

            var reason = ARSession.notTrackingReason;
            m_RelocalizingSeconds = reason == NotTrackingReason.Relocalizing
                ? m_RelocalizingSeconds + Time.unscaledDeltaTime
                : 0f;

            var timeout = m_Settings != null ? m_Settings.relocalizingTimeout : 3f;
            var (status, scanReason) = ScanStatusResolver.Resolve(
                ARSession.state, reason, m_DeskDecided, m_RelocalizingSeconds, timeout);
            StageContext.SetScanStatus(status, scanReason);
        }
    }
}
