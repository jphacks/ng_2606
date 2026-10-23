using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>
    /// 箱づくりと読み取りの補助で調整する数値（B_scan_data.md の Step 3）。
    /// 初期値は手順書の値。実機で試しながら Inspector で変える。
    /// </summary>
    [CreateAssetMenu(fileName = "BoxSettings", menuName = "DeskStage/B/箱の設定")]
    public class BoxSettings : ScriptableObject
    {
        [Header("箱づくり")]

        [Tooltip("隣り合う箱の上面の差がこれ未満なら、高い方にそろえる（m）")]
        [Min(0f)] public float heightMergeThreshold = 0.008f;

        [Tooltip("箱と箱のすき間がこれ未満なら、片方を広げてなくす（m）。キャラの太さ")]
        [Min(0f)] public float gapCloseThreshold = 0.016f;

        [Tooltip("これより低い上面は箱にしない（m）")]
        [Min(0f)] public float minBoxHeight = 0.005f;

        [Header("読み取りの補助")]

        [Tooltip("補助で足す箱の一辺（m）")]
        [Min(0f)] public float addBoxSize = 0.05f;

        [Tooltip("動かす・大きさを変えるときにそろえる単位（m）")]
        [Min(0f)] public float moveResizeStep = 0.005f;

        [Tooltip("指を離したとき、この角度ごとに吸い付ける（度）")]
        [Min(0f)] public float rotateSnapStep = 15f;

        [Tooltip("吸い付けの角度からこの差以内なら吸い付ける（度）")]
        [Min(0f)] public float rotateSnapTolerance = 5f;

        [Tooltip("補助で変えられる箱の高さの下限（m）")]
        [Min(0f)] public float assistMinHeight = 0.005f;

        [Tooltip("補助で変えられる箱の高さの上限（m）")]
        [Min(0f)] public float assistMaxHeight = 0.40f;

        [Tooltip("補助で変えられる箱の幅・奥行きの下限（m）")]
        [Min(0f)] public float assistMinWidth = 0.01f;

        [Tooltip("補助で変えられる箱の幅・奥行きの上限（m）")]
        [Min(0f)] public float assistMaxWidth = 0.60f;

        [Tooltip("もどすでさかのぼれる回数")]
        [Min(0)] public int undoLimit = 20;

        [Header("机の板")]

        [Tooltip("机の板の厚さ（m）。上面が高さ0になるように置く")]
        [Min(0f)] public float deskThickness = 0.01f;
    }
}
