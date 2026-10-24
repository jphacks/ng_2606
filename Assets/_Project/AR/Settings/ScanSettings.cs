using UnityEngine;

namespace DeskStage.AR
{
    /// <summary>
    /// スキャンの調整値（A_ar.md Step 2）。
    /// 仮の値なので、P0 の検証の結果で変える。コードに直接書かず、ここを見る。
    /// </summary>
    [CreateAssetMenu(fileName = "ScanSettings", menuName = "DeskStage/Scan Settings")]
    public class ScanSettings : ScriptableObject
    {
        [Header("面を捨てる条件")]
        [Tooltip("机の長方形より外にある面を捨てる距離（m）")]
        public float outsideDeskMargin = 0.05f;

        [Tooltip("これより低い面は机の一部として捨てる（m）。C の「歩いて登れる段差」と同じ値")]
        public float deskSurfaceHeight = 0.008f;

        [Tooltip("これより高い面は捨てる（m）。棚やモニターの上など")]
        public float maxSurfaceHeight = 0.40f;

        [Tooltip("これより小さい面は捨てる（㎡）。16cm² = 4cm×4cm")]
        public float minSurfaceArea = 0.0016f;

        [Header("面を整える")]
        [Tooltip("高さの値を新しい値へ寄せる速さ（秒）")]
        public float heightSmoothingTime = 1.0f;

        [Tooltip("同じ物の上面とみなす高さの差（m）")]
        public float mergeHeightTolerance = 0.01f;

        [Tooltip("同じ物の上面とみなすすき間（m）")]
        public float mergeGapTolerance = 0.01f;

        [Tooltip("ARKit の面が実物より小さい分を補う量（m）")]
        public float edgeExpansion = 0.005f;

        [Tooltip("形が大きく変わらなくなってから一覧に出すまでの時間（秒）")]
        public float settleTime = 1.0f;

        [Tooltip("「形が大きく変わった」とみなす割合。面積の変化、または重心の移動（面の大きさに対する割合）がこれを超えたら、安定した時間を数え直す")]
        public float settleChangeRatio = 0.1f;

        [Header("知らせ")]
        [Tooltip("SurfacesChanged の最小間隔（秒）。1秒に4回まで")]
        public float notifyInterval = 0.25f;

        [Tooltip("位置の再推定がこれ以上続いたら Lost とする（秒）")]
        public float relocalizingTimeout = 3.0f;
    }
}
