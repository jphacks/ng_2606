using DeskStage.Shared;
using NUnit.Framework;

namespace DeskStage.UI.Tests
{
    public class GuidanceLogicTests
    {
        [TestCase(ScanStatus.Starting, ScanReason.None, "guide.starting")]
        [TestCase(ScanStatus.FindingDesk, ScanReason.None, "guide.findingDesk")]
        [TestCase(ScanStatus.Scanning, ScanReason.None, "guide.scanning")]
        [TestCase(ScanStatus.Limited, ScanReason.Dark, "guide.limitedDark")]
        [TestCase(ScanStatus.Limited, ScanReason.FastMotion, "guide.limitedFast")]
        [TestCase(ScanStatus.Limited, ScanReason.FewFeatures, "guide.limitedFew")]
        [TestCase(ScanStatus.Lost, ScanReason.None, "guide.lost")]
        [TestCase(ScanStatus.NotStarted, ScanReason.None, null)]
        [TestCase(ScanStatus.Finished, ScanReason.None, null)]
        public void 状態ごとの案内(ScanStatus status, ScanReason reason, string expected)
        {
            var logic = new GuidanceLogic();
            logic.SetStatus(status, reason);
            Assert.AreEqual(expected, logic.CurrentKey);
        }

        [Test]
        public void 机を探して10秒たつと案内が変わる()
        {
            var logic = new GuidanceLogic();
            logic.SetStatus(ScanStatus.FindingDesk, ScanReason.None);
            logic.Tick(9.9f);
            Assert.AreEqual("guide.findingDesk", logic.CurrentKey);
            logic.Tick(0.2f);
            Assert.AreEqual("guide.findingDeskLong", logic.CurrentKey);
        }

        [Test]
        public void 机の面が見つかったらタップをすすめる()
        {
            var logic = new GuidanceLogic();
            logic.SetStatus(ScanStatus.FindingDesk, ScanReason.None);
            logic.Tick(20f);
            logic.DeskFound = true;
            Assert.AreEqual("guide.deskFound", logic.CurrentKey);
        }

        [Test]
        public void 困った状態から戻ると前の案内を1秒残す()
        {
            var logic = new GuidanceLogic();
            logic.SetStatus(ScanStatus.Limited, ScanReason.Dark);
            logic.SetStatus(ScanStatus.Scanning, ScanReason.None);
            Assert.AreEqual("guide.limitedDark", logic.CurrentKey);
            logic.Tick(0.5f);
            Assert.AreEqual("guide.limitedDark", logic.CurrentKey);
            logic.Tick(0.6f);
            Assert.AreEqual("guide.scanning", logic.CurrentKey);
        }

        [Test]
        public void ふつうの切り替えはすぐ変わる()
        {
            var logic = new GuidanceLogic();
            logic.SetStatus(ScanStatus.Starting, ScanReason.None);
            logic.SetStatus(ScanStatus.FindingDesk, ScanReason.None);
            Assert.AreEqual("guide.findingDesk", logic.CurrentKey);
        }
    }
}
