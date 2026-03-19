using System.IO;
using System.Linq;
using System.Reflection;
using LineworkLite.Common.Utils;
using LineworkLite.FreeOutline;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class LineworkLiteOutlineInstaller
{
    private const string RendererPath = "Assets/_Project/Settings/PC_Renderer.asset";
    private const string SettingsDirectory = "Assets/_Project/Settings";
    private const string SettingsPath = SettingsDirectory + "/InteractableFreeOutlineSettings.asset";
    private const uint HighlightRenderingLayerMask = 1u << 1;

    static LineworkLiteOutlineInstaller()
    {
        EditorApplication.delayCall += EnsureInstalled;
    }

    [MenuItem("Tools/OpenFPS/Install Linework Lite Outline")]
    public static void EnsureInstalled()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (rendererData == null)
        {
            return;
        }

        var settings = EnsureSettingsAsset();
        var feature = EnsureRendererFeature(rendererData, settings);

        if (feature == null)
        {
            return;
        }

        feature.Create();
        EditorUtility.SetDirty(feature);
        EditorUtility.SetDirty(settings);
        EditorUtility.SetDirty(rendererData);
        AssetDatabase.SaveAssets();

        Debug.Log("Linework Lite Free Outline configured on PC_Renderer for interactable highlights.");
    }

    private static FreeOutlineSettings EnsureSettingsAsset()
    {
        var settings = AssetDatabase.LoadAssetAtPath<FreeOutlineSettings>(SettingsPath);
        if (settings == null)
        {
            if (!AssetDatabase.IsValidFolder(SettingsDirectory))
            {
                Directory.CreateDirectory(SettingsDirectory);
                AssetDatabase.Refresh();
            }

            settings = ScriptableObject.CreateInstance<FreeOutlineSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }

        ConfigureSettings(settings);
        return settings;
    }

    private static void ConfigureSettings(FreeOutlineSettings settings)
    {
        var serializedSettings = new SerializedObject(settings);
        serializedSettings.FindProperty("injectionPoint").enumValueIndex = (int)InjectionPoint.AfterRenderingPostProcessing;
        serializedSettings.FindProperty("showInSceneView").boolValue = true;

        var outlinesProperty = serializedSettings.FindProperty("outlines");
        if (outlinesProperty.arraySize == 0)
        {
            var outline = ScriptableObject.CreateInstance<Outline>();
            outline.name = "Interactable Outline";
            AssetDatabase.AddObjectToAsset(outline, settings);
            outlinesProperty.InsertArrayElementAtIndex(0);
            outlinesProperty.GetArrayElementAtIndex(0).objectReferenceValue = outline;
        }

        serializedSettings.ApplyModifiedPropertiesWithoutUndo();

        var outlineAsset = settings.Outlines.FirstOrDefault();
        if (outlineAsset == null)
        {
            return;
        }

        outlineAsset.SetActive(true);
        outlineAsset.RenderingLayer = HighlightRenderingLayerMask;
        outlineAsset.layerMask = ~0;
        outlineAsset.renderQueue = OutlineRenderQueue.OpaqueAndTransparent;
        outlineAsset.occlusion = Occlusion.WhenNotOccluded;
        outlineAsset.maskingStrategy = MaskingStrategy.Stencil;
        outlineAsset.color = new Color(1f, 0.92f, 0.35f, 1f);
        outlineAsset.enableOcclusion = false;
        outlineAsset.occludedColor = outlineAsset.color;
        outlineAsset.blendMode = BlendingMode.Alpha;
        outlineAsset.extrusionMethod = ExtrusionMethod.ClipSpaceNormalVector;
        outlineAsset.scaling = Scaling.ConstantScreenSize;
        outlineAsset.width = 6f;
        outlineAsset.minWidth = 0f;
        outlineAsset.scaleWithResolution = true;
        outlineAsset.referenceResolution = LineworkLite.FreeOutline.Resolution._1080;
        outlineAsset.customResolution = 1080f;
        outlineAsset.materialType = MaterialType.Basic;
        outlineAsset.customMaterial = null;
        outlineAsset.hideFlags = HideFlags.HideInHierarchy;

        EditorUtility.SetDirty(outlineAsset);
        settings.Changed();
    }

    private static FreeOutline EnsureRendererFeature(UniversalRendererData rendererData, FreeOutlineSettings settings)
    {
        var existingFeature = rendererData
            .rendererFeatures
            .OfType<FreeOutline>()
            .FirstOrDefault();

        if (existingFeature != null)
        {
            AssignSettings(existingFeature, settings);
            return existingFeature;
        }

        var feature = ScriptableObject.CreateInstance<FreeOutline>();
        feature.name = "FreeOutline";
        AssetDatabase.AddObjectToAsset(feature, rendererData);
        AssignSettings(feature, settings);

        var serializedRenderer = new SerializedObject(rendererData);
        var rendererFeatures = serializedRenderer.FindProperty("m_RendererFeatures");
        rendererFeatures.InsertArrayElementAtIndex(rendererFeatures.arraySize);
        rendererFeatures.GetArrayElementAtIndex(rendererFeatures.arraySize - 1).objectReferenceValue = feature;
        serializedRenderer.ApplyModifiedPropertiesWithoutUndo();

        var onValidateMethod = typeof(ScriptableRendererData).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);
        onValidateMethod?.Invoke(rendererData, null);

        return feature;
    }

    private static void AssignSettings(FreeOutline feature, FreeOutlineSettings settings)
    {
        var serializedFeature = new SerializedObject(feature);
        serializedFeature.FindProperty("settings").objectReferenceValue = settings;
        serializedFeature.ApplyModifiedPropertiesWithoutUndo();
    }
}