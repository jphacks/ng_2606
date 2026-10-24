using NUnit.Framework;

namespace DeskStage.Shared.Tests
{
    /// <summary>写真の方式で足した知らせと依頼（00_overview.md 9.3）。</summary>
    public class PhotoScanEventsTests
    {
        [Test]
        public void 答えのJSONがそのまま届く()
        {
            string received = null;
            void OnResult(string json) => received = json;
            ScanContext.ScanResultReceived += OnResult;
            try
            {
                ScanContext.RaiseScanResultReceived("{\"paperFound\":true}");
            }
            finally
            {
                ScanContext.ScanResultReceived -= OnResult;
            }

            Assert.That(received, Is.EqualTo("{\"paperFound\":true}"));
        }

        [Test]
        public void 写真を撮る依頼が届く()
        {
            var count = 0;
            void OnTakePhoto() => count++;
            UiRequests.TakePhoto += OnTakePhoto;
            try
            {
                UiRequests.RequestTakePhoto();
                UiRequests.RequestTakePhoto();
            }
            finally
            {
                UiRequests.TakePhoto -= OnTakePhoto;
            }

            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void 受ける人がいなくても落ちない()
        {
            Assert.DoesNotThrow(() => ScanContext.RaiseScanResultReceived("{}"));
            Assert.DoesNotThrow(UiRequests.RequestTakePhoto);
        }
    }
}
