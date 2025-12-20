namespace TurkceRumenceCeviri.Utilities;

/// <summary>
/// Debug ve test amaçlý yardýmcý sýnýf
/// </summary>
public static class DebugHelper
{
    public static void LogMessage(string message, string category = "INFO")
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logMessage = $"[{timestamp}] [{category}] {message}";
        
        System.Diagnostics.Debug.WriteLine(logMessage);
        Console.WriteLine(logMessage);
    }

    public static void LogError(string message, Exception? ex = null)
    {
        LogMessage(message, "ERROR");
        if (ex != null)
        {
            LogMessage($"Exception: {ex.Message}", "ERROR");
            LogMessage($"StackTrace: {ex.StackTrace}", "ERROR");
        }
    }

    public static void LogSuccess(string message)
    {
        LogMessage(message, "SUCCESS");
    }

    public static void LogWarning(string message)
    {
        LogMessage(message, "WARNING");
    }
}
