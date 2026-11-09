using NUnit.Framework;

namespace DeskStage.Stage.Tests
{
    /// <summary>保存したステージの、箱の上面の写真のファイルの名前（Docs/specs/saved-photos の REQ-2・5）。</summary>
    public class StageFaceFileTests
    {
        [Test]
        public void 箱のidから名前を作る()
        {
            Assert.AreEqual("face-box-01.jpg", StageStorage.FaceFile("box-01"));
            Assert.AreEqual("face-m_02.jpg", StageStorage.FaceFile("m_02"));
        }

        [Test]
        public void 形の違う箱のidからは_名前を作らない()
        {
            Assert.AreEqual("", StageStorage.FaceFile("../x"));
            Assert.AreEqual("", StageStorage.FaceFile("a/b"));
            Assert.AreEqual("", StageStorage.FaceFile(""));
            Assert.AreEqual("", StageStorage.FaceFile(new string('a', 41)));
        }

        [Test]
        public void 読むときは_決まった形の名前だけ()
        {
            Assert.IsTrue(StageStorage.IsFaceFile("face-box-01.jpg"));
            Assert.IsFalse(StageStorage.IsFaceFile("thumbnail.jpg"));
            Assert.IsFalse(StageStorage.IsFaceFile("face-../../stage.jpg"));
            Assert.IsFalse(StageStorage.IsFaceFile("face-box-01.png"));
            Assert.IsFalse(StageStorage.IsFaceFile(null));
            Assert.IsNull(StageStorage.SavedFacePath("stage-1", "../stage.json"));
            Assert.IsNull(StageStorage.SavedFacePath("../other", "face-box-01.jpg"));
            Assert.IsNotNull(StageStorage.SavedFacePath("stage-1", "face-box-01.jpg"));
        }
    }
}
