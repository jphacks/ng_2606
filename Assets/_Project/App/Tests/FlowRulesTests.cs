using DeskStage.Shared;
using NUnit.Framework;

namespace DeskStage.App.Tests
{
    /// <summary>FlowRules：使うときの流れの決まり。</summary>
    public class FlowRulesTests
    {
        [TestCase(AppScreenId.Title, AppMode.Menu)]
        [TestCase(AppScreenId.Home, AppMode.Menu)]
        [TestCase(AppScreenId.HowTo, AppMode.Menu)]
        [TestCase(AppScreenId.Characters, AppMode.Menu)]
        [TestCase(AppScreenId.Backdrop, AppMode.Menu)]
        [TestCase(AppScreenId.Capture, AppMode.Scan)]
        [TestCase(AppScreenId.Generating, AppMode.Scan)]
        [TestCase(AppScreenId.Check, AppMode.Check)]
        [TestCase(AppScreenId.Play, AppMode.Play)]
        [TestCase(AppScreenId.Clear, AppMode.Play)]
        public void 画面に対応する場面(AppScreenId screen, AppMode expected)
        {
            Assert.AreEqual(expected, FlowRules.ModeFor(screen));
        }

        [Test]
        public void すべての画面に場面が決まっている()
        {
            foreach (AppScreenId id in System.Enum.GetValues(typeof(AppScreenId)))
            {
                Assert.DoesNotThrow(() => FlowRules.ModeFor(id));
            }
        }

        [TestCase(false, false, ScanStatus.NotStarted, CaptureStep.Preparing)]
        [TestCase(false, false, ScanStatus.Starting, CaptureStep.Preparing)]
        [TestCase(false, false, ScanStatus.FindingDesk, CaptureStep.Top)]
        [TestCase(true, false, ScanStatus.Scanning, CaptureStep.Side)]
        [TestCase(true, true, ScanStatus.Scanning, CaptureStep.Sending)]
        [TestCase(false, false, ScanStatus.Limited, CaptureStep.Top)]
        [TestCase(true, false, ScanStatus.Limited, CaptureStep.Side)]
        [TestCase(true, true, ScanStatus.Lost, CaptureStep.Failed)]
        [TestCase(false, false, ScanStatus.Lost, CaptureStep.Failed)]
        public void 撮影の状態(bool top, bool side, ScanStatus status, CaptureStep expected)
        {
            Assert.AreEqual(expected, FlowRules.CaptureStepFor(top, side, status));
        }

        [Test]
        public void あそべるのは_スタートとゴールがあり箱があるとき()
        {
            Assert.IsNull(FlowRules.CannotPlayReason(true, true, 3, true));
        }

        [Test]
        public void スタートかゴールが無いとあそべない()
        {
            Assert.AreEqual(AppText.CheckNeedCourse, FlowRules.CannotPlayReason(false, true, 3, true));
            Assert.AreEqual(AppText.CheckNeedCourse, FlowRules.CannotPlayReason(true, false, 3, true));
            Assert.AreEqual(AppText.CheckNeedCourse, FlowRules.CannotPlayReason(false, false, 3, true));
        }

        [Test]
        public void 箱が無い_ステージができていないとあそべない()
        {
            Assert.AreEqual(AppText.ErrorNoBoxes, FlowRules.CannotPlayReason(true, true, 0, true));
            Assert.AreEqual(AppText.ErrorNoBoxes, FlowRules.CannotPlayReason(true, true, 3, false));
        }

        [TestCase(0, true, false)]
        [TestCase(3, false, false)]
        [TestCase(3, true, true)]
        public void たしかめへ進めるのは_箱ができ組み立ったとき(int boxes, bool ready, bool expected)
        {
            Assert.AreEqual(expected, FlowRules.CanEnterCheck(boxes, ready));
        }

        [Test]
        public void カメラが許可されていなければ_カメラの失敗()
        {
            Assert.AreEqual(FailureKind.CameraDenied, FlowRules.ClassifyFailure(true, false, ""));
        }

        [Test]
        public void 写真を送ったあとの失敗は_メッセージから原因を決める()
        {
            Assert.AreEqual(FailureKind.Timeout, FlowRules.ClassifyFailure(false, true, "時間がかかりすぎました"));
            Assert.AreEqual(FailureKind.Network, FlowRules.ClassifyFailure(false, true, "サーバーにつながらない"));
            Assert.AreEqual(FailureKind.Server, FlowRules.ClassifyFailure(false, true, "サーバーが断りました（401）"));
            Assert.AreEqual(FailureKind.Server, FlowRules.ClassifyFailure(false, true, null));
        }

        [Test]
        public void 写真を送る前の失敗で_カメラ以外なら不明()
        {
            Assert.AreEqual(FailureKind.Unknown, FlowRules.ClassifyFailure(false, false, ""));
        }

        [Test]
        public void すべての失敗に子ども向けの文がある()
        {
            foreach (FailureKind kind in System.Enum.GetValues(typeof(FailureKind)))
            {
                Assert.IsNotEmpty(FlowRules.FailureText(kind));
            }
        }

        [TestCase(0f, "0:00.0")]
        [TestCase(5.04f, "0:05.0")]
        [TestCase(83.4f, "1:23.4")]
        [TestCase(600f, "10:00.0")]
        [TestCase(-3f, "0:00.0")]
        public void タイムの表し方(float seconds, string expected)
        {
            Assert.AreEqual(expected, FlowRules.FormatTime(seconds));
        }

        [Test]
        public void ステージの名前はいまの数に1を足す()
        {
            Assert.AreEqual("ステージ 1", FlowRules.DefaultStageName(0));
            Assert.AreEqual("ステージ 4", FlowRules.DefaultStageName(3));
        }
    }
}
