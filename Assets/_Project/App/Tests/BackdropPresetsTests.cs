using NUnit.Framework;
using UnityEngine;

namespace DeskStage.App.Tests
{
    public class BackdropPresetsTests
    {
        static readonly string[] Ids = { "sky", "sunset", "forest", "sea", "night", "room" };

        [Test]
        public void すべてのプリセットが決まった大きさの絵になる()
        {
            foreach (var id in Ids)
            {
                var pixels = BackdropPresets.Render(id, 64, 32);
                Assert.IsNotNull(pixels, id);
                Assert.AreEqual(64 * 32, pixels.Length, id);
            }
        }

        [Test]
        public void 知らないidは_nullを返す()
        {
            Assert.IsNull(BackdropPresets.Render("nothing", 64, 32));
            Assert.IsNull(BackdropPresets.Render("sky", 0, 32));
        }

        [Test]
        public void 同じ条件なら_いつも同じ絵になる()
        {
            foreach (var id in Ids)
            {
                CollectionAssert.AreEqual(BackdropPresets.Render(id, 48, 24), BackdropPresets.Render(id, 48, 24), id);
            }
        }

        [Test]
        public void 絵はひとつの色ではなく不透明である()
        {
            foreach (var id in Ids)
            {
                var pixels = BackdropPresets.Render(id, 64, 32);
                var first = pixels[0];
                var varied = false;
                foreach (var p in pixels)
                {
                    Assert.AreEqual(255, p.a, id);
                    if (p.r != first.r || p.g != first.g || p.b != first.b) varied = true;
                }
                Assert.IsTrue(varied, id);
            }
        }

        [Test]
        public void 左のはしと右のはしがなめらかにつながる()
        {
            // 背景は、ぐるっと囲む壁なので、つなぎ目で色が大きく飛ばないこと（左右のはしの差を、行ごとに平均する）
            foreach (var id in new[] { "sky", "sunset", "forest", "sea", "night", "room" })
            {
                const int w = 256, h = 64;
                var pixels = BackdropPresets.Render(id, w, h);
                var total = 0f;
                for (var y = 0; y < h; y++)
                {
                    var l = pixels[y * w];
                    var r = pixels[y * w + w - 1];
                    total += (Mathf.Abs(l.r - r.r) + Mathf.Abs(l.g - r.g) + Mathf.Abs(l.b - r.b)) / 255f;
                }
                Assert.Less(total / h, 0.1f, id); // 星のように、ところどころ光る点はあるので、行の平均で見る
            }
        }

        [Test]
        public void 名前はどれも空ではない()
        {
            foreach (var p in BackdropPresets.All)
            {
                Assert.IsNotEmpty(p.Name);
                Assert.IsTrue(BackdropPresets.IsKnown(p.Id));
            }
        }
    }
}
