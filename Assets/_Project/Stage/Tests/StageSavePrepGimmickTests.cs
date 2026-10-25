using System;
using System.Collections.Generic;
using DeskStage.Shared;
using NUnit.Framework;

namespace DeskStage.Stage.Tests
{
    /// <summary>保存するときに、箱のしかけが落ちないこと。</summary>
    public class StageSavePrepGimmickTests
    {
        static BoxData Box(string id, string gimmick) => new BoxData
        {
            id = id,
            center = new[] { 0f, 0.05f, 0f },
            size = new[] { 0.1f, 0.1f, 0.1f },
            gimmick = gimmick,
        };

        [Test]
        public void 保存した箱にもしかけが残る()
        {
            var working = new List<BoxData> { Box("box-01", BoxData.GimmickWater), Box("box-02", ""), Box("box-03", BoxData.GimmickHeal) };
            var saved = StageSavePrep.Prepare(new StageData(), working, "テスト", DateTimeOffset.Now);
            Assert.AreEqual(BoxData.GimmickWater, saved.boxes[0].gimmick);
            Assert.AreEqual("", saved.boxes[1].gimmick);
            Assert.AreEqual(BoxData.GimmickHeal, saved.boxes[2].gimmick);
        }

        [Test]
        public void 作業中の箱が無いときは読み込んだステージの箱から保存する()
        {
            var current = new StageData { boxes = new[] { Box("box-01", BoxData.GimmickFire) } };
            var saved = StageSavePrep.Prepare(current, new List<BoxData>(), "テスト", DateTimeOffset.Now);
            Assert.AreEqual(BoxData.GimmickFire, saved.boxes[0].gimmick);
        }

        [Test]
        public void しかけがnullでも空として保存する()
        {
            var working = new List<BoxData> { Box("box-01", null) };
            var saved = StageSavePrep.Prepare(new StageData(), working, "テスト", DateTimeOffset.Now);
            Assert.AreEqual("", saved.boxes[0].gimmick);
        }
    }
}
