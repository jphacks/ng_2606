using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.UI
{
    /// <summary>
    /// 開発用：他の担当の部品がまだないうちに、知らせを自分で出すボタン用（Step 1）。
    /// 合流前（Step 15）に DevMarker と一緒に消す。
    /// </summary>
    public class DevEvents : MonoBehaviour
    {
        [SerializeField] ScreenRouter router;
        [SerializeField] GuidanceView guidance;

        public void SetRouter(ScreenRouter value) => router = value;

        public void SetGuidance(GuidanceView value) => guidance = value;

        // スキャン状態を順に変えて、案内の表示を確かめる（Step 5）。本当は A が書く。
        static readonly (ScanStatus status, ScanReason reason, bool deskFound)[] ScanSteps =
        {
            (ScanStatus.Starting, ScanReason.None, false),
            (ScanStatus.FindingDesk, ScanReason.None, false),
            (ScanStatus.FindingDesk, ScanReason.None, true),
            (ScanStatus.Scanning, ScanReason.None, false),
            (ScanStatus.Limited, ScanReason.Dark, false),
            (ScanStatus.Limited, ScanReason.FastMotion, false),
            (ScanStatus.Limited, ScanReason.FewFeatures, false),
            (ScanStatus.Scanning, ScanReason.None, false),
            (ScanStatus.Lost, ScanReason.None, false),
            (ScanStatus.Finished, ScanReason.None, false),
            (ScanStatus.NotStarted, ScanReason.None, false),
        };

        int scanStep = -1;

        public void NextScanStatus()
        {
            scanStep = (scanStep + 1) % ScanSteps.Length;
            var step = ScanSteps[scanStep];
            StageContext.SetScanStatus(step.status, step.reason);
            if (guidance != null)
            {
                guidance.SetDeskFound(step.deskFound);
            }
            Debug.Log("DEV スキャン状態: " + step.status + " / " + step.reason + (step.deskFound ? "（机の面が見つかった）" : ""));
        }

        public void RaiseDeskDecided() => StageContext.RaiseDeskDecided();

        public void RaiseStageBuilt() => StageContext.RaiseStageBuilt();

        public void RaiseSaveOk() => StageContext.RaiseSaveCompleted(true);

        /// <summary>ゴールした知らせを出して、クリア画面へ移る。</summary>
        public void RaiseGoal()
        {
            GameEvents.RaiseGoal(12.34f, true);
            router.Show(ScreenId.Clear);
        }
    }
}
