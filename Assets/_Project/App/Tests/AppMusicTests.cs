using NUnit.Framework;
using UnityEngine;

namespace DeskStage.App.Tests
{
    /// <summary>スタート画面の BGM（AppMusic）の曲のデータ。候補の曲は、どれも同じ決まりで作る。</summary>
    public class AppMusicTests
    {
        [Test]
        public void どの曲もメロディーは8小節ぶんで和音も8つ()
        {
            foreach (var song in AppMusic.Songs)
            {
                var beats = 0f;
                foreach (var (_, b) in song.Melody) beats += b;
                Assert.AreEqual(AppMusic.Bars * 4f, beats, 1e-4f, song.Name);
                Assert.AreEqual(AppMusic.Bars, song.Chords.Length, song.Name);
            }
        }

        [Test]
        public void どの曲もちょうど8小節で音が割れない()
        {
            foreach (var song in AppMusic.Songs)
            {
                var data = AppMusic.Samples(song);
                var seconds = 60f / song.Bpm * 4f * AppMusic.Bars;
                Assert.AreEqual(Mathf.RoundToInt(22050 * seconds), data.Length, song.Name);
                var peak = 0f;
                foreach (var v in data) peak = Mathf.Max(peak, Mathf.Abs(v));
                Assert.LessOrEqual(peak, 0.951f, song.Name);
                Assert.Greater(peak, 0.1f, song.Name + "：音が鳴っている");
            }
        }

        [Test]
        public void くり返しのつなぎ目で音が途切れない()
        {
            // 曲の頭と終わりのあたりも、音が鳴っている（響きを頭に重ねているので、無音にならない）
            foreach (var song in AppMusic.Songs)
            {
                var data = AppMusic.Samples(song);
                float Loudness(int from, int count)
                {
                    var sum = 0f;
                    for (var i = 0; i < count; i++) sum += Mathf.Abs(data[(from + i) % data.Length]);
                    return sum / count;
                }
                Assert.Greater(Loudness(0, 2205), 0.01f, song.Name + "：頭の 0.1 秒");
                Assert.Greater(Loudness(data.Length - 2205, 2205), 0.01f, song.Name + "：終わりの 0.1 秒");
            }
        }
    }
}
