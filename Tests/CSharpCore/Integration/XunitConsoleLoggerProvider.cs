// Logger provider that writes Microsoft.Extensions.Logging entries to a shared log file.
// This works safely from background threads.

using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Samples.Integration
{
    /// <summary>
    /// A simple <see cref="ILoggerProvider"/> that writes to a shared log file
    /// for diagnostic tracing during test execution.
    /// </summary>
    public sealed class XunitConsoleLoggerProvider : ILoggerProvider
    {
        private readonly StreamWriter _writer;

        public XunitConsoleLoggerProvider(string logFilePath = null)
        {
            logFilePath ??= Path.Combine(Path.GetTempPath(), "snmp_test_diagnostic.log");
            _writer = new StreamWriter(logFilePath, append: false) { AutoFlush = true };
            _writer.WriteLine($"=== Log started at {DateTime.Now:O} ===");
        }

        public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, _writer);

        public void Dispose()
        {
            _writer?.Dispose();
        }

        private sealed class FileLogger : ILogger
        {
            private readonly string _category;
            private readonly StreamWriter _writer;

            public FileLogger(string category, StreamWriter writer)
            {
                _category = category;
                _writer = writer;
            }

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                var message = formatter(state, exception);
                lock (_writer)
                {
                    _writer.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{logLevel}] {_category}: {message}");
                    if (exception != null)
                    {
                        _writer.WriteLine(exception.ToString());
                    }
                }
            }

            private sealed class NullScope : IDisposable
            {
                public static readonly NullScope Instance = new NullScope();
                public void Dispose() { }
            }
        }
    }
}
