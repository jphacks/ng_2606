using DeskStage.Shared;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace DeskStage.AR
{
    /// <summary>
    /// AR の状態を、子ども向けの案内に使う共通のスキャン状態に置き換える計算（A_ar.md Step 9）。
    /// MonoBehaviour から切り離してあり、EditMode のテストで確かめられる。
    /// </summary>
    public static class ScanStatusResolver
    {
        /// <param name="state">AR Session の状態</param>
        /// <param name="reason">追跡できない理由</param>
        /// <param name="deskDecided">机が決まっているか</param>
        /// <param name="relocalizingSeconds">位置の再推定が続いている秒数</param>
        /// <param name="relocalizingTimeout">これ以上続いたら Lost とみなす秒数</param>
        public static (ScanStatus status, ScanReason reason) Resolve(
            ARSessionState state,
            NotTrackingReason reason,
            bool deskDecided,
            float relocalizingSeconds,
            float relocalizingTimeout)
        {
            switch (state)
            {
                case ARSessionState.None:
                case ARSessionState.Unsupported:
                    return (ScanStatus.NotStarted, ScanReason.None);

                case ARSessionState.CheckingAvailability:
                case ARSessionState.NeedsInstall:
                case ARSessionState.Installing:
                case ARSessionState.Ready:
                    return (ScanStatus.Starting, ScanReason.None);
            }

            // ここから先は、初期化中か追跡中
            switch (reason)
            {
                case NotTrackingReason.InsufficientLight:
                    return (ScanStatus.Limited, ScanReason.Dark);
                case NotTrackingReason.ExcessiveMotion:
                    return (ScanStatus.Limited, ScanReason.FastMotion);
                case NotTrackingReason.InsufficientFeatures:
                    return (ScanStatus.Limited, ScanReason.FewFeatures);
                case NotTrackingReason.Relocalizing:
                    // 短い再推定はよくあるので、timeout を超えるまでは通常どおりにする
                    if (relocalizingSeconds >= relocalizingTimeout)
                    {
                        return (ScanStatus.Lost, ScanReason.None);
                    }
                    break;
            }

            if (state == ARSessionState.SessionInitializing &&
                (reason == NotTrackingReason.None || reason == NotTrackingReason.Initializing))
            {
                return (ScanStatus.Starting, ScanReason.None);
            }

            return (deskDecided ? ScanStatus.Scanning : ScanStatus.FindingDesk, ScanReason.None);
        }
    }
}
