using System.Collections.Generic;
using DeskStage.Shared;
using NUnit.Framework;
using UnityEngine;

namespace DeskStage.App.Tests
{
    /// <summary>ワープのつながり（Docs/specs/warp-pairs の REQ-1・3・4・6・7）。</summary>
    public class WarpPairsTests
    {
        static BoxData Warp(string id, float x, string to = "") => new BoxData
        {
            id = id, gimmick = BoxData.GimmickWarp, warpTo = to,
            center = new[] { x, 0.02f, 0f }, size = new[] { 0.05f, 0.04f, 0.05f },
        };

        static BoxData Plain(string id, float x) => new BoxData { id = id, center = new[] { x, 0.02f, 0f }, size = new[] { 0.05f, 0.04f, 0.05f } };

        static void AssertSymmetric(IList<BoxData> boxes)
        {
            foreach (var b in boxes)
            {
                if (string.IsNullOrEmpty(b.warpTo)) continue;
                var partner = WarpPairs.Partner(boxes, b.warpTo);
                Assert.AreEqual(b.id, partner, $"{b.id} → {b.warpTo} が、行き来になっていない");
            }
        }

        [Test]
        public void 自動では_近い組から2つずつつなぐ()
        {
            var boxes = new List<BoxData> { Warp("a", 0f), Warp("b", 0.5f), Warp("c", 0.05f), Warp("d", 0.55f) };
            WarpPairs.AutoPair(boxes);
            Assert.AreEqual("c", WarpPairs.Partner(boxes, "a"));
            Assert.AreEqual("d", WarpPairs.Partner(boxes, "b"));
            AssertSymmetric(boxes);
        }

        [Test]
        public void 奇数なら1つ余り_1つだけならつながらない()
        {
            var three = new List<BoxData> { Warp("a", 0f), Warp("b", 0.1f), Warp("c", 0.5f) };
            WarpPairs.AutoPair(three);
            Assert.AreEqual("", WarpPairs.Partner(three, "c"));
            AssertSymmetric(three);

            var one = new List<BoxData> { Warp("a", 0f), Plain("p", 0.1f) };
            WarpPairs.AutoPair(one);
            Assert.AreEqual("", WarpPairs.Partner(one, "a"));
            Assert.AreEqual(1, WarpPairs.Count(one));
        }

        [Test]
        public void 押すと相手が次に変わり_余った物どうしがつながり直す()
        {
            var boxes = new List<BoxData> { Warp("a", 0f), Warp("b", 0.1f), Warp("c", 0.2f), Warp("d", 0.3f) };
            WarpPairs.AutoPair(boxes); // a⇔b、c⇔d
            Assert.AreEqual("b", WarpPairs.Partner(boxes, "a"));
            WarpPairs.Cycle(boxes, "a"); // a の次の相手は、b の次に近い c
            Assert.AreEqual("c", WarpPairs.Partner(boxes, "a"));
            Assert.AreEqual("d", WarpPairs.Partner(boxes, "b"), "余った b と d がつながる");
            AssertSymmetric(boxes);
            WarpPairs.Cycle(boxes, "a");
            WarpPairs.Cycle(boxes, "a");
            Assert.AreEqual("b", WarpPairs.Partner(boxes, "a"), "最後の次は、最初に戻る");
            AssertSymmetric(boxes);
        }

        [Test]
        public void 壊れたつながりは直す()
        {
            var boxes = new List<BoxData>
            {
                Warp("a", 0f, "nobody"), Warp("b", 0.1f, "a"), Warp("c", 0.2f, "c"),
                new BoxData { id = "p", gimmick = BoxData.GimmickFire, warpTo = "a", center = new[] { 0.3f, 0f, 0f }, size = new[] { 0.05f, 0.04f, 0.05f } },
            };
            WarpPairs.Repair(boxes);
            Assert.AreEqual("", boxes[0].warpTo);
            Assert.AreEqual("", boxes[1].warpTo, "相手が自分を指していない");
            Assert.AreEqual("", boxes[2].warpTo, "自分自身");
            Assert.AreEqual("", boxes[3].warpTo, "ワープでない箱");
        }

        [Test]
        public void 番号はワープの箱だけに順に付く()
        {
            var boxes = new List<BoxData> { Plain("p", 0f), Warp("a", 0.1f), Warp("b", 0.2f) };
            var n = WarpPairs.Numbers(boxes);
            Assert.AreEqual(2, n.Count);
            Assert.AreEqual(1, n["a"]);
            Assert.AreEqual(2, n["b"]);
        }

        [Test]
        public void つながりは保存して読み直しても残り_古い保存データは空()
        {
            var box = Warp("a", 0f, "b");
            Assert.AreEqual("b", JsonUtility.FromJson<BoxData>(JsonUtility.ToJson(box)).warpTo);
            Assert.AreEqual("", JsonUtility.FromJson<BoxData>("{\"id\":\"x\"}").warpTo);
        }
    }
}
