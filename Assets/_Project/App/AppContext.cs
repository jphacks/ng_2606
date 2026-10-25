using DeskStage.Merge;
using DeskStage.UI;
using UnityEngine;

namespace DeskStage.App
{
    /// <summary>画面どうしで共有する物（画面の入れ物、切り替え役、全体の流れ、3D の見た目の設定）。</summary>
    public class AppContext
    {
        public Canvas Canvas;

        /// <summary>安全な範囲（ノッチなどを避けた範囲）の入れ物。多くの画面は、この中に作る。</summary>
        public RectTransform SafeArea;

        /// <summary>画面いっぱい（安全な範囲の外まで）の入れ物。背景など。</summary>
        public RectTransform Full;

        public AppRouter Router;

        public AppFlow Flow;

        public AppOverlays Overlays;

        public ViewLook ViewLook;

        public ViewGestureSettings ViewGestureSettings;

        public Wording Wording;

        public Sprite[] TipSprites = new Sprite[0];

        public CharacterCapture Camera;

        /// <summary>タイトル画面の写真（アイキャッチ）。無ければ、コードで描いた景色。</summary>
        public Texture2D TitleImage;

        /// <summary>箱のしかけ（水・炎・回復）と、ハート。</summary>
        public GimmickSystem Gimmicks;
    }
}
