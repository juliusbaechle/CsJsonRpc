namespace JsonRpc {
    public static class Logging {
        internal static void LogDebug(string msg) { LogHandler("DEBUG: " + msg); }
        internal static void LogInfo(string msg) { LogHandler("INFO : " + msg); }
        internal static void LogWarning(string msg) { LogHandler("WARN : " + msg); }
        internal static void LogError(string msg) { LogHandler("ERROR: " + msg); }

        public static event Action<string> LogHandler = Console.WriteLine;
    }
}
