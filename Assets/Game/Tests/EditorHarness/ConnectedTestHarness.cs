using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace BombSwap.Tests.Harness
{
    [InitializeOnLoad]
    public static class ConnectedTestHarness
    {
        public const string StatusPath =
            "Artifacts/Verification/connected-test-status.json";

        private const string ActiveRunKey = "BombSwap.ConnectedTestHarness.ActiveRun";
        private const string StartedAtKey = "BombSwap.ConnectedTestHarness.StartedAt";
        private static readonly TestRunnerApi Api;
        private static readonly Callbacks CallbackInstance;

        static ConnectedTestHarness()
        {
            Api = ScriptableObject.CreateInstance<TestRunnerApi>();
            CallbackInstance = new Callbacks();
            Api.RegisterCallbacks(CallbackInstance);
        }

        public static void RunEditMode(params string[] testNames)
        {
            Schedule(TestMode.EditMode, testNames);
        }

        public static void RunPlayMode(params string[] testNames)
        {
            Schedule(TestMode.PlayMode, testNames);
        }

        [MenuItem("Bomb Swap/Verification/Run All EditMode Tests Connected")]
        private static void RunAllEditModeMenu()
        {
            RunEditMode();
        }

        [MenuItem("Bomb Swap/Verification/Run All PlayMode Tests Connected")]
        private static void RunAllPlayModeMenu()
        {
            RunPlayMode();
        }

        private static void Schedule(TestMode mode, string[] testNames)
        {
            if (!string.IsNullOrEmpty(SessionState.GetString(ActiveRunKey, string.Empty)))
            {
                throw new InvalidOperationException(
                    "A connected Unity test run is already active.");
            }

            string runId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
            string startedAtUtc = DateTime.UtcNow.ToString("O");
            SessionState.SetString(ActiveRunKey, runId);
            SessionState.SetString(StartedAtKey, startedAtUtc);
            WriteStatus(new ConnectedTestStatus
            {
                runId = runId,
                mode = mode.ToString(),
                state = "Scheduled",
                startedAtUtc = startedAtUtc,
                finishedAtUtc = string.Empty,
                total = 0,
                passed = 0,
                failed = 0,
                skipped = 0,
                failures = Array.Empty<ConnectedTestFailure>(),
            });

            var filter = new Filter { testMode = mode };
            if (testNames != null && testNames.Length > 0)
            {
                filter.testNames = (string[])testNames.Clone();
            }

            EditorApplication.delayCall += () =>
                Api.Execute(new ExecutionSettings(filter));
        }

        private static void WriteStatus(ConnectedTestStatus status)
        {
            string absolutePath = Path.GetFullPath(StatusPath);
            string directory = Path.GetDirectoryName(absolutePath);
            if (string.IsNullOrEmpty(directory))
            {
                throw new InvalidOperationException("Connected test status path has no parent.");
            }

            Directory.CreateDirectory(directory);
            string json = JsonUtility.ToJson(status, true);
            File.WriteAllText(absolutePath, json);
            string runDirectory = Path.Combine(directory, "ConnectedTests");
            Directory.CreateDirectory(runDirectory);
            File.WriteAllText(Path.Combine(runDirectory, status.runId + ".json"), json);
        }

        private sealed class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
                string runId = SessionState.GetString(ActiveRunKey, string.Empty);
                if (string.IsNullOrEmpty(runId))
                {
                    return;
                }

                WriteStatus(new ConnectedTestStatus
                {
                    runId = runId,
                    mode = testsToRun.TestMode.ToString(),
                    state = "Running",
                    startedAtUtc = SessionState.GetString(
                        StartedAtKey,
                        DateTime.UtcNow.ToString("O")),
                    finishedAtUtc = string.Empty,
                    total = 0,
                    passed = 0,
                    failed = 0,
                    skipped = 0,
                    failures = Array.Empty<ConnectedTestFailure>(),
                });
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                string runId = SessionState.GetString(ActiveRunKey, string.Empty);
                if (string.IsNullOrEmpty(runId))
                {
                    return;
                }

                var accumulator = new ResultAccumulator();
                Accumulate(result, accumulator);
                WriteStatus(new ConnectedTestStatus
                {
                    runId = runId,
                    mode = result.Test.TestMode.ToString(),
                    state = result.ResultState,
                    startedAtUtc = SessionState.GetString(StartedAtKey, string.Empty),
                    finishedAtUtc = DateTime.UtcNow.ToString("O"),
                    total = accumulator.Total,
                    passed = accumulator.Passed,
                    failed = accumulator.Failed,
                    skipped = accumulator.Skipped,
                    failures = accumulator.Failures.ToArray(),
                });
                SessionState.EraseString(ActiveRunKey);
                SessionState.EraseString(StartedAtKey);
                Debug.Log(
                    $"BOMBSWAP_CONNECTED_TEST_FINISHED|mode={result.Test.TestMode}|" +
                    $"state={result.ResultState}|passed={accumulator.Passed}|" +
                    $"failed={accumulator.Failed}|skipped={accumulator.Skipped}");
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }

            private static void Accumulate(
                ITestResultAdaptor result,
                ResultAccumulator accumulator)
            {
                if (result.HasChildren)
                {
                    foreach (ITestResultAdaptor child in result.Children)
                    {
                        Accumulate(child, accumulator);
                    }
                    return;
                }

                accumulator.Total++;
                switch (result.TestStatus)
                {
                    case TestStatus.Passed:
                        accumulator.Passed++;
                        break;
                    case TestStatus.Failed:
                        accumulator.Failed++;
                        accumulator.Failures.Add(new ConnectedTestFailure
                        {
                            fullName = result.FullName,
                            message = result.Message,
                            stackTrace = result.StackTrace,
                        });
                        break;
                    default:
                        accumulator.Skipped++;
                        break;
                }
            }
        }

        private sealed class ResultAccumulator
        {
            public int Total;
            public int Passed;
            public int Failed;
            public int Skipped;
            public readonly List<ConnectedTestFailure> Failures =
                new List<ConnectedTestFailure>();
        }

        [Serializable]
        private sealed class ConnectedTestStatus
        {
            public string runId;
            public string mode;
            public string state;
            public string startedAtUtc;
            public string finishedAtUtc;
            public int total;
            public int passed;
            public int failed;
            public int skipped;
            public ConnectedTestFailure[] failures;
        }

        [Serializable]
        private sealed class ConnectedTestFailure
        {
            public string fullName;
            public string message;
            public string stackTrace;
        }
    }
}
