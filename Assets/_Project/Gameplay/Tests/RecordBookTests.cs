using NUnit.Framework;

namespace DeskStage.Gameplay.Tests
{
    /// <summary>
    /// 最速記録（C_gameplay.md の Step 16）の比べ方を確かめる。
    /// </summary>
    public class RecordBookTests
    {
        RecordBook book;

        [SetUp]
        public void SetUp()
        {
            book = new RecordBook(new MemoryRecordStorage());
        }

        [Test]
        public void キーはbest_とステージID()
        {
            Assert.AreEqual("best_sample-01", RecordBook.Key("sample-01"));
        }

        [Test]
        public void 初めてのゴールは最速記録()
        {
            Assert.IsNull(book.GetBest("s1"));
            Assert.IsTrue(book.Submit("s1", 30f));
            Assert.AreEqual(30f, book.GetBest("s1"));
        }

        [Test]
        public void 速ければ更新する()
        {
            book.Submit("s1", 30f);
            Assert.IsTrue(book.Submit("s1", 25f));
            Assert.AreEqual(25f, book.GetBest("s1"));
        }

        [Test]
        public void 遅いか同じなら更新しない()
        {
            book.Submit("s1", 30f);
            Assert.IsFalse(book.Submit("s1", 35f));
            Assert.IsFalse(book.Submit("s1", 30f));
            Assert.AreEqual(30f, book.GetBest("s1"));
        }

        [Test]
        public void 新しいステージは記録も新しく始まる()
        {
            book.Submit("s1", 10f);
            Assert.IsNull(book.GetBest("s2"));
            Assert.IsTrue(book.Submit("s2", 50f));
        }

        [Test]
        public void 記録を消せる()
        {
            book.Submit("s1", 10f);
            book.Clear("s1");
            Assert.IsNull(book.GetBest("s1"));
        }
    }
}
