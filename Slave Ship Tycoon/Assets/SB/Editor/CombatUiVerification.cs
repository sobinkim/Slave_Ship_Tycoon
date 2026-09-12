using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SB.Editor
{
    public static class CombatUiVerification
    {
        [MenuItem("Tools/SB/Run Targeting Regression Tests")]
        public static void RunTests()
        {
            TestRunnerApi api = ScriptableObject.CreateInstance<TestRunnerApi>();
            Results callback = new Results();
            api.RegisterCallbacks(callback);
            try
            {
                api.Execute(new ExecutionSettings(new Filter
                {
                    testMode = TestMode.EditMode,
                    testNames = new[] { "SB.Editor.Tests.ShipTargetingTests" }
                }) { runSynchronously = true });
            }
            finally
            {
                api.UnregisterCallbacks(callback);
                UnityEngine.Object.DestroyImmediate(api);
            }
        }

        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                TestRunnerApi.SaveResultToFile(result, "Temp/ShipTargetingResults.xml");
                Debug.Log($"Targeting regression: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped.");
            }
        }
    }
}
