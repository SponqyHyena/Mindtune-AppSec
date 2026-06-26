public static class DebugLogger
{
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message) => UnityEngine.Debug.Log(message);
    
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message) => UnityEngine.Debug.LogWarning(message);
    
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(string message) => UnityEngine.Debug.LogError(message);
}