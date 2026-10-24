using System;

namespace DeskStage.Shared
{
    /// <summary>
    /// ステージ一覧の1件（00_overview.md 4.3）。B が作り、D のステージ一覧の画面が表示する。
    /// </summary>
    [Serializable]
    public class StageSummary
    {
        /// <summary>ステージID（フォルダ名と同じ）。SelectStage と DeleteStage に渡す。</summary>
        public string id = "";

        /// <summary>表示名。</summary>
        public string name = "";

        /// <summary>作った日時（ISO 8601）。一覧は新しい順。</summary>
        public string createdAt = "";

        /// <summary>読み込めるか。false なら D は灰色にして選べないようにする。</summary>
        public bool loadable;

        /// <summary>読み込めない理由（例：古い形式）。読み込めるなら空。</summary>
        public string reason = "";
    }
}
