using Microsoft.Extensions.Logging;
using static TightWiki.Library.Constants;

namespace TightWiki.Repository
{
    public class DatabaseLogger
        : ILogger
    {
        private readonly string _category;

        public DatabaseLogger(string category)
        {
            _category = category;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            var severity = logLevel switch
            {
                LogLevel.Trace or LogLevel.Debug => WikiSeverity.Verbose,
                LogLevel.Information => WikiSeverity.Information,
                LogLevel.Warning => WikiSeverity.Warning,
                LogLevel.Error or LogLevel.Critical => WikiSeverity.Error,
                _ => WikiSeverity.Verbose,
            };

            try
            {
                LoggingRepository.WriteLog(severity, message, exception?.GetBaseException()?.Message, exception?.StackTrace);
            }
            catch
            {
                Console.WriteLine($"{message} {exception}");
            }
        }

        IDisposable? ILogger.BeginScope<TState>(TState state)
        {
            return null;
        }
    }
}
