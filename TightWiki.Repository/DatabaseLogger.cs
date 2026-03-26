using Microsoft.Extensions.Logging;

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
            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);

            _ = Task.Run(async () =>
            {
                try
                {
                    await LoggingRepository.WriteLog(
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
