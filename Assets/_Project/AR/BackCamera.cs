using UnityEngine;

namespace DeskStage.AR
{
    /// <summary>
    /// 撮影に使う、背面のふつうのカメラ（広角）を選ぶ（2026-11-14）。
    /// iPhone 15 で、ステージを撮るときに前のカメラ（内カメラ）が開いていた。前は「前向きでない最初のカメラ」を選んでいたが、
    /// iPhone のカメラの一覧には、広角・超広角・望遠・深度つき（TrueDepth など）がいくつも並び、その中から思わぬ物が選ばれていた。
    /// 前向きの物と、名前に Front / TrueDepth が入る物は使わず、背面の広角を いちばんに選ぶ。
    /// 選び方は、カメラの名前・向き・種類だけで決める計算なので、EditMode テストで確かめる。
    /// </summary>
    public static class BackCamera
    {
        /// <summary>カメラ 1 つ分の、選ぶのに使う情報。</summary>
        public struct Info
        {
            public string Name;
            public bool IsFrontFacing;
            public WebCamKind Kind;
        }

        /// <summary>今の端末のカメラから選ぶ。一覧をログに出す。カメラが無ければ null。</summary>
        public static string FindName(string tag)
        {
            var devices = WebCamTexture.devices;
            var infos = new Info[devices.Length];
            var list = new System.Text.StringBuilder();
            for (var i = 0; i < devices.Length; i++)
            {
                infos[i] = new Info { Name = devices[i].name, IsFrontFacing = devices[i].isFrontFacing, Kind = devices[i].kind };
                list.Append($"\n  {i}: {devices[i].name}（前向き {devices[i].isFrontFacing}、種類 {devices[i].kind}）");
            }
            var index = Pick(infos);
            Debug.Log($"[{tag}] カメラ {devices.Length} 個：{list}\n  → 使う：{(index >= 0 ? infos[index].Name : "なし")}");
            return index >= 0 ? infos[index].Name : null;
        }

        /// <summary>使うカメラの番号。背面の広角 → 背面のほかの種類 → 前向き以外の何か → 先頭、の順。無ければ -1。</summary>
        public static int Pick(Info[] cameras)
        {
            if (cameras == null || cameras.Length == 0)
            {
                return -1;
            }
            var best = -1;
            var bestScore = int.MinValue;
            for (var i = 0; i < cameras.Length; i++)
            {
                var score = Score(cameras[i]);
                if (score > bestScore)
                {
                    best = i;
                    bestScore = score;
                }
            }
            return best;
        }

        static int Score(Info c)
        {
            var name = c.Name ?? "";
            if (c.IsFrontFacing || name.Contains("Front") || name.Contains("TrueDepth"))
            {
                return 0;
            }
            var score = 10;
            if (name.Contains("Back")) score += 5;
            if (c.Kind == WebCamKind.WideAngle) score += 20;
            else if (c.Kind == WebCamKind.ColorAndDepth) score -= 5;
            return score;
        }
    }
}
