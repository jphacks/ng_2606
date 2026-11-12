using NUnit.Framework;

namespace DeskStage.App.Tests
{
    /// <summary>足もとの土煙（DustEffects）の、数と大きさの決め方。</summary>
    public class DustEffectsTests
    {
        [Test]
        public void 歩くと小さく少なく走ると大きく多い()
        {
            Assert.AreEqual(2, DustEffects.StepPuffs(0.3f), "ゆっくり歩く");
            Assert.AreEqual(4, DustEffects.StepPuffs(1f), "走る");
            Assert.Less(DustEffects.StepSize(0.3f), DustEffects.StepSize(1f));
        }

        [Test]
        public void 速く落ちてきたほど着地の煙が大きく多い()
        {
            Assert.Less(DustEffects.LandSize(0.4f), DustEffects.LandSize(1.4f));
            Assert.LessOrEqual(DustEffects.LandPuffs(0.4f), DustEffects.LandPuffs(1.4f));
            // 着地の煙は、歩いたときの煙より大きい
            Assert.Greater(DustEffects.LandSize(0.3f), DustEffects.StepSize(0.3f));
        }

        [Test]
        public void 走るほど煙が横長にのびる()
        {
            Assert.Less(DustEffects.StepStretch(0.3f), DustEffects.StepStretch(1f));
            Assert.Greater(DustEffects.StepStretch(1f), 2f, "走ると、丸ではなく流れる形");
            Assert.GreaterOrEqual(DustEffects.StepStretch(0f), 1f, "歩いても、縮まない");
        }

        [Test]
        public void 煙はふくらんでからしぼむ()
        {
            Assert.Less(DustEffects.Swell(0f), DustEffects.Swell(0.2f));
            Assert.AreEqual(1f, DustEffects.Swell(0.2f), 1e-4f);
            Assert.Less(DustEffects.Swell(1f), DustEffects.Swell(0.5f));
            Assert.Greater(DustEffects.Swell(1f), 0f);
        }

        [Test]
        public void 煙はうすい()
        {
            Assert.LessOrEqual(DustEffects.Opacity, 0.5f);
        }
    }
}
