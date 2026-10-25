using DeskStage.AR;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.App
{
    /// <summary>
    /// 全体の流れの係。画面の「次へ」「戻る」「失敗」と、他の担当の部品（A の撮影、B の保存、C のゲーム）への依頼は、ここで決める。
    /// 画面は、自分の見た目だけを持ち、次に何をするかは、ここを呼ぶ。流れの決まりは FlowRules（EditMode テストで確かめる）。
    ///
    ///  ・新しく作る：遊び方 → 撮影（2 枚）→ つくっている → たしかめ → あそぶ → クリア
    ///  ・保存したステージ：いちらん →（読み込み）→ あそぶ → クリア → いちらん
    ///  ・失敗は、必ず撮影の画面に戻り、原因に合う文と「もういちど」を出す
    ///  ・画面から出るとき、撮影を閉じる、ゲームを片付ける、を忘れない（ScreenChanged でまとめて行う）
    /// </summary>
    public class AppFlow : MonoBehaviour
    {
        // サーバーの答えが届いてから、ステージが組み立つまで待つ上限（秒）
        const float BuildWaitSeconds = 12f;
        // 保存したステージを読み込んで組み立てるまで待つ上限（秒）
        const float LoadWaitSeconds = 8f;

        AppContext context;
        PhotoCapture photo;
        ScanClient client;

        bool scanStartedOnce;
        bool photosSent;
        bool stageBuilt;
        bool waitingBuilt;
        float buildDeadline;
        bool startPlacedByUser;
        bool saving;

        string pendingPlayId;
        float loadDeadline;

        AppScreenId previousScreen = AppScreenId.Title;

        /// <summary>いま、撮った（まだ保存していない）ステージがあるか。</summary>
        public bool HasNewStage { get; private set; }

        /// <summary>撮ったステージを、保存したか。</summary>
        public bool NewStageSaved { get; private set; }

        /// <summary>いま遊んでいるのが、保存したステージ（いちらんから選んだ物）か。</summary>
        public bool PlayingSaved { get; private set; }

        public void Init(AppContext ctx)
        {
            context = ctx;
            context.Router.ScreenChanged += OnScreenChanged;
            StageContext.ScanStatusChanged += OnScanStatusChanged;
            StageContext.StageBuilt += OnStageBuilt;
            StageContext.SaveCompleted += OnSaveCompleted;
            StageContext.ThermalStateChanged += OnThermal;
            GameEvents.Goal += OnGoal;
            GameEvents.CoursePlaced += OnCoursePlaced;
            context.Gimmicks.GameOver += OnGameOver;
        }

        void OnDestroy()
        {
            if (context == null) return;
            if (context.Router != null) context.Router.ScreenChanged -= OnScreenChanged;
            StageContext.ScanStatusChanged -= OnScanStatusChanged;
            StageContext.StageBuilt -= OnStageBuilt;
            StageContext.SaveCompleted -= OnSaveCompleted;
            StageContext.ThermalStateChanged -= OnThermal;
            GameEvents.Goal -= OnGoal;
            GameEvents.CoursePlaced -= OnCoursePlaced;
            if (context.Gimmicks != null) context.Gimmicks.GameOver -= OnGameOver;
        }

        PhotoCapture Photo
        {
            get
            {
                if (photo == null) photo = FindFirstObjectByType<PhotoCapture>();
                return photo;
            }
        }

        ScanClient Client
        {
            get
            {
                if (client == null) client = FindFirstObjectByType<ScanClient>();
                return client;
            }
        }

        // ---- いちらん ----

        public void OpenCharacters() => context.Router.Show(AppScreenId.Characters);

        public void OpenBackdrop() => context.Router.Show(AppScreenId.Backdrop);

        public void StartNewStage() => context.Router.Show(AppScreenId.HowTo);

        /// <summary>保存したステージを開いて、あそぶ。読み込みが終わるまで「待ち」を出す。</summary>
        public void PlayStage(string id)
        {
            if (string.IsNullOrEmpty(id) || context.Overlays.IsBusy)
            {
                return;
            }
            pendingPlayId = id;
            stageBuilt = false;
            context.Gimmicks.UseWorkingBoxes = false; // 保存したステージは、保存されたしかけを使う
            loadDeadline = Time.realtimeSinceStartup + LoadWaitSeconds;
            context.Overlays.Busy(AppText.HomeLoading);
            UiRequests.RequestSelectStage(id);
        }

        public void DeleteStage(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            UiRequests.RequestDeleteStage(id);
            context.Overlays.Toast(AppText.HomeDeleted, AppOverlays.ToastKind.Info);
        }

        // ---- 撮影 ----

        /// <summary>撮影を始める（遊び方の「とりはじめる」、失敗のあとの「もういちど」、たしかめの「もういちど とる」）。</summary>
        public void BeginCapture(bool push)
        {
            photosSent = false;
            stageBuilt = false;
            waitingBuilt = false;
            startPlacedByUser = false;
            HasNewStage = false;
            NewStageSaved = false;
            PlayingSaved = false;
            if (!scanStartedOnce)
            {
                scanStartedOnce = true;
                UiRequests.RequestStartScan();
            }
            else
            {
                UiRequests.RequestRescan(); // 前のステージと写真を片付けて、カメラを開き直す
            }
            context.Router.Show(AppScreenId.Capture, null, push);
        }

        public void TakePhoto() => UiRequests.RequestTakePhoto();

        /// <summary>2 枚目を撮り終えて、写真を送り始めた。</summary>
        public void PhotosSent()
        {
            photosSent = true;
            context.Router.Show(AppScreenId.Generating, null, false);
        }

        /// <summary>つくっている途中で、やめる。撮影からやり直す。</summary>
        public void CancelGeneration()
        {
            BeginCapture(false);
        }

        /// <summary>撮影・たしかめから、いちらんへ戻る。撮影は閉じる（ScreenChanged で行う）。</summary>
        public void LeaveToHome()
        {
            waitingBuilt = false;
            context.Router.Show(AppScreenId.Home);
        }

        // ---- たしかめ ----

        public int BoxCount
        {
            get
            {
                var working = ScanContext.WorkingBoxes.Count;
                var stage = StageContext.CurrentStage;
                var saved = stage != null && stage.boxes != null ? stage.boxes.Length : 0;
                return Mathf.Max(working, saved);
            }
        }

        public bool StageReady => stageBuilt;

        public bool GoalPlaced => StageContext.CurrentStage != null && StageContext.CurrentStage.goal.radius > 0f;

        public bool StartPlaced
        {
            get
            {
                if (startPlacedByUser) return true;
                var stage = StageContext.CurrentStage;
                return stage != null && Vec3.To(stage.spawn.position).sqrMagnitude > 1e-6f;
            }
        }

        public void SaveStage()
        {
            if (saving || NewStageSaved) return;
            var count = 0;
            foreach (var s in StageContext.StageList)
            {
                if (!s.id.StartsWith("sample")) count++;
            }
            saving = true;
            UiRequests.RequestSaveStage(FlowRules.DefaultStageName(count));
        }

        /// <summary>たしかめの「あそぶ」。押せない理由があれば、ひとこと出して、進まない。</summary>
        public void PlayFromCheck()
        {
            var reason = FlowRules.CannotPlayReason(StartPlaced, GoalPlaced, BoxCount, stageBuilt);
            if (reason != null)
            {
                UiSound.Play(UiSound.Cue.Error);
                context.Overlays.Toast(reason, AppOverlays.ToastKind.Bad);
                return;
            }
            PlayingSaved = false;
            context.Router.Show(AppScreenId.Play);
        }

        // ---- あそぶ・クリア ----

        public void SetPaused(bool paused) => UiRequests.RequestSetPaused(paused);

        public void RetryPlay() => UiRequests.RequestRetry();

        /// <summary>あそんだあと、いちらんへ。</summary>
        public void BackToList() => context.Router.Show(AppScreenId.Home);

        /// <summary>クリアのあとの「もういちど」。ゲームを片付けず、同じステージでやり直す。</summary>
        public void PlayAgain()
        {
            UiRequests.RequestRetry();
            context.Router.Show(AppScreenId.Play, null, false);
        }

        // ---- 外の出来事 ----

        void Update()
        {
            if (waitingBuilt && Time.realtimeSinceStartup > buildDeadline)
            {
                Fail(FailureKind.Server);
            }
            if (pendingPlayId != null && Time.realtimeSinceStartup > loadDeadline)
            {
                pendingPlayId = null;
                context.Overlays.HideBusy();
                UiSound.Play(UiSound.Cue.Error);
                context.Overlays.Toast(AppText.PlayLoadFailed, AppOverlays.ToastKind.Bad);
            }
        }

        void OnScanStatusChanged(ScanStatus status, ScanReason reason)
        {
            var current = context.Router.Current;
            var inScan = current == AppScreenId.Capture || current == AppScreenId.Generating;
            if (!inScan)
            {
                return;
            }
            if (status == ScanStatus.Lost)
            {
                var denied = Photo != null && !photosSent && !Application.HasUserAuthorization(UserAuthorization.WebCam);
                var message = Client != null ? Client.LastMessage : "";
                Fail(FlowRules.ClassifyFailure(denied, photosSent, message));
            }
            else if (status == ScanStatus.Finished && current == AppScreenId.Generating)
            {
                UiRequests.RequestFinishScan();
                waitingBuilt = true;
                buildDeadline = Time.realtimeSinceStartup + BuildWaitSeconds;
                TryEnterCheck();
            }
        }

        void OnStageBuilt()
        {
            stageBuilt = true;
            if (waitingBuilt)
            {
                TryEnterCheck();
            }
            if (pendingPlayId != null)
            {
                pendingPlayId = null;
                context.Overlays.HideBusy();
                PlayingSaved = true;
                context.Router.Show(AppScreenId.Play);
            }
        }

        void TryEnterCheck()
        {
            if (!stageBuilt || context.Router.Current != AppScreenId.Generating)
            {
                return;
            }
            waitingBuilt = false;
            if (FlowRules.CanEnterCheck(BoxCount, stageBuilt))
            {
                HasNewStage = true;
                context.Gimmicks.UseWorkingBoxes = true; // 撮ったばかりのステージは、直したあとの箱を見る
                UiSound.Play(UiSound.Cue.Success);
                context.Router.Show(AppScreenId.Check, null, false);
            }
            else
            {
                Fail(FailureKind.NoBoxes);
            }
        }

        void Fail(FailureKind kind)
        {
            waitingBuilt = false;
            Debug.LogWarning($"[App] 失敗：{kind}");
            UiSound.Play(UiSound.Cue.Error);
            context.Router.Show(AppScreenId.Capture, kind, false);
        }

        void OnSaveCompleted(bool ok, string reason)
        {
            saving = false;
            if (ok)
            {
                NewStageSaved = true;
                UiSound.Play(UiSound.Cue.Success);
                context.Overlays.Toast(AppText.CheckSaveOk, AppOverlays.ToastKind.Good);
            }
            else
            {
                UiSound.Play(UiSound.Cue.Error);
                context.Overlays.Toast(AppText.CheckSaveFailed, AppOverlays.ToastKind.Bad);
            }
        }

        void OnCoursePlaced(CourseItem item, PlacementResult result, string reason)
        {
            if (item == CourseItem.Start && result != PlacementResult.Rejected)
            {
                startPlacedByUser = true;
            }
        }

        void OnGoal(float seconds, bool isBest)
        {
            if (context.Router.Current == AppScreenId.Play)
            {
                context.Router.Show(AppScreenId.Clear, new ClearInfo { Seconds = seconds, IsBest = isBest }, false);
            }
        }

        // ハートが 0 になった：ゲームを止めて、ゲームオーバーの画面へ
        void OnGameOver()
        {
            if (context.Router.Current != AppScreenId.Play)
            {
                return;
            }
            UiRequests.RequestSetPaused(true);
            UiSound.Play(UiSound.Cue.Error);
            context.Router.Show(AppScreenId.GameOver, null, false);
        }

        void OnThermal(ThermalState state)
        {
            if (state >= ThermalState.Serious && context.Router.Current == AppScreenId.Play)
            {
                context.Overlays.Toast(AppText.PlayHot, AppOverlays.ToastKind.Bad, 4f);
            }
        }

        static bool InGame(AppScreenId id) => id == AppScreenId.Play || id == AppScreenId.Clear || id == AppScreenId.GameOver;

        static bool InScan(AppScreenId id) => id == AppScreenId.Capture || id == AppScreenId.Generating;

        // 画面が切り替わるときの、後片付けと準備（どの経路でも、ここを通る）
        void OnScreenChanged(AppScreenId id)
        {
            if (id == AppScreenId.Play && !InGame(previousScreen))
            {
                UiRequests.RequestPlay();
            }
            if (InGame(previousScreen) && !InGame(id))
            {
                UiRequests.RequestBackToList();
            }
            if (InScan(previousScreen) && !InScan(id) && id != AppScreenId.Check && Photo != null)
            {
                Photo.StopScan();
            }
            // 撮影、つくっている、あそぶの間は、画面を暗くしない
            Screen.sleepTimeout = InScan(id) || id == AppScreenId.Play ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
            previousScreen = id;
        }
    }

    /// <summary>クリアの画面へ渡す、結果。</summary>
    public class ClearInfo
    {
        public float Seconds;
        public bool IsBest;
    }
}
