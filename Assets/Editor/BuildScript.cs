using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;

public static class BuildScript
{
    public static void BuildIOS()
    {
        Debug.Log("Starting iOS build...");

        EnsureXRSettings();

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/ARPiano.unity" },
            locationPathName = "Build/iOS",
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log($"Build succeeded: {report.summary.totalSize} bytes");
        else
        {
            Debug.LogError($"Build failed: {report.summary.totalErrors} errors");
            System.Environment.Exit(1);
        }
    }

    static void EnsureXRSettings()
    {
        var buildTarget = BuildTargetGroup.iOS;

        // Find the per-build-target settings asset
        XRGeneralSettingsPerBuildTarget perBuildTarget = null;
        EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out perBuildTarget);
        if (perBuildTarget == null)
        {
            var guids = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                perBuildTarget = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(path);
                if (perBuildTarget != null)
                    EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perBuildTarget, true);
            }
        }
        Debug.Log($"[VRPiano] PerBuildTarget asset: {perBuildTarget != null}");

        // Check if iOS settings exist
        var settings = perBuildTarget.SettingsForBuildTarget(buildTarget);
        if (settings == null)
        {
            Debug.Log("[VRPiano] Creating new XRGeneralSettings for iOS...");
            perBuildTarget.CreateDefaultSettingsForBuildTarget(buildTarget);
            settings = perBuildTarget.SettingsForBuildTarget(buildTarget);
        }
        Debug.Log($"[VRPiano] iOS XRGeneralSettings: {settings != null}");

        if (settings == null)
        {
            Debug.LogError("[VRPiano] Failed to create XRGeneralSettings for iOS!");
            return;
        }

        // Ensure XRManagerSettings exists
        if (settings.Manager == null)
        {
            Debug.Log("[VRPiano] Creating XRManagerSettings...");
            perBuildTarget.CreateDefaultManagerSettingsForBuildTarget(buildTarget);
        }

        var manager = settings.Manager;
        Debug.Log($"[VRPiano] XRManagerSettings: {manager != null}, loaders: {manager?.activeLoaders?.Count ?? 0}");

        // Ensure ARKit loader is assigned
        if (manager != null && manager.activeLoaders.Count == 0)
        {
            Debug.Log("[VRPiano] Adding ARKit loader...");
            var loaderGuids = AssetDatabase.FindAssets("t:UnityEngine.XR.ARKit.ARKitLoader");
            if (loaderGuids.Length > 0)
            {
                var loaderPath = AssetDatabase.GUIDToAssetPath(loaderGuids[0]);
                var loader = AssetDatabase.LoadAssetAtPath<XRLoader>(loaderPath);
                if (loader != null)
                {
                    manager.TryAddLoader(loader);
                    Debug.Log($"[VRPiano] Added ARKit loader: {loader.name}");
                }
            }
            else
            {
                Debug.LogError("[VRPiano] ARKitLoader asset not found!");
            }
        }

        settings.InitManagerOnStart = false;
        EditorUtility.SetDirty(perBuildTarget);
        EditorUtility.SetDirty(settings);
        if (manager != null) EditorUtility.SetDirty(manager);
        AssetDatabase.SaveAssets();

        // Add to preloaded assets
        var preloaded = PlayerSettings.GetPreloadedAssets().Where(x => x != null).ToList();
        if (!preloaded.Contains(settings))
        {
            preloaded.Add(settings);
            PlayerSettings.SetPreloadedAssets(preloaded.ToArray());
            Debug.Log("[VRPiano] Added XRGeneralSettings to preloaded assets");
        }

        Debug.Log($"[VRPiano] Final state - Settings: {settings.name}, Manager: {manager?.name}, Loaders: {manager?.activeLoaders?.Count}, InitOnStart: {settings.InitManagerOnStart}");
    }
}
