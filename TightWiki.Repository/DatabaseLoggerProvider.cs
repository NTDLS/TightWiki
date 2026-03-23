using Microsoft.Extensions.Logging;

namespace TightWiki.Repository
{
    public class DatabaseLoggerProvider
        : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
            => new DatabaseLogger(categoryName);

        public void Dispose() { }
    }
}
