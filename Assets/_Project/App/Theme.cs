using TMPro;
using UnityEngine;

namespace DeskStage.App
{
    /// <summary>
    /// 新しい UI の見た目の決まり（色、大きさ、フォント）。全部の画面が、ここだけを見て作る。
    /// 大きさは、基準の画面（横 2532 × 縦 1170。CanvasScaler の基準）での値。
    /// 子どもが押す物は、高さ 160 以上（D_ui.md の決まり）。
    /// </summary>
    public static class Theme
    {
        /// <summary>日本語のフォント（TMP のフォントアセット）。AppRoot が入れる。</summary>
        public static TMP_FontAsset Font;

        // ---- 色 ----
        public static readonly Color Cream = Hex("FFF4DF");      // 画面の地の色
        public static readonly Color Sky = Hex("BFE4FF");        // 空の色（タイトル）
        public static readonly Color SkyLight = Hex("EAF7FF");
        public static readonly Color Ink = Hex("27304A");         // 文字の色（濃い紺）
        public static readonly Color InkSoft = Hex("5C6784");     // 補いの文字
        public static readonly Color White = Color.white;
        public static readonly Color Card = Hex("FFFFFF");
        public static readonly Color Orange = Hex("FFB000");      // いちばん大事なボタン
        public static readonly Color OrangeDeep = Hex("E58A00");
        public static readonly Color Blue = Hex("3B82F6");        // ふつうのボタン
        public static readonly Color BlueDeep = Hex("2563EB");
        public static readonly Color Green = Hex("22B573");       // できた、OK
        public static readonly Color Red = Hex("EF4B6C");         // 消す、失敗
        public static readonly Color Gray = Hex("A9B2C3");        // 押せない
        public static readonly Color Navy = Hex("1B2540");        // 3D の上に重ねる帯
        public static readonly Color Wood = Hex("D9A066");        // 机の色（飾り）

        /// <summary>ブロックの飾りの色（タイトルなど）。</summary>
        public static readonly Color[] BlockColors =
        {
            Hex("FF7A8A"), Hex("FFC24D"), Hex("6CCB8B"), Hex("5BB5FF"), Hex("B58CFF"),
        };

        // ---- 大きさ ----
        public const float Margin = 56f;
        public const float CardRadius = 44f;
        public const float ButtonRadius = 200f;     // 丸いボタン（ふち一杯に丸める）
        public const float ButtonHeight = 170f;
        public const float SmallButtonHeight = 130f; // 帯の中の小さなボタン（それでも 160 未満にしない場所では使わない）
        public const float BarHeight = 160f;

        // ---- 文字の大きさ ----
        public const float Hero = 132f;
        public const float H1 = 76f;
        public const float H2 = 58f;
        public const float Body = 46f;
        public const float Small = 36f;

        // ---- 時間（秒） ----
        public const float FadeSeconds = 0.22f;
        public const float PressScale = 0.94f;

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        public static Color Darken(Color c, float amount) =>
            new Color(c.r * (1f - amount), c.g * (1f - amount), c.b * (1f - amount), c.a);
    }
}
