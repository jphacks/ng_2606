using NUnit.Framework;

namespace DeskStage.AR.Tests
{
    /// <summary>ScanClient の小さな計算（A の Step 25）。通信そのものは Test_A_Photo で確かめる。</summary>
    public class ScanClientTests
    {
        [TestCase("http://127.0.0.1:8000", "http://127.0.0.1:8000/scan")]
        [TestCase("http://127.0.0.1:8000/", "http://127.0.0.1:8000/scan")]
        [TestCase(" http://192.168.0.10:8000 ", "http://192.168.0.10:8000/scan")]
        public void 読み取りの受け口のURLを作る(string server, string expected)
        {
            Assert.That(ScanClient.ScanUrl(server), Is.EqualTo(expected));
        }

        [TestCase("{\"paperFound\":false}", true)]
        [TestCase("  {}  ", true)]
        [TestCase("", false)]
        [TestCase(null, false)]
        [TestCase("<html>Bad Gateway</html>", false)]
        public void 答えがJSONのオブジェクトらしいか(string text, bool expected)
        {
            Assert.That(ScanClient.LooksLikeJsonObject(text), Is.EqualTo(expected));
        }
    }
}
