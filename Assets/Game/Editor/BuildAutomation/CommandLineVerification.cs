using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BombSwap.Editor.ContentValidation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BombSwap.Editor.Verification
{
    public static class CommandLineVerification
    {
        private const string ArtifactsArgument = "-bombswapArtifacts";
        private const string BuildPathArgument = "-bombswapBuildPath";

        public static void CompileAndValidate()
        {
            var artifactsDirectory = GetRequiredArgument(ArtifactsArgument);
            Directory.CreateDirectory(artifactsDirectory);

            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                if (EditorUtility.scriptCompilationFailed)
                {
                    throw new InvalidOperationException("Unity reports script compilation errors.");
                }

                var errors = ValidateProject();
                var report = new EditorValidationReport(
                    Application.unityVersion,
                    DateTime.UtcNow.ToString("O"),
                    errors.Count == 0 ? "passed" : "failed",
                    errors.ToArray());
                WriteJson(Path.Combine(artifactsDirectory, "editor-validation.json"), report);

                if (errors.Count > 0)
                {
                    throw new InvalidOperationException($"Editor validation failed: {string.Join(" | ", errors)}");
                }

                Debug.Log("BOMBSWAP_VERIFY|compile|passed");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void BuildDevelopmentWebGL()
        {
            var artifactsDirectory = GetRequiredArgument(ArtifactsArgument);
            var buildPath = GetRequiredArgument(BuildPathArgument);
            Directory.CreateDirectory(artifactsDirectory);
            Directory.CreateDirectory(buildPath);

            try
            {
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                {
                    throw new InvalidOperationException(
                        $"Active build target is {EditorUserBuildSettings.activeBuildTarget}, expected WebGL. " +
                        "Run Unity with -buildTarget WebGL.");
                }

                if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                {
                    throw new InvalidOperationException("This Unity installation does not include WebGL build support.");
                }

                var scenePaths = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled && File.Exists(scene.path))
                    .Select(scene => scene.path)
                    .ToArray();
                if (scenePaths.Length == 0)
                {
                    throw new InvalidOperationException("No enabled, existing scenes are available in Build Settings.");
                }

                var options = new BuildPlayerOptions
                {
                    scenes = scenePaths,
                    locationPathName = buildPath,
                    target = BuildTarget.WebGL,
                    options = BuildOptions.Development | BuildOptions.DetailedBuildReport,
                };
                var report = BuildPipeline.BuildPlayer(options);
                var summary = report.summary;
                var result = new WebGlBuildReport(
                    summary.result.ToString(),
                    summary.outputPath,
                    summary.totalSize,
                    summary.totalTime.TotalSeconds,
                    summary.totalWarnings,
                    summary.totalErrors,
                    scenePaths,
                    report.SummarizeErrors());
                WriteJson(Path.Combine(artifactsDirectory, "webgl-build-report.json"), result);

                if (summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"WebGL build did not succeed ({summary.result}): {report.SummarizeErrors()}");
                }

                Debug.Log($"BOMBSWAP_VERIFY|webgl|passed|bytes={summary.totalSize}");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static List<string> ValidateProject()
        {
            var errors = new List<string>();
            ValidateAssemblyDefinition("Assets/Game/Core/BombSwap.Core.asmdef", errors);
            ValidateAssemblyDefinition("Assets/Game/BombSwap.Unity.asmdef", errors);
            ValidateAssemblyDefinition("Assets/Game/Editor/BombSwap.Editor.asmdef", errors);
            ValidateAssemblyDefinition("Assets/Game/Tests/EditMode/BombSwap.Core.Tests.asmdef", errors);
            ValidateAssemblyDefinition("Assets/Game/Tests/PlayMode/BombSwap.Unity.Tests.asmdef", errors);
            ValidateAssemblyDefinition(
                "Assets/Game/Tests/EditorHarness/BombSwap.ConnectedTestHarness.asmdef",
                errors);
            PrototypeContentValidator.Validate(errors);

            return errors;
        }

        private static void ValidateAssemblyDefinition(string assetPath, ICollection<string> errors)
        {
            if (!File.Exists(assetPath))
            {
                errors.Add($"Missing assembly definition: {assetPath}");
                return;
            }

            if (AssetDatabase.LoadMainAssetAtPath(assetPath) == null)
            {
                errors.Add($"Unity could not import assembly definition: {assetPath}");
            }
        }

        private static string GetRequiredArgument(string name)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], name, StringComparison.Ordinal))
                {
                    return Path.GetFullPath(arguments[index + 1]);
                }
            }

            throw new ArgumentException($"Missing required command-line argument: {name}");
        }

        private static void WriteJson(string path, object value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Missing parent directory."));
            File.WriteAllText(path, JsonUtility.ToJson(value, true));
        }

        [Serializable]
        private sealed class EditorValidationReport
        {
            public string unityVersion;
            public string generatedAtUtc;
            public string status;
            public string[] errors;

            public EditorValidationReport(string unityVersion, string generatedAtUtc, string status, string[] errors)
            {
                this.unityVersion = unityVersion;
                this.generatedAtUtc = generatedAtUtc;
                this.status = status;
                this.errors = errors;
            }
        }

        [Serializable]
        private sealed class WebGlBuildReport
        {
            public string result;
            public string outputPath;
            public ulong totalSizeBytes;
            public double totalTimeSeconds;
            public int totalWarnings;
            public int totalErrors;
            public string[] scenes;
            public string errorSummary;

            public WebGlBuildReport(
                string result,
                string outputPath,
                ulong totalSizeBytes,
                double totalTimeSeconds,
                int totalWarnings,
                int totalErrors,
                string[] scenes,
                string errorSummary)
            {
                this.result = result;
                this.outputPath = outputPath;
                this.totalSizeBytes = totalSizeBytes;
                this.totalTimeSeconds = totalTimeSeconds;
                this.totalWarnings = totalWarnings;
                this.totalErrors = totalErrors;
                this.scenes = scenes;
                this.errorSummary = errorSummary;
            }
        }
    }
}
