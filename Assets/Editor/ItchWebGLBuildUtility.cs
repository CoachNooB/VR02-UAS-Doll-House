using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VR2026Recap.EditorTools
{
    public static class ItchWebGLBuildUtility
    {
        private const string OutputDirectory = "Builds/WebGL-Itch";
        private const string BuildsDirectory = "Builds";
        private const string FallbackProductName = "VR02_Kelompok1";
        private const string FallbackVersion = "dev";

        private static readonly string[] RecapScenePaths =
        {
            "Assets/Scenes/Main_Scene.unity",
        };

        [MenuItem("Build/Itch.io/Configure WebGL Player Settings")]
        public static void ConfigureWebGLPlayerSettings()
        {
            ApplyWebGLPlayerSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[ItchWebGLBuildUtility] WebGL player settings configured for itch.io.");
        }

        [MenuItem("Build/Itch.io/Use VR Recap Scenes")]
        public static void UseVRRecapScenes()
        {
            string[] existingScenes = RecapScenePaths
                .Where(File.Exists)
                .ToArray();

            if (existingScenes.Length == 0)
                throw new InvalidOperationException("No VR recap scenes were found under Assets/Scenes.");

            EditorBuildSettings.scenes = existingScenes
                .Select(path => new EditorBuildSettingsScene(path, enabled: true))
                .ToArray();

            AssetDatabase.SaveAssets();
            Debug.Log($"[ItchWebGLBuildUtility] Build scenes configured: {string.Join(", ", existingScenes)}");
        }

        [MenuItem("Build/Itch.io/Build WebGL Zip")]
        public static void BuildWebGLZip()
        {
            ApplyWebGLPlayerSettings();

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                throw new InvalidOperationException("Failed to switch active build target to WebGL.");

            string[] scenes = ResolveEnabledBuildScenes();
            if (scenes.Length == 0)
                throw new InvalidOperationException("No enabled scenes found in EditorBuildSettings.");

            string outputDirectory = ResolveProjectPath(OutputDirectory);
            string zipOutputPath = GetZipOutputPath();
            EnsurePathIsUnderProjectBuilds(outputDirectory);
            EnsurePathIsUnderProjectBuilds(zipOutputPath);

            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, recursive: true);

            Directory.CreateDirectory(outputDirectory);

            BuildPlayerOptions options = new()
            {
                scenes = scenes,
                locationPathName = outputDirectory,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"WebGL build failed: {report.summary.result}");

            if (File.Exists(zipOutputPath))
                File.Delete(zipOutputPath);

            Directory.CreateDirectory(Path.GetDirectoryName(zipOutputPath) ?? string.Empty);
            System.IO.Compression.ZipFile.CreateFromDirectory(
                outputDirectory,
                zipOutputPath,
                System.IO.Compression.CompressionLevel.Optimal,
                includeBaseDirectory: false);

            Debug.Log($"[ItchWebGLBuildUtility] WebGL itch.io zip created: {zipOutputPath}");
            EditorUtility.RevealInFinder(zipOutputPath);
        }

        private static string[] ResolveEnabledBuildScenes()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                .ToArray();

            if (scenes.Length == 1 && scenes[0] == "Assets/Scenes/SampleScene.unity")
            {
                string[] recapScenes = RecapScenePaths
                    .Where(File.Exists)
                    .ToArray();

                if (recapScenes.Length > 0)
                    return recapScenes;
            }

            return scenes;
        }

        private static string GetZipOutputPath()
        {
            string productName = string.IsNullOrWhiteSpace(PlayerSettings.productName)
                ? FallbackProductName
                : PlayerSettings.productName;
            string version = string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion)
                ? FallbackVersion
                : PlayerSettings.bundleVersion;
            string zipFileName = $"{SanitizeFileNamePart(productName)}-WebGL-Itch-{SanitizeFileNamePart(version)}.zip";
            return ResolveProjectPath(Path.Combine(BuildsDirectory, zipFileName));
        }

        private static string SanitizeFileNamePart(string value)
        {
            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            string sanitized = new(value
                .Select(character => invalidCharacters.Contains(character) ? '-' : character)
                .ToArray());
            return sanitized.Trim();
        }

        private static void ApplyWebGLPlayerSettings()
        {
            PlayerSettings.runInBackground = true;

            Type webGLSettingsType = typeof(PlayerSettings).GetNestedType("WebGL", BindingFlags.Public | BindingFlags.Static);
            SetStaticEnumProperty(webGLSettingsType, "compressionFormat", "Brotli", fallbackValue: 2);
            SetStaticProperty(webGLSettingsType, "decompressionFallback", true);
            SetStaticProperty(webGLSettingsType, "threadsSupport", false);
            SetStaticProperty(webGLSettingsType, "dataCaching", false);
        }

        private static void SetStaticProperty(Type ownerType, string propertyName, object value)
        {
            if (ownerType == null)
                return;

            PropertyInfo property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
            if (property == null || !property.CanWrite)
                return;

            property.SetValue(null, value);
        }

        private static void SetStaticEnumProperty(Type ownerType, string propertyName, string enumName, int fallbackValue)
        {
            if (ownerType == null)
                return;

            PropertyInfo property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
            if (property == null || !property.CanWrite || !property.PropertyType.IsEnum)
                return;

            object value = Enum.IsDefined(property.PropertyType, enumName)
                ? Enum.Parse(property.PropertyType, enumName)
                : Enum.ToObject(property.PropertyType, fallbackValue);
            property.SetValue(null, value);
        }

        private static string ResolveProjectPath(string projectRelativePath)
        {
            string projectRoot = Directory.GetCurrentDirectory();
            return Path.GetFullPath(Path.Combine(projectRoot, projectRelativePath));
        }

        private static void EnsurePathIsUnderProjectBuilds(string path)
        {
            string buildsRoot = ResolveProjectPath("Builds");
            string fullPath = Path.GetFullPath(path);
            if (!fullPath.StartsWith(buildsRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Build output path must stay under {buildsRoot}: {fullPath}");
        }
    }
}
