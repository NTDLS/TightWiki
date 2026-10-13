using LocalizerScan;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace TightWiki.TranslationSite.Services
{
    public static class SuggestionStatus
    {
        /// <summary>
        /// Shown unless the votes have pushed it below the auto-hide score.
        /// </summary>
        public const int Normal = 0;

        /// <summary>
        /// Approved by an administrator, always shown regardless of votes.
        /// </summary>
        public const int Approved = 1;

        /// <summary>
        /// Hidden by an administrator.
        /// </summary>
        public const int Hidden = 2;
    }

    public sealed record Suggestion(long Id, string English, string Language, string Text, DateTime CreatedUtc,
        int Status, int UpVotes, int DownVotes, int MyVote, string SubmitterKey)
    {
        public int Score => UpVotes - DownVotes;
    }

    public sealed record SubmitResult(bool Ok, string? Error, Suggestion? Suggestion, bool WasDuplicate);

    public sealed record VoteResult(bool Ok, string? Error, int MyVote);

    public sealed record SiteStats(int Suggestions, int SuggestionsThisWeek, int Votes, int VotesThisWeek);

    /// <summary>
    /// Stores the suggestions and the votes on them in their own database (so that the translation database
    ///  can be replaced on every publish), and enforces the rules about who can suggest and vote.
    /// </summary>
    public sealed class SuggestionService
    {
        private readonly TranslationCatalog _catalog;
        private readonly SiteOptions _options;
        private readonly TimeProvider _time;
        private readonly string _connectionString;
        private readonly SlidingWindowLimiter _suggestionLimiter;
        private readonly SlidingWindowLimiter _voteLimiter;

        /// <summary>
        /// Raised whenever a suggestion is added, voted on, or changed, so that open pages can refresh themselves.
        /// </summary>
        public event Action? Changed;

        public SuggestionService(TranslationCatalog catalog, IOptions<SiteOptions> options, TimeProvider? time = null)
        {
            _catalog = catalog;
            _options = options.Value;
            _time = time ?? TimeProvider.System;
            _suggestionLimiter = new SlidingWindowLimiter(_time);
            _voteLimiter = new SlidingWindowLimiter(_time);

            var path = _options.SuggestionsPath;

            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                ForeignKeys = true,
                DefaultTimeout = 15
            }.ToString();

            VerifyWritable(path);

            try
            {
                InitializeSchema();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode is 8 or 14)
            {
                //SQLITE_READONLY or SQLITE_CANTOPEN. Folder permissions are not the only way to get here, so say what was checked.
                throw new InvalidOperationException(DescribeNotWritable(path, ex.Message), ex);
            }
        }

        /// <summary>
        /// SQLite only says "attempt to write a readonly database", which does not say what to fix, so check the things
        ///  that cause it and report which account the site is running as and exactly what it was not allowed to do.
        /// </summary>
        private static void VerifyWritable(string path)
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".";

            try
            {
                Directory.CreateDirectory(directory);

                //SQLite creates its -wal and -shm files (and any journal) next to the database, so the folder must be writable too.
                var probe = Path.Combine(directory, $".write-test-{Guid.NewGuid():N}.tmp");
                File.WriteAllText(probe, "x");
                File.Delete(probe);

                foreach (var file in new[] { path, path + "-wal", path + "-shm" })
                {
                    if (File.Exists(file))
                    {
                        using var stream = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
                    }
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                throw new InvalidOperationException(DescribeNotWritable(path, ex.Message), ex);
            }
        }

        private static string DescribeNotWritable(string path, string detail)
            => $"The suggestions database '{path}' cannot be written to. The site is running as '{Environment.UserDomainName}\\{Environment.UserName}'. "
             + $"That account needs Modify permission on the folder '{Path.GetDirectoryName(Path.GetFullPath(path))}' and on the files in it "
             + $"(including any '{Path.GetFileName(path)}-wal' and '{Path.GetFileName(path)}-shm' left behind by another account), and the files must not be marked read-only. ({detail})";

        private SqliteConnection Open()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            return connection;
        }

        private void InitializeSchema()
        {
            using var connection = Open();

            Execute(connection, "PRAGMA journal_mode=WAL;");
            Execute(connection, """
                CREATE TABLE IF NOT EXISTS Suggestion (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    English TEXT NOT NULL COLLATE NOCASE,
                    Language TEXT NOT NULL,
                    Text TEXT NOT NULL COLLATE NOCASE,
                    SubmitterKey TEXT NOT NULL,
                    CreatedUtc TEXT NOT NULL,
                    Status INTEGER NOT NULL DEFAULT 0,
                    UpVotes INTEGER NOT NULL DEFAULT 0,
                    DownVotes INTEGER NOT NULL DEFAULT 0
                );
                CREATE UNIQUE INDEX IF NOT EXISTS UX_Suggestion ON Suggestion (English, Language, Text);
                CREATE INDEX IF NOT EXISTS IX_Suggestion_English ON Suggestion (English);
                CREATE TABLE IF NOT EXISTS SuggestionVote (
                    SuggestionId INTEGER NOT NULL REFERENCES Suggestion (Id) ON DELETE CASCADE,
                    VoterKey TEXT NOT NULL,
                    Value INTEGER NOT NULL,
                    CreatedUtc TEXT NOT NULL,
                    PRIMARY KEY (SuggestionId, VoterKey)
                );
                """);
        }

        private static void Execute(SqliteConnection connection, string sql, SqliteTransaction? transaction = null)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        private string Now() => _time.GetUtcNow().UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

        private void RaiseChanged() => Changed?.Invoke();

        #region Suggest.

        /// <summary>
        /// Adds a suggested translation. The suggester starts it off with their own up-vote.
        /// </summary>
        public SubmitResult Submit(string visitorId, string english, string language, string text)
        {
            var phrase = _catalog.FindByEnglish(english ?? string.Empty);
            if (phrase == null)
            {
                return new SubmitResult(false, "That phrase could not be found.", null, false);
            }

            var languageInfo = _catalog.FindLanguage(language);
            if (languageInfo == null)
            {
                return new SubmitResult(false, "Please choose a language.", null, false);
            }

            text = (text ?? string.Empty).Trim();
            if (text.Length == 0)
            {
                return new SubmitResult(false, "Please enter a translation.", null, false);
            }

            if (text.Length > _options.MaxSuggestionLength)
            {
                return new SubmitResult(false, $"Suggestions can be at most {_options.MaxSuggestionLength} characters.", null, false);
            }

            if (text.Any(char.IsControl))
            {
                return new SubmitResult(false, "Suggestions must be a single line of text.", null, false);
            }

            //The same checks that are used when the translations are produced: wrong alphabet, damaged placeholders, stray markup.
            var problems = TranslationValidator.GetProblems(languageInfo.Code, phrase.English, text);
            if (problems.Count > 0)
            {
                return new SubmitResult(false, $"That does not look right for {languageInfo.Name}: {string.Join("; ", problems)}.", null, false);
            }

            if (phrase.Translations.TryGetValue(languageInfo.Name, out var current)
                && TranslationCatalog.Normalize(current) == TranslationCatalog.Normalize(text))
            {
                return new SubmitResult(false, "That is already the current translation.", null, false);
            }

            if (!_suggestionLimiter.TryAcquire(visitorId, _options.SuggestionsPerHour, TimeSpan.FromHours(1)))
            {
                return new SubmitResult(false, "You have made a lot of suggestions recently. Please try again a little later.", null, false);
            }

            using var connection = Open();
            using var transaction = connection.BeginTransaction();

            long id;
            using (var find = connection.CreateCommand())
            {
                find.Transaction = transaction;
                find.CommandText = "SELECT Id FROM Suggestion WHERE English = @english AND Language = @language AND Text = @text";
                find.Parameters.AddWithValue("@english", phrase.English);
                find.Parameters.AddWithValue("@language", languageInfo.Name);
                find.Parameters.AddWithValue("@text", text);

                var existing = find.ExecuteScalar();
                if (existing != null)
                {
                    //Someone already suggested exactly this, so count it as a vote for that one.
                    transaction.Commit();
                    id = Convert.ToInt64(existing, CultureInfo.InvariantCulture);
                    Vote(visitorId, id, 1, onlyIfNotVoted: true);

                    var duplicate = GetById(visitorId, id);
                    return new SubmitResult(true, null, duplicate, true);
                }
            }

            using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO Suggestion (English, Language, Text, SubmitterKey, CreatedUtc, Status, UpVotes, DownVotes)
                    VALUES (@english, @language, @text, @submitter, @created, 0, 1, 0);
                    SELECT last_insert_rowid();
                    """;
                insert.Parameters.AddWithValue("@english", phrase.English);
                insert.Parameters.AddWithValue("@language", languageInfo.Name);
                insert.Parameters.AddWithValue("@text", text);
                insert.Parameters.AddWithValue("@submitter", visitorId);
                insert.Parameters.AddWithValue("@created", Now());
                id = Convert.ToInt64(insert.ExecuteScalar(), CultureInfo.InvariantCulture);
            }

            using (var vote = connection.CreateCommand())
            {
                vote.Transaction = transaction;
                vote.CommandText = "INSERT INTO SuggestionVote (SuggestionId, VoterKey, Value, CreatedUtc) VALUES (@id, @voter, 1, @created)";
                vote.Parameters.AddWithValue("@id", id);
                vote.Parameters.AddWithValue("@voter", visitorId);
                vote.Parameters.AddWithValue("@created", Now());
                vote.ExecuteNonQuery();
            }

            transaction.Commit();
            RaiseChanged();

            return new SubmitResult(true, null, GetById(visitorId, id), false);
        }

        #endregion

        #region Vote.

        /// <summary>
        /// Casts a vote (+1 or -1). Voting the same way again takes the vote back, voting the other way changes it.
        /// </summary>
        public VoteResult Vote(string visitorId, long suggestionId, int value, bool onlyIfNotVoted = false)
        {
            if (value != 1 && value != -1)
            {
                return new VoteResult(false, "Invalid vote.", 0);
            }

            if (!_voteLimiter.TryAcquire(visitorId, _options.VotesPerMinute, TimeSpan.FromMinutes(1)))
            {
                return new VoteResult(false, "You are voting very quickly. Please slow down a little.", 0);
            }

            int newVote;

            using (var connection = Open())
            using (var transaction = connection.BeginTransaction())
            {
                string? submitter = null;
                int status = 0;
                using (var find = connection.CreateCommand())
                {
                    find.Transaction = transaction;
                    find.CommandText = "SELECT SubmitterKey, Status FROM Suggestion WHERE Id = @id";
                    find.Parameters.AddWithValue("@id", suggestionId);
                    using var reader = find.ExecuteReader();
                    if (reader.Read())
                    {
                        submitter = reader.GetString(0);
                        status = reader.GetInt32(1);
                    }
                }

                if (submitter == null || status == SuggestionStatus.Hidden)
                {
                    return new VoteResult(false, "That suggestion is no longer available.", 0);
                }

                if (submitter == visitorId)
                {
                    return new VoteResult(false, "You cannot vote on your own suggestion.", 1);
                }

                int currentVote = 0;
                using (var existing = connection.CreateCommand())
                {
                    existing.Transaction = transaction;
                    existing.CommandText = "SELECT Value FROM SuggestionVote WHERE SuggestionId = @id AND VoterKey = @voter";
                    existing.Parameters.AddWithValue("@id", suggestionId);
                    existing.Parameters.AddWithValue("@voter", visitorId);
                    var result = existing.ExecuteScalar();
                    currentVote = result == null ? 0 : Convert.ToInt32(result, CultureInfo.InvariantCulture);
                }

                if (onlyIfNotVoted && currentVote != 0)
                {
                    return new VoteResult(true, null, currentVote);
                }

                newVote = currentVote == value ? 0 : value;

                int upChange = (newVote == 1 ? 1 : 0) - (currentVote == 1 ? 1 : 0);
                int downChange = (newVote == -1 ? 1 : 0) - (currentVote == -1 ? 1 : 0);

                using (var write = connection.CreateCommand())
                {
                    write.Transaction = transaction;
                    write.CommandText = newVote == 0
                        ? "DELETE FROM SuggestionVote WHERE SuggestionId = @id AND VoterKey = @voter"
                        : "INSERT OR REPLACE INTO SuggestionVote (SuggestionId, VoterKey, Value, CreatedUtc) VALUES (@id, @voter, @value, @created)";
                    write.Parameters.AddWithValue("@id", suggestionId);
                    write.Parameters.AddWithValue("@voter", visitorId);
                    write.Parameters.AddWithValue("@value", newVote);
                    write.Parameters.AddWithValue("@created", Now());
                    write.ExecuteNonQuery();
                }

                using (var counts = connection.CreateCommand())
                {
                    counts.Transaction = transaction;
                    counts.CommandText = "UPDATE Suggestion SET UpVotes = UpVotes + @up, DownVotes = DownVotes + @down WHERE Id = @id";
                    counts.Parameters.AddWithValue("@up", upChange);
                    counts.Parameters.AddWithValue("@down", downChange);
                    counts.Parameters.AddWithValue("@id", suggestionId);
                    counts.ExecuteNonQuery();
                }

                transaction.Commit();
            }

            RaiseChanged();
            return new VoteResult(true, null, newVote);
        }

        #endregion

        #region Queries.

        private const string SelectColumns = """
            SELECT s.Id, s.English, s.Language, s.Text, s.CreatedUtc, s.Status, s.UpVotes, s.DownVotes,
                   COALESCE(v.Value, 0) AS MyVote, s.SubmitterKey
            FROM Suggestion s
            LEFT JOIN SuggestionVote v ON v.SuggestionId = s.Id AND v.VoterKey = @visitor
            """;

        private const string VisibleFilter = "(s.Status = 1 OR (s.Status = 0 AND s.UpVotes - s.DownVotes > @hide))";

        private List<Suggestion> Query(string visitorId, string tail, Action<SqliteCommand>? bind = null)
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = SelectColumns + " " + tail;
            command.Parameters.AddWithValue("@visitor", visitorId ?? string.Empty);
            command.Parameters.AddWithValue("@hide", _options.AutoHideScore);
            bind?.Invoke(command);

            var results = new List<Suggestion>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new Suggestion(
                    reader.GetInt64(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                    reader.GetInt32(5),
                    reader.GetInt32(6),
                    reader.GetInt32(7),
                    reader.GetInt32(8),
                    reader.GetString(9)));
            }

            return results;
        }

        public Suggestion? GetById(string visitorId, long id)
            => Query(visitorId, "WHERE s.Id = @id", c => c.Parameters.AddWithValue("@id", id)).FirstOrDefault();

        /// <summary>
        /// The newest suggestions that are being shown.
        /// </summary>
        public List<Suggestion> GetRecent(string visitorId, int take)
            => Query(visitorId, $"WHERE {VisibleFilter} ORDER BY s.Id DESC LIMIT @take", c => c.Parameters.AddWithValue("@take", take));

        /// <summary>
        /// Suggestions that this visitor could vote on, starting with the ones that have the fewest votes.
        /// </summary>
        public List<Suggestion> GetNeedingVotes(string visitorId, int take)
            => Query(visitorId,
                $"WHERE {VisibleFilter} AND s.SubmitterKey <> @visitor AND v.Value IS NULL ORDER BY (s.UpVotes + s.DownVotes) ASC, RANDOM() LIMIT @take",
                c => c.Parameters.AddWithValue("@take", take));

        /// <summary>
        /// The suggestions for some phrases, best first. When a language is given only that language is returned.
        /// </summary>
        public List<Suggestion> GetForPhrases(string visitorId, IReadOnlyCollection<string> english, string? language)
        {
            if (english.Count == 0)
            {
                return new List<Suggestion>();
            }

            var names = english.Select((_, i) => $"@e{i}").ToList();
            var tail = $"WHERE {VisibleFilter} AND s.English IN ({string.Join(",", names)})"
                + (string.IsNullOrEmpty(language) ? string.Empty : " AND s.Language = @language")
                + " ORDER BY (s.UpVotes - s.DownVotes) DESC, s.Id";

            return Query(visitorId, tail, c =>
            {
                int i = 0;
                foreach (var phrase in english)
                {
                    c.Parameters.AddWithValue($"@e{i++}", phrase);
                }

                if (!string.IsNullOrEmpty(language))
                {
                    c.Parameters.AddWithValue("@language", language);
                }
            });
        }

        public SiteStats GetStats()
        {
            var weekAgo = _time.GetUtcNow().UtcDateTime.AddDays(-7).ToString("O", CultureInfo.InvariantCulture);

            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = $"""
                SELECT
                    (SELECT COUNT(*) FROM Suggestion s WHERE {VisibleFilter}),
                    (SELECT COUNT(*) FROM Suggestion s WHERE {VisibleFilter} AND s.CreatedUtc >= @week),
                    (SELECT COUNT(*) FROM SuggestionVote),
                    (SELECT COUNT(*) FROM SuggestionVote WHERE CreatedUtc >= @week)
                """;
            command.Parameters.AddWithValue("@hide", _options.AutoHideScore);
            command.Parameters.AddWithValue("@week", weekAgo);

            using var reader = command.ExecuteReader();
            reader.Read();
            return new SiteStats(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3));
        }

        #endregion

        #region Administration.

        /// <summary>
        /// All suggestions, newest first. The filter is one of: all, shown, approved, hidden.
        /// </summary>
        public List<Suggestion> GetAll(string filter, int take)
        {
            var where = filter switch
            {
                "shown" => $"WHERE {VisibleFilter}",
                "approved" => "WHERE s.Status = 1",
                "hidden" => $"WHERE NOT {VisibleFilter}",
                _ => string.Empty
            };

            return Query(string.Empty, $"{where} ORDER BY s.Id DESC LIMIT @take", c => c.Parameters.AddWithValue("@take", take));
        }

        public void SetStatus(long id, int status)
        {
            using (var connection = Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE Suggestion SET Status = @status WHERE Id = @id";
                command.Parameters.AddWithValue("@status", status);
                command.Parameters.AddWithValue("@id", id);
                command.ExecuteNonQuery();
            }

            RaiseChanged();
        }

        public void Delete(long id)
        {
            using (var connection = Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM Suggestion WHERE Id = @id";
                command.Parameters.AddWithValue("@id", id);
                command.ExecuteNonQuery();
            }

            RaiseChanged();
        }

        /// <summary>
        /// The best suggestion for each phrase and language, as long as its score is at least the minimum.
        /// These are what get merged back into TightWiki's translations.
        /// </summary>
        public List<Suggestion> GetTopSuggestions(int minimumScore)
            => Query(string.Empty, $"""
                WHERE s.Id IN (
                    SELECT Id FROM (
                        SELECT x.Id, ROW_NUMBER() OVER (PARTITION BY x.English, x.Language ORDER BY (x.UpVotes - x.DownVotes) DESC, x.Id) AS Rank
                        FROM Suggestion x
                        WHERE x.Status <> 2 AND (x.Status = 1 OR x.UpVotes - x.DownVotes > @hide)
                    ) WHERE Rank = 1
                ) AND s.UpVotes - s.DownVotes >= @minimum
                ORDER BY s.English, s.Language
                """, c => c.Parameters.AddWithValue("@minimum", minimumScore));

        #endregion
    }

    /// <summary>
    /// Allows a number of actions per key within a period of time.
    /// </summary>
    public sealed class SlidingWindowLimiter(TimeProvider time)
    {
        private readonly Dictionary<string, Queue<DateTimeOffset>> _actions = new();
        private readonly Lock _lock = new();

        public bool TryAcquire(string key, int limit, TimeSpan period)
        {
            lock (_lock)
            {
                var now = time.GetUtcNow();

                if (!_actions.TryGetValue(key, out var queue))
                {
                    queue = new Queue<DateTimeOffset>();
                    _actions[key] = queue;
                }

                while (queue.Count > 0 && now - queue.Peek() >= period)
                {
                    queue.Dequeue();
                }

                if (queue.Count >= limit)
                {
                    return false;
                }

                queue.Enqueue(now);

                //Don't let keys that are no longer active pile up.
                if (_actions.Count > 5000)
                {
                    foreach (var stale in _actions.Where(o => o.Value.Count == 0 || now - o.Value.Peek() >= period).Select(o => o.Key).ToList())
                    {
                        _actions.Remove(stale);
                    }
                }

                return true;
            }
        }
    }
}
