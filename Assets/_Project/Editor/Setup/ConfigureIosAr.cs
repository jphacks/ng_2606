using System.Linq;
using UnityEditor;
using UnityEditor.XR.ARKit;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;

namespace DeskStage.EditorTools
{
    /// <summary>
    /// 全体 Step 1 の4〜7：iOS への切り替え、ARKit の有効化と Required、
    /// AR Background Renderer Feature の追加。
    /// 7 を忘れるとカメラ映像が真っ黒になるので、手作業ではなくここで入れる。
    /// </summary>
    public static class ConfigureIosAr
    {
        const string ArKitLoader = "UnityEngine.XR.ARKit.ARKitLoader";
        const string MobileRendererPath = "Assets/Settings/Mobile_Renderer.asset";
        const string XrFolder = "Assets/XR";
        const string XrSettingsPath = XrFolder + "/XRGeneralSettingsPerBuildTarget.asset";

        [MenuItem("DeskStage/A/2. iOS と AR の設定を入れる")]
        public static void Run()
        {
            var ok = true;
            ok &= SwitchToIos();
            ok &= EnableArKit();
            ok &= SetArKitRequired();
            ok &= AddBackgroundRendererFeature();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(ok ? "[Setup] iOS と AR の設定が入りました" : "[Setup] 入らなかった項目があります");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(ok ? 0 : 1);
            }
        }

        static bool SwitchToIos()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                Debug.LogError("[Setup] iOS への切り替えに失敗しました");
                return false;
            }
            Debug.Log("[Setup] ビルド対象: iOS");
            return true;
        }

        /// <summary>
        /// XR Management の GetOrCreate は internal なので、同じことをここで行う。
        /// </summary>
        static XRGeneralSettingsPerBuildTarget GetOrCreatePerBuildTarget()
        {
            EditorBuildSettings.TryGetConfigObject(
                XRGeneralSettings.settingsKey, out XRGeneralSettingsPerBuildTarget perTarget);

            if (perTarget == null)
            {
                var found = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
                if (found.Length > 0)
                {
                    perTarget = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(
                        AssetDatabase.GUIDToAssetPath(found[0]));
                }
            }

            if (perTarget == null)
            {
                if (!AssetDatabase.IsValidFolder(XrFolder))
                {
                    AssetDatabase.CreateFolder("Assets", "XR");
                }
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, XrSettingsPath);
                AssetDatabase.SaveAssets();
            }

            EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, perTarget, true);
            return perTarget;
        }

        static bool EnableArKit()
        {
            var perTarget = GetOrCreatePerBuildTarget();
            if (perTarget == null)
            {
                Debug.LogError("[Setup] XR の設定を作れませんでした");
                return false;
            }

            if (!perTarget.HasSettingsForBuildTarget(BuildTargetGroup.iOS))
            {
                perTarget.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.iOS);
            }
            if (!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.iOS))
            {
                perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.iOS);
            }

            var settings = perTarget.SettingsForBuildTarget(BuildTargetGroup.iOS);
            if (settings == null || settings.Manager == null)
            {
                Debug.LogError("[Setup] iOS 用の XR 設定が作れませんでした");
                return false;
            }

            if (!XRPackageMetadataStore.AssignLoader(settings.Manager, ArKitLoader, BuildTargetGroup.iOS))
            {
                Debug.LogError("[Setup] ARKit の有効化に失敗しました");
                return false;
            }

            EditorUtility.SetDirty(settings.Manager);
            EditorUtility.SetDirty(perTarget);
            Debug.Log("[Setup] ARKit を有効にしました");
            return true;
        }

        static bool SetArKitRequired()
        {
            var arkit = ARKitSettings.GetOrCreateSettings();
            if (arkit == null)
            {
                Debug.LogError("[Setup] ARKit の設定を作れませんでした");
                return false;
            }
            arkit.requirement = ARKitSettings.Requirement.Required;
            EditorUtility.SetDirty(arkit);
            Debug.Log("[Setup] ARKit の Requirement: Required");
            return true;
        }

        static bool AddBackgroundRendererFeature()
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(MobileRendererPath);
            if (rendererData == null)
            {
                Debug.LogError("[Setup] " + MobileRendererPath + " が見つかりません");
                return false;
            }

            if (rendererData.rendererFeatures.Any(f => f is ARBackgroundRendererFeature))
            {
                Debug.Log("[Setup] AR Background Renderer Feature は既に入っています");
                return true;
            }

            var feature = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();
            feature.name = "AR Background Renderer Feature";
            AssetDatabase.AddObjectToAsset(feature, rendererData);
            AssetDatabase.SaveAssets();

            var serialized = new SerializedObject(rendererData);
            var features = serialized.FindProperty("m_RendererFeatures");
            var featureMap = serialized.FindProperty("m_RendererFeatureMap");

            features.arraySize++;
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;

            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);
            featureMap.arraySize++;
            featureMap.GetArrayElementAtIndex(featureMap.arraySize - 1).longValue = localId;

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(rendererData);

            Debug.Log("[Setup] AR Background Renderer Feature を Mobile_Renderer に追加しました");
            return true;
        }
    }
}
