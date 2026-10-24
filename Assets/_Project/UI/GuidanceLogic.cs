using DeskStage.Shared;

namespace DeskStage.UI
{
    /// <summary>
    /// スキャン状態から、出す案内（Wording のキー）を決める。計算だけの普通のクラス（Step 5）。
    /// 困った状態（Limited、Lost）から戻ったときは、前の案内を1秒残してから切り替える。
    /// </summary>
    public class GuidanceLogic
    {
        public const float FindingDeskHintSeconds = 10f;
        public const float RecoverHoldSeconds = 1f;

        ScanStatus status = ScanStatus.NotStarted;
        ScanReason reason = ScanReason.None;
        float findingDeskTime;
        float holdTime;
        string heldKey;

        /// <summary>机の面が見つかったか（FindingDesk のときだけ使う）。</summary>
        public bool DeskFound { get; set; }

        /// <summary>今出す案内のキー。出さないときは null。</summary>
        public string CurrentKey => heldKey ?? KeyFor(status, reason, findingDeskTime, DeskFound);

        public void SetStatus(ScanStatus newStatus, ScanReason newReason)
        {
            var wasTrouble = IsTrouble(status);
            var previousKey = CurrentKey;
            status = newStatus;
            reason = newReason;
            if (newStatus != ScanStatus.FindingDesk)
            {
                findingDeskTime = 0f;
                DeskFound = false;
            }
            if (wasTrouble && !IsTrouble(newStatus))
            {
                heldKey = previousKey;
                holdTime = RecoverHoldSeconds;
            }
            else
            {
                heldKey = null;
            }
        }

        /// <summary>毎フレーム呼ぶ。dt は経過秒。</summary>
        public void Tick(float dt)
        {
            if (status == ScanStatus.FindingDesk)
            {
                findingDeskTime += dt;
            }
            if (heldKey != null)
            {
                holdTime -= dt;
                if (holdTime <= 0f)
                {
                    heldKey = null;
                }
            }
        }

        static bool IsTrouble(ScanStatus s) => s == ScanStatus.Limited || s == ScanStatus.Lost;

        public static string KeyFor(ScanStatus status, ScanReason reason, float findingDeskSeconds, bool deskFound)
        {
            switch (status)
            {
                case ScanStatus.Starting:
                    return "guide.starting";
                case ScanStatus.FindingDesk:
                    if (deskFound)
                    {
                        return "guide.deskFound";
                    }
                    return findingDeskSeconds >= FindingDeskHintSeconds ? "guide.findingDeskLong" : "guide.findingDesk";
                case ScanStatus.Scanning:
                    return "guide.scanning";
                case ScanStatus.Limited:
                    switch (reason)
                    {
                        case ScanReason.Dark: return "guide.limitedDark";
                        case ScanReason.FastMotion: return "guide.limitedFast";
                        case ScanReason.FewFeatures: return "guide.limitedFew";
                        default: return "guide.scanning";
                    }
                case ScanStatus.Lost:
                    return "guide.lost";
                default:
                    return null;
            }
        }
    }
}
