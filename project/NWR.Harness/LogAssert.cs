using System;
using System.IO;
using System.Text.RegularExpressions;

namespace NWR.Harness
{
    public static class LogAssert
    {
        private static readonly string[] FailurePatterns = {
            @"saveGame\.IO\(\)",
            @"saveGame\(\):",
            @"loadGame\(\):",
            @"loadGame\.io\(\)",
            @"terrainsLoad\(\): fail",
            @"playerLoad\(\): fail",
            @"Critical error"
        };

        public static void RequireLogMarkers(string logPath, params string[] required)
        {
            if (!File.Exists(logPath)) {
                throw new InvalidOperationException("Log not found: " + logPath);
            }
            string text = File.ReadAllText(logPath);
            foreach (string marker in required) {
                if (text.IndexOf(marker, StringComparison.Ordinal) < 0) {
                    throw new InvalidOperationException("Missing log marker: " + marker);
                }
            }
        }

        public static void RequireNoFailurePatterns(string logPath)
        {
            if (!File.Exists(logPath)) {
                return;
            }
            string text = File.ReadAllText(logPath);
            foreach (string pattern in FailurePatterns) {
                if (Regex.IsMatch(text, pattern)) {
                    throw new InvalidOperationException("Log failure pattern matched: " + pattern);
                }
            }
        }
    }
}
