using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace KS.Fiks.ASiC_E.Test;

/// <summary>
/// A logger factory for tests that keeps every logged message, so a test can check that a warning was logged.
/// </summary>
public sealed class CapturingLoggerFactory : ILoggerFactory, ILogger
{
    public List<LogEntry> Entries { get; } = new List<LogEntry>();

    public ILogger CreateLogger(string categoryName)
    {
        return this;
    }

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
    }

    public void Dispose()
    {
    }
}
