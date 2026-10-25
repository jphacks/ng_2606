using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeskStage.AR.EditorTools
{
    /// <summary>
    /// 合流用の AR_System プレハブを作る（A_ar.md Step 14）。
    /// 写真方式（00_overview.md 9章）の部品だけを入れる：カメラと撮影（PhotoCapture）、サーバーとの送受信（ScanClient）。
    /// AR Session、XR Origin、面の検出（PlaneWatcher など）は、写真方式では使わないので入れない。
    /// サーバーの設定（ServerConfig）は Git に入らないので参照せず、ScanClient が実行時に Resources から探す。
    /// </summary>
    public static class CreateArSystemPrefab
    {
        public const string PrefabPath = "Assets/_Project/AR/Prefabs/AR_System.prefab";
        const string SettingsPath = "Assets/_Project/AR/Settings/ScanSettings.asset";

        [MenuItem("DeskStage/A/6. AR_System プレハブを作る")]
        public static void Run()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

            var root = new GameObject("AR_System");
            try
            {
                var capture = root.AddComponent<PhotoCapture>();
                var captureSo = new SerializedObject(capture);
                captureSo.FindProperty("settings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ScanSettings>(SettingsPath);
                captureSo.ApplyModifiedPropertiesWithoutUndo();

                var client = root.AddComponent<ScanClient>();
                var clientSo = new SerializedObject(client);
                clientSo.FindProperty("capture").objectReferenceValue = capture;
                clientSo.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            Debug.Log($"[Merge] 作りました：{PrefabPath}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
