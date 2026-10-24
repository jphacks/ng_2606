using System;
using System.IO;
using NUnit.Framework;

namespace DeskStage.Merge.Tests
{
    /// <summary>CharacterStore：写真のキャラクターの保存・一覧・削除・選択。</summary>
    public class CharacterStoreTests
    {
        string dir;
        CharacterStore store;

        [SetUp]
        public void SetUp()
        {
            dir = Path.Combine(Path.GetTempPath(), "DeskStageCharacterTests-" + Guid.NewGuid().ToString("N"));
            store = new CharacterStore(dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }

        static readonly byte[] Png = { 1, 2, 3, 4 };

        [Test]
        public void 足したキャラが一覧に出る()
        {
            var a = store.Add("くまさん", Png, 0.5f, new DateTimeOffset(2026, 10, 24, 10, 0, 0, TimeSpan.Zero));
            var list = store.List();

            Assert.IsNotNull(a);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("くまさん", list[0].name);
            Assert.AreEqual(0.5f, list[0].aspect, 1e-5f);
            CollectionAssert.AreEqual(Png, store.LoadPng(a.id));
        }

        [Test]
        public void 一覧は新しい順()
        {
            var old = store.Add("古い", Png, 1f, new DateTimeOffset(2026, 10, 24, 10, 0, 0, TimeSpan.Zero));
            var recent = store.Add("新しい", Png, 1f, new DateTimeOffset(2026, 10, 24, 11, 0, 0, TimeSpan.Zero));
            var list = store.List();

            Assert.AreEqual(recent.id, list[0].id);
            Assert.AreEqual(old.id, list[1].id);
        }

        [Test]
        public void 名前が空なら番号つきの名前になる()
        {
            var a = store.Add("", Png, 1f, DateTimeOffset.Now);
            Assert.AreEqual("キャラ 1", a.name);
        }

        [Test]
        public void 同じ秒に足しても上書きしない()
        {
            var now = new DateTimeOffset(2026, 10, 24, 10, 0, 0, TimeSpan.Zero);
            var a = store.Add("a", Png, 1f, now);
            var b = store.Add("b", Png, 1f, now);

            Assert.AreNotEqual(a.id, b.id);
            Assert.AreEqual(2, store.List().Count);
        }

        [Test]
        public void 選んだキャラを覚える()
        {
            var a = store.Add("a", Png, 1f, DateTimeOffset.Now);
            Assert.AreEqual("", store.SelectedId);

            store.SelectedId = a.id;
            Assert.AreEqual(a.id, store.SelectedId);

            store.SelectedId = "";
            Assert.AreEqual("", store.SelectedId);
        }

        [Test]
        public void 消したキャラは選びも外れる()
        {
            var a = store.Add("a", Png, 1f, DateTimeOffset.Now);
            store.SelectedId = a.id;

            Assert.IsTrue(store.Delete(a.id));
            Assert.AreEqual("", store.SelectedId);
            Assert.AreEqual(0, store.List().Count);
            Assert.IsNull(store.LoadPng(a.id));
        }

        [Test]
        public void 存在しないキャラの選びは空として扱う()
        {
            store.SelectedId = "char-none";
            Assert.AreEqual("", store.SelectedId);
        }

        [TestCase("../evil")]
        [TestCase("a/b")]
        [TestCase("")]
        [TestCase("キャラ")]
        public void 危ない_idは受けつけない(string id)
        {
            Assert.IsFalse(CharacterStore.IsSafeId(id));
            Assert.IsNull(store.LoadPng(id));
            Assert.IsFalse(store.Delete(id));
        }

        [Test]
        public void 空の保存は失敗する()
        {
            Assert.IsNull(store.Add("a", null, 1f, DateTimeOffset.Now));
            Assert.IsNull(store.Add("a", new byte[0], 1f, DateTimeOffset.Now));
        }
    }
}
