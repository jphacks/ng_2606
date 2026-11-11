using System.Collections.Generic;
using NUnit.Framework;

namespace DeskStage.App.Tests
{
    /// <summary>ステージごとの上位の記録（StageRecords）と、速くなった秒の書き方（FlowRules.FormatGain）。</summary>
    public class StageRecordsTests
    {
        [Test]
        public void 速い順に並び3つまで覚える()
        {
            var list = new List<float>();
            Assert.AreEqual(0, StageRecords.Insert(list, 30f));
            Assert.AreEqual(0, StageRecords.Insert(list, 20f));
            Assert.AreEqual(1, StageRecords.Insert(list, 25f));
            Assert.AreEqual(3, list.Count);
            // 4 つ目に速いのは、入らない
            Assert.AreEqual(-1, StageRecords.Insert(list, 40f));
            // 2 位に入ると、いちばん遅いのが消える
            Assert.AreEqual(1, StageRecords.Insert(list, 22f));
            CollectionAssert.AreEqual(new[] { 20f, 22f, 25f }, list);
        }

        [Test]
        public void 同じタイムは前からある方が上()
        {
            var list = new List<float> { 20f, 25f };
            Assert.AreEqual(1, StageRecords.Insert(list, 20f));
            CollectionAssert.AreEqual(new[] { 20f, 20f, 25f }, list);
        }

        [Test]
        public void 保存した文字を読み直せる()
        {
            var list = new List<float> { 12.34f, 15.5f, 21f };
            CollectionAssert.AreEqual(list, StageRecords.Parse(StageRecords.Format(list)));
            // 読めない所は飛ばし、速い順に直し、3 つまで
            CollectionAssert.AreEqual(new[] { 1f, 2f, 3f }, StageRecords.Parse("3,x,1,,2,9"));
            Assert.AreEqual(0, StageRecords.Parse("").Count);
        }

        [Test]
        public void 速くなった秒は画面のタイムの差()
        {
            // 25.5 → 23.4 は 2.1 秒
            Assert.AreEqual("2.1", FlowRules.FormatGain(25.5f, 23.4f));
            // 画面では 0:23.4 と 0:21.0（十分の一秒で切る）なので、差は 2.4
            Assert.AreEqual("2.4", FlowRules.FormatGain(23.49f, 21.01f));
            // 十分の一秒では同じに見えるときは、百分の一秒で書く
            Assert.AreEqual("0.04", FlowRules.FormatGain(23.48f, 23.44f));
            Assert.AreEqual("12.0", FlowRules.FormatGain(35f, 23f));
        }
    }
}
