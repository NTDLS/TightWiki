using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using NTDLS.SqliteDapperWrapper;
using TightWiki.Library;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;

namespace TightWiki.Repository
{
    public partial class TranslationRepository
        : ITwTranslationRepository
    {
        private readonly SupportedCultures _supportedCultures = new();

        public SqliteManagedFactory TranslationFactory { get; private set; }
        public bool IsAvailable { get; private set; }

        public TranslationRepository(IConfiguration configuration)
        {
            //Unlike the other databases, this one is part of the application and is not stored in the (user) database path.
            var connectionString = configuration.GetConnectionString("TranslationsConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = Path.Combine(AppContext.BaseDirectory, "Translations", "Translations.db");
            }

            TranslationFactory = new SqliteManagedFactory(connectionString);

            //Opening a missing SQLite file would create an empty database, so we check first (without opening it).
            var databaseFile = connectionString.Contains('=')
                ? new SqliteConnectionStringBuilder(connectionString).DataSource
                : connectionString; //Just a path.
            IsAvailable = File.Exists(databaseFile);
            if (!IsAvailable)
            {
                //Without translations the application still works, it just displays the English text.
                Console.Error.WriteLine($"The translation database '{databaseFile}' was not found. All text will be displayed in English.");
            }
        }

        public async Task<List<TwTranslationPhrase>> GetTranslationsByLanguage(string language)
        {
            if (!IsAvailable)
            {
                return new();
            }

            //The language is the name of a column, which cannot be a parameter, so it must be one of the known languages.
            if (!_supportedCultures.TryGetByName(language, out var culture))
            {
                throw new ArgumentException($"The language '{language}' is not supported.", nameof(language));
            }

            var column = $"\"{culture.Name}\"";

            return await TranslationFactory.QueryAsync<TwTranslationPhrase>(
                $"SELECT English, {column} as Translation FROM Translation WHERE {column} IS NOT NULL AND {column} != ''");
        }
    }
}
