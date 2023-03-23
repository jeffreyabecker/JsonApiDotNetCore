using System.Text;
using JsonApiDotNetCore;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace TestBuildingBlocks;

internal class XUnitLogger : ILogger
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly string _categoryName;
    private readonly IExternalScopeProvider _scopeProvider = new NoExternalScopeProvider();

    public XUnitLogger(ITestOutputHelper testOutputHelper, string categoryName)
    {
        ArgumentGuard.NotNull(testOutputHelper);
        ArgumentGuard.NotNull(categoryName);

        _testOutputHelper = testOutputHelper;
        _categoryName = categoryName;
    }

    public static ILogger CreateLogger(ITestOutputHelper testOutputHelper)
    {
        return new XUnitLogger(testOutputHelper, string.Empty);
    }

    public static ILogger<T> CreateLogger<T>(ITestOutputHelper testOutputHelper)
    {
        return new XUnitLogger<T>(testOutputHelper);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return _scopeProvider.Push(state);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var builder = new StringBuilder();
        builder.Append(GetLogLevelString(logLevel)).Append(" [").Append(_categoryName).Append("] ").Append(formatter(state, exception));

        if (exception != null)
        {
            builder.Append('\n').Append(exception);
        }

        _scopeProvider.ForEachScope((scope, nextState) =>
        {
            nextState.Append("\n => ");
            nextState.Append(scope);
        }, builder);

        _testOutputHelper.WriteLine(builder.ToString());
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRCE",
            LogLevel.Debug => "DBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "FAIL",
            LogLevel.Critical => "CRIT",
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
        };
    }

    private sealed class NoExternalScopeProvider : IExternalScopeProvider
    {
        public void ForEachScope<TState>(Action<object?, TState> callback, TState state)
        {
        }

        public IDisposable Push(object? state)
        {
            return EmptyDisposable.Instance;
        }

        private sealed class EmptyDisposable : IDisposable
        {
            public static EmptyDisposable Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}

internal sealed class XUnitLogger<T> : XUnitLogger, ILogger<T>
{
    public XUnitLogger(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper, typeof(T).FullName!)
    {
    }
}
