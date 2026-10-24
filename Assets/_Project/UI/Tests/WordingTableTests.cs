using NUnit.Framework;
using UnityEngine;

namespace DeskStage.UI.Tests
{
    public class WordingTableTests
    {
        const string Sample =
            "# 画面の文言表\n" +
            "\n" +
            "| キー | 文 | 使う場面 |\n" +
            "| --- | --- | --- |\n" +
            "| title.start | はじめる | タイトルのボタン |\n" +
            "| scan.boxCount | みつけた はこ：{0} | 画面の上 |\r\n" +
            "ふつうの文の行\n";

        [Test]
        public void 表の行だけを読む()
        {
            var entries = WordingTable.Parse(Sample);
            Assert.AreEqual(2, entries.Count);
            Assert.AreEqual("title.start", entries[0].Key);
            Assert.AreEqual("はじめる", entries[0].Text);
            Assert.AreEqual("みつけた はこ：{0}", entries[1].Text);
        }

        [Test]
        public void ないキーは括弧付きのキーを返す()
        {
            var wording = ScriptableObject.CreateInstance<Wording>();
            wording.SetEntries(WordingTable.Parse(Sample));
            Assert.AreEqual("はじめる", wording.Get("title.start"));
            Assert.AreEqual("[nothing.here]", wording.Get("nothing.here"));
            Object.DestroyImmediate(wording);
        }
    }
}
