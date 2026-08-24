using Microsoft.Extensions.Logging;

namespace TightWiki.Data.EfCore.Postgres
{
    /// <summary>
    /// Minimal <see cref="ILogger"/> implementation used as the bootstrap-time logger for
    /// PostgresDatabaseManager, mirroring TightWiki.Library.ConsoleLogger (the equivalent used by the
    /// SQLite <c>DatabaseManager</c> before its own <c>DatabaseLogger</c> is available) and
    /// TightWiki.Data.EfCore.SqlServer.ConsoleLogger. Kept as a separate copy rather than referencing either
    /// of those - see TightWiki.Data.EfCore.SqlServer.ConsoleLogger's own remarks for why.
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        IDisposable? ILogger.BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);

            var color = logLevel switch
            {
                LogLevel.Information => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.DarkRed,
                _ => ConsoleColor.Gray
            };

            var originalColor = Console.ForegroundColor;

            Console.ForegroundColor = color;
            Console.Write($"[{DateTime.Now:HH:mm:ss}] [{logLevel}] ");
            Console.ForegroundColor = originalColor;

            Console.WriteLine(message);

            if (exception != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(exception);
                Console.ForegroundColor = originalColor;
            }
        }
    }
}
