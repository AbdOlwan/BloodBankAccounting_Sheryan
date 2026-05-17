using System.IO;

namespace BloodBankAccounting.Helpers;

public static class DiagnosticLogger
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BloodBankAccounting", "crash.log");

    public static void Log(string context, Exception ex)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);

            var msg = $"""
                [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{context}]
                Type    : {ex.GetType().FullName}
                Message : {ex.Message}
                Inner   : {ex.InnerException?.GetType().FullName} — {ex.InnerException?.Message}
                Inner2  : {ex.InnerException?.InnerException?.Message}
                Stack   :
                {ex.StackTrace}
                InnerStack:
                {ex.InnerException?.StackTrace}
                -----------------------------------------------

                """;

            File.AppendAllText(LogPath, msg);
        }
        catch { /* لا تسمح للـ Logger بكسر البرنامج */ }
    }

    public static string GetLogPath() => LogPath;
}