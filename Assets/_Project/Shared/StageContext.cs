using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeskStage.Shared
{
    /// <summary>スキャンの状態（00_overview.md 4.3）。</summary>
    public enum ScanStatus
    {
        /// <summary>AR を使っていない（3D画面で遊んでいる、など）。</summary>
        NotStarted,
        /// <summary>AR の準備中。</summary>
        Starting,
        /// <summary>机の天面を探している。</summary>
        FindingDesk,
        /// <summary>机が決まり、物の上面を探している。</summary>
        Scanning,
        /// <summary>AR が不安定。理由は ScanReason。</summary>
        Limited,
        /// <summary>位置を見失った。</summary>
        Lost,
        /// <summary>スキャンを終えた。</summary>
        Finished,
    }

    /// <summary>Limited になった理由。</summary>
    public enum ScanReason
    {
        None,
        /// <summary>暗い。</summary>
        Dark,
        /// <summary>動きが速すぎる。</summary>
        FastMotion,
        /// <summary>模様が少ない。</summary>
        FewFeatures,
    }

    /// <summary>今の場面。</summary>
    public enum AppMode
    {
        Menu,
        Scan,
        Check,
        Play,
    }

    /// <summary>端末の発熱状態（iOS の4段階）。</summary>
    public enum ThermalState
    {
        Nominal,
        Fair,
        Serious,
        Critical,
    }

    /// <summary>
    /// 共通の掲示板（00_overview.md 4.3）。
    /// 4人の部品はここを通してやり取りする。直接相手を呼ばない。
    /// </summary>
    public static class StageContext
    {
        /// <summary>今のステージデータ。書くのは B。</summary>
        public static StageData CurrentStage { get; set; }

        /// <summary>ステージ座標の原点。書くのは A（スキャン中）と B（遊ぶとき）。</summary>
        public static Transform StageRoot { get; set; }

        /// <summary>今の場面。書くのは D。</summary>
        public static AppMode Mode { get; set; } = AppMode.Menu;

        static ScanStatus s_scanStatus = ScanStatus.NotStarted;
        static ScanReason s_scanReason = ScanReason.None;

        /// <summary>スキャンの状態。書くのは A。</summary>
        public static ScanStatus ScanStatus => s_scanStatus;

        /// <summary>Limited の理由。書くのは A。</summary>
        public static ScanReason ScanReason => s_scanReason;

        /// <summary>
        /// スキャンの状態を変える（A が呼ぶ）。
        /// 状態と理由の両方が同じなら ScanStatusChanged は出さない。
        /// </summary>
        public static void SetScanStatus(ScanStatus status, ScanReason reason = ScanReason.None)
        {
            if (s_scanStatus == status && s_scanReason == reason)
            {
                return;
            }
            s_scanStatus = status;
            s_scanReason = reason;
            ScanStatusChanged?.Invoke(status, reason);
        }

        /// <summary>stage.json を読み終えた（B → 全員）。</summary>
        public static event Action<StageData> StageLoaded;

        /// <summary>スキャンの状態が変わった（A → B、D）。</summary>
        public static event Action<ScanStatus, ScanReason> ScanStatusChanged;

        /// <summary>机を決め、StageRoot を置いた（A → B、D）。</summary>
        public static event Action DeskDecided;

        /// <summary>机の板と箱のオブジェクトを作り終えた（B → C、D）。</summary>
        public static event Action StageBuilt;

        /// <summary>保存が終わった。成功したかと、失敗の理由（A → なし。B → D）。</summary>
        public static event Action<bool, string> SaveCompleted;

        /// <summary>端末の発熱状態が変わった（A → D）。</summary>
        public static event Action<ThermalState> ThermalStateChanged;

        static readonly List<StageSummary> s_stageList = new List<StageSummary>();

        /// <summary>端末に保存してあるステージの一覧（新しい順）。書くのは B、表示するのは D。</summary>
        public static IReadOnlyList<StageSummary> StageList => s_stageList;

        /// <summary>ステージ一覧が変わった（B → D）。起動したとき、保存したとき、消したとき。</summary>
        public static event Action StageListChanged;

        /// <summary>ステージ一覧を書き換えて、StageListChanged を出す（B が呼ぶ）。</summary>
        public static void SetStageList(IEnumerable<StageSummary> list)
        {
            s_stageList.Clear();
            s_stageList.AddRange(list);
            StageListChanged?.Invoke();
        }

        public static void RaiseStageLoaded(StageData stage)
        {
            CurrentStage = stage;
            StageLoaded?.Invoke(stage);
        }

        public static void RaiseDeskDecided() => DeskDecided?.Invoke();

        public static void RaiseStageBuilt() => StageBuilt?.Invoke();

        public static void RaiseSaveCompleted(bool ok, string reason = "") => SaveCompleted?.Invoke(ok, reason);

        public static void RaiseThermalStateChanged(ThermalState state) => ThermalStateChanged?.Invoke(state);
    }
}
