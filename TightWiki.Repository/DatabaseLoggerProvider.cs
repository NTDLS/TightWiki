using Microsoft.Extensions.Logging;

namespace TightWiki.Repository
{
    public class DatabaseLoggerProvider : ILoggerProvider
    {
        private readonly LogLevel _minLevel;

        public DatabaseLoggerProvider(LogLevel minLevel = LogLevel.Information)
        {
            _minLevel = minLevel;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new DatabaseLogger(categoryName, _minLevel);
        }

        public void Dispose() { }
    }
}
