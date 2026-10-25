using UnityEngine;

namespace DeskStage.App
{
    /// <summary>箱のしかけの決まり（どこに乗ったか、水が流す向き、回復の間隔など）。3D に触れない計算だけ。EditMode テストで確かめる。</summary>
    public static class GimmickRules
    {
        /// <summary>水が流す速さ（m/秒）。歩く速さ（0.15）の 3 分の 1。「少し」流される。</summary>
        public const float WaterSpeed = 0.05f;

        /// <summary>緑の箱で、ハートを 1 つ回復するまでの時間（秒）。</summary>
        public const float HealSeconds = 1f;

        // 足元が、箱の上面から この範囲にあるとき、「乗っている」（箱の上面の高さを基準に、下は 1cm、上は キャラの身長 3cm + 余裕）
        const float BelowTop = 0.01f;
        const float AboveTop = 0.035f;

        /// <summary>
        /// 箱の上に乗っているか。local は、箱の座標（箱の中心が原点で、1 辺が 1 の立方体）での、キャラの足元。
        /// heightScale は、箱の高さ（m）。上面からの高さを、m に直すために使う。
        /// </summary>
        public static bool IsOnTop(Vector3 local, float heightScale)
        {
            if (Mathf.Abs(local.x) > 0.5f || Mathf.Abs(local.z) > 0.5f)
            {
                return false;
            }
            var above = (local.y - 0.5f) * heightScale;
            return above >= -BelowTop && above <= AboveTop;
        }

        /// <summary>箱の id から、水の流れる向き（箱の座標の x・z の、どれか 1 方向）を決める。同じ id なら、いつも同じ向き。</summary>
        public static Vector2 WaterDirection(string boxId)
        {
            unchecked
            {
                var hash = 17;
                foreach (var ch in boxId ?? "")
                {
                    hash = hash * 31 + ch;
                }
                switch (((hash % 4) + 4) % 4)
                {
                    case 0: return new Vector2(1f, 0f);
                    case 1: return new Vector2(0f, 1f);
                    case 2: return new Vector2(-1f, 0f);
                    default: return new Vector2(0f, -1f);
                }
            }
        }

        /// <summary>しかけの名前（子ども向け）。</summary>
        public static string Name(string gimmick)
        {
            switch (gimmick)
            {
                case Shared.BoxData.GimmickWater: return AppText.GimmickWater;
                case Shared.BoxData.GimmickFire: return AppText.GimmickFire;
                case Shared.BoxData.GimmickHeal: return AppText.GimmickHeal;
                default: return "";
            }
        }
    }
}
