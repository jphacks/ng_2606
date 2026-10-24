using System.Collections.Generic;

namespace DeskStage.UI
{
    /// <summary>
    /// Docs/Wording.md の表を読む。計算だけの普通のクラス。
    /// 「| キー | 文 | 使う場面 |」の行のうち、キーに「.」を含む行だけを文として読む。
    /// </summary>
    public static class WordingTable
    {
        public static List<Wording.Entry> Parse(string markdown)
        {
            var result = new List<Wording.Entry>();
            foreach (var raw in markdown.Split('\n'))
            {
                var line = raw.Trim();
                if (!line.StartsWith("|"))
                {
                    continue;
                }
                var cells = line.Trim('|').Split('|');
                if (cells.Length < 2)
                {
                    continue;
                }
                var key = cells[0].Trim();
                // 見出しの行（キー）と区切りの行（---）は飛ばす
                if (!key.Contains(".") || key.StartsWith("-"))
                {
                    continue;
                }
                result.Add(new Wording.Entry { Key = key, Text = cells[1].Trim() });
            }
            return result;
        }
    }
}
