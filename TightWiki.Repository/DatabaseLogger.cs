using Microsoft.Extensions.Logging;
using static TightWiki.Library.Constants;

namespace TightWiki.Repository
{
    public class DatabaseLogger
        : ILogger
    {
        private readonly string _category;
        private readonly LogLevel _minLevel;

        public DatabaseLogger(string category, LogLevel minLevel)
        {
            _category = category;
            _minLevel = minLevel;
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
            try
            {
                LoggingRepository.WriteLog(logLevel, message, exception?.GetBaseException()?.Message, exception?.StackTrace);
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
