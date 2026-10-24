using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeskStage.Shared
{
    /// <summary>
    /// 整えた上面1件（00_overview.md 4.4）。A が作り、B が箱にする。
    /// </summary>
    public class SurfaceData
    {
        /// <summary>A が付ける番号。まとめた面は新しい番号にする。</summary>
        public int Id;

        /// <summary>机の天面からの高さ（m、1mm単位）。</summary>
        public float Height;

        /// <summary>上から見た境界の点の並び（ステージ座標の XZ）。</summary>
        public Vector2[] Outline = Array.Empty<Vector2>();

        /// <summary>最後に変わった時刻（Time.realtimeSinceStartup）。</summary>
        public float UpdatedAt;
    }

    /// <summary>箱に何が起きたか。</summary>
    public enum BoxChange
    {
        Added,
        Modified,
        Removed,
    }

    /// <summary>
    /// スキャン中の受け渡し（00_overview.md 4.4）。
    /// A（面）→ B（箱）→ D（見せ方）の順に流れる。保存するまで箱はここにある。
    /// </summary>
    public static class ScanContext
    {
        /// <summary>机の長方形の幅と奥行き（m）。書くのは A。</summary>
        public static Vector2 DeskSize { get; set; }

        /// <summary>整えた上面の一覧。書くのは A。写真の方式（00_overview.md 9章）では使わない。</summary>
        public static List<SurfaceData> Surfaces { get; } = new List<SurfaceData>();

        /// <summary>作業中の箱の一覧。書くのは B。</summary>
        public static List<BoxData> WorkingBoxes { get; } = new List<BoxData>();

        /// <summary>補助で選んでいる箱の id。なければ空。書くのは B。</summary>
        public static string SelectedBoxId { get; set; } = "";

        /// <summary>撮影中のカメラの映像。書くのは A、D の撮影画面が映す（00_overview.md 9.3）。カメラを閉じたら null。</summary>
        public static Texture CameraPreview { get; set; }

        /// <summary>写真を読んだ Claude の答え（JSON の文字列、Docs/LlmScanFormat.md の形）が届いた（A → B）。</summary>
        public static event Action<string> ScanResultReceived;

        public static void RaiseScanResultReceived(string json) => ScanResultReceived?.Invoke(json);

        /// <summary>上面が増えた・変わった・消えた（A → B、D）。1秒に4回まで。写真の方式では使わない。</summary>
        public static event Action SurfacesChanged;

        /// <summary>箱が変わった（B → D）。箱の id、何が起きたか、auto か manual か。</summary>
        public static event Action<string, BoxChange, string> BoxesChanged;

        public static void RaiseSurfacesChanged() => SurfacesChanged?.Invoke();

        public static void RaiseBoxesChanged(string boxId, BoxChange change, string source)
            => BoxesChanged?.Invoke(boxId, change, source);

        /// <summary>もういちどスキャンのときに、面と箱の記録を消す。</summary>
        public static void Clear()
        {
            DeskSize = Vector2.zero;
            Surfaces.Clear();
            WorkingBoxes.Clear();
            SelectedBoxId = "";
        }
    }
}
