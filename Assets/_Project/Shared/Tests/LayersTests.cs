using NUnit.Framework;

namespace DeskStage.Shared.Tests
{
    /// <summary>
    /// レイヤー番号が 00_overview.md 3.2 の表と合っていることを確かめる。
    /// ここがずれると、キャラが箱に乗れないなどの分かりにくい不具合になる。
    /// </summary>
    public class LayersTests
    {
        [Test]
        public void レイヤー番号が手順書と一致する()
        {
            Assert.AreEqual(8, Layers.Terrain);
            Assert.AreEqual(9, Layers.ScanView);
            Assert.AreEqual(10, Layers.Player);
            Assert.AreEqual(11, Layers.Gimmick);
        }

        [Test]
        public void TerrainMaskはTerrainだけを指す()
        {
            Assert.AreEqual(1 << Layers.Terrain, Layers.TerrainMask);
            Assert.AreNotEqual(0, Layers.TerrainMask & (1 << Layers.Terrain));
            Assert.AreEqual(0, Layers.TerrainMask & (1 << Layers.Player));
            Assert.AreEqual(0, Layers.TerrainMask & (1 << Layers.ScanView));
        }
    }
}
