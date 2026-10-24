using UnityEngine;

namespace DeskStage.UI
{
    /// <summary>
    /// 視点の操作（ViewGesture）の調整値（Step 4）。仮の値。実機と子どもテストで直す。
    /// </summary>
    [CreateAssetMenu(menuName = "DeskStage/D/ViewGestureSettings", fileName = "ViewGestureSettings")]
    public class ViewGestureSettings : ScriptableObject
    {
        [Tooltip("画面の高さぶん指をなぞったときに回る角度（度）")]
        public float DegreesPerScreenHeight = 180f;

        [Tooltip("2本指の間隔が画面の高さぶん広がったときに近づく距離（m）")]
        public float ZoomMetersPerScreenHeight = 0.6f;

        [Tooltip("PC のマウスホイール1目盛りで近づく距離（m）。開発用")]
        public float ZoomMetersPerScrollStep = 0.03f;

        [Tooltip("縦のなぞりを逆にする（上になぞると見下ろす）")]
        public bool InvertVertical;
    }
}
