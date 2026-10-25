using DeskStage.Shared;

namespace DeskStage.App
{
    /// <summary>画面。</summary>
    public enum AppScreenId
    {
        Title,
        Home,
        HowTo,
        Reach,
        Capture,
        Generating,
        Check,
        Play,
        Clear,
        GameOver,
        Characters,
        Backdrop,
    }

    /// <summary>撮影の画面が、いま何を待っているか。</summary>
    public enum CaptureStep
    {
        /// <summary>カメラの準備中。</summary>
        Preparing,
        /// <summary>1枚目（真上）を撮る。</summary>
        Top,
        /// <summary>2枚目（斜め）を撮る。</summary>
        Side,
        /// <summary>2枚そろって、サーバーの答えを待つ。</summary>
        Sending,
        /// <summary>うまくいかなかった。</summary>
        Failed,
    }

    /// <summary>撮影・送信が、うまくいかなかった原因。</summary>
    public enum FailureKind
    {
        /// <summary>カメラが許可されていない（設定アプリで許可が要る）。</summary>
        CameraDenied,
        /// <summary>インターネットにつながらない。</summary>
        Network,
        /// <summary>サーバーが断った、混んでいる、答えが変だった。</summary>
        Server,
        /// <summary>時間がかかりすぎた。</summary>
        Timeout,
        /// <summary>答えは届いたが、箱が 1 つも作れなかった。</summary>
        NoBoxes,
        /// <summary>その他。</summary>
        Unknown,
    }

    /// <summary>
    /// 使うときの流れの決まり（どの画面で、何ができるか）。画面や通信に触れない計算だけを置いて、EditMode テストで確かめる。
    /// 画面のコードは、迷ったら、ここを見て決める。
    /// </summary>
    public static class FlowRules
    {
        /// <summary>画面に対応する場面（StageContext.Mode）。D の ScreenModeMap と同じ決まり。</summary>
        public static AppMode ModeFor(AppScreenId screen)
        {
            switch (screen)
            {
                case AppScreenId.Capture:
                case AppScreenId.Generating:
                    return AppMode.Scan;
                case AppScreenId.Check:
                    return AppMode.Check;
                case AppScreenId.Play:
                case AppScreenId.Clear:
                case AppScreenId.GameOver:
                    return AppMode.Play;
                default:
                    return AppMode.Menu;
            }
        }

        /// <summary>撮影の画面の状態。写真の枚数と、スキャンの状態から決める。</summary>
        public static CaptureStep CaptureStepFor(bool hasTop, bool hasSide, ScanStatus status)
        {
            if (status == ScanStatus.Lost)
            {
                return CaptureStep.Failed;
            }
            if (hasSide)
            {
                return CaptureStep.Sending;
            }
            if (status == ScanStatus.NotStarted || status == ScanStatus.Starting)
            {
                return CaptureStep.Preparing;
            }
            return hasTop ? CaptureStep.Side : CaptureStep.Top;
        }

        /// <summary>
        /// 「あそぶ」を押せない理由（文言）。押せるなら null。
        /// スタートとゴールを置いていて、箱が 1 つ以上あり、ステージが組み立っているときだけ押せる。
        /// </summary>
        public static string CannotPlayReason(bool startPlaced, bool goalPlaced, int boxCount, bool stageReady)
        {
            if (boxCount <= 0 || !stageReady)
            {
                return AppText.ErrorNoBoxes;
            }
            if (!startPlaced || !goalPlaced)
            {
                return AppText.CheckNeedCourse;
            }
            return null;
        }

        /// <summary>撮影の答えが届いたとき、たしかめへ進んでよいか（箱が 1 つ以上できているときだけ）。</summary>
        public static bool CanEnterCheck(int boxCount, bool stageReady) => boxCount > 0 && stageReady;

        /// <summary>失敗の原因を決める。</summary>
        /// <param name="cameraDenied">カメラが許可されていない。</param>
        /// <param name="photosSent">2枚の写真をサーバーに送ったあとか。</param>
        /// <param name="message">ScanClient の最後のメッセージ（原因のヒント）。</param>
        public static FailureKind ClassifyFailure(bool cameraDenied, bool photosSent, string message)
        {
            if (cameraDenied && !photosSent)
            {
                return FailureKind.CameraDenied;
            }
            if (!photosSent)
            {
                return FailureKind.Unknown;
            }
            var m = message ?? "";
            if (m.Contains("時間") || m.ToLowerInvariant().Contains("timeout"))
            {
                return FailureKind.Timeout;
            }
            if (m.Contains("つながら") || m.Contains("通信") || m.ToLowerInvariant().Contains("connect") || m.ToLowerInvariant().Contains("network"))
            {
                return FailureKind.Network;
            }
            return FailureKind.Server;
        }

        /// <summary>失敗の原因に合う、子ども向けの文。</summary>
        public static string FailureText(FailureKind kind)
        {
            switch (kind)
            {
                case FailureKind.CameraDenied: return AppText.ErrorCameraDenied;
                case FailureKind.Network: return AppText.ErrorNetwork;
                case FailureKind.Timeout: return AppText.ErrorTimeout;
                case FailureKind.NoBoxes: return AppText.ErrorNoBoxes;
                case FailureKind.Server: return AppText.ErrorServer;
                default: return AppText.ErrorServer;
            }
        }

        /// <summary>タイムを「分:秒.十分の一秒」にする。例：83.4 → 1:23.4</summary>
        public static string FormatTime(float seconds)
        {
            if (seconds < 0f || float.IsNaN(seconds)) seconds = 0f;
            var tenths = (int)(seconds * 10f + 0.001f);
            var minutes = tenths / 600;
            var rest = tenths % 600;
            return $"{minutes}:{rest / 10:00}.{rest % 10}";
        }

        /// <summary>保存するときの、ステージの名前（「ステージ 3」のように、いまの数 + 1）。</summary>
        public static string DefaultStageName(int savedCount) => $"ステージ {savedCount + 1}";
    }
}
