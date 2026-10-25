using UnityEditor;

namespace DeskStage.App.EditorTools
{
    /// <summary>
    /// App/Art の写真（タイトルの写真など）の取り込み設定。縦横の比が変わらないよう、2 のべきの大きさに伸ばさない（NPOT Scale: None）。
    /// 標準のままだと、1600 x 840 が 2048 x 1024 に伸ばされて、写真がゆがむ。
    /// </summary>
    public class TitleImageImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/_Project/App/Art/"))
            {
                return;
            }
            var importer = (TextureImporter)assetImporter;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }
}
