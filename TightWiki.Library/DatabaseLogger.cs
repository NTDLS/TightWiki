using Microsoft.Extensions.Logging;
using TightWiki.Plugin.Interfaces.Repository;

namespace TightWiki.Library
{
    public class DatabaseLogger
        : ILogger
    {
        private readonly LogLevel _minLevel;
        private readonly ITwLoggingRepository _loggingRepository;

        public DatabaseLogger(ITwLoggingRepository loggingRepository, LogLevel minLevel)
        {
            _minLevel = minLevel;
            _loggingRepository = loggingRepository;
        }

        public bool IsEnabled(LogLevel logLevel)
            => logLevel >= _minLevel;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);

            _ = Task.Run(async () =>
            {
                try
                {
                    await _loggingRepository.WriteLog(
                        logLevel,
                        message,
                        exception?.GetBaseException()?.Message,
                        exception?.StackTrace);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Logging failed: {ex}");
                }
            });
        }

        IDisposable? ILogger.BeginScope<TState>(TState state)
        {
            return null;
        }
    }
}
