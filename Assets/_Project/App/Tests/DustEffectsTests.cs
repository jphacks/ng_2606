using NUnit.Framework;

namespace DeskStage.App.Tests
{
    /// <summary>足もとの土煙（DustEffects）の、数と大きさの決め方。</summary>
    public class DustEffectsTests
    {
        [Test]
        public void 歩くと小さく少なく走ると大きく多い()
        {
            Assert.AreEqual(1, DustEffects.StepPuffs(0.3f), "ゆっくり歩く");
            Assert.AreEqual(3, DustEffects.StepPuffs(1f), "走る");
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
    }
}
