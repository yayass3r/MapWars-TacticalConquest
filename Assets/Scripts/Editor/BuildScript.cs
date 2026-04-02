// ===================================================================
// Map Wars: Tactical Conquest - CI/CD Build Script
// Description: Static build method called by game-ci/unity-builder
//              GitHub Action for automated APK generation.
// ===================================================================

using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MapWars.Editor
{
    /// <summary>
    /// Build script for CI/CD pipeline.
    /// Called by game-ci/unity-builder GitHub Action.
    /// Usage: -buildMethod MapWars.Editor.BuildScript.BuildAndroid
    /// </summary>
    public static class BuildScript
    {
        /// <summary>
        /// Builds the Android APK with production settings.
        /// Called by the GitHub Actions CI/CD pipeline.
        /// </summary>
        /// <returns>0 for success, 1 for failure</returns>
        public static int BuildAndroid()
        {
            Debug.Log("[BuildScript] Starting Android build...");

            string[] scenes = EditorBuildSettings.scenes;
            if (scenes.Length == 0)
            {
                Debug.LogError("[BuildScript] No scenes in build settings!");
                return 1;
            }

            // Configure player settings for Android
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,
                "com.mapwarsstudio.tacticalconquest");
            PlayerSettings.productName = "Map Wars";
            PlayerSettings.companyName = "MapWarsStudio";
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // Performance settings
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,
                ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android,
                ManagedStrippingLevel.High);
            PlayerSettings.Android.useAPKExpansionFiles = false;

            // Build options
            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
                locationPathName = "build/MapWars.apk"
            };

            // Execute build
            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] Build SUCCESS!");
                Debug.Log($"[BuildScript] Size: {report.summary.totalSize / (1024 * 1024):F1} MB");
                Debug.Log($"[BuildScript] Time: {report.summary.totalTime}");
                return 0;
            }
            else
            {
                Debug.LogError($"[BuildScript] Build FAILED: {report.summary.result}");
                foreach (var step in report.steps)
                {
                    if (!string.IsNullOrEmpty(step.messages))
                    {
                        foreach (var msg in step.messages)
                        {
                            if (msg.type == LogType.Error)
                            {
                                Debug.LogError($"  {msg.content}");
                            }
                        }
                    }
                }
                return 1;
            }
        }
    }
}
