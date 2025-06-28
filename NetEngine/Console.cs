namespace NetEngine;

public enum LogType
{
    Info,
    Warn,
    Error
}

public class ConsoleLogEntry
{
    public LogType Type;
    public string Message;
    public DateTime Timestamp;
    public bool IsEditorMsg = false;

    public ConsoleLogEntry(LogType type, string message, bool isEditorMsg = false)
    {
        Type = type;
        Message = message;
        Timestamp = DateTime.Now;
        IsEditorMsg = isEditorMsg;
    }
}

public static class Console
{
    private static readonly List<ConsoleLogEntry> _logs = new();
    public static List<ConsoleLogEntry> GetLogs() => _logs;

    public static void Log(string message) =>
        _logs.Add(new ConsoleLogEntry(LogType.Info, message));

    public static void Warning(string message) =>
        _logs.Add(new ConsoleLogEntry(LogType.Warn, message));

    public static void Error(string message) =>
        _logs.Add(new ConsoleLogEntry(LogType.Error, message));

    public static void EditorLog(string message) =>
        _logs.Add(new ConsoleLogEntry(LogType.Info, message, true));

    public static void EditorWarning(string message) =>
        _logs.Add(new ConsoleLogEntry(LogType.Warn, message, true));

    public static void EditorError(string message) =>
        _logs.Add(new ConsoleLogEntry(LogType.Error, message, true));
    public static void Clear() =>
        _logs.Clear();
}
