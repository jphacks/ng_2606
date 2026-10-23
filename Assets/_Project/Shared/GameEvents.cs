using System;

namespace DeskStage.Shared
{
    /// <summary>コースの物を置こうとした結果（00_overview.md 4.5）。</summary>
    public enum PlacementResult
    {
        /// <summary>置けた。</summary>
        Placed,
        /// <summary>置けない（箱の側面など）。</summary>
        Rejected,
        /// <summary>置けたが、キャラが届かないかもしれない。</summary>
        MayBeUnreachable,
    }

    /// <summary>
    /// C が出す知らせ（00_overview.md 4.5）。
    /// </summary>
    public static class GameEvents
    {
        /// <summary>キャラがスタートに出た。</summary>
        public static event Action GameStarted;

        /// <summary>チェックポイントに初めて触れた。何番目か。</summary>
        public static event Action<int> CheckpointPassed;

        /// <summary>ゴールに触れた。タイム（秒）と、最速記録かどうか。</summary>
        public static event Action<float, bool> Goal;

        /// <summary>落下と判定した。</summary>
        public static event Action Fell;

        /// <summary>プレイ中の経過時間（秒）。毎フレーム。</summary>
        public static event Action<float> ElapsedTime;

        /// <summary>コースの物を置こうとした。置いた物、結果、理由。</summary>
        public static event Action<CourseItem, PlacementResult, string> CoursePlaced;

        public static void RaiseGameStarted() => GameStarted?.Invoke();

        public static void RaiseCheckpointPassed(int index) => CheckpointPassed?.Invoke(index);

        public static void RaiseGoal(float seconds, bool isBest) => Goal?.Invoke(seconds, isBest);

        public static void RaiseFell() => Fell?.Invoke();

        public static void RaiseElapsedTime(float seconds) => ElapsedTime?.Invoke(seconds);

        public static void RaiseCoursePlaced(CourseItem item, PlacementResult result, string reason = "")
            => CoursePlaced?.Invoke(item, result, reason);
    }
}
