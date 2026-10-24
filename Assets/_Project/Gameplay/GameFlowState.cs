namespace DeskStage.Gameplay
{
    /// <summary>ゲームの状態（C_gameplay.md の Step 11）。</summary>
    public enum GameState
    {
        /// <summary>ステージ待ち</summary>
        WaitingStage,
        /// <summary>準備（キャラを出してスタートへ移す）</summary>
        Preparing,
        /// <summary>プレイ中</summary>
        Playing,
        /// <summary>一時停止</summary>
        Paused,
        /// <summary>クリア</summary>
        Cleared,
    }

    /// <summary>
    /// ゲームの状態の切り替えの決まり（Step 11 の表）。切り替える前に「行けるか」を確かめ、
    /// 行けないときは false を返す（呼ぶ側は無視してログを出す）。MonoBehaviour から切り離して EditMode でテストする。
    /// </summary>
    public class GameFlowState
    {
        public GameState State { get; private set; } = GameState.WaitingStage;

        /// <summary>StageBuilt を受けて、遊べるステージがあるか</summary>
        public bool StageReady { get; private set; }

        /// <summary>B がステージを組み立て終えた（StageBuilt）。</summary>
        public void OnStageBuilt() => StageReady = true;

        /// <summary>ステージ待ち →（StageBuilt の後、Play）→ 準備</summary>
        public bool TryPlay()
        {
            if (State != GameState.WaitingStage || !StageReady) return false;
            State = GameState.Preparing;
            return true;
        }

        /// <summary>準備 →（すぐ）→ プレイ中</summary>
        public bool TryStart()
        {
            if (State != GameState.Preparing) return false;
            State = GameState.Playing;
            return true;
        }

        /// <summary>プレイ中 ⇄ 一時停止（SetPaused）</summary>
        public bool TrySetPaused(bool paused)
        {
            if (paused && State == GameState.Playing)
            {
                State = GameState.Paused;
                return true;
            }
            if (!paused && State == GameState.Paused)
            {
                State = GameState.Playing;
                return true;
            }
            return false;
        }

        /// <summary>プレイ中 →（ゴールに触れる）→ クリア</summary>
        public bool TryGoal()
        {
            if (State != GameState.Playing) return false;
            State = GameState.Cleared;
            return true;
        }

        /// <summary>
        /// クリア・プレイ中・一時停止 →（Retry）→ 準備。D の一時停止メニューにも「もういちど」があるため、プレイ中と一時停止中からも戻れる
        /// </summary>
        public bool TryRetry()
        {
            if (State != GameState.Cleared && State != GameState.Playing && State != GameState.Paused) return false;
            State = GameState.Preparing;
            return true;
        }

        /// <summary>どの状態でも →（BackToList）→ ステージ待ち。次は新しい StageBuilt を待つ</summary>
        public void BackToList()
        {
            State = GameState.WaitingStage;
            StageReady = false;
        }
    }
}
