using NUnit.Framework;

namespace DeskStage.App.Tests
{
    /// <summary>たしかめの画面の、水の流れの向きの言葉（画面の上の角度から）。</summary>
    public class FlowWordTests
    {
        [Test]
        public void 画面の上の向きが_うえ_した_みぎ_ひだりの言葉になる()
        {
            Assert.AreEqual(AppText.FlowRight, CheckStageScreen.FlowWord(10f));
            Assert.AreEqual(AppText.FlowUp, CheckStageScreen.FlowWord(80f));
            Assert.AreEqual(AppText.FlowLeft, CheckStageScreen.FlowWord(179f));
            Assert.AreEqual(AppText.FlowLeft, CheckStageScreen.FlowWord(-170f));
            Assert.AreEqual(AppText.FlowDown, CheckStageScreen.FlowWord(-90f));
            Assert.AreEqual(AppText.FlowRight, CheckStageScreen.FlowWord(370f), "1 周より大きくても");
        }
    }
}
