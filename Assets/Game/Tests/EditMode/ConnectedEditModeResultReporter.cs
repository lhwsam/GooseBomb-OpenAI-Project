using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestRunner;

[assembly: TestRunCallback(typeof(BombSwap.Tests.EditMode.ConnectedEditModeResultReporter))]

namespace BombSwap.Tests.EditMode
{
    public sealed class ConnectedEditModeResultReporter : ITestRunCallback
    {
        public const string LogPrefix = "BOMBSWAP_EDITMODE_RESULT";

        public void RunStarted(ITest testsToRun)
        {
            Debug.LogFormat("{0} STARTED count={1}", LogPrefix, testsToRun.TestCaseCount);
        }

        public void RunFinished(ITestResult testResults)
        {
            Debug.LogFormat(
                "{0} FINISHED state={1} passed={2} failed={3} skipped={4} inconclusive={5}",
                LogPrefix,
                testResults.ResultState,
                testResults.PassCount,
                testResults.FailCount,
                testResults.SkipCount,
                testResults.InconclusiveCount);
        }

        public void TestStarted(ITest test)
        {
        }

        public void TestFinished(ITestResult result)
        {
            if (result.HasChildren || result.FailCount == 0)
            {
                return;
            }

            Debug.LogErrorFormat(
                "{0} FAILED name={1} message={2}\n{3}",
                LogPrefix,
                result.FullName,
                result.Message,
                result.StackTrace);
        }
    }
}
