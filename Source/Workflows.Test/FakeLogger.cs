namespace Workflows.Test;

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

internal class FakeLogger : ILogger
{
    public List<string> Lines { get; set; } = new();

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = exception is null ? formatter(state, null!) : $"{exception.Message}. {formatter(state, exception)}";
        Lines.Add(message);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state) => default;
}