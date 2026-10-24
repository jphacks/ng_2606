using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Gameplay.Tests
{
    /// <summary>
    /// 落下とはさまりの判定（C_gameplay.md の Step 15）を確かめる。
    /// </summary>
    public class SafetyGuardTests
    {
        [Test]
        public void 机の天面からfallDepthより下で落ちたとする()
        {
            Assert.IsFalse(SafetyMath.HasFallen(0f, 0.10f));
            Assert.IsFalse(SafetyMath.HasFallen(-0.10f, 0.10f));
            Assert.IsTrue(SafetyMath.HasFallen(-0.101f, 0.10f));
        }

        [Test]
        public void 進もうとして3秒動けないとはさまったとする()
        {
            var d = new StuckDetector();
            var p = Vector3.zero;
            Assert.IsFalse(d.Update(0f, p, true, 3f));
            Assert.IsFalse(d.Update(2.9f, p + Vector3.right * 0.001f, true, 3f));
            Assert.IsTrue(d.Update(3f, p + Vector3.right * 0.002f, true, 3f));
        }

        [Test]
        public void 動けていればはさまりではない()
        {
            var d = new StuckDetector();
            for (int i = 0; i <= 40; i++)
            {
                // 0.1 秒ごとに 1cm 進む
                Assert.IsFalse(d.Update(i * 0.1f, new Vector3(0f, 0f, i * 0.01f), true, 3f));
            }
        }

        [Test]
        public void スティックを離すと数え直す()
        {
            var d = new StuckDetector();
            d.Update(0f, Vector3.zero, true, 3f);
            d.Update(2f, Vector3.zero, false, 3f);
            Assert.IsFalse(d.Update(2.5f, Vector3.zero, true, 3f));
            Assert.IsFalse(d.Update(5f, Vector3.zero, true, 3f));
            Assert.IsTrue(d.Update(5.5f, Vector3.zero, true, 3f));
        }
    }
}
