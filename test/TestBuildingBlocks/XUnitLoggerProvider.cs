using System.Text;
using JsonApiDotNetCore;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace TestBuildingBlocks;

// Based on https://www.meziantou.net/how-to-get-asp-net-core-logs-in-the-output-of-xunit-tests.htm.
public sealed class XUnitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _testOutputHelper;

    public XUnitLoggerProvider(ITestOutputHelper testOutputHelper)
    {
        ArgumentGuard.NotNull(testOutputHelper);

        _testOutputHelper = testOutputHelper;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new XUnitLogger(_testOutputHelper, categoryName);
    }

    public void Dispose()
    {
    }

    private sealed class XUnitLogger : ILogger
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

            try
            {
                _testOutputHelper.WriteLine(builder.ToString());
            }
            catch (InvalidOperationException)
            {
                // Silently ignore when there is no currently active test.
            }
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
}
