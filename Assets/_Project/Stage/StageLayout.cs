using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>StageRoot から見た位置・回転・大きさ。</summary>
    public struct LocalPlacement
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    /// <summary>
    /// stage.json の値から、机の板と箱をどこに・どの大きさで置くかを求める計算（B の Step 5）。
    /// MonoBehaviour から切り離して、EditMode テストで確かめられるようにする。
    /// </summary>
    public static class StageLayout
    {
        /// <summary>机の板：上面が高さ0になるよう、中心を厚さの半分だけ下げる。</summary>
        public static LocalPlacement Desk(DeskData desk, float thickness)
        {
            return new LocalPlacement
            {
                position = new Vector3(0f, -thickness / 2f, 0f),
                rotation = Quaternion.identity,
                scale = new Vector3(desk.width, thickness, desk.depth),
            };
        }

        /// <summary>箱：中心と大きさはそのまま、回転は上から見た向き（Y軸まわり）だけ。</summary>
        public static LocalPlacement Box(BoxData box)
        {
            return new LocalPlacement
            {
                position = Vec3.To(box.center),
                rotation = Quaternion.Euler(0f, box.yawDegrees, 0f),
                scale = Vec3.To(box.size),
            };
        }
    }
}
