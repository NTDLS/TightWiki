using Microsoft.Extensions.Logging;

namespace TightWiki.Library
{
    public class ConsoleLogger
        : ILogger
    {
        public ConsoleLogger()
        {
        }

        IDisposable? ILogger.BeginScope<TState>(TState state)
        {
            return null;
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
