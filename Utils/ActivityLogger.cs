using System.Diagnostics;

namespace MauiAppIT13.Utils;

public sealed class ActivityLogger
{
    public void LogInfo(string message)
    {
        Debug.WriteLine($"[INFO] {DateTime.UtcNow:o} - {message}");
    }

    public void LogWarning(string message)
    {
        Debug.WriteLine($"[WARN] {DateTime.UtcNow:o} - {message}");
    }

    public void LogError(string message, Exception? ex = null)
    {
        Debug.WriteLine($"[ERROR] {DateTime.UtcNow:o} - {message} :: {ex}");
    }
}
