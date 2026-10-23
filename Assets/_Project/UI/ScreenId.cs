using DeskStage.Shared;

namespace DeskStage.UI
{
    /// <summary>7つの画面（D_ui.md「7つの画面」）。</summary>
    public enum ScreenId
    {
        Title,
        List,
        Tips,
        Scan,
        Check,
        Play,
        Clear,
    }

    /// <summary>画面と StageContext.Mode の対応。計算だけの普通のクラス。</summary>
    public static class ScreenModeMap
    {
        /// <summary>
        /// 画面に対応する場面を返す。
        /// クリア画面はステージを組み立てたままなので Play のまま（Menu に戻すのは一覧に戻るとき）。
        /// </summary>
        public static AppMode ToMode(ScreenId id)
        {
            switch (id)
            {
                case ScreenId.Scan: return AppMode.Scan;
                case ScreenId.Check: return AppMode.Check;
                case ScreenId.Play:
                case ScreenId.Clear: return AppMode.Play;
                default: return AppMode.Menu;
            }
        }
    }
}
