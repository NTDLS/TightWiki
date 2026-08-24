using Microsoft.Extensions.Logging;

namespace TightWiki.Data.EfCore.SqlServer
{
    /// <summary>
    /// Minimal <see cref="ILogger"/> implementation used as the bootstrap-time logger for
    /// <see cref="SqlServerDatabaseManager"/>, mirroring TightWiki.Library.ConsoleLogger (the equivalent
    /// used by the SQLite <c>DatabaseManager</c> before its own <c>DatabaseLogger</c> is available). Kept as a
    /// separate copy rather than referencing TightWiki.Library.ConsoleLogger - this class predates this
    /// project's TightWiki.Library ProjectReference (added in phase 2a.2 for <c>ApplicationDbContext</c>,
    /// Database-Providers-Plan.md chapter 4.1.1) and there is no need to churn it now that the reference exists.
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
