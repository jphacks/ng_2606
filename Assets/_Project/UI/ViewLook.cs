using UnityEngine;

namespace DeskStage.UI
{
    /// <summary>
    /// 見た目の素材の組と、遊ぶ画面の背景・光源の設定（Step 7）。
    /// メニュー「DeskStage/D/見た目の素材を作る」で Rendering/ に作る。
    /// </summary>
    [CreateAssetMenu(menuName = "DeskStage/D/ViewLook", fileName = "ViewLook")]
    public class ViewLook : ScriptableObject
    {
        [Header("素材（D_ui.md Step 7 の表）")]
        [Tooltip("A の上面の表示。明るい水色、半透明 0.35")]
        public Material ScanSurface;
        [Tooltip("スキャン中の机の板。枠の線だけ")]
        public Material DeskOutline;
        [Tooltip("スキャン中・たしかめ画面の箱。半透明 0.6")]
        public Material BoxScan;
        [Tooltip("遊ぶ画面の箱。色は箱ごとに BoxPalette から選ぶ")]
        public Material BoxPlay;
        [Tooltip("遊ぶ画面の机の板。木目のような落ち着いた色、縁を少し濃く")]
        public Material DeskPlay;
        [Tooltip("補助で選んだ箱。太い黄色のふち")]
        public Material BoxSelected;
        [Tooltip("机が決まったときに、机のふちをなぞる光の線（Step 8）")]
        public Material ScanLine;

        [Header("遊ぶ画面の箱の色（隣どうしが同じ色にならないように順に使う）")]
        public Color[] BoxPalette =
        {
            new Color(1.00f, 0.55f, 0.45f),
            new Color(1.00f, 0.82f, 0.35f),
            new Color(0.50f, 0.80f, 0.45f),
            new Color(0.40f, 0.70f, 0.95f),
            new Color(0.75f, 0.55f, 0.90f),
        };

        [Header("遊ぶ画面")]
        [Tooltip("背景の色（【未決】カメラ映像にすると決まったら変える）")]
        public Color PlayBackground = new Color(0.85f, 0.93f, 1.00f);

        [Tooltip("箱が隣どうしとみなす距離（m）")]
        public float NeighborMargin = 0.01f;

        /// <summary>場面に合う机の素材。</summary>
        public Material DeskFor(Shared.AppMode mode) => mode == Shared.AppMode.Play ? DeskPlay : DeskOutline;

        /// <summary>場面に合う箱の素材。</summary>
        public Material BoxFor(Shared.AppMode mode) => mode == Shared.AppMode.Play ? BoxPlay : BoxScan;
    }
}
