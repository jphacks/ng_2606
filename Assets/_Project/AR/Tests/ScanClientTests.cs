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

        [TestCase("https://scan.example.com/scan", false, true)]
        [TestCase("http://127.0.0.1:8000/scan", false, true)]
        [TestCase("http://localhost:8000/scan", false, true)]
        [TestCase("http://192.168.0.10:8000/scan", true, true)]
        [TestCase("http://10.0.0.5:8000/scan", true, true)]
        [TestCase("http://172.20.1.2:8000/scan", true, true)]
        [TestCase("http://192.168.0.10:8000/scan", false, false)]
        [TestCase("http://scan.example.com/scan", true, false)]
        [TestCase("http://172.40.1.2:8000/scan", true, false)]
        [TestCase("ftp://127.0.0.1/scan", true, false)]
        [TestCase("not a url", true, false)]
        public void 暗号化なしで外に送らない(string url, bool allowLanHttp, bool expected)
        {
            Assert.That(ScanClient.IsAllowedUrl(url, allowLanHttp, out var reason), Is.EqualTo(expected));
            Assert.That(reason.Length > 0, Is.EqualTo(!expected), "断るときは理由がある");
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
