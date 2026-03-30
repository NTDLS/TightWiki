using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NTDLS.SqliteDapperWrapper;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using TightWiki.Repository.Extensions;
using TightWiki.Repository.Helpers;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Repository
{
    public class LoggingRepository
        : ITwLoggingRepository
    {
        readonly private ITwConfigurationRepository _configurationRepository;
        public SqliteManagedFactory LoggingFactory { get; private set; }

        public LoggingRepository(IConfiguration configuration, ITwConfigurationRepository configurationRepository)
        {
            _configurationRepository = configurationRepository;
            var configDatabaseFile = configurationRepository.ConfigFactory.Ephemeral(o => o.NativeConnection.DataSource);
            var connectionString = configuration.GetDatabaseConnectionString("LoggingConnection", "logging.db", configDatabaseFile);
            LoggingFactory = new SqliteManagedFactory(connectionString);

            CreateTablesIfNotExist().Wait();
        }

        public async Task PurgeLogs()
        {
            await LoggingFactory.ExecuteAsync("PurgeLogs.sql");
        }

        public async Task CreateTablesIfNotExist()
        {
            if (!LoggingFactory.DoesTableExist("Severity"))
            {
                await LoggingFactory.ExecuteAsync(@"Scripts\CreateSeverityTable.sql");
            }

            if (!LoggingFactory.DoesTableExist("Log"))
            {
                await LoggingFactory.ExecuteAsync(@"Scripts\CreateLogTable.sql");
            }
        }

        public async Task WriteException(string? text = null, string? exceptionText = null, string? stackTrace = null)
            => await WriteLog(LogLevel.Error, text, exceptionText, stackTrace);

        public async Task WriteLog(LogLevel severity, string? text = null, string? exceptionText = null, string? stackTrace = null)
        {
            if (severity >= LogLevel.Warning)
            {
                Console.WriteLine($"{text} {exceptionText} {stackTrace}");
            }

            var param = new
            {
                SeverityName = severity.ToString(),
                Text = text,
                ExceptionText = exceptionText,
                StackTrace = stackTrace,
                CreatedDate = DateTime.UtcNow,
            };

            await LoggingFactory.ExecuteAsync("InsertLog.sql", param);
        }

        /*
        public void WriteException(Exception ex)
        {
            var stackTrace = new StackTrace();
            var method = stackTrace.GetFrame(1)?.GetMethod();
            var message = string.IsNullOrEmpty(method?.Name) ? string.Empty : $"Error in {method?.Name}";
            WriteLog(WikiSeverity.Error, message, ex.Message, ex.StackTrace);
        }

        public void WriteException(Exception ex, string text)
        {
            WriteLog(WikiSeverity.Error, text, ex.Message, ex.StackTrace);
        }
        */

        public async Task<int> GetExceptionCount()
        {
            return await LoggingFactory.ExecuteScalarAsync<int>("GetExceptionCount.sql");
        }

        public async Task<List<TwLogEntry>> GetLogEntriesPaged(int pageNumber,
            string? orderBy = null, string? orderByDirection = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = paginationSize
            };

            var query = RepositoryHelpers.TransposeOrderby("GetLogEntriesPaged.sql", orderBy, orderByDirection);
            return await LoggingFactory.QueryAsync<TwLogEntry>(query, param);
        }

        public async Task<TwLogEntry> GetLogEntryById(int id)
        {
            var param = new
            {
                Id = id
            };

            return await LoggingFactory.QuerySingleAsync<TwLogEntry>("GetLogEntryById.sql", param);
        }
    }
}
