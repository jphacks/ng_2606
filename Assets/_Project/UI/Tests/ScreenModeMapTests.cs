using DeskStage.Shared;
using NUnit.Framework;

namespace DeskStage.UI.Tests
{
    public class ScreenModeMapTests
    {
        [TestCase(ScreenId.Title, AppMode.Menu)]
        [TestCase(ScreenId.List, AppMode.Menu)]
        [TestCase(ScreenId.Tips, AppMode.Menu)]
        [TestCase(ScreenId.Scan, AppMode.Scan)]
        [TestCase(ScreenId.Check, AppMode.Check)]
        [TestCase(ScreenId.Play, AppMode.Play)]
        [TestCase(ScreenId.Clear, AppMode.Play)]
        public void 画面に対応する場面を返す(ScreenId id, AppMode expected)
        {
            Assert.AreEqual(expected, ScreenModeMap.ToMode(id));
        }
    }
}
