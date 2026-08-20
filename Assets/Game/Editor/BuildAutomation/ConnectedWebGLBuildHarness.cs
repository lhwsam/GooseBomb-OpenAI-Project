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
    public static class ConnectedWebGLBuildHarness
    {
        private static string _scheduledArtifactsDirectory;
        private static string _scheduledBuildPath;
        private static string _scheduledScenePath;
        private static string _scheduledStatusPath;

        [MenuItem("Bomb Swap/Verification/Build Development WebGL Connected")]
        private static void ScheduleDevelopmentMenu()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string artifactsDirectory = Path.Combine(
                "Artifacts",
                "Verification",
                timestamp + "-connected-web");
            string buildPath = Path.Combine(artifactsDirectory, "WebGLBuild");
            string statusPath = ScheduleDevelopment(artifactsDirectory, buildPath);
            Debug.Log(
                "BOMBSWAP_CONNECTED_WEBGL_BUILD SCHEDULED " + statusPath);
        }

        public static string ScheduleDevelopment(
            string artifactsDirectory,
            string buildPath)
        {
            return ScheduleDevelopmentInternal(
                artifactsDirectory,
                buildPath,
                scenePath: null);
        }

        public static string ScheduleDevelopmentScene(
            string artifactsDirectory,
            string buildPath,
            string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                throw new ArgumentException(
                    "Scene path is required.",
                    nameof(scenePath));
            }

            return ScheduleDevelopmentInternal(
                artifactsDirectory,
                buildPath,
                scenePath);
        }

        private static string ScheduleDevelopmentInternal(
            string artifactsDirectory,
            string buildPath,
            string scenePath)
        {
            if (string.IsNullOrWhiteSpace(artifactsDirectory))
            {
                throw new ArgumentException(
                    "Artifacts directory is required.",
                    nameof(artifactsDirectory));
            }
            if (string.IsNullOrWhiteSpace(buildPath))
            {
                throw new ArgumentException(
                    "WebGL build path is required.",
                    nameof(buildPath));
            }
            if (!string.IsNullOrEmpty(_scheduledStatusPath))
            {
                throw new InvalidOperationException(
                    "A connected WebGL build is already scheduled or running.");
            }

            string absoluteArtifacts = Path.GetFullPath(artifactsDirectory);
            Directory.CreateDirectory(absoluteArtifacts);
            _scheduledArtifactsDirectory = artifactsDirectory;
            _scheduledBuildPath = buildPath;
            _scheduledScenePath = scenePath;
            _scheduledStatusPath = Path.Combine(
                absoluteArtifacts,
                "webgl-build-status.txt");
            File.WriteAllText(_scheduledStatusPath, "Scheduled");
            EditorApplication.update -= ExecuteScheduledBuild;
            EditorApplication.update += ExecuteScheduledBuild;
            EditorApplication.QueuePlayerLoopUpdate();
            return _scheduledStatusPath;
        }

        public static string BuildDevelopment(
            string artifactsDirectory,
            string buildPath)
        {
            return BuildDevelopmentInternal(
                artifactsDirectory,
                buildPath,
                scenes: null);
        }

        public static string BuildDevelopmentScene(
            string artifactsDirectory,
            string buildPath,
            string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                throw new ArgumentException(
                    "Scene path is required.",
                    nameof(scenePath));
            }
            if (!File.Exists(scenePath))
            {
                throw new InvalidOperationException(
                    $"WebGL scene does not exist: '{scenePath}'.");
            }

            return BuildDevelopmentInternal(
                artifactsDirectory,
                buildPath,
                new[] { scenePath });
        }

        private static string BuildDevelopmentInternal(
            string artifactsDirectory,
            string buildPath,
            string[] scenes)
        {
            if (string.IsNullOrWhiteSpace(artifactsDirectory))
            {
                throw new ArgumentException(
                    "Artifacts directory is required.",
                    nameof(artifactsDirectory));
            }
            if (string.IsNullOrWhiteSpace(buildPath))
            {
                throw new ArgumentException(
                    "WebGL build path is required.",
                    nameof(buildPath));
            }

            string absoluteArtifacts = Path.GetFullPath(artifactsDirectory);
            string absoluteBuild = Path.GetFullPath(buildPath);
            Directory.CreateDirectory(absoluteArtifacts);
            Directory.CreateDirectory(absoluteBuild);
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                throw new InvalidOperationException(
                    $"Active build target is {EditorUserBuildSettings.activeBuildTarget}, expected WebGL.");
            }
            if (!BuildPipeline.IsBuildTargetSupported(
                    BuildTargetGroup.WebGL,
                    BuildTarget.WebGL))
            {
                throw new InvalidOperationException(
                    "This Unity installation does not include WebGL build support.");
            }

            var validationErrors = new List<string>();
            PrototypeContentValidator.Validate(validationErrors);
            if (validationErrors.Count != 0)
            {
                throw new InvalidOperationException(
                    $"Content validation failed before WebGL build: " +
                    string.Join(" | ", validationErrors));
            }

            scenes ??= EditorBuildSettings.scenes
                .Where(scene => scene.enabled && File.Exists(scene.path))
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                throw new InvalidOperationException(
                    "No enabled, existing scenes are available in Build Settings.");
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = absoluteBuild,
                target = BuildTarget.WebGL,
                options = BuildOptions.Development | BuildOptions.DetailedBuildReport,
            };
            BuildReport report;
            using (ResponsiveWebGLTemplateScope.Activate())
            {
                report = BuildPipeline.BuildPlayer(options);
            }
            BuildSummary summary = report.summary;
            var artifact = new ConnectedWebGLBuildReport(
                summary.result.ToString(),
                summary.outputPath,
                summary.totalSize,
                summary.totalTime.TotalSeconds,
                summary.totalWarnings,
                summary.totalErrors,
                scenes,
                report.SummarizeErrors());
            string reportPath = Path.Combine(
                absoluteArtifacts,
                "webgl-build-report.json");
            File.WriteAllText(reportPath, JsonUtility.ToJson(artifact, true));
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"WebGL build did not succeed ({summary.result}): " +
                    report.SummarizeErrors());
            }
            if (!File.Exists(Path.Combine(absoluteBuild, "index.html")))
            {
                throw new InvalidOperationException(
                    $"WebGL build succeeded without index.html at '{absoluteBuild}'.");
            }
            return reportPath;
        }

        private static void ExecuteScheduledBuild()
        {
            EditorApplication.update -= ExecuteScheduledBuild;
            string artifactsDirectory = _scheduledArtifactsDirectory;
            string buildPath = _scheduledBuildPath;
            string scenePath = _scheduledScenePath;
            string statusPath = _scheduledStatusPath;
            try
            {
                File.WriteAllText(statusPath, "Running");
                string reportPath = string.IsNullOrEmpty(scenePath)
                    ? BuildDevelopment(artifactsDirectory, buildPath)
                    : BuildDevelopmentScene(
                        artifactsDirectory,
                        buildPath,
                        scenePath);
                File.WriteAllText(
                    statusPath,
                    "Passed" + Environment.NewLine + reportPath);
                Debug.Log(
                    "BOMBSWAP_CONNECTED_WEBGL_BUILD PASSED " + reportPath);
            }
            catch (Exception exception)
            {
                File.WriteAllText(
                    statusPath,
                    "Failed" + Environment.NewLine + exception);
                Debug.LogException(exception);
            }
            finally
            {
                _scheduledArtifactsDirectory = null;
                _scheduledBuildPath = null;
                _scheduledScenePath = null;
                _scheduledStatusPath = null;
            }
        }

        [Serializable]
        private sealed class ConnectedWebGLBuildReport
        {
            public string result;
            public string outputPath;
            public ulong totalSizeBytes;
            public double totalTimeSeconds;
            public int totalWarnings;
            public int totalErrors;
            public string[] scenes;
            public string errorSummary;

            public ConnectedWebGLBuildReport(
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
