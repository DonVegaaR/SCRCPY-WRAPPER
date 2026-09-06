using System.IO;
using System.Text;

namespace ScrcpyLauncher.App.Infrastructure;

public static class StartupLogger
{
    private static readonly object Gate = new();
    private static string? logFilePath;

    public static string LogFilePath
    {
        get
        {
            if (logFilePath is null)
            {
                logFilePath = ResolveLogFilePath();
            }

            return logFilePath;
        }
    }

    public static void ResetSession()
    {
        lock (Gate)
        {
            var directory = Path.GetDirectoryName(LogFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.AppendAllText(
                LogFilePath,
                Environment.NewLine + "========== New Session " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ==========" + Environment.NewLine,
                Encoding.UTF8);
        }
    }

    public static void Write(string message)
    {
        lock (Gate)
        {
            var directory = Path.GetDirectoryName(LogFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";
            File.AppendAllText(LogFilePath, line, Encoding.UTF8);
        }
    }

    public static void WriteException(string context, Exception exception)
    {
        Write($"{context}: {exception}");
    }

    private static string ResolveLogFilePath()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("SCRCPY_LAUNCHER_LOG");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return Path.GetFullPath(fromEnvironment);
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScrcpyLauncher",
            "launcher-debug.log");
    }
}
