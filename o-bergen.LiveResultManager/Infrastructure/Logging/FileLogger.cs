using o_bergen.LiveResultManager.Core.Interfaces;

namespace o_bergen.LiveResultManager.Infrastructure.Logging;

/// <summary>
/// Simple file-based logger implementation
/// </summary>
public class FileLogger : ILogger
{
    private readonly string _logPath;
    private readonly object _lock = new();

    public FileLogger(string logPath)
    {
        _logPath = logPath ?? throw new ArgumentNullException(nameof(logPath));
        
        // Ensure directory exists
        var directory = Path.GetDirectoryName(_logPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }

    public void LogInformation(string message)
    {
        WriteLog("INFO", message);
    }

    public void LogWarning(string message)
    {
        WriteLog("WARNING", message);
    }

    public void LogError(string message, Exception? exception = null)
    {
        var fullMessage = exception != null 
            ? $"{message}\nException: {exception.Message}\n{exception.StackTrace}" 
            : message;
        WriteLog("ERROR", fullMessage);
    }

    public void LogDebug(string message)
    {
        WriteLog("DEBUG", message);
    }

    public void LogSuccess(string message)
    {
        WriteLog("SUCCESS", message);
    }

    private void WriteLog(string level, string message)
    {
        lock (_lock)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logLine = $"[{timestamp}] [{level}] {message}";
                File.AppendAllText(_logPath, logLine + Environment.NewLine);
            }
            catch
            {
                // Silently fail if we can't write to log
            }
        }
    }
}
