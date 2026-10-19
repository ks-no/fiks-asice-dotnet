using Microsoft.Extensions.Logging;

namespace KS.Fiks.ASiC_E.Test;

/// <summary>
/// A message captured by <see cref="CapturingLoggerFactory"/>.
/// </summary>
public sealed class LogEntry
{
    public LogEntry(LogLevel level, string message)
    {
        Level = level;
        Message = message;
    }

    public LogLevel Level { get; }

    public string Message { get; }
}
