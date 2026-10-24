using System;
using System.IO;
using NUnit.Framework;

namespace DeskStage.Merge.Tests
{
    /// <summary>BackdropStore：ステージの背景に使う写真の保存・読み込み・消去。</summary>
    public class BackdropStoreTests
    {
        string dir;
        BackdropStore store;

        [SetUp]
        public void SetUp()
        {
            dir = Path.Combine(Path.GetTempPath(), "DeskStageBackdropTests-" + Guid.NewGuid().ToString("N"));
            store = new BackdropStore(dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }

        [Test]
        public void 最初は背景の写真がない()
        {
            Assert.IsFalse(store.Exists);
            Assert.IsNull(store.Load());
        }

        [Test]
        public void 保存した写真を読める()
        {
            var jpeg = new byte[] { 1, 2, 3 };
            Assert.IsTrue(store.Save(jpeg));
            Assert.IsTrue(store.Exists);
            CollectionAssert.AreEqual(jpeg, store.Load());
        }

        [Test]
        public void 保存し直すと置き換わる()
        {
            store.Save(new byte[] { 1 });
            store.Save(new byte[] { 9, 9 });
            CollectionAssert.AreEqual(new byte[] { 9, 9 }, store.Load());
        }

        [Test]
        public void 消すと背景の写真がなくなる()
        {
            store.Save(new byte[] { 1 });
            Assert.IsTrue(store.Clear());
            Assert.IsFalse(store.Exists);
            // もともと無くても、消せたことにする
            Assert.IsTrue(store.Clear());
        }

        [Test]
        public void 空の写真は保存しない()
        {
            Assert.IsFalse(store.Save(null));
            Assert.IsFalse(store.Save(new byte[0]));
            Assert.IsFalse(store.Exists);
        }
    }
}
