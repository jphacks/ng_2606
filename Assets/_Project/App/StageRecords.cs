using System.Collections.Generic;
using System.Globalization;
using DeskStage.Gameplay;
using UnityEngine;

namespace DeskStage.App
{
    /// <summary>
    /// ステージごとの、速いタイムの上位 3 つ（2026-11-11）。クリアの画面で、前の記録からどれだけ速くなったかと、これまでの上位を見せる。
    /// C の RecordStore は、いちばん速いタイム 1 つだけを持ち、ゴールしたときに上書きする（ゴールのあとでは、前の記録が分からない）。
    /// そのため、App で別に覚える（この iPhone の中。PlayerPrefs の「top_＋ステージID」に、速い順に「,」でつないだ秒）。
    /// この仕組みより前の記録は、遊び始めるときに、C の記録から引き継ぐ（Seed）。記録は、しばりで分けない。
    /// </summary>
    public static class StageRecords
    {
        /// <summary>覚えておく数。</summary>
        public const int Keep = 3;

        public struct Result
        {
            /// <summary>今回より前の、いちばん速いタイム。初めてのクリアなら null。</summary>
            public float? PreviousBest;
            /// <summary>今回を入れたあとの、速い順のタイム（Keep 個まで）。</summary>
            public float[] Top;
            /// <summary>今回のタイムの順位（0 が 1 位）。上位に入らなければ -1。</summary>
            public int Rank;
        }

        static string Key(string stageId) => "top_" + stageId;

        /// <summary>保存した文字を、タイムの並びにする（読めない所は飛ばす）。</summary>
        public static List<float> Parse(string text)
        {
            var list = new List<float>();
            if (string.IsNullOrEmpty(text)) return list;
            foreach (var part in text.Split(','))
            {
                if (float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds > 0f)
                {
                    list.Add(seconds);
                }
            }
            list.Sort();
            if (list.Count > Keep) list.RemoveRange(Keep, list.Count - Keep);
            return list;
        }

        public static string Format(List<float> list)
        {
            var parts = new string[list.Count];
            for (var i = 0; i < list.Count; i++) parts[i] = list[i].ToString("R", CultureInfo.InvariantCulture);
            return string.Join(",", parts);
        }

        /// <summary>
        /// 速い順の並び list に、今回のタイムを入れる（同じタイムは、前からある方を上にする）。Keep 個を超えたら、遅い方を消す。
        /// 返すのは今回の順位（0 が 1 位）。上位に入らなければ -1。
        /// </summary>
        public static int Insert(List<float> list, float seconds)
        {
            var at = 0;
            while (at < list.Count && list[at] <= seconds) at++;
            if (at >= Keep) return -1;
            list.Insert(at, seconds);
            if (list.Count > Keep) list.RemoveRange(Keep, list.Count - Keep);
            return at;
        }

        /// <summary>遊び始めるとき：まだ上位を覚えていないステージは、C の最速記録を 1 位として引き継ぐ。</summary>
        public static void Seed(string stageId)
        {
            if (string.IsNullOrEmpty(stageId) || PlayerPrefs.HasKey(Key(stageId))) return;
            var bestKey = RecordBook.Key(stageId);
            if (!PlayerPrefs.HasKey(bestKey)) return;
            var best = PlayerPrefs.GetFloat(bestKey, 0f);
            if (best <= 0f) return;
            PlayerPrefs.SetString(Key(stageId), Format(new List<float> { best }));
            PlayerPrefs.Save();
        }

        /// <summary>ゴールしたタイムを覚えて、前の最速と、上位と、今回の順位を返す。</summary>
        public static Result Add(string stageId, float seconds)
        {
            var list = Parse(string.IsNullOrEmpty(stageId) ? "" : PlayerPrefs.GetString(Key(stageId), ""));
            var result = new Result { PreviousBest = list.Count > 0 ? list[0] : (float?)null };
            result.Rank = Insert(list, seconds);
            result.Top = list.ToArray();
            if (!string.IsNullOrEmpty(stageId))
            {
                PlayerPrefs.SetString(Key(stageId), Format(list));
                PlayerPrefs.Save();
            }
            return result;
        }
    }
}
