using System;
using UnityEngine;

namespace DeskStage.Shared
{
    /// <summary>たしかめ画面で置くコースの物（00_overview.md 4.6）。</summary>
    public enum CourseItem
    {
        Start,
        Goal,
        Checkpoint,
    }

    /// <summary>
    /// D が出す依頼（00_overview.md 4.6）。
    /// 依頼ごとに「出す関数」と「受けるイベント」を組にしてある。
    /// D は出す関数を呼び、受ける側（A・B・C）はイベントを購読する。
    /// </summary>
    public static class UiRequests
    {
        // --- ステージ（→ B） ---

        /// <summary>ステージを選んだ。ステージID。</summary>
        public static event Action<string> SelectStage;
        public static void RequestSelectStage(string stageId) => SelectStage?.Invoke(stageId);

        /// <summary>ステージを保存する。表示名。</summary>
        public static event Action<string> SaveStage;
        public static void RequestSaveStage(string displayName) => SaveStage?.Invoke(displayName);

        /// <summary>ステージ一覧へ戻る（→ B、C）。</summary>
        public static event Action BackToList;
        public static void RequestBackToList() => BackToList?.Invoke();

        // --- スキャン（→ A、B） ---

        /// <summary>スキャンをはじめる（→ A）。</summary>
        public static event Action StartScan;
        public static void RequestStartScan() => StartScan?.Invoke();

        /// <summary>写真を1枚撮る（→ A）。1枚目は真上、2枚目は斜め（00_overview.md 9.2）。</summary>
        public static event Action TakePhoto;
        public static void RequestTakePhoto() => TakePhoto?.Invoke();

        /// <summary>机はここ（→ A）。画面のタップ位置。写真の方式（9章）では使わない。</summary>
        public static event Action<Vector2> SetDesk;
        public static void RequestSetDesk(Vector2 screenPosition) => SetDesk?.Invoke(screenPosition);

        /// <summary>スキャンを終える（→ A、B）。</summary>
        public static event Action FinishScan;
        public static void RequestFinishScan() => FinishScan?.Invoke();

        /// <summary>もういちどスキャン（→ A、B）。物を並べ直したとき。</summary>
        public static event Action Rescan;
        public static void RequestRescan() => Rescan?.Invoke();

        // --- 読み取りの補助（→ B） ---

        /// <summary>箱を足す。画面のタップ位置。</summary>
        public static event Action<Vector2> AddBox;
        public static void RequestAddBox(Vector2 screenPosition) => AddBox?.Invoke(screenPosition);

        /// <summary>箱を選ぶ。画面のタップ位置。何もない所なら選ぶのをやめる。</summary>
        public static event Action<Vector2> SelectBox;
        public static void RequestSelectBox(Vector2 screenPosition) => SelectBox?.Invoke(screenPosition);

        /// <summary>箱を動かす。机の面に沿った移動量（m、XZ）。</summary>
        public static event Action<Vector2> MoveBox;
        public static void RequestMoveBox(Vector2 deltaXZ) => MoveBox?.Invoke(deltaXZ);

        /// <summary>箱の大きさを変える。幅・高さ・奥行きの増減（m）。</summary>
        public static event Action<Vector3> ResizeBox;
        public static void RequestResizeBox(Vector3 delta) => ResizeBox?.Invoke(delta);

        /// <summary>箱を回す。角度（度）。</summary>
        public static event Action<float> RotateBox;
        public static void RequestRotateBox(float degrees) => RotateBox?.Invoke(degrees);

        /// <summary>箱を消す。</summary>
        public static event Action DeleteBox;
        public static void RequestDeleteBox() => DeleteBox?.Invoke();

        /// <summary>もどす。補助の操作を1つ取り消す。</summary>
        public static event Action UndoBox;
        public static void RequestUndoBox() => UndoBox?.Invoke();

        // --- コースと遊ぶ（→ C） ---

        /// <summary>コースの物を置く。置く物と、画面のタップ位置。</summary>
        public static event Action<CourseItem, Vector2> PlaceCourseItem;
        public static void RequestPlaceCourseItem(CourseItem item, Vector2 screenPosition)
            => PlaceCourseItem?.Invoke(item, screenPosition);

        /// <summary>遊ぶ。スタートにキャラを出して始める。</summary>
        public static event Action Play;
        public static void RequestPlay() => Play?.Invoke();

        /// <summary>視点を回す。横と縦の量。</summary>
        public static event Action<Vector2> OrbitView;
        public static void RequestOrbitView(Vector2 delta) => OrbitView?.Invoke(delta);

        /// <summary>視点を近づける。</summary>
        public static event Action<float> ZoomView;
        public static void RequestZoomView(float delta) => ZoomView?.Invoke(delta);

        /// <summary>一時停止・再開。</summary>
        public static event Action<bool> SetPaused;
        public static void RequestSetPaused(bool paused) => SetPaused?.Invoke(paused);

        /// <summary>もう一度。</summary>
        public static event Action Retry;
        public static void RequestRetry() => Retry?.Invoke();
    }
}
