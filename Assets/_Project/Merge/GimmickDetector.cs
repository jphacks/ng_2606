using UnityEngine;

namespace DeskStage.Merge
{
    /// <summary>
    /// 箱の上面の写真（切り出した画素）が、一面、青・赤・緑のどれかなら、その箱のしかけ（水・炎・回復）を返す計算。
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
            // 緑は、青緑まで含める（黄緑〜青緑：75〜195 度）。青は、それより青い側（200〜265 度）。
            if (degrees >= 200f && degrees <= 265f && s >= 0.40f)
            {
                return Hue.Blue;
            }
            if (degrees >= 75f && degrees < 200f && s >= 0.35f)
            {
                return Hue.Green;
            }
            if ((degrees <= 14f || degrees >= 335f) && s >= 0.40f && v >= 0.30f)
            {
                return Hue.Red;
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
            var blue = 0;
            var red = 0;
            var green = 0;
            foreach (var p in pixels)
            {
                switch (Classify(p))
                {
                    case Hue.Blue: blue++; break;
                    case Hue.Red: red++; break;
                    case Hue.Green: green++; break;
                }
            }
            var need = Mathf.CeilToInt(pixels.Length * Ratio - 0.001f); // 誤差で、ちょうどの割合が足りなくならないよう
            if (blue >= need) return Shared.BoxData.GimmickWater;
            if (red >= need) return Shared.BoxData.GimmickFire;
            if (green >= need) return Shared.BoxData.GimmickHeal;
            return "";
        }
    }
}
