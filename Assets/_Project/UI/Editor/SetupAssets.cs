using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace DeskStage.UI.Editor
{
    /// <summary>
    /// D のアセットを作るメニュー（Step 1〜3）。
    /// 日本語フォントのアセット、入力アセットの C# クラス設定、文言のアセット。
    /// </summary>
    public static class SetupAssets
    {
        public const string FontFolder = "Assets/_Project/UI/Fonts";
        public const string FontSourcePath = FontFolder + "/NotoSansJP-Regular.otf";
        const string FontAssetPath = FontFolder + "/NotoSansJP-Regular SDF.asset";

        const string InputPath = "Assets/_Project/Shared/Input/DeskStageControls.inputactions";
        const string InputCodePath = "Assets/_Project/Shared/Input/DeskStageControls.cs";

        const string WordingDocPath = "Docs/Wording.md";
        public const string WordingAssetPath = "Assets/_Project/UI/Wording.asset";

        const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        const string TmpEssentialsPackage = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        /// <summary>
        /// バッチモードから3つをまとめて作る。
        /// TMP Essentials がないときは取り込みだけをして終わるので、もう一度実行する。
        /// </summary>
        public static void RunBatch()
        {
            if (ImportTmpEssentialsIfMissing())
            {
                Debug.Log("TMP Essentials を取り込みました。もう一度実行してください。");
                return;
            }
            CreateJapaneseFont();
            ConfigureInputAsset();
            ImportWording();
            CreateViewGestureSettings();
            CreateLookAssets.Run();
            CreateTipsAssets.Run();
        }

        public const string ViewGestureSettingsPath = "Assets/_Project/UI/ViewGestureSettings.asset";

        /// <summary>視点の操作の設定アセットを作る（Step 4）。もうあれば何もしない（調整した値を消さない）。</summary>
        [MenuItem("DeskStage/D/視点の操作の設定を作る")]
        public static void CreateViewGestureSettings()
        {
            if (AssetDatabase.LoadAssetAtPath<ViewGestureSettings>(ViewGestureSettingsPath) != null)
            {
                return;
            }
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ViewGestureSettings>(), ViewGestureSettingsPath);
            AssetDatabase.SaveAssets();
            Debug.Log("視点の操作の設定を作りました: " + ViewGestureSettingsPath);
        }

        /// <summary>
        /// バッチモード用：TMP Essentials を取り込み、終わったらエディタを閉じる。
        /// 取り込みは後から進むので、-quit を付けずに呼ぶ。
        /// </summary>
        public static void ImportTmpEssentialsBatch()
        {
            if (File.Exists(TmpSettingsPath))
            {
                EditorApplication.Exit(0);
                return;
            }
            AssetDatabase.importPackageCompleted += _ => EditorApplication.Exit(0);
            AssetDatabase.importPackageFailed += (_, error) =>
            {
                Debug.LogError("TMP Essentials を取り込めませんでした: " + error);
                EditorApplication.Exit(1);
            };
            AssetDatabase.ImportPackage(TmpEssentialsPackage, false);
        }

        /// <summary>TextMeshPro の基本ファイル（TMP Settings など）がなければ取り込む。取り込んだら true。</summary>
        static bool ImportTmpEssentialsIfMissing()
        {
            if (File.Exists(TmpSettingsPath))
            {
                return false;
            }
            AssetDatabase.ImportPackage(TmpEssentialsPackage, false);
            return true;
        }

        [MenuItem("DeskStage/D/日本語フォントを作る")]
        public static void CreateJapaneseFont()
        {
            if (ImportTmpEssentialsIfMissing())
            {
                Debug.Log("TMP Essentials を取り込みました。取り込みが終わったら、もう一度このメニューを実行してください。");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath) != null)
            {
                Debug.Log("日本語フォントのアセットはもうあります: " + FontAssetPath);
                return;
            }
            var source = AssetDatabase.LoadAssetAtPath<Font>(FontSourcePath);
            if (source == null)
            {
                Debug.LogError("フォントのファイルがありません: " + FontSourcePath);
                return;
            }

            // 動的：使った文字だけをその場でアトラスに足す（ひらがな・漢字を全部入れなくてよい）
            var fontAsset = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            fontAsset.name = Path.GetFileNameWithoutExtension(FontAssetPath);
            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

            // アトラスの画像と素材を同じファイルの中に入れる
            fontAsset.atlasTextures[0].name = fontAsset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            fontAsset.material.name = fontAsset.name + " Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            Debug.Log("日本語フォントのアセットを作りました: " + FontAssetPath);
        }

        [MenuItem("DeskStage/D/入力アセットの設定をする")]
        public static void ConfigureInputAsset()
        {
            // 00_overview.md 4.7：C# クラスを作る設定。クラス名 DeskStageControls、名前空間 DeskStage.Shared
            var importer = AssetImporter.GetAtPath(InputPath);
            if (importer == null)
            {
                Debug.LogError("入力アセットがありません: " + InputPath);
                return;
            }
            var so = new SerializedObject(importer);
            var generate = so.FindProperty("m_GenerateWrapperCode");
            var path = so.FindProperty("m_WrapperCodePath");
            var className = so.FindProperty("m_WrapperClassName");
            var ns = so.FindProperty("m_WrapperCodeNamespace");
            if (generate == null || path == null || className == null || ns == null)
            {
                Debug.LogError("入力アセットの設定の項目が見つかりません。Inspector で Generate C# Class を手で設定してください。");
                return;
            }
            generate.boolValue = true;
            path.stringValue = InputCodePath;
            className.stringValue = "DeskStageControls";
            ns.stringValue = "DeskStage.Shared";
            so.ApplyModifiedPropertiesWithoutUndo();
            importer.SaveAndReimport();
            Debug.Log("入力アセットに C# クラスを作る設定をしました: " + InputCodePath);
        }

        [MenuItem("DeskStage/D/文言の表を読み込む")]
        public static void ImportWording()
        {
            if (!File.Exists(WordingDocPath))
            {
                Debug.LogError("文言の表がありません: " + WordingDocPath);
                return;
            }
            var entries = WordingTable.Parse(File.ReadAllText(WordingDocPath));

            var wording = AssetDatabase.LoadAssetAtPath<Wording>(WordingAssetPath);
            if (wording == null)
            {
                wording = ScriptableObject.CreateInstance<Wording>();
                AssetDatabase.CreateAsset(wording, WordingAssetPath);
            }
            wording.SetEntries(entries);
            EditorUtility.SetDirty(wording);
            AssetDatabase.SaveAssets();
            Debug.Log("文言の表を読み込みました: " + entries.Count + " 件");
        }
    }
}
