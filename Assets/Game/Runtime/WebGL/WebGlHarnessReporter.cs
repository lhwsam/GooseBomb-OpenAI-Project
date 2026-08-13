using System.Runtime.InteropServices;

namespace BombSwap
{
    public static class WebGlHarnessReporter
    {
#if UNITY_WEBGL && !UNITY_EDITOR && DEVELOPMENT_BUILD
        [DllImport("__Internal")]
        private static extern void BombSwapHarnessReport(string eventName);
#endif

        public static void Report(string eventName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR && DEVELOPMENT_BUILD
            BombSwapHarnessReport(eventName);
#endif
        }
    }
}
