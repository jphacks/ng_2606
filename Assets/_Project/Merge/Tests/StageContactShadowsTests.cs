using NUnit.Framework;

namespace DeskStage.Merge.Tests
{
    /// <summary>物が机に接するところの暗がり（StageContactShadows）の形。</summary>
    public class StageContactShadowsTests
    {
        [Test]
        public void ふちがいちばん暗く外へ向かって薄くなり広がる幅で消える()
        {
            const float margin = 0.02f;
            Assert.AreEqual(StageContactShadows.EdgeAlpha, StageContactShadows.Alpha(0f, margin), 1e-5f);
            var a1 = StageContactShadows.Alpha(0.005f, margin);
            var a2 = StageContactShadows.Alpha(0.012f, margin);
            Assert.Less(a1, StageContactShadows.EdgeAlpha);
            Assert.Less(a2, a1);
            Assert.AreEqual(0f, StageContactShadows.Alpha(margin, margin), 1e-5f);
            Assert.AreEqual(0f, StageContactShadows.Alpha(0.1f, margin), 1e-5f);
        }

        [Test]
        public void 背の高い物ほど暗がりが広い_ただし限りがある()
        {
            Assert.Less(StageContactShadows.Margin(0.02f), StageContactShadows.Margin(0.08f));
            Assert.LessOrEqual(StageContactShadows.Margin(1f), 0.05f);
            Assert.GreaterOrEqual(StageContactShadows.Margin(0.001f), 0.008f);
        }
    }
}
