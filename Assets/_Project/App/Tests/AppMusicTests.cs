using NUnit.Framework;
using UnityEngine;

namespace DeskStage.App.Tests
{
    /// <summary>スタート画面の BGM（AppMusic）の曲のデータ。</summary>
    public class AppMusicTests
    {
        [Test]
        public void 曲はちょうど8小節で音が割れない()
        {
            var data = AppMusic.Samples();
            // 4 拍子で、AppMusic.Bars 小節
            var seconds = 60f / AppMusic.Bpm * 4f * AppMusic.Bars;
            Assert.AreEqual(Mathf.RoundToInt(22050 * seconds), data.Length);
            var peak = 0f;
            foreach (var v in data) peak = Mathf.Max(peak, Mathf.Abs(v));
            Assert.LessOrEqual(peak, 0.951f);
            Assert.Greater(peak, 0.1f, "音が鳴っている");
        }

        [Test]
        public void くり返しのつなぎ目で音が途切れない()
        {
            // 曲の頭と終わりのあたりも、音が鳴っている（響きを頭に重ねているので、無音にならない）
            var data = AppMusic.Samples();
            float Loudness(int from, int count)
            {
                var sum = 0f;
                for (var i = 0; i < count; i++) sum += Mathf.Abs(data[(from + i) % data.Length]);
                return sum / count;
            }
            Assert.Greater(Loudness(0, 2205), 0.01f, "頭の 0.1 秒");
            Assert.Greater(Loudness(data.Length - 2205, 2205), 0.01f, "終わりの 0.1 秒");
        }
    }
}
