using UnityEngine;

namespace DeskStage.Merge
{
    /// <summary>
    /// 箱の上面の写真（切り出した画素）が、一面、青・赤・緑・黄・紫・ピンクのどれかなら、その箱のしかけ
    /// （水・炎・回復・ジャンプ台・敵・ワープ）を返す計算。
    /// 「少し その色が入っているだけ」では、しかけにしない。上面の画素の ほとんど（Ratio 以上）が、同じ色のときだけ。
    /// 色は、明るさのちがいに強いよう、色相（H）・彩度（S）・明るさ（V）で見分ける。茶色（段ボール）、白、黒、灰色は、どれにもならない。
    /// </summary>
    public static class GimmickDetector
    {
        /// <summary>しかけにする割合。上面の画素の、これ以上が同じ色なら、しかけ（影やふちを見込んで、全部ではない）。</summary>
        public const float Ratio = 0.85f;

        /// <summary>画素の色の種類。</summary>
        public enum Hue
        {
            None,
            Blue,
            Red,
            Green,
            Yellow,
            Purple,
            Pink,
        }

        /// <summary>画素 1 つの色を見分ける。</summary>
        public static Hue Classify(Color32 c)
        {
            Color.RGBToHSV(c, out var h, out var s, out var v);
            var degrees = h * 360f;
            if (v < 0.22f)
            {
                return Hue.None;
            }
            // 実機で撮った折り紙の色（2026-10-25）：青は 色相 213〜215、緑は 176〜179（青緑）、赤は 356〜358（彩度は 0.53 から）。
            // 緑は、青緑まで含める（黄緑〜青緑：75〜195 度）。青は、それより青い側（200〜250 度）。
            // 2026-11-06 に、黄・紫・ピンクを足した（実機ではまだ測っていない）。
            //  ・紫は、青より赤寄り（250〜300 度）。そのため青を 265 度から 250 度までに狭めた（実機の青は 213〜215 度）
            //  ・ピンクは、赤紫（300〜335 度）と、赤の色相で淡い色（彩度 0.18〜0.47、明るさ 0.6 以上）
            //  ・赤は、赤の色相で、あざやかな色（彩度 0.47 から。実機の赤は 0.53 から）
            //  ・黄は 40〜72 度（段ボールや茶色のテープは 20〜35 度、オレンジは 30 度前後なので、どれにもならない）
            if (degrees >= 200f && degrees < 250f && s >= 0.40f)
            {
                return Hue.Blue;
            }
            if (degrees >= 250f && degrees < 300f && s >= 0.25f)
            {
                return Hue.Purple;
            }
            if (degrees >= 75f && degrees < 200f && s >= 0.35f)
            {
                return Hue.Green;
            }
            if (degrees >= 40f && degrees <= 72f && s >= 0.50f && v >= 0.45f)
            {
                return Hue.Yellow;
            }
            if (degrees >= 300f && degrees < 335f && s >= 0.20f && v >= 0.30f)
            {
                return Hue.Pink;
            }
            if (degrees <= 14f || degrees >= 335f)
            {
                if (s >= 0.47f && v >= 0.30f)
                {
                    return Hue.Red;
                }
                if (s >= 0.18f && v >= 0.60f)
                {
                    return Hue.Pink;
                }
            }
            return Hue.None;
        }

        /// <summary>上面の画素の列から、しかけを決める。決まらなければ空。</summary>
        public static string Detect(Color32[] pixels)
        {
            if (pixels == null || pixels.Length == 0)
            {
                return "";
            }
            var counts = new int[System.Enum.GetValues(typeof(Hue)).Length];
            foreach (var p in pixels)
            {
                counts[(int)Classify(p)]++;
            }
            var need = Mathf.CeilToInt(pixels.Length * Ratio - 0.001f); // 誤差で、ちょうどの割合が足りなくならないよう
            if (counts[(int)Hue.Blue] >= need) return Shared.BoxData.GimmickWater;
            if (counts[(int)Hue.Red] >= need) return Shared.BoxData.GimmickFire;
            if (counts[(int)Hue.Green] >= need) return Shared.BoxData.GimmickHeal;
            if (counts[(int)Hue.Yellow] >= need) return Shared.BoxData.GimmickJumpPad;
            if (counts[(int)Hue.Purple] >= need) return Shared.BoxData.GimmickEnemy;
            if (counts[(int)Hue.Pink] >= need) return Shared.BoxData.GimmickWarp;
            return "";
        }
    }
}
