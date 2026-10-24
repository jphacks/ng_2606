using System.Collections.Generic;
using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>
    /// 1枚目（真上）の写真と、その中の机・箱の上面の位置（答えの deskInPhoto1、topFaceInPhoto1）。
    /// StageReader が答えから書き、合流のつなぎ（Merge の StagePhotoTexturer）が 3D のステージに写真を貼るときと、
    /// StageLibrary が保存のときに一覧の絵（サムネイル）を作るときに読む。
    /// 共通コード（Shared）ではなく B の中に置いている。他の担当は直接使わない。
    /// </summary>
    public static class StagePhotoRegions
    {
        /// <summary>1枚目の写真の中の、机の四隅（0〜1 の割合）。分からなければ null。</summary>
        public static Vector2[] DeskQuad { get; private set; }

        /// <summary>箱の id ごとの、1枚目の写真の中の上面の四隅（0〜1 の割合）。</summary>
        public static Dictionary<string, Vector2[]> TopFaces { get; } = new Dictionary<string, Vector2[]>();

        /// <summary>1枚目（真上）の写真（JPEG）。A の PhotoCapture が撮ったもの。まだなら null。</summary>
        public static byte[] TopJpeg { get; set; }

        /// <summary>答えの位置を入れ替えた。</summary>
        public static event System.Action Changed;

        public static void Set(Vector2[] deskQuad, Dictionary<string, Vector2[]> topFaces)
        {
            DeskQuad = deskQuad;
            TopFaces.Clear();
            if (topFaces != null)
            {
                foreach (var pair in topFaces)
                {
                    TopFaces[pair.Key] = pair.Value;
                }
            }
            Changed?.Invoke();
        }

        /// <summary>もういちど撮るとき。位置と写真を消す。</summary>
        public static void Clear()
        {
            DeskQuad = null;
            TopFaces.Clear();
            TopJpeg = null;
            Changed?.Invoke();
        }
    }
}
