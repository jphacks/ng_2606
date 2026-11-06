using UnityEngine;

namespace DeskStage.App
{
    /// <summary>箱のしかけの決まり（どこに乗ったか、水が流す向き、回復の間隔など）。3D に触れない計算だけ。EditMode テストで確かめる。</summary>
    public static class GimmickRules
    {
        /// <summary>水が流す速さ（m/秒）。歩く速さ（0.15）の 3 分の 1。「少し」流される。</summary>
        public const float WaterSpeed = 0.05f;

        /// <summary>水の坂が流す速さ（m/秒）。歩く速さ（0.15）より速いので、流れにさからっては進めない（下り向きなら下りるだけ、上り向きなら流れに乗ってのぼれる）。</summary>
        public const float WaterRampSpeed = 0.25f;

        // 水の坂は、ジャンプで浮いている間も流す（跳んでのぼれないように）。2 段ジャンプの高さ（約 24cm）より上まで
        const float AboveRampWater = 0.30f;

        /// <summary>緑の箱で、ハートを 1 つ回復するまでの時間（秒）。</summary>
        public const float HealSeconds = 1f;

        // 足元が、箱の上面から この範囲にあるとき、「乗っている」（箱の上面の高さを基準に、下は 1cm、上は キャラの身長 3cm + 余裕）
        const float BelowTop = 0.01f;
        const float AboveTop = 0.035f;

        /// <summary>
        /// 箱の上に乗っているか。local は、箱の座標（箱の中心が原点で、1 辺が 1 の立方体）での、キャラの足元。
        /// heightScale は、箱の高さ（m）。上面からの高さを、m に直すために使う。
        /// </summary>
        public static bool IsOnTop(Vector3 local, float heightScale, bool ramp = false)
        {
            if (Mathf.Abs(local.x) > 0.5f || Mathf.Abs(local.z) > 0.5f)
            {
                return false;
            }
            // 坂は、斜めの面（箱の座標で y = x）からの高さを見る
            var surfaceY = ramp ? local.x : 0.5f;
            var above = (local.y - surfaceY) * heightScale;
            return above >= -BelowTop && above <= AboveTop;
        }

        /// <summary>
        /// 坂の上（斜めの面の上、ジャンプで浮いている間も含む）にいるか。水の坂が流すかどうかに使う。local は箱の座標、heightScale は箱の高さ（m）。
        /// </summary>
        public static bool IsOverRamp(Vector3 local, float heightScale)
        {
            if (Mathf.Abs(local.x) > 0.5f || Mathf.Abs(local.z) > 0.5f)
            {
                return false;
            }
            var above = (local.y - local.x) * heightScale;
            return above >= -BelowTop && above <= AboveRampWater;
        }

        /// <summary>
        /// 水の流れる向き（箱の座標の x・z の、どれか 1 方向）。flow が 1〜4 なら、選んだ向き（+x、+z、-x、-z）。
        /// 0 なら、箱の id から決める（同じ id なら、いつも同じ向き）。
        /// </summary>
        public static Vector2 WaterDirection(string boxId, int flow = 0)
        {
            switch (WaterIndex(boxId, flow))
            {
                case 0: return new Vector2(1f, 0f);
                case 1: return new Vector2(0f, 1f);
                case 2: return new Vector2(-1f, 0f);
                default: return new Vector2(0f, -1f);
            }
        }

        /// <summary>水の向きの番号（0〜3 ＝ +x、+z、-x、-z）。flow が 1〜4 なら、選んだ向き。0 なら id から。</summary>
        public static int WaterIndex(string boxId, int flow = 0)
        {
            if (flow >= 1 && flow <= 4)
            {
                return flow - 1;
            }
            unchecked
            {
                var hash = 17;
                foreach (var ch in boxId ?? "")
                {
                    hash = hash * 31 + ch;
                }
                return ((hash % 4) + 4) % 4;
            }
        }

        /// <summary>押すたびに、次の向き（90 度まわる）にする。いま見えている向きから、次へ。戻りは flow（1〜4）。</summary>
        public static int NextFlow(string boxId, int flow)
        {
            return (WaterIndex(boxId, flow) + 1) % 4 + 1;
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
