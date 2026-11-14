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
        Mode,
        Mission,
        GimmickGuide,
    }

    /// <summary>いま遊んでいるステージの出どころ（Docs/specs/home-modes）。いちらんへ戻る先と、「なおす」「ほぞん」を出すかが変わる。</summary>
    public enum StageSource
    {
        Own,
        Mission,
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
        /// <summary>
        /// BGM（AppMusic）を流す画面か（2026-11-12）。メニューの画面（スタート、どれで あそぶ？、いちらん、撮り方の説明、キャラ、背景など）を
        /// 行き来している間は流し続け、写真を撮る・ステージを作る・たしかめ・あそぶ・クリア・ゲームオーバーでは止める。
        /// </summary>
        public static bool PlaysMusic(AppScreenId screen) => ModeFor(screen) == AppMode.Menu;

        /// <summary>遊んでいるときの BGM を流す画面か（2026-11-15）。遊ぶ画面だけ（クリア・ゲームオーバーでは止めて、その音を鳴らす）。</summary>
        public static bool PlaysGameMusic(AppScreenId screen) => screen == AppScreenId.Play;

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

        /// <summary>
        /// 前の記録から、どれだけ速くなったか（秒。例：25.5 → 23.4 は「2.1」）。FormatTime と同じく十分の一秒で切った値どうしの差にして、
        /// 画面に並ぶ 2 つのタイムと、差が食い違わないようにする。十分の一秒で同じに見えるときは、百分の一秒で書く（「0.04」）。
        /// </summary>
        public static string FormatGain(float previous, float now)
        {
            var tenths = (int)(previous * 10f + 0.001f) - (int)(now * 10f + 0.001f);
            if (tenths > 0) return $"{tenths / 10}.{tenths % 10}";
            var hundredths = System.Math.Max(1, (int)System.Math.Round((previous - now) * 100f));
            return $"0.{hundredths:00}";
        }

        /// <summary>保存するときの、ステージの名前（「ステージ 3」のように、いまの数 + 1）。</summary>
        public static string DefaultStageName(int savedCount) => $"ステージ {savedCount + 1}";
    }
}
