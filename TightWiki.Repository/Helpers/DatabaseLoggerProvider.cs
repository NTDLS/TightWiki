using Microsoft.Extensions.Logging;
using TightWiki.Plugin.Interfaces.Repository;

namespace TightWiki.Repository.Helpers
{
    public class DatabaseLoggerProvider
        : ILoggerProvider
    {
        private readonly LogLevel _minLevel;
        private readonly ITwLoggingRepository _loggingRepository;

        public DatabaseLoggerProvider(ITwLoggingRepository loggingRepository, LogLevel minLevel = LogLevel.Information)
        {
            _minLevel = minLevel;
            _loggingRepository = loggingRepository;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new DatabaseLogger(_loggingRepository, _minLevel);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
