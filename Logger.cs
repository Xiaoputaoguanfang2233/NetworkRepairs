using System;
using System.IO;
using System.Reflection;

public static class Logger
{
    private static readonly string LogDir = Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "",
        "logs");

    static Logger()
    {
        if (!Directory.Exists(LogDir))
            Directory.CreateDirectory(LogDir);
    }

    public static void Info(string message)
    {
        WriteLog("INFO", message);
    }

    public static void Warning(string message)
    {
        WriteLog("WARN", message);
    }

    public static void Error(string message, Exception ex = null)
    {
        string fullMessage = message;
        if (ex != null)
            fullMessage += $"\nException: {ex.GetType()}: {ex.Message}\nStackTrace: {ex.StackTrace}";
        WriteLog("ERROR", fullMessage);
    }

    private static void WriteLog(string level, string message)
    {
        string logFile = Path.Combine(LogDir, $"{DateTime.Now:yyyy-MM-dd}.log");
        string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        try
        {
            File.AppendAllText(logFile, line + Environment.NewLine);
        }
        catch
        {
            // 日志写入失败时无法记录，只能忽略
        }
    }
}